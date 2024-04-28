using ShareModels.Network.Interface;

namespace QueueHubServer.Models
{
    public class ErrorResponse(string errorMessage) : IAPIResponse
    {
        public string ErrorMessage { get; private set; } = errorMessage;

        public bool Ok { get; } = false;
    }
}
