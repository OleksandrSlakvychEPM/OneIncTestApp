using Microsoft.AspNetCore.Mvc;
using OneIncTestApp.API.Models.Request;
using OneIncTestApp.API.Services.Interfaces;
using OneIncTestApp.Models.Request;

namespace OneIncTestApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProcessingController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly ILogger<ProcessingController> _logger;

        public ProcessingController(IJobService jobService, ILogger<ProcessingController> logger)
        {
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
                await _jobService.StartProcessing(input: request.Input, connectionId: request.ConnectionId, tabId: request.TabId);

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
        public async Task<IActionResult> CancelProcessing([FromBody] CancelJobRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ConnectionId))
            {
                return BadRequest("Connection ID is required.");
            }

            var result = await _jobService.CancelProcessing(request.ConnectionId, request.TabId);

            return result ? Ok() : NotFound();
        }
    }
}
