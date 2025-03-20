using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MonitoringDashboard.Models;

[ApiController]
[Route("alert-thresholds")]
public class AlertController : ControllerBase
{
    private readonly AlertConfigService _configService;

    public AlertController()
    {
        _configService = new AlertConfigService();
    }

    [HttpGet]
    public IActionResult GetThresholds()
    {
        var thresholds = _configService.GetThresholds();
        return Ok(thresholds);
    }

    [HttpPost("update")]
    public IActionResult UpdateThreshold([FromBody] AlertUpdateRequest request)
    {
        bool success = _configService.UpdateThreshold(request.Category, request.Threshold);

        if (!success)
        {
            return BadRequest(new { message = $"Threshold category '{request.Category}' not found." });
        }

        return Ok(new { message = "Threshold updated successfully" });
    }

}
