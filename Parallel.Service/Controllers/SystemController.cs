// Copyright 2026 Kyle Ebbinga

using Microsoft.AspNetCore.Mvc;
using Parallel.Core.Utils;
using Parallel.Service.Utils;

namespace Parallel.Service.Controllers
{
    [ApiController]
    [Route("system")]
    public class SystemController(ProcessMonitor _monitor) : Controller
    {
        [HttpGet, Route("health")]
        public Task<ActionResult> GetHealthAsync()
        {
            DateTime startTime = _monitor.StartTime.ToUniversalTime();
            double uptimeMs = _monitor.Uptime.TotalMilliseconds;
            return Task.FromResult<ActionResult>(Json(new { startTime, uptimeMs }));
        }
    }
}