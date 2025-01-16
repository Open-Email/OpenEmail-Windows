namespace OpenEmail.Contracts.Application
{
    public interface IPlatformDispatcher
    {
        void ExecuteOnDispatcher(Action action);
    }
}
