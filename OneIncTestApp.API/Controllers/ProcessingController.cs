using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OneIncTestApp.Hub;
using OneIncTestApp.Models.Request;
using OneIncTestApp.Services;

namespace OneIncTestApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessingController : ControllerBase
    {
        private readonly IHubContext<ProcessingHub> _hubContext;
        private readonly IJobService _jobService;
        private readonly ILogger<ProcessingController> _logger;

        public ProcessingController(IHubContext<ProcessingHub> hubContext, IJobService jobService, ILogger<ProcessingController> logger)
        {
            _hubContext = hubContext;
            _jobService = jobService;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartProcessing([FromBody] StartJobRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Input))
            {
                return BadRequest("Input cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(request.ConnectionId))
            {
                return BadRequest("Connection ID is required.");
            }

            try
            {
                await _jobService.StartProcessing(input: request.Input, connectionId: request.ConnectionId);

                _logger.LogInformation($"Processing job started for connection {request.ConnectionId}.");
                return Ok(new { Message = "Job started successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting job.");
                return StatusCode(500, "An error occurred while starting the job.");
            }
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelProcessing([FromBody] string connectionId)
        {
            var result = await _jobService.CancelProcessing(connectionId);

            return result ? Ok() : NotFound();
        }
    }
}
