// Copyright 2026 Kyle Ebbinga

using Microsoft.AspNetCore.Mvc;
using Parallel.Core.Utils;

namespace Parallel.Service.Controllers
{
    [ApiController]
    [Route("system")]
    public class SystemController(ProcessMonitor _monitor) : Controller
    {
        [HttpGet, Route("health")]
        public async Task<ActionResult> GetHealthAsync()
        {
            DateTime startTime = _monitor.StartTime.ToUniversalTime();
            double uptimeMs = _monitor.Uptime.TotalMilliseconds;
            return Json(_monitor.Uptime);
        }
    }
}