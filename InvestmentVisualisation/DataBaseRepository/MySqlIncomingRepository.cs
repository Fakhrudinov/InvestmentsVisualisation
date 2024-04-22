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

            if (StaticData.FreeMoney is null)
            {
                _commonRepo.FillFreeMoney();
            }
        }


        public async Task<int> GetIncomingCount(CancellationToken cancellationToken)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Incoming", "GetIncomingCount.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return 0;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                return await _commonRepo.GetTableCountBySqlQuery(cancellationToken, query);
            }
        }

        public async Task<List<IncomingModel>> GetPageFromIncoming(
            CancellationToken cancellationToken, 
            int itemsAtPage, 
            int pageNumber)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                $"GetPageFromIncoming start with itemsAtPage={itemsAtPage} page={pageNumber}");

            List<IncomingModel> result = new List<IncomingModel>();

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "Incoming", 
                "GetPageFromIncoming.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                    $"GetPageFromIncoming execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        cmd.Parameters.AddWithValue("@lines_count", itemsAtPage);
                        cmd.Parameters.AddWithValue("@page_number", pageNumber);

                        try
                        {
                            await con.OpenAsync(cancellationToken);

                            using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
                            {
                                while (await sdr.ReadAsync(cancellationToken))
                                {
                                    IncomingModel newIncoming = new IncomingModel();

                                    newIncoming.Id = sdr.GetInt32("id");
                                    newIncoming.Date = sdr.GetDateTime("date");
                                    newIncoming.SecCode = sdr.GetString("seccode");
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
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                                $"GetPageFromIncoming Exception!\r\n{ex.Message}");
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

        public async Task<string> CreateNewIncoming(CancellationToken cancellationToken, CreateIncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                $"CreateNewIncoming start, newModel is\r\n" +
                $"{newIncoming.Date} {newIncoming.SecCode} SecBoard={newIncoming.SecBoard} Category={newIncoming.Category} " +
                $"Value={newIncoming.Value} Comission={newIncoming.Comission}");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Incoming", "CreateNewIncoming.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return "MySqlIncomingRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                if (newIncoming.Comission is null)
                {
                    query = query.Replace(", `comission`", "");
                    query = query.Replace(", @comission", "");
                }

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                    $"CreateNewIncoming execute query \r\n{query}");

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
                            await con.OpenAsync(cancellationToken);

                            //Return Int32 Number of rows affected
                            int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                                $"CreateNewIncoming execution affected {insertResult} lines");

                            return insertResult.ToString();

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                                $"CreateNewIncoming Exception!\r\n{ex.Message}");
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

        public async Task<IncomingModel> GetSingleIncomingById(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                $"GetSingleIncomingById={id} start");

            IncomingModel result = new IncomingModel();

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "Incoming", 
                "GetSingleIncomingById.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                
                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                    $"GetSingleIncomingById={id} execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        cmd.Parameters.AddWithValue("@id", id);

                        try
                        {
                            await con.OpenAsync(cancellationToken);

                            using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
                            {
                                while (await sdr.ReadAsync(cancellationToken))
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
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                                $"GetSingleIncomingById={id} Exception!\r\n{ex.Message}");
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

        public async Task<string> EditSingleIncoming(CancellationToken cancellationToken, IncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository EditSingleIncoming start, newModel is\r\n" +
                $"Id={newIncoming.Id} {newIncoming.Date} {newIncoming.SecCode} SecBoard={newIncoming.SecBoard} " +
                $"Category={newIncoming.Category} Value={newIncoming.Value} Comission={newIncoming.Comission}");

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "Incoming", 
                "EditSingleIncoming.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return "MySqlIncomingRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                    $"EditSingleIncoming execute query \r\n{query}");

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
                            await con.OpenAsync(cancellationToken);

                            //Return Int32 Number of rows affected
                            int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                                $"EditSingleIncoming execution affected {insertResult} lines");

                            return insertResult.ToString();

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                                $"EditSingleIncoming Exception!\r\n{ex.Message}");
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

        public async Task<string> DeleteSingleIncoming(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                $"DeleteSingleIncoming id={id} start");

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "Incoming", 
                "DeleteSingleIncoming.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return "MySqlIncomingRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                query = query.Replace("@id", id.ToString());

                return await _commonRepo.ExecuteNonQueryAsyncByQueryText(cancellationToken, query);
            }
        }

        public async Task<string> GetSecCodeFromLastRecord(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                $"GetSecCodeFromLastRecord start");

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "Incoming", 
                "GetSecCodeFromLastRecord.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return "MySqlIncomingRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                    $"GetSecCodeFromLastRecord execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        try
                        {
                            await con.OpenAsync(cancellationToken);

                            return (string)await cmd.ExecuteScalarAsync(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                                $"GetSecCodeFromLastRecord Exception!\r\n{ex.Message}");
                            return $"GetSecCodeFromLastRecord Exception! {ex.Message}";
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }
            }
        }

        public async Task<int> GetIncomingSpecificSecCodeCount(CancellationToken cancellationToken, string secCode)
        {
            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "Incoming", 
                "GetIncomingSpecificSecCodeCount.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return 0;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                query = query.Replace("@secCode", secCode);

                return await _commonRepo.GetTableCountBySqlQuery(cancellationToken, query);
            }
        }

        public async Task<List<IncomingModel>> GetPageFromIncomingSpecificSecCode(
            CancellationToken cancellationToken, 
            string secCode, 
            int itemsAtPage, 
            int pageNumber)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                $"GetPageFromIncomingSpecificSecCode start " +
                $"with secCode={secCode} itemsAtPage={itemsAtPage} page={pageNumber}");

            List<IncomingModel> result = new List<IncomingModel>();

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "Incoming", 
                "GetPageFromIncomingSpecificSecCode.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                    $"GetPageFromIncomingSpecificSecCode execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        cmd.Parameters.AddWithValue("@lines_count", itemsAtPage);
                        cmd.Parameters.AddWithValue("@page_number", pageNumber);
                        cmd.Parameters.AddWithValue("@secCode", secCode);

                        try
                        {
                            await con.OpenAsync(cancellationToken);

                            using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
                            {
                                while (await sdr.ReadAsync(cancellationToken))
                                {
                                    IncomingModel newIncoming = new IncomingModel();

                                    newIncoming.Id = sdr.GetInt32("id");
                                    newIncoming.Date = sdr.GetDateTime("date");
                                    newIncoming.SecCode = sdr.GetString("seccode");
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
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlIncomingRepository " +
                                $"GetPageFromIncomingSpecificSecCode Exception!\r\n{ex.Message}");
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
    }
}
