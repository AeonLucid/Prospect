using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prospect.Server.Api.Models.Client;
using Prospect.Server.Api.Services.Auth.Entity;
using Prospect.Server.Api.Services.CloudScript;

namespace Prospect.Server.Api.Controllers;

[Route("CloudScript")]
[ApiController]
[Authorize(AuthenticationSchemes = EntityAuthenticationOptions.DefaultScheme)]
public class CloudScriptController : Controller
{
    private readonly ILogger<CloudScriptController> _logger;
    private readonly CloudScriptService _cloudScriptService;

    public CloudScriptController(ILogger<CloudScriptController> logger, CloudScriptService cloudScriptService)
    {
        _logger = logger;
        _cloudScriptService = cloudScriptService;
    }

    [HttpPost("ExecuteFunction")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<IActionResult> ExecuteFunction(FExecuteFunctionRequest request)
    {
        _logger.LogDebug("Executing CloudScript function {Function}", request.FunctionName);

        var result = await _cloudScriptService.ExecuteAsync(request.FunctionName, request.FunctionParameter, request.GeneratePlayStreamEvent);
        if (result != null)
        { 
            return Ok(new ClientResponse<FExecuteFunctionResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FExecuteFunctionResult
                {
                    ExecutionTimeMilliseconds = 12,
                    FunctionName = request.FunctionName,
                    FunctionResult = result
                }
            });
        }

        return StatusCode(500);
    }
}