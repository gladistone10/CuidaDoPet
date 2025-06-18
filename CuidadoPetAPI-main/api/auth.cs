using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using MySqlConnector;
using System.ComponentModel.Design;

namespace A3.Api {
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IOptions<JwtSettings> jwtSettings, ILogger<AuthController> logger) {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(AuthResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthResponse))]
        [EndpointDescription("Autentica um usuario com base nas credenciais fornecidas.")]
        public async Task<IActionResult> Login([FromForm] LoginRequest request) {

            if (!ModelState.IsValid) {
                return BadRequest(new { message = ErrorMessages.InvalidData });
            }

            var authResponse = await AuthenticateUserAsync(request.User, request.Password);

            if (authResponse == null) {
                return Unauthorized(new { message = ErrorMessages.InvalidCredentials });
            }

            return Ok(authResponse);
        }
        
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(AuthResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthResponse))]
        [EndpointDescription("Cadastra um usuario na base.")]
        public async Task<IActionResult> Register([FromForm] RegisterRequest request) {

            if (!ModelState.IsValid) {
                return BadRequest(new { message = ErrorMessages.InvalidData });
            }

            AuthResponse authResponse = await RegisterUserAsync(request.name, request.email, request.password);

            if (authResponse.Message != SuccessMessages.RegistrationSuccess) {
                return Unauthorized(new { message = ErrorMessages.EmailExists });
            }

            return Ok(authResponse);
        }

        private async Task<AuthResponse> AuthenticateUserAsync(string user, string password) {
            try {
                _logger.LogInformation("Attempting to authenticate user: {User}", user);

                using(MySqlConnection con = new MySqlConnection(SqlSettings.DefaultConnection)) {
                    con.Open();
                    using(MySqlCommand com = new MySqlCommand("SELECT name, user_id FROM users WHERE name = @name AND password = MD5(@password)", con)) {
                        com.Parameters.AddWithValue("@name", user);
                        com.Parameters.AddWithValue("@password", password);
                        using(MySqlDataReader reader = com.ExecuteReader()) {
                            while(reader.Read())
                            {
                                return new AuthResponse {
                                    Message = SuccessMessages.AuthenticationSuccess,
                                    Token = GenerateJwtToken(reader["user_id"].ToString())
                                };
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error during authentication for user: {User}", user);
                return null;
            }
        }

        private async Task<AuthResponse> RegisterUserAsync(string user, string email, string pass) {
            try {
                _logger.LogInformation("Attempting to registrate user: {User}", user);

                using(MySqlConnection con = new MySqlConnection(SqlSettings.DefaultConnection)) {
                    con.Open();
                    using(MySqlCommand com = new MySqlCommand("SELECT * FROM users WHERE email = @email", con)) {
                        com.Parameters.AddWithValue("@email", email);
                        var reader = com.ExecuteScalar();
                        
                        if(reader != null) {
                            return null;
                        }
                        else {
                            com.CommandText = "INSERT INTO `users` (`email`, `password`, `name`) VALUES (@email, MD5(@password), @name);";
                            com.Parameters.AddWithValue("@password", pass);
                            com.Parameters.AddWithValue("@name", user);

                            com.ExecuteNonQuery();
                            return new AuthResponse {
                                Message = SuccessMessages.RegistrationSuccess
                            };
                        }
                    }
                }
            }
            catch(Exception ex) {
                _logger.LogError(ex, "Error during registration for user: {User}", user);
                return null;
            }
        }

        private string GenerateJwtToken(string id) {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                signingCredentials: creds,
                expires: DateTime.Now.AddMonths(1)
            );

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            return jwtToken;
        }
        
        public class RegisterRequest {
            [Required(ErrorMessage = "Email é obrigatório.")]
            public string email { get; set; }

            [Required(ErrorMessage = "Name é obrigatório.")]
            public string name { get; set; }
            
            [Required(ErrorMessage = "Password é obrigatório.")]
            public string password { get; set; }
    
        }

        public class LoginRequest {
            [Required(ErrorMessage = "Usuário é obrigatório.")]
            public string User { get; set; }

            [Required(ErrorMessage = "Senha é obrigatória.")]
            public string Password { get; set; }
        }

        public class AuthResponse {
            public string Message { get; set; }
            public string Token { get; set; }
        }

        public static class ErrorMessages {
            public const string InvalidData = "Dados inválidos.";
            public const string InvalidCredentials = "Credenciais inválidas.";
            public const string EmailExists = "Este email ja exite.";
        }

        public static class SuccessMessages {
            public const string AuthenticationSuccess = "Autenticação bem-sucedida.";
            public const string RegistrationSuccess = "Usuario cadastrado com sucesso.";
        }

    }
}
