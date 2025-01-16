namespace OpenEmail.Contracts.Application
{
    public interface IConfigurationService
    {
        void Set(string key, object value);
        T Get<T>(string key, T defaultValue = default);
    }
}
