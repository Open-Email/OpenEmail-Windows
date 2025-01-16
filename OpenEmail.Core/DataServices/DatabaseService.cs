using OpenEmail.Contracts.Configuration;
using OpenEmail.Contracts.DataServices;
using OpenEmail.Domain.Entities;
using SQLite;

namespace OpenEmail.Core.DataServices
{
    public class DatabaseService : IDatabaseService<ISQLiteAsyncConnection>
    {
        private const string DatabaseFileName = "OpenEmail.db3";

        protected IApplicationConfiguration Configuration { get; }

        public ISQLiteAsyncConnection ConnectionHandle { get; private set; }

        private bool isInitialized;

        public DatabaseService(IApplicationConfiguration configuration)
        {
            Configuration = configuration;

        }

        public async Task InitializeAsync()
        {
            if (isInitialized) return;

            ConnectionHandle ??= new SQLiteAsyncConnection(Path.Combine(Configuration.ApplicationDataFolderPath, DatabaseFileName));

            await CreateTablesAsync().ConfigureAwait(false);

            isInitialized = true;
        }

        private Task CreateTablesAsync()
            => ConnectionHandle.CreateTablesAsync(CreateFlags.None,
                typeof(Account),
                typeof(AccountContact),
                typeof(Notification),
                typeof(Message),
                typeof(MessageAttachment)
                );

        public async Task CloseAndDeleteAsync()
        {
            var filePath = ConnectionHandle.DatabasePath;

            await ConnectionHandle.CloseAsync();

            ConnectionHandle = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            File.Delete(filePath);

            isInitialized = false;
        }
    }
}
