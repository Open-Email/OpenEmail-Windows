namespace OpenEmail.Contracts.Configuration
{
    public interface IApplicationConfiguration
    {
        /// <summary>
        /// Application data folder.
        /// </summary>
        string ApplicationDataFolderPath { get; }
    }
}
