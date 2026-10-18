using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;

namespace WebAPI.endpoints;

public sealed class Health : EndpointBase
{
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Get() => Ok(new { status = "ok" });
}
