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
    public class healthController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<healthController> _logger;
        private readonly GamificationService _gamificationService;

        public healthController(IOptions<JwtSettings> jwtSettings, ILogger<healthController> logger, GamificationService gamificationService)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gamificationService = gamificationService ?? throw new ArgumentNullException(nameof(gamificationService));
        }

        [HttpPost("health/question")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HealthAnswerResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
        [EndpointDescription("Enviar perguntas sobre cuidados e saúde do pet para obter orientação.")]
        public async Task<IActionResult> AskHealthQuestion([FromForm] HealthQuestionRequest request)
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
                _logger.LogInformation("Usuário {UserId} enviou pergunta de saúde: {Question}", userId, request.Question);

                string responseFromAI = await Ia.AskAsync(request.Question);

                if (request.PetId.HasValue)
                {
                    await _gamificationService.AddPointsAsync(request.PetId.Value, 10);
                    _logger.LogInformation("Adicionados 10 pontos de gamificação para pet {PetId}", request.PetId.Value);
                }

                return Ok(new HealthAnswerResponse
                {
                    Answer = responseFromAI
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar pergunta de saúde do usuário {UserId}", userId);
                return StatusCode(500, new ErrorResponse { Message = "Erro interno do servidor." });
            }
        }
        [HttpGet("tips")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointDescription("Obter dicas de saúde e bem-estar para pets.")]
        public async Task<IActionResult> GetHealthTips()
        {
            try
            {
                var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                {
                    return Unauthorized("Token não fornecido.");
                }

                var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken;
                try
                {
                    jwtToken = handler.ReadJwtToken(token);
                }
                catch
                {
                    return Unauthorized("Token inválido.");
                }

                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("Usuário inválido no token.");
                }

                string tips = await Ia.AskAsync("Me fale uma dica de saúde e bem-estar para pets.");

                if (int.TryParse(userIdClaim, out int userId))
                {
                    await _gamificationService.AddPointsAsync(userId, 10);
                }

                return Ok(tips);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dicas de saúde.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        public class HealthQuestionRequest
        {
            [Required(ErrorMessage = "Pergunta é obrigatória.")]
            public string Question { get; set; }

            public long? PetId { get; set; }
        }

        public class HealthAnswerResponse
        {
            public string Answer { get; set; }
        }

        public class ErrorResponse
        {
            public string Message { get; set; }
        }

    }
}
