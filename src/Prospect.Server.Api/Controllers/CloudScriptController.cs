using Microsoft.AspNetCore.Mvc;
using Prospect.Server.Api.Models.Client;

namespace Prospect.Server.Api.Controllers
{
    [Route("CloudScript")]
    [ApiController]
    public class CloudScriptController : Controller
    {
        [HttpPost("ExecuteFunction")]
        [Produces("application/json")]
        public IActionResult ExecuteFunction(FExecuteFunctionRequest request)
        {
            return Ok(new ClientResponse<FExecuteFunctionResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FExecuteFunctionResult
                {
                    ExecutionTimeMilliseconds = 12,
                    FunctionName = request.FunctionName
                }
            });
        }
    }
}