namespace ShareModels.Network.Interface
{
    public interface IAPIResponse
    {
        bool Ok { get; }
        string ErrorMessage { get; }
    }
}
