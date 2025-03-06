using Microsoft.AspNetCore.Mvc;
using MonitoringDashboard.Services;

namespace MonitoringDashboard.Controllers
{
    [ApiController]
    [Route("rabbitmq")]
    public class RabbitMQController : ControllerBase
    {
        private readonly RabbitMQService _rabbitMQService;

        public RabbitMQController(RabbitMQService rabbitMQService)
        {
            _rabbitMQService = rabbitMQService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverviewWithGraph([FromQuery] int lengths_age = 60, int lengths_incr = 5, int msg_rates = 60, int msg_rates_incr = 5)
        {
            var overviewWithGraph = await _rabbitMQService.GetRabbitMQOverviewWithGraphAsync(lengths_age, lengths_incr, msg_rates, msg_rates_incr);
            return Ok(overviewWithGraph);
        }

        [HttpGet("queues")]
        public async Task<IActionResult> GetQueues([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var queues = await _rabbitMQService.GetQueuesAsync(page, pageSize);
                return Ok(queues);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}