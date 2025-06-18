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
using System.Data;

namespace A3.Api
{
    [ApiController]
    [Route("[controller]")]
    class feedController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<feedController> _logger;
        private readonly GamificationService _gamificationService;

        public feedController(IOptions<JwtSettings> jwtSettings, ILogger<feedController> logger, GamificationService gamificationService)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gamificationService = gamificationService ?? throw new ArgumentNullException(nameof(gamificationService));
        }
        
        [HttpPost("{petId}/feed")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SuccessResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
        [EndpointDescription("Registrar alimentação do pet.")]
        public async Task<IActionResult> PostFeed(long petId, [FromForm] FeedRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponse { Message = "Dados inválidos." });

            var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return Unauthorized(new ErrorResponse { Message = "Token não fornecido." });

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = handler.ReadJwtToken(token);
            }
            catch
            {
                return Unauthorized(new ErrorResponse { Message = "Token inválido." });
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new ErrorResponse { Message = "Usuário inválido no token." });

            try
            {
                await using var con = new MySqlConnection(SqlSettings.DefaultConnection);
                await con.OpenAsync();

                var permCmd = new MySqlCommand(@"SELECT COUNT(*) FROM pet_permissions WHERE pet_id = @petId AND user_id = @userId AND permission_type = 'edit'", con);
                permCmd.Parameters.AddWithValue("@petId", petId);
                permCmd.Parameters.AddWithValue("@userId", userId);

                var permCount = Convert.ToInt32(await permCmd.ExecuteScalarAsync());
                if (permCount == 0)
                    return Forbid("Sem permissão para registrar alimentação neste pet.");

                var cmd = new MySqlCommand(@"INSERT INTO pet_feeding (pet_id, food_type, quantity, feeding_time) VALUES (@petId, @foodType, @quantity, @feedingTime)", con);

                cmd.Parameters.AddWithValue("@petId", petId);
                cmd.Parameters.AddWithValue("@foodType", request.FoodType);
                cmd.Parameters.AddWithValue("@quantity", request.Quantity);
                cmd.Parameters.AddWithValue("@feedingTime", DateTime.Parse(request.FeedingTime));

                await cmd.ExecuteNonQueryAsync();

                await _gamificationService.AddPointsAsync(petId, 20);

                return Ok(new SuccessResponse { Message = "Alimentação registrada com sucesso e XP atualizado!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar alimentação do pet {PetId} para usuário {UserId}", petId, userId);
                return StatusCode(500, new ErrorResponse { Message = "Erro interno do servidor." });
            }
        }

        [HttpGet("{petId}/feed")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<FeedResponse>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
        [EndpointDescription("Listar todas as refeições do pet.")]
        public async Task<IActionResult> GetFeeds(long petId)
        {
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return Unauthorized(new ErrorResponse { Message = "Token não fornecido." });

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = handler.ReadJwtToken(token);
            }
            catch
            {
                return Unauthorized(new ErrorResponse { Message = "Token inválido." });
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new ErrorResponse { Message = "Usuário inválido no token." });

            try
            {
                await using var con = new MySqlConnection(SqlSettings.DefaultConnection);
                await con.OpenAsync();

                var permCmd = new MySqlCommand(@"SELECT COUNT(*) FROM pet_permissions  WHERE pet_id = @petId AND user_id = @userId AND permission_type IN ('view','edit')", con);
                permCmd.Parameters.AddWithValue("@petId", petId);
                permCmd.Parameters.AddWithValue("@userId", userId);

                var permCount = Convert.ToInt32(await permCmd.ExecuteScalarAsync());
                if (permCount == 0)
                    return Forbid("Sem permissão para visualizar alimentação deste pet.");

                var cmd = new MySqlCommand(@"SELECT feeding_id, food_type, quantity, feeding_time FROM pet_feeding WHERE pet_id = @petId ORDER BY feeding_time DESC", con);
                cmd.Parameters.AddWithValue("@petId", petId);

                var reader = await cmd.ExecuteReaderAsync();
                var feeds = new List<FeedResponse>();
                while (await reader.ReadAsync())
                {
                    feeds.Add(new FeedResponse
                    {
                        FeedingId = reader.GetInt32("feeding_id"),
                        FoodType = reader.GetString("food_type"),
                        Quantity = reader.GetInt32("quantity"),
                        FeedingTime = reader.GetDateTime("feeding_time").ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                return Ok(feeds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter alimentação do pet {PetId} para usuário {UserId}", petId, userId);
                return StatusCode(500, new ErrorResponse { Message = "Erro interno do servidor." });
            }
        }

        public class FeedRequest
        {
            [Required(ErrorMessage = "Tipo de comida é obrigatório.")]
            public string FoodType { get; set; }

            [Required(ErrorMessage = "Quantidade é obrigatória.")]
            public int Quantity { get; set; }

            [Required(ErrorMessage = "Horário da alimentação é obrigatório.")]
            public string FeedingTime { get; set; }
        }

        public class FeedResponse
        {
            public int FeedingId { get; set; }
            public string FoodType { get; set; }
            public int Quantity { get; set; }
            public string FeedingTime { get; set; }
        }

        public class SuccessResponse
        {
            public string Message { get; set; }
        }

        public class ErrorResponse
        {
            public string Message { get; set; }
        }

    }
}