using ShareModels.Network.Interface;

namespace QueueServer.Models
{
    public class ErrorResponse(string errorMessage) : IAPIResponse
    {
        public string ErrorMessage { get; private set; } = errorMessage;

        public bool Ok { get; } = false;
    }
}
