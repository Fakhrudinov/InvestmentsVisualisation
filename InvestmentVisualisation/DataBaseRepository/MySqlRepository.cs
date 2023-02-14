using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Reflection.PortableExecutable;

namespace DataBaseRepository
{
    public class MySqlRepository : IDataBaseRepository
    {
        private ILogger<MySqlRepository> _logger;
        //private DataBaseConnectionSettings _connection;
        private readonly string _connectionString;

        public MySqlRepository(IOptions<DataBaseConnectionSettings> connection, ILogger<MySqlRepository> logger)
        {
            //_connection = connection.Value;
            _logger = logger;
            _connectionString = $"" +
                $"Server={connection.Value.Server};" +
                $"User ID={connection.Value.UserId};" +
                $"Password={connection.Value.Password};" +
                $"Port={connection.Value.Port};" +
                $"Database={connection.Value.Database}";
        }

        public async Task GetTest()
        {
            MySqlConnection db = new MySqlConnection(_connectionString);
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"SELECT `id`, `name` FROM money_test.category;";

            await db.OpenAsync();

            using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
            {
                while (await sdr.ReadAsync())
                {
                    Console.WriteLine($"{sdr.GetInt32(0)} = {sdr.GetString(1)}");
                }
            }

            await db.CloseAsync();
            await db.DisposeAsync();
        }
    }
}
