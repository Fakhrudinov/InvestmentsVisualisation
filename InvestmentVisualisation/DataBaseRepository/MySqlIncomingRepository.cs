using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Incoming;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace DataBaseRepository
{
    public class MySqlIncomingRepository : IMySqlIncomingRepository
    {
        private ILogger<MySqlIncomingRepository> _logger;
        private readonly string _connectionString;
        private IMySqlCommonRepository _commonRepo;

        public MySqlIncomingRepository(
            IOptions<DataBaseConnectionSettings> connection, 
            ILogger<MySqlIncomingRepository> logger,
            IMySqlCommonRepository commonRepo)
        {
            _logger = logger;
            _commonRepo = commonRepo;
           
            _connectionString = $"" +
                $"Server={connection.Value.Server};" +
                $"User ID={connection.Value.UserId};" +
                $"Password={connection.Value.Password};" +
                $"Port={connection.Value.Port};" +
                $"Database={connection.Value.Database}";

            if (StaticData.Categories.Count == 0)
            {
                _commonRepo.FillStaticCategories();
            }

            if(StaticData.SecBoards.Count == 0)
            {
                _commonRepo.FillStaticSecBoards();
            }

            if (StaticData.SecCodes.Count == 0)
            {
                _commonRepo.FillStaticSecCodes();
            }
        }


        public async Task<int> GetIncomingCount()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Incoming", "GetIncomingCount.sql");
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

        public async Task<List<IncomingModel>> GetPageFromIncoming(int itemsAtPage, int pageNumber)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository GetPageFromIncoming start " +
                $"with itemsAtPage={itemsAtPage} page={pageNumber}");

            List<IncomingModel> result = new List<IncomingModel>();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Incoming", "GetPageFromIncoming.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository Error! File with SQL script not found at " + filePath);
                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository GetPageFromIncoming execute query \r\n{query}");

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
                                    newIncoming.Value = sdr.GetDecimal("value").ToString();

                                    int checkForNull = sdr.GetOrdinal("comission");
                                    if (!sdr.IsDBNull(checkForNull))
                                    {
                                        newIncoming.Comission = sdr.GetDecimal("comission").ToString();
                                    }

                                    result.Add(newIncoming);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository GetPageFromIncoming Exception!" +
                                $"\r\n{ex.Message}");
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

        public async Task<string> CreateNewIncoming(CreateIncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository CreateNewIncoming start, newModel is\r\n" +
                $"{newIncoming.Date} {newIncoming.SecCode} SecBoard={newIncoming.SecBoard} Category={newIncoming.Category} " +
                $"Value={newIncoming.Value} Comission={newIncoming.Comission}");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Incoming", "CreateNewIncoming.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository Error! File with SQL script not found at " + filePath);
                return "MySqlRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                if (newIncoming.Comission is null)
                {
                    query = query.Replace(", `comission`", "");
                    query = query.Replace(", @comission", "");
                }

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository CreateNewIncoming execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        //(@date_time, @seccode, @secboard, @category, @value, @comission);
                        cmd.Parameters.AddWithValue("@date_time", newIncoming.Date);
                        cmd.Parameters.AddWithValue("@seccode", newIncoming.SecCode);
                        cmd.Parameters.AddWithValue("@secboard", newIncoming.SecBoard);
                        cmd.Parameters.AddWithValue("@category", newIncoming.Category);
                        cmd.Parameters.AddWithValue("@value", newIncoming.Value);
                        if (newIncoming.Comission is not null)
                        {
                            cmd.Parameters.AddWithValue("@comission", newIncoming.Comission);
                        }

                        try
                        {
                            await con.OpenAsync();

                            //Return Int32 Number of rows affected
                            var insertResult = await cmd.ExecuteNonQueryAsync();
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository CreateNewIncoming execution " +
                                $"affected {insertResult} lines");

                            return insertResult.ToString();

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository CreateNewIncoming Exception!" +
                                $"\r\n{ex.Message}");
                            return ex.Message;
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }                
            }

            //INSERT INTO `incoming` (`date`, `seccode`, `secboard`, `category`, `value`)
            //  VALUES ('2023-02-16', 'TRMK',         '1', '1', '4482.8');
            //INSERT INTO `incoming` (`date`, `seccode`, `secboard`, `category`, `value`, `comission`)
            //  VALUES ('2023-02-16', 'RU000A101FG8', '2', '1', '432.4', '49.32');
        }

        public async Task<IncomingModel> GetSingleIncomingById(int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository GetSingleIncomingById={id} start");

            IncomingModel result = new IncomingModel();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Incoming", "GetSingleIncomingById.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository Error! File with SQL script not found at " + filePath);
                
                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository GetSingleIncomingById={id} execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        cmd.Parameters.AddWithValue("@id", id);

                        try
                        {
                            await con.OpenAsync();

                            using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
                            {
                                while (await sdr.ReadAsync())
                                {
                                    result.Id = sdr.GetInt32("id");
                                    result.Date = sdr.GetDateTime("date");
                                    result.SecCode = sdr.GetString("seccode");
                                    result.SecBoard = sdr.GetInt32("secboard");
                                    result.Category= sdr.GetInt32("category");
                                    result.Value = sdr.GetDecimal("value").ToString();

                                    int checkForNull = sdr.GetOrdinal("comission");
                                    if (!sdr.IsDBNull(checkForNull))
                                    {
                                        result.Comission = sdr.GetDecimal("comission").ToString();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository GetSingleIncomingById={id} Exception!" +
                                $"\r\n{ex.Message}");
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

        public async Task<string> EditSingleIncoming(IncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository EditSingleIncoming start, newModel is\r\n" +
                $"Id={newIncoming.Id} {newIncoming.Date} {newIncoming.SecCode} SecBoard={newIncoming.SecBoard} " +
                $"Category={newIncoming.Category} Value={newIncoming.Value} Comission={newIncoming.Comission}");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Incoming", "EditSingleIncoming.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository Error! File with SQL script not found at " + filePath);
                return "MySqlRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository EditSingleIncoming execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;                       

                        //@id @date_time, @seccode, @secboard, @category, @value, @comission
                        cmd.Parameters.AddWithValue("@id", newIncoming.Id);
                        cmd.Parameters.AddWithValue("@date_time", newIncoming.Date);
                        cmd.Parameters.AddWithValue("@seccode", newIncoming.SecCode);
                        cmd.Parameters.AddWithValue("@secboard", newIncoming.SecBoard);
                        cmd.Parameters.AddWithValue("@category", newIncoming.Category);
                        cmd.Parameters.AddWithValue("@value", newIncoming.Value);
                        if (newIncoming.Comission is not null)
                        {
                            cmd.Parameters.AddWithValue("@comission", newIncoming.Comission);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@comission", null);
                        }

                        try
                        {
                            await con.OpenAsync();

                            //Return Int32 Number of rows affected
                            int insertResult = await cmd.ExecuteNonQueryAsync();
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository EditSingleIncoming execution " +
                                $"affected {insertResult} lines");

                            return insertResult.ToString();

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository EditSingleIncoming Exception!" +
                                $"\r\n{ex.Message}");
                            return ex.Message;
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }
            }
        }

        public async Task<string> DeleteSingleIncoming(int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository DeleteSingleIncoming id={id} start");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Incoming", "DeleteSingleIncoming.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository Error! File with SQL script not found at " + filePath);
                return "MySqlRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                query = query.Replace("@id", id.ToString());

                return await _commonRepo.DeleteSingleRecordByQuery(query);
            }
        }
    }
}
