using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using AspNetCoreRateLimit;
using Scalar.AspNetCore;
using Serilog;
using A3;
using System.Web;

Console.Clear();
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddMemoryCache();
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOrigin", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"] ?? "default_key"))
        };
    });

builder.Services.AddRazorPages();
builder.Services.AddControllers(options =>
{
    options.AllowEmptyInputInBodyModelBinding = true;
}).ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton(new GamificationService(SqlSettings.DefaultConnection));

var app = builder.Build();

app.UseHttpsRedirection();
// app.UseHsts(); 

app.UseExceptionHandler("/Error");
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAnyOrigin");
app.UseAuthentication();
app.UseAuthorization();
app.UseIpRateLimiting();

// app.Use(async (context, next) =>
// {
//     var headers = context.Response.Headers;

//     headers["X-Content-Type-Options"] = "nosniff";
//     headers["X-Frame-Options"] = "DENY";
//     headers["Referrer-Policy"] = "no-referrer";
//     headers["X-XSS-Protection"] = "1; mode=block";
//     headers["Permissions-Policy"] = "geolocation=(), microphone=()";
//     headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; object-src 'none';";

//     await next();
// });

app.Use(async (context, next) =>
{
    var url = Uri.UnescapeDataString(context.Request.Path + context.Request.QueryString);
    string[] excludedHeaders = { "Authorization", "Cookie", "Set-Cookie", "Proxy-Authorization" };

    var headers = string.Join(";",
        context.Request.Headers
            .Where(h => !excludedHeaders.Contains(h.Key, StringComparer.OrdinalIgnoreCase))
            .Select(h => $"{h.Key}:{h.Value}")
    );

    var bodyContent = "";
    if (context.Request.Body.CanRead)
    {
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
        {
            bodyContent = await reader.ReadToEndAsync();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyContent));
        }
    }

    var body = url + bodyContent + headers;
    body = HttpUtility.UrlDecode(body);
    var blacklist = new string[]
    {
         // SQL Injection - More specific variants
         @"(?i)(\%27|')\s*=\s*(\d+|\%27|')",                // Simple SQLi (e.g., ' OR 1=1)
         @"(?i)(select|insert|update|delete|drop)\s+[^\s]+\s+from\s+[^\s]+",  // SQL statements with SELECT/INSERT/UPDATE/DELETE/DROP
         // Cross-Site Scripting (XSS)
         @"(?i)<script.*?>.*?</script>",                     // Basic XSS (script tags)
         @"(?i)javascript:",                                 // JavaScript URI scheme (javascript: in href)
         @"(?i)<img\s+[^>]*\s+src\s*=\s*['\""]?javascript:",    // JavaScript URL in image sources
         // Path Traversal
         @"(?i)\.\./|\%2e\%2e/",                             // Attempted directory traversal (../ or encoded)
         @"(?i)(\.\./)+",                                    // Another form of path traversal (multiple ../)
         @"(?i)[a-zA-Z]:\\.*\.\./",                           // Windows path traversal (e.g., C:\path\..\)
         // Remote Code Execution (RCE) via shell commands
         @"(?i)(curl|wget)\s+[^\s]+",                        // curl or wget command followed by a URL (possible RCE)
         @"(?i)(bash|sh)\s+-c\s+.*",                         // Bash shell command execution (e.g., bash -c "command")
         // Dangerous SQL commands
         @"(?i)(drop\s+table|exec|xp_cmdshell|system\()",     // Dangerous SQL commands (e.g., DROP TABLE, EXEC)
         @"(?i)(select|insert|update|delete)\s+.*\s+into\s+", // SQL statements involving INSERT INTO or similar
         @"(?i)union\s+select\s+",                            // SQL UNION SELECT injection
         @"(?i)alter\s+table\s+[^\s]+\s+add\s+column\s+",     // SQL ALTER TABLE injection
         // Command Injection - Shell command execution via web requests
         @"(?i)(;|\||&)\s*(rm\s+-rf\s*|cat\s*/etc/passwd|sh\s+-c\s*)", // Dangerous shell commands (e.g., rm -rf, cat /etc/passwd)
         @"(?i)(exec|system|passthru|shell_exec|popen|eval)\(", // Functions used to execute shell commands (e.g., system(), exec())
      
         // HTTP Header Injection and Response Splitting
         @"(?i)(\r|\n)(set-cookie|location|refresh|content-type|content-disposition):", // HTTP Header Injection
         @"(?i)\r\n.*?Location.*?:",                           // HTTP Response Splitting attempt (Location header)
         // Directory Traversal using encoded characters
         @"(?i)%2e%2e%2f",                                    // URL-encoded directory traversal (%2e%2e%2f is ../)
         // LDAP Injection (malicious LDAP queries)
         @"(?i)(select|insert|update|delete)\s+.*?from\s+.*?where\s+.*?=", // Common LDAP injection attempts
         @"(?i)(\|).*?select",                                 // LDAP command chaining
         // Remote File Inclusion (RFI)
         @"(?i)include\s+\w+\s*\(\s*['\""]?http[s]?://",          // Attempted remote file inclusion via HTTP
         @"(?i)require\s+\w+\s*\(\s*['\""]?http[s]?://",          // Same for require() function in PHP-like syntax
    };
    bool IsMalicious(string input)
    {
        foreach (var pattern in blacklist)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                Console.WriteLine($"[WAF BLOCK] Regex triggered: {pattern}");
                return true;
            }
        }
        return false;
    }
    if (IsMalicious(body))
    {
        Console.WriteLine($"[WAF BLOCK] => {body}");
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Request blocked by WAF.");
        return;
    }
    await next();
});


app.Use(async (context, next) =>
{
    Console.WriteLine($"[REQUEST] {context.Request.Method} {context.Request.Path}");
    await next();
    Console.WriteLine($"[RESPONSE] {context.Response.StatusCode}");
});

app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options.WithTitle("CuidadoPet API");
    options.WithOpenApiRoutePattern("/openapi/v1.json");
    options.WithTheme(ScalarTheme.Saturn);
    options.DarkMode = true;
    options.HideModels = true;
    options.HideClientButton = true;
});

app.MapRazorPages();
app.MapControllers();

var dateTimeNow = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
int innerWidth = 72;
string label = "🕒 Inicializado em:        ";
string lineContent = label + dateTimeNow;
int paddingRight = innerWidth - lineContent.Length;
if (paddingRight < 0) paddingRight = 0;
string paddedLineContent = lineContent + new string(' ', paddingRight);

Console.WriteLine(@$"
╔════════════════════════════════════════════════════════════════════════════╗
║                                                                            ║
║   🚀 CuidadoPet API iniciada com sucesso!                                  ║
║                                                                            ║
║   🔐 Autenticação JWT:        Ativada                                      ║
║   🛡️ WAF:                     Ativado                                      ║
║   📈 Rate Limiting:           Ativado                                      ║
║                                                                            ║
║   📄 Documentação Swagger:   http://localhost:5053/scalar/v1               ║
║   🧪 Endpoints de API:       http://localhost:5053/                        ║
║                                                                            ║
║   {paddedLineContent} ║
║                                                                            ║
╚════════════════════════════════════════════════════════════════════════════╝
");



app.Run();
