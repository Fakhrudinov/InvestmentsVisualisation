using DataAbstraction.Interfaces;
using DataAbstraction.Models.Deals;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataBaseRepository
{
    public class MySqlDealsRepository : IMySqlDealsRepository
    {
        private ILogger<MySqlDealsRepository> _logger;
        private readonly string _connectionString;
        private IMySqlCommonRepository _commonRepo;

        public MySqlDealsRepository(
            IOptions<DataBaseConnectionSettings> connection, 
            ILogger<MySqlDealsRepository> logger,
            IMySqlCommonRepository commonRepo)
        {
            _logger = logger;
            _commonRepo=commonRepo;

            _connectionString = $"" +
                $"Server={connection.Value.Server};" +
                $"User ID={connection.Value.UserId};" +
                $"Password={connection.Value.Password};" +
                $"Port={connection.Value.Port};" +
                $"Database={connection.Value.Database}";            
        }

        public async Task<int> GetDealsCount()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Deals", "GetDealsCount.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository Error! File with SQL script not found at " + filePath);
                return 0;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                return await _commonRepo.GetTableCountBySqlQuery(query);
            }
        }

        public Task<List<DealModel>> GetPageFromDeals(int itemsAtPage, int v)
        {
            throw new NotImplementedException();
        }
    }
}
