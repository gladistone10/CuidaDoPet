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
    [Route("care")]
    public class CareController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<CareController> _logger;
        private readonly GamificationService _gamificationService;

        public CareController(IOptions<JwtSettings> jwtSettings, ILogger<CareController> logger, GamificationService gamificationService)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gamificationService = gamificationService ?? throw new ArgumentNullException(nameof(gamificationService));
        }


        [HttpGet("{petId}/vaccines")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VaccineResponse[]))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
        [EndpointDescription("Consultar vacinas do pet e datas de vencimento.")]
        public async Task<IActionResult> GetVaccines(long petId)
        {
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return Unauthorized(new  { Message = "Token não fornecido." });

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = handler.ReadJwtToken(token);
            }
            catch
            {
                return Unauthorized(new  { Message = "Token inválido." });
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new  { Message = "Usuário inválido no token." });

            try
            {
                await using var con = new MySqlConnection(SqlSettings.DefaultConnection);
                await con.OpenAsync();

                var permCmd = new MySqlCommand(@"SELECT COUNT(*) FROM pet_permissions WHERE pet_id = @petId AND user_id = @userId AND permission_type IN ('view', 'edit')", con);
                permCmd.Parameters.AddWithValue("@petId", petId);
                permCmd.Parameters.AddWithValue("@userId", userId);
                var permCount = Convert.ToInt32(await permCmd.ExecuteScalarAsync());
                if (permCount == 0)
                    return StatusCode(403, "Sem permissão para visualizar as vacinas deste pet.");

                var cmd = new MySqlCommand("SELECT vaccine_id, vaccine_name, vaccine_date, expiration_date FROM pet_vaccines WHERE pet_id = @petId", con);
                cmd.Parameters.AddWithValue("@petId", petId);

                var reader = await cmd.ExecuteReaderAsync();

                var vaccines = new List<VaccineResponse>();
                while (await reader.ReadAsync())
                {
                    vaccines.Add(new VaccineResponse
                    {
                        VaccineId = reader.GetInt32("vaccine_id"),
                        VaccineName = reader.GetString("vaccine_name"),
                        VaccineDate = reader.GetDateTime("vaccine_date").ToString("yyyy-MM-dd"),
                        ExpirationDate = reader.IsDBNull("expiration_date") ? null : reader.GetDateTime("expiration_date").ToString("yyyy-MM-dd")
                    });
                }
                return Ok(vaccines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter vacinas do pet {PetId} para usuário {UserId}", petId, userId);
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { Message = "Erro interno do servidor." });
            }
        }

        [HttpPost("{petId}/vaccination")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SuccessResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
        [EndpointDescription("Registrar vacina administrada no pet.")]
        public async Task<IActionResult> PostVaccination(long petId, [FromForm] VaccinationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new  { Message = "Dados inválidos." });

            var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return Unauthorized(new  { Message = "Token não fornecido." });

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = handler.ReadJwtToken(token);
            }
            catch
            {
                return Unauthorized(new  { Message = "Token inválido." });
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new  { Message = "Usuário inválido no token." });

            try
            {
                await using var con = new MySqlConnection(SqlSettings.DefaultConnection);
                await con.OpenAsync();

                var permCmd = new MySqlCommand(@"SELECT COUNT(*) FROM pet_permissions WHERE pet_id = @petId AND user_id = @userId AND permission_type = 'edit'", con);
                permCmd.Parameters.AddWithValue("@petId", petId);
                permCmd.Parameters.AddWithValue("@userId", userId);
                var permCount = Convert.ToInt32(await permCmd.ExecuteScalarAsync());
                if (permCount == 0)
                    return StatusCode(403, "Sem permissão para registrar vacina neste pet.");

                var cmd = new MySqlCommand(@"INSERT INTO pet_vaccines (pet_id, vaccine_name, vaccine_date, expiration_date) VALUES (@petId, @vaccineName, @vaccineDate, @expirationDate)", con);

                cmd.Parameters.AddWithValue("@petId", petId);
                cmd.Parameters.AddWithValue("@vaccineName", request.VaccineName);
                cmd.Parameters.AddWithValue("@vaccineDate", DateTime.Parse(request.VaccineDate));
                if (string.IsNullOrEmpty(request.ExpirationDate))
                    cmd.Parameters.AddWithValue("@expirationDate", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@expirationDate", DateTime.Parse(request.ExpirationDate));

                await cmd.ExecuteNonQueryAsync();

                await _gamificationService.AddPointsAsync(petId, 30);

                return Ok(new SuccessResponse { Message = "Vacina registrada com sucesso e XP atualizado!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar vacina para pet {PetId} para usuário {UserId}", petId, userId);
                return StatusCode(500, new  { Message = "Erro interno do servidor." });
            }
        }

        public class VaccineResponse
        {
            public int VaccineId { get; set; }
            public string VaccineName { get; set; }
            public string VaccineDate { get; set; }
            public string ExpirationDate { get; set; }
        }

        public class VaccinationRequest
        {
            [Required]
            public string VaccineName { get; set; }

            [Required]
            public string VaccineDate { get; set; }

            public string ExpirationDate { get; set; }
        }

        public class ErrorResponse
        {
            public string Message { get; set; }
        }

        public class SuccessResponse
        {
            public string Message { get; set; }
        }

    }

}
