using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;


namespace DataBaseRepository
{
    public class MySqlRepository : IDataBaseRepository
    {
        private ILogger<MySqlRepository> _logger;
        private readonly string _connectionString;

        public MySqlRepository(IOptions<DataBaseConnectionSettings> connection, ILogger<MySqlRepository> logger)
        {
            _logger = logger;
           
            _connectionString = $"" +
                $"Server={connection.Value.Server};" +
                $"User ID={connection.Value.UserId};" +
                $"Password={connection.Value.Password};" +
                $"Port={connection.Value.Port};" +
                $"Database={connection.Value.Database}";

            if (StaticData.Categories.Count == 0)
            {
                FillStaticCategories();
            }

            if(StaticData.SecBoards.Count == 0)
            {
                FillStaticSecBoards();
            }

            if (StaticData.SecCodes.Count == 0)
            {
                FillStaticSecCodes();
            }
        }

        private void FillStaticSecCodes()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "GetActualSecCodeAndSecBoard.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} Error! File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} FillStaticSecCodes execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        con.Open();
                        using (MySqlDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
                            {
                                StaticSecCode newSecBoard = new StaticSecCode();

                                newSecBoard.SecBoard = sdr.GetInt32("secboard");
                                newSecBoard.SecCode = sdr.GetString("seccode");

                                StaticData.SecCodes.Add(newSecBoard);
                            }
                        }
                        con.Close();
                    }
                }
            }
        }

        private void FillStaticSecBoards()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "GetAllFromSecboard.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} Error! File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} FillStaticSecBoards execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        con.Open();
                        using (MySqlDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
                            {
                                SecBoardCategory newSecBoard = new SecBoardCategory();

                                newSecBoard.Id = sdr.GetInt32(0);
                                newSecBoard.Name = sdr.GetString(1);

                                StaticData.SecBoards.Add(newSecBoard);
                            }
                        }
                        con.Close();
                    }
                }
            }
        }

        private void FillStaticCategories()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "GetAllFromCategory.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} Error! File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} FillStaticCategories execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        con.Open();
                        using (MySqlDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
                            {
                                IncomingMoneyCategory newCategory = new IncomingMoneyCategory();

                                newCategory.Id = sdr.GetInt32(0);
                                newCategory.Name = sdr.GetString(1);

                                StaticData.Categories.Add(newCategory);
                            }
                        }
                        con.Close();
                    }
                }
            }
        }

        public async Task<int> GetIncomingCount()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "GetIncomingCount.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} Error! File with SQL script not found at " + filePath);
                return 0;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                return await GetTableCountBySqlQuery(query);
            }
        }

        private async Task<int> GetTableCountBySqlQuery(string query)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} GetTableCountBySqlQuery start with \r\n{query}");

            int result = 0;

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    try
                    {
                        await con.OpenAsync();
                        result =  (int)(long)await cmd.ExecuteScalarAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} GetLastRecordsFromIncoming Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;
        }

        public async Task<List<IncomingModel>> GetPageFromIncoming(int itemsAtPage, int pageNumber)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} GetPageFromIncoming start with itemsAtPage={itemsAtPage} page={pageNumber}");

            List<IncomingModel> result = new List<IncomingModel>();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "GetPageFromIncoming.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} Error! File with SQL script not found at " + filePath);
                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} GetPageFromIncoming execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        cmd.Parameters.AddWithValue("@lines_count", itemsAtPage);
                        cmd.Parameters.AddWithValue("@page_number", pageNumber);

                        try
                        {
                            await con.OpenAsync();

                            using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
                            {
                                while (await sdr.ReadAsync())
                                {
                                    IncomingModel newIncoming = new IncomingModel();

                                    newIncoming.Id = sdr.GetInt32("id");
                                    newIncoming.Date = sdr.GetDateTime("date");
                                    newIncoming.SecCode = sdr.GetString("seccode");
                                    //newIncoming.SecBoard = StaticData.SecBoards[StaticData.SecBoards.FindIndex(sb => sb.Id == sdr.GetInt32("secboard"))];
                                    //newIncoming.Category = StaticData.Categories[StaticData.Categories.FindIndex(sb => sb.Id == sdr.GetInt32("category"))];
                                    newIncoming.SecBoard = sdr.GetInt32("secboard");
                                    newIncoming.Category= sdr.GetInt32("category");
                                    newIncoming.Value = sdr.GetDecimal("value");

                                    int checkForNull = sdr.GetOrdinal("comission");
                                    if (!sdr.IsDBNull(checkForNull))
                                    {
                                        newIncoming.Comission = sdr.GetDecimal("comission");
                                    }

                                    result.Add(newIncoming);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} GetPageFromIncoming Exception!\r\n{ex.Message}");
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }

                return result;
            }
        }

        //public async Task GetTest()
        //{
        //    foreach (var category in StaticData.Categories)
        //    {
        //        Console.WriteLine($"{category.Id} {category.Name}");
        //    }

        //    //MySqlConnection db = new MySqlConnection(_connectionString);
        //    //using var cmd = db.CreateCommand();
        //    //cmd.CommandText = @"SELECT `id`, `name` FROM money_test.category;";

        //    //await db.OpenAsync();

        //    //using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
        //    //{
        //    //    while (await sdr.ReadAsync())
        //    //    {
        //    //        Console.WriteLine($"{sdr.GetInt32(0)} = {sdr.GetString(1)}");
        //    //    }
        //    //}

        //    //await db.CloseAsync();
        //    //await db.DisposeAsync();

        //    //using (MySqlConnection con = new MySqlConnection(_connectionString))
        //    //{
        //    //    string query = @"SELECT `id`, `name` FROM money_test.category;";

        //    //    using (MySqlCommand cmd = new MySqlCommand(query))
        //    //    {
        //    //        cmd.Connection = con;
        //    //        await con.OpenAsync();

        //    //        using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
        //    //        {
        //    //            while (await sdr.ReadAsync())
        //    //            {
        //    //                Console.WriteLine($"{sdr.GetInt32(0)} = {sdr.GetString(1)}");
        //    //            }
        //    //        }

        //    //        await con.CloseAsync();
        //    //    }
        //    //}
        //}
    }
}
