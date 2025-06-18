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
    public class PetsController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;
        private readonly GamificationService _gamificationService;

        public PetsController(IOptions<JwtSettings> jwtSettings, ILogger<AuthController> logger, GamificationService gamificationService)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gamificationService = gamificationService ?? throw new ArgumentNullException(nameof(gamificationService));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PetInfoResponse>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointDescription("Obter todos os pets que o usuário tem permissão.")]
        public async Task<IActionResult> Get()
        {
            try
            {
                var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                    return Unauthorized(new { message = "Token não fornecido" });

                var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Usuário inválido no token" });

                List<PetInfoResponse> pets = new List<PetInfoResponse>();

                using (MySqlConnection con = new MySqlConnection(SqlSettings.DefaultConnection))
                {
                    await con.OpenAsync();

                    string query = @"SELECT p.pet_id, p.name, p.breed, p.birthdate, p.gender, COALESCE(ppoints.points, 0) AS points FROM pets p INNER JOIN pet_permissions pp ON pp.pet_id = p.pet_id LEFT JOIN pet_points ppoints ON ppoints.pet_id = p.pet_id WHERE pp.user_id = @userID;";

                    using (MySqlCommand com = new MySqlCommand(query, con))
                    {
                        com.Parameters.AddWithValue("@userID", userId);

                        using (var reader = await com.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                pets.Add(new PetInfoResponse
                                {
                                    Id = reader.GetInt64("pet_id"),
                                    Name = reader.GetString("name"),
                                    Breed = reader.GetString("breed"),
                                    Birthdate = reader.GetDateTime("birthdate").ToShortDateString(),
                                    Gender = reader.GetString("gender"),
                                    Points = reader.GetInt32("points")
                                });
                            }
                        }
                    }
                }

                return Ok(pets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pets for user");
                return StatusCode(500, new { message = "Erro interno no servidor" });
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AddPetResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(AddPetResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AddPetResponse))]
        [EndpointDescription("Adicionar um novo pet.")]
        public async Task<IActionResult> Post([FromForm] AddPetRequest request)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = ErrorMessages.InvalidData });
            }

            var addPetResponse = await RegisterPetAsync(request, HttpContext);

            if (addPetResponse == null)
            {
                return Unauthorized(new { message = ErrorMessages.FailedAddPet });
            }

            return Ok(addPetResponse);
        }

        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointDescription("Deletar um pet, somente se usuário tiver permissão 'edit'.")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                    return Unauthorized(new { message = "Token não fornecido" });

                var token = authorizationHeader.Substring("Bearer ".Length).Trim();

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Usuário inválido no token" });

                using (MySqlConnection con = new MySqlConnection(SqlSettings.DefaultConnection))
                {
                    await con.OpenAsync();

                    string checkPermissionQuery = @"SELECT COUNT(*) FROM pet_permissions WHERE pet_id = @petID AND user_id = @userID AND permission_type = 'edit'";

                    using (MySqlCommand checkCmd = new MySqlCommand(checkPermissionQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@petID", id);
                        checkCmd.Parameters.AddWithValue("@userID", userId);

                        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count == 0)
                            return Forbid("Permissão 'edit' necessária para deletar este pet.");
                    }

                    string deletePetQuery = "DELETE FROM pets WHERE pet_id = @petID";
                    using (MySqlCommand deleteCmd = new MySqlCommand(deletePetQuery, con))
                    {
                        deleteCmd.Parameters.AddWithValue("@petID", id);
                        int rowsAffected = await deleteCmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                            return NotFound(new { message = "Pet não encontrado." });
                    }

                    string deletePermissionsQuery = "DELETE FROM pet_permissions WHERE pet_id = @petID";
                    using (MySqlCommand deletePermCmd = new MySqlCommand(deletePermissionsQuery, con))
                    {
                        deletePermCmd.Parameters.AddWithValue("@petID", id);
                        await deletePermCmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { message = "Pet deletado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting pet with id {PetId}", id);
                return StatusCode(500, new { message = "Erro interno no servidor" });
            }
        }

        [HttpPost("{petId}/trust")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TrustPetResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(TrustPetResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(TrustPetResponse))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(TrustPetResponse))]
        [EndpointDescription("Libera informações do pet para outro usuário.")]
        public async Task<IActionResult> TrustPet(long petId, [FromForm] TrustPetRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new TrustPetResponse { Message = "Dados inválidos." });
            }

            var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new TrustPetResponse { Message = "Token não fornecido." });
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
                return Unauthorized(new TrustPetResponse { Message = "Token inválido." });
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new TrustPetResponse { Message = "Usuário inválido no token." });
            }

            try
            {
                using var con = new MySqlConnection(SqlSettings.DefaultConnection);
                await con.OpenAsync();

                var checkPermCmd = new MySqlCommand(@"SELECT COUNT(*) FROM pet_permissions WHERE pet_id = @petId AND user_id = @userId AND permission_type = 'edit'", con);
                checkPermCmd.Parameters.AddWithValue("@petId", petId);
                checkPermCmd.Parameters.AddWithValue("@userId", userId);

                var permCount = Convert.ToInt32(await checkPermCmd.ExecuteScalarAsync());
                if (permCount == 0)
                {
                    return Forbid("Você não tem permissão para conceder acesso a este pet.");
                }

                var insertPermCmd = new MySqlCommand(@"INSERT INTO pet_permissions (pet_id, user_id, permission_type, granted_at)VALUES (@petId, @targetUserId, 'view', NOW())ON DUPLICATE KEY UPDATE granted_at = NOW()", con);
                insertPermCmd.Parameters.AddWithValue("@petId", petId);
                insertPermCmd.Parameters.AddWithValue("@targetUserId", request.TargetUserId);

                await insertPermCmd.ExecuteNonQueryAsync();

                await _gamificationService.AddPointsAsync(userId, 50);
                return Ok(new TrustPetResponse { Message = $"Acesso concedido ao usuário {request.TargetUserId} para o pet {petId}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conceder acesso ao pet {PetId}", petId);
                return StatusCode(500, new TrustPetResponse { Message = "Erro interno do servidor." });
            }
        }

        private async Task<AddPetResponse> RegisterPetAsync(AddPetRequest request, HttpContext context)
        {
            try
            {
                var authorizationHeader = context.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                    throw new Exception("Token não fornecido");

                var token = authorizationHeader.Substring("Bearer ".Length).Trim();

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("Usuário inválido no token");

                _logger.LogInformation("Attempting to register pet for user: {User}", userId);

                using (MySqlConnection con = new MySqlConnection(SqlSettings.DefaultConnection))
                {
                    await con.OpenAsync();

                    using (MySqlCommand com = new MySqlCommand("INSERT INTO `pets` (`name`, `breed`, `birthdate`, `gender`) VALUES (@nome, @raca, @aniversario, @genero);", con))
                    {
                        com.Parameters.AddWithValue("@nome", request.name);
                        com.Parameters.AddWithValue("@raca", request.breed);
                        com.Parameters.AddWithValue("@aniversario", request.birthdate);
                        com.Parameters.AddWithValue("@genero", request.gender);

                        await com.ExecuteNonQueryAsync();
                        long petID = com.LastInsertedId;

                        com.Parameters.Clear();
                        com.CommandText = "INSERT INTO `pet_permissions` (`pet_id`, `user_id`, `permission_type`, `granted_at`) VALUES (@petID, @userID, 'edit', NOW())";
                        com.Parameters.AddWithValue("@petID", petID);
                        com.Parameters.AddWithValue("@userID", userId);

                        await com.ExecuteNonQueryAsync();

                        return new AddPetResponse { Message = "Pet cadastrado com sucesso!" };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {User}", request.name);
                return null;
            }
        }

        public class TrustPetRequest
        {
            [Required(ErrorMessage = "TargetUserId é obrigatório.")]
            public int TargetUserId { get; set; }
        }

        public class TrustPetResponse
        {
            public string Message { get; set; }
        }

        public class AddPetRequest
        {
            [Required(ErrorMessage = "Nome é obrigatório.")]
            public string name { get; set; }

            [Required(ErrorMessage = "Raca é obrigatório.")]
            public string breed { get; set; }

            [Required(ErrorMessage = "Aniversario é obrigatório.")]
            public string birthdate { get; set; }

            [Required(ErrorMessage = "Genero é obrigatório.")]
            public string gender { get; set; }
        }

        public class AddPetResponse
        {
            public string Message { get; set; }
        }
        public class PetInfoResponse
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Breed { get; set; }
            public string Birthdate { get; set; }
            public string Gender { get; set; }
            public int Points { get; set; }
        }
        public static class ErrorMessages
        {
            public const string InvalidData = "Dados inválidos.";
            public const string FailedAddPet = "Falha ao cadastrar pet.";
            public const string EmailExists = "Este email ja exite.";
        }

        public static class SuccessMessages
        {
            public const string AuthenticationSuccess = "Autenticação bem-sucedida.";
            public const string RegistrationSuccess = "Usuario cadastrado com sucesso.";
        }

    }
}
