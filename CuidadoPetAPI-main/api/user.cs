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

namespace A3.Api
{
    [ApiController]
    [Route("[controller]")]
    public class userController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;

        public userController(IOptions<JwtSettings> jwtSettings, ILogger<AuthController> logger)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("user/profile")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserProfileResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointDescription("Obter perfil do usuário autenticado.")]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                    return Unauthorized(new { message = "Token não fornecido" });

                var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Usuário inválido no token" });

                using (MySqlConnection con = new MySqlConnection(SqlSettings.DefaultConnection))
                {
                    await con.OpenAsync();

                    string query = "SELECT user_id, email, name, created_at, updated_at FROM users WHERE user_id = @userId";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var profile = new UserProfileResponse
                                {
                                    UserId = reader.GetInt32("user_id"),
                                    Email = reader.GetString("email"),
                                    Name = reader.GetString("name"),
                                    CreatedAt = reader.GetDateTime("created_at"),
                                    UpdatedAt = reader.GetDateTime("updated_at")
                                };

                                return Ok(profile);
                            }
                            else
                            {
                                return NotFound(new { message = "Usuário não encontrado." });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter perfil do usuário.");
                return StatusCode(500, new { message = "Erro interno no servidor" });
            }
        }
        [HttpPut("user/profile")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserProfileResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointDescription("Atualizar perfil do usuário autenticado.")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Dados inválidos." });

            try
            {
                var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                    return Unauthorized(new { message = "Token não fornecido" });

                var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Usuário inválido no token" });

                using (MySqlConnection con = new MySqlConnection(SqlSettings.DefaultConnection))
                {
                    await con.OpenAsync();

                    string query = "UPDATE users SET name = @name WHERE user_id = @userId";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", request.Name);
                        cmd.Parameters.AddWithValue("@userId", userId);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                            return NotFound(new { message = "Usuário não encontrado." });

                        string selectQuery = "SELECT user_id, email, name, created_at, updated_at FROM users WHERE user_id = @userId";
                        using (MySqlCommand selectCmd = new MySqlCommand(selectQuery, con))
                        {
                            selectCmd.Parameters.AddWithValue("@userId", userId);
                            using (var reader = await selectCmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    var updatedProfile = new UserProfileResponse
                                    {
                                        UserId = reader.GetInt32("user_id"),
                                        Email = reader.GetString("email"),
                                        Name = reader.GetString("name"),
                                        CreatedAt = reader.GetDateTime("created_at"),
                                        UpdatedAt = reader.GetDateTime("updated_at")
                                    };
                                    return Ok(updatedProfile);
                                }
                                else
                                {
                                    return NotFound(new { message = "Usuário não encontrado após atualização." });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar perfil do usuário.");
                return StatusCode(500, new { message = "Erro interno no servidor" });
            }
        }

        public class UpdateUserProfileRequest
        {
            [Required(ErrorMessage = "Nome é obrigatório.")]
            public string Name { get; set; }
        }

        public class UserProfileResponse
        {
            public int UserId { get; set; }
            public string Email { get; set; }
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }


    }
}
