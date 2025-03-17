namespace OpenEmail.Contracts.Services
{
    public interface IWindowService
    {
        /// <summary>
        /// Closes login window and opens a new window with shell.
        /// </summary>
        void StartShellApplication();

        /// <summary>
        /// Closes shell window and opens login window.
        /// </summary>
        void GoBackLogin();
    }
}
