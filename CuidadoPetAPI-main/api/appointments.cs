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
    public class appointmentsController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<appointmentsController> _logger;

        public appointmentsController(IOptions<JwtSettings> jwtSettings, ILogger<appointmentsController> logger)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("appointments")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScheduleAppointmentResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointDescription("Agendar uma consulta (veterinário, banho, tosa, etc) para um pet.")]
        public async Task<IActionResult> ScheduleAppointment([FromForm] ScheduleAppointmentRequest request)
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
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Usuário inválido no token" });

                using (MySqlConnection con = new MySqlConnection(SqlSettings.DefaultConnection))
                {
                    await con.OpenAsync();

                    string checkPermissionQuery = @"SELECT COUNT(*) FROM pet_permissions WHERE pet_id = @petID AND user_id = @userID";

                    using (MySqlCommand checkCmd = new MySqlCommand(checkPermissionQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@petID", request.PetId);
                        checkCmd.Parameters.AddWithValue("@userID", userId);

                        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count == 0)
                            return Forbid("Sem permissão para agendar consulta para este pet.");
                    }

                    string insertQuery = @"INSERT INTO appointments (pet_id, user_id, appointment_type, appointment_date) VALUES (@petID, @userID, @appointmentType, @appointmentDate)";

                    using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.AddWithValue("@petID", request.PetId);
                        insertCmd.Parameters.AddWithValue("@userID", userId);
                        insertCmd.Parameters.AddWithValue("@appointmentType", request.AppointmentType);
                        insertCmd.Parameters.AddWithValue("@appointmentDate", request.AppointmentDate);

                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new ScheduleAppointmentResponse { Message = "Consulta agendada com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao agendar consulta para pet {PetId}", request.PetId);
                return StatusCode(500, new { message = "Erro interno no servidor" });
            }
        }

        [HttpGet("appointments")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<AppointmentResponse>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointDescription("Listar todos os agendamentos do usuário autenticado.")]
        public async Task<IActionResult> GetAppointments()
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

                var appointments = new List<AppointmentResponse>();

                using (MySqlConnection con = new MySqlConnection(SqlSettings.DefaultConnection))
                {
                    await con.OpenAsync();

                    string query = @"SELECT a.appointment_id, a.pet_id, p.name AS pet_name, a.appointment_type, a.appointment_date, a.status FROM appointments a JOIN pets p ON a.pet_id = p.pet_id WHERE a.user_id = @userID ORDER BY a.appointment_date DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@userID", userId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                appointments.Add(new AppointmentResponse
                                {
                                    AppointmentId = reader.GetInt32("appointment_id"),
                                    PetId = reader.GetInt32("pet_id"),
                                    PetName = reader.GetString("pet_name"),
                                    AppointmentType = reader.GetString("appointment_type"),
                                    AppointmentDate = reader.GetDateTime("appointment_date"),
                                    Status = reader.GetString("status")
                                });
                            }
                        }
                    }
                }

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar agendamentos do usuário.");
                return StatusCode(500, new { message = "Erro interno no servidor" });
            }
        }

        [HttpGet("appointments/{appointmentId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AppointmentResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointDescription("Obter detalhes de um agendamento pelo ID.")]
        public async Task<IActionResult> GetAppointmentById(int appointmentId)
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

                    string query = @"SELECT a.appointment_id, a.pet_id, p.name AS pet_name, a.appointment_type, a.appointment_date, a.status FROM appointments a JOIN pets p ON a.pet_id = p.pet_id WHERE a.appointment_id = @appointmentId AND a.user_id = @userID";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@appointmentId", appointmentId);
                        cmd.Parameters.AddWithValue("@userID", userId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                                return NotFound(new { message = "Agendamento não encontrado." });

                            var appointment = new AppointmentResponse
                            {
                                AppointmentId = reader.GetInt32("appointment_id"),
                                PetId = reader.GetInt32("pet_id"),
                                PetName = reader.GetString("pet_name"),
                                AppointmentType = reader.GetString("appointment_type"),
                                AppointmentDate = reader.GetDateTime("appointment_date"),
                                Status = reader.GetString("status")
                            };

                            return Ok(appointment);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter agendamento {AppointmentId}.", appointmentId);
                return StatusCode(500, new { message = "Erro interno no servidor" });
            }
        }

        [HttpPost("appointments/{appointmentId}/reschedule")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AppointmentResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointDescription("Reagendar um agendamento pelo ID.")]
        public async Task<IActionResult> RescheduleAppointment(int appointmentId, [FromBody] RescheduleRequest request)
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
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Usuário inválido no token" });

                using (MySqlConnection con = new MySqlConnection(SqlSettings.DefaultConnection))
                {
                    await con.OpenAsync();

                    string selectQuery = "SELECT appointment_id FROM appointments WHERE appointment_id = @appointmentId AND user_id = @userID";
                    using (MySqlCommand selectCmd = new MySqlCommand(selectQuery, con))
                    {
                        selectCmd.Parameters.AddWithValue("@appointmentId", appointmentId);
                        selectCmd.Parameters.AddWithValue("@userID", userId);

                        var result = await selectCmd.ExecuteScalarAsync();
                        if (result == null)
                            return NotFound(new { message = "Agendamento não encontrado." });
                    }

                    string updateQuery = "UPDATE appointments SET appointment_date = @newDate, updated_at = NOW() WHERE appointment_id = @appointmentId";
                    using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, con))
                    {
                        updateCmd.Parameters.AddWithValue("@newDate", request.NewAppointmentDate);
                        updateCmd.Parameters.AddWithValue("@appointmentId", appointmentId);

                        var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                            return StatusCode(500, new { message = "Falha ao reagendar agendamento." });
                    }

                    string selectUpdated = @"SELECT a.appointment_id, a.pet_id, p.name AS pet_name, a.appointment_type, a.appointment_date, a.status FROM appointments a JOIN pets p ON a.pet_id = p.pet_id WHERE a.appointment_id = @appointmentId";

                    using (MySqlCommand cmd = new MySqlCommand(selectUpdated, con))
                    {
                        cmd.Parameters.AddWithValue("@appointmentId", appointmentId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var appointment = new AppointmentResponse
                                {
                                    AppointmentId = reader.GetInt32("appointment_id"),
                                    PetId = reader.GetInt32("pet_id"),
                                    PetName = reader.GetString("pet_name"),
                                    AppointmentType = reader.GetString("appointment_type"),
                                    AppointmentDate = reader.GetDateTime("appointment_date"),
                                    Status = reader.GetString("status")
                                };
                                return Ok(appointment);
                            }
                            else
                            {
                                return NotFound(new { message = "Agendamento não encontrado após atualização." });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reagendar agendamento {AppointmentId}.", appointmentId);
                return StatusCode(500, new { message = "Erro interno no servidor" });
            }
        }

        [HttpGet("appointments/{appointmentId}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointDescription("Consultar status de um agendamento pelo ID.")]
        public async Task<IActionResult> GetAppointmentStatus(int appointmentId)
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

                    string query = "SELECT status FROM appointments WHERE appointment_id = @appointmentId AND user_id = @userID";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@appointmentId", appointmentId);
                        cmd.Parameters.AddWithValue("@userID", userId);

                        var status = await cmd.ExecuteScalarAsync();

                        if (status == null)
                            return NotFound(new { message = "Agendamento não encontrado." });

                        return Ok(new { AppointmentId = appointmentId, Status = status.ToString() });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar status do agendamento {AppointmentId}.", appointmentId);
                return StatusCode(500, new { message = "Erro interno no servidor" });
            }
        }

        public class RescheduleRequest
        {
            [Required]
            public DateTime NewAppointmentDate { get; set; }
        }

        public class AppointmentResponse
        {
            public int AppointmentId { get; set; }
            public int PetId { get; set; }
            public string PetName { get; set; }
            public string AppointmentType { get; set; }
            public DateTime AppointmentDate { get; set; }
            public string Status { get; set; }
        }

        public class ScheduleAppointmentRequest
        {
            [Required]
            public long PetId { get; set; }

            [Required]
            [RegularExpression("vet|grooming|bath|other", ErrorMessage = "Tipo de consulta inválido.")]
            public string AppointmentType { get; set; }

            [Required]
            public DateTime AppointmentDate { get; set; }
        }

        public class ScheduleAppointmentResponse
        {
            public string Message { get; set; }
        }

    }
}
