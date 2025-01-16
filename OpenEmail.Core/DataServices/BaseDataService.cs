using OpenEmail.Contracts.DataServices;
using SQLite;

namespace OpenEmail.Core.DataServices
{
    public class BaseDataService
    {
        private readonly IDatabaseService<ISQLiteAsyncConnection> _databaseService;

        protected ISQLiteAsyncConnection Connection => _databaseService.ConnectionHandle;

        public BaseDataService(IDatabaseService<ISQLiteAsyncConnection> databaseService)
        {
            _databaseService = databaseService;
        }

        public Task DisposeConnectionAsync() => _databaseService.CloseAndDeleteAsync();
    }
}
