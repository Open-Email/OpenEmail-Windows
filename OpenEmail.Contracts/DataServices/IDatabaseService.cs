namespace OpenEmail.Contracts.DataServices
{
    public interface IDatabaseService<T> where T : class
    {
        T ConnectionHandle { get; }
        Task InitializeAsync();
        Task CloseAndDeleteAsync();
    }
}
