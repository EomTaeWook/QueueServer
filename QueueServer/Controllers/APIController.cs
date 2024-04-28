using Dignus.Log;
using Microsoft.AspNetCore.Mvc;
using QueueServer.Internals;
using QueueServer.Models;
using ShareModels.Network.Interface;
using System.Runtime.CompilerServices;

namespace QueueServer.Controllers
{
    [Route("[controller]")]
    public abstract class APIController<T> : ControllerBase where T : IAPIRequest
    {
        protected abstract Task<IAPIResponse> Process(T request);

        [HttpPost]
        public async Task<JsonResult> Post([FromBody] T request)
        {
            LogHelper.Info($"{HttpContext.Request.Path}");
            if (request == null)
            {
                return new JsonResult(MakeCommonErrorMessage($"invalid request"));
            }
            if (RequestValidator.CheckProperties(request) == false)
            {
                return new JsonResult(MakeCommonErrorMessage($"invalid request"));
            }
            var response = await Process(request);
            return new JsonResult(response);
        }

        protected IAPIResponse MakeCommonErrorMessage(string message, [CallerFilePath] string fileName = "", [CallerLineNumber] int fileNumber = 0)
        {
            LogHelper.Error($"message : {message}", fileName, fileNumber);
            return new ErrorResponse(message);
        }
    }
}
