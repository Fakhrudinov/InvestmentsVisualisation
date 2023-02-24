using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.SecCodes;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace DataBaseRepository
{
    public class MySqlSecCodesRepository : IMySqlSecCodesRepository
    {
        private ILogger<MySqlSecCodesRepository> _logger;
        private readonly string _connectionString;
        private IMySqlCommonRepository _commonRepo;

        public MySqlSecCodesRepository(
            IOptions<DataBaseConnectionSettings> connection,
            ILogger<MySqlSecCodesRepository> logger,
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

            if (StaticData.SecBoards.Count == 0)
            {
                _commonRepo.FillStaticSecBoards();
            }
        }

        public async Task<List<SecCodeInfo>> GetPageFromSecCodes(int itemsAtPage, int pageNumber)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository GetPageFromSecCodes start " +
                $"with itemsAtPage={itemsAtPage} page={pageNumber}");

            List<SecCodeInfo> result = new List<SecCodeInfo>();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "SecCodes", "GetPageFromSecCodes.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository Error! File with SQL script not found at " + filePath);
                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository GetPageFromSecCodes execute query \r\n{query}");

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
                                    SecCodeInfo secCode = new SecCodeInfo();

                                    secCode.SecCode = sdr.GetString("seccode");
                                    secCode.SecBoard = sdr.GetInt32("secboard");

                                    secCode.Name = sdr.GetString("name");
                                    secCode.FullName = sdr.GetString("full_name");
                                    secCode.ISIN = sdr.GetString("isin");

                                    int checkForNull = sdr.GetOrdinal("expired_date");
                                    if (!sdr.IsDBNull(checkForNull))
                                    {
                                        secCode.ExpiredDate = sdr.GetDateTime("expired_date");
                                    }

                                    result.Add(secCode);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository GetPageFromSecCodes Exception!" +
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

        public async Task<int> GetSecCodesCount()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "SecCodes", "GetSecCodesCount.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository Error! File with SQL script not found at " + filePath);
                return 0;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                return await _commonRepo.GetTableCountBySqlQuery(query);
            }
        }

        public async Task<string> CreateNewSecCode(SecCodeInfo model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository CreateNewSecCode start, newModel is\r\n" +
                $"{model.SecCode} {model.Name} SecBoard={model.SecBoard} ISIN={model.ISIN} {model.FullName} ExpiredDate={model.ExpiredDate}");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "SecCodes", "CreateNewSecCode.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository Error! File with SQL script not found at " + filePath);
                return "MySqlRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);

                if (model.ExpiredDate is null)
                {
                    query = query.Replace(", `expired_date`", "");
                    query = query.Replace(", @expired_date", "");
                }

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository CreateNewSecCode execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        //(@seccode, @secboard, @name, @full_name, @isin, @expired_date);
                        cmd.Parameters.AddWithValue("@seccode", model.SecCode);
                        cmd.Parameters.AddWithValue("@secboard", model.SecBoard);
                        cmd.Parameters.AddWithValue("@name", model.Name);
                        cmd.Parameters.AddWithValue("@full_name", model.FullName);
                        cmd.Parameters.AddWithValue("@isin", model.ISIN);

                        if (model.ExpiredDate is not null)
                        {
                            cmd.Parameters.AddWithValue("@expired_date", model.ExpiredDate);
                        }

                        try
                        {
                            await con.OpenAsync();

                            //Return Int32 Number of rows affected
                            var insertResult = await cmd.ExecuteNonQueryAsync();
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository CreateNewSecCode execution " +
                                $"affected {insertResult} lines");

                            return insertResult.ToString();

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository CreateNewSecCode Exception!" +
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
            //INSERT INTO `seccode_info`
            //  (`seccode`, `secboard`, `name`, `full_name`, `isin`, `expired_date`)
            //VALUES
            //  (@seccode, @secboard, @name, @full_name, @isin, @expired_date);

        }

        public async Task<string> EditSingleSecCode(SecCodeInfo model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository EditSingleSecCode start, newModel is\r\n" +
                $"{model.SecCode} {model.Name} SecBoard={model.SecBoard} ISIN={model.ISIN} {model.FullName} ExpiredDate={model.ExpiredDate}");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "SecCodes", "EditSingleSecCode.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository Error! File with SQL script not found at " + filePath);
                return "MySqlRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository EditSingleSecCode execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        //(@seccode, @secboard, @name, @full_name, @isin, @expired_date);
                        cmd.Parameters.AddWithValue("@seccode", model.SecCode);
                        cmd.Parameters.AddWithValue("@secboard", model.SecBoard);
                        cmd.Parameters.AddWithValue("@name", model.Name);
                        cmd.Parameters.AddWithValue("@full_name", model.FullName);
                        cmd.Parameters.AddWithValue("@isin", model.ISIN);

                        if (model.ExpiredDate is not null)
                        {
                            cmd.Parameters.AddWithValue("@expired_date", model.ExpiredDate);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@expired_date", null);
                        }


                        try
                        {
                            await con.OpenAsync();

                            //Return Int32 Number of rows affected
                            int insertResult = await cmd.ExecuteNonQueryAsync();
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository EditSingleSecCode execution " +
                                $"affected {insertResult} lines");

                            return insertResult.ToString();

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository EditSingleSecCode Exception!" +
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

        public async Task<SecCodeInfo> GetSingleSecCodeBySecCode(string secCode)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository GetSingleSecCodeBySecCode={secCode} start");

            SecCodeInfo result = new SecCodeInfo();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "SecCodes", "GetSingleSecCodeBySecCode.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository Error! File with SQL script not found at " + filePath);

                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository GetSingleSecCodeBySecCode={secCode} execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        cmd.Parameters.AddWithValue("@secCode", secCode);

                        try
                        {
                            await con.OpenAsync();

                            using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
                            {
                                while (await sdr.ReadAsync())
                                {
                                    result.SecCode = sdr.GetString("seccode");
                                    result.SecBoard = sdr.GetInt32("secboard");
                                    result.Name= sdr.GetString("name");
                                    result.FullName = sdr.GetString("full_name");
                                    result.ISIN = sdr.GetString("isin");

                                    int checkForNull = sdr.GetOrdinal("expired_date");
                                    if (!sdr.IsDBNull(checkForNull))
                                    {
                                        result.ExpiredDate = sdr.GetDateTime("expired_date");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository GetSingleSecCodeBySecCode={secCode} Exception!" +
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

        public async Task<string> GetSecCodeByISIN(string isin)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository GetSecCodeByISIN start " +
                $"with isin={isin}");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "SecCodes", "GetSecCodeByISIN.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository Error! File with SQL script not found at " + filePath);
                return "CommonRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository GetSecCodeByISIN execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        cmd.Parameters.AddWithValue("@isin", isin);

                        try
                        {
                            await con.OpenAsync();

                            return (string)await cmd.ExecuteScalarAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository GetSecCodeByISIN Exception!" +
                                $"\r\n{ex.Message}");
                            return $"GetPageFromIncoming Exception! {ex.Message}";
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }
            }
        }

        public void RenewStaticSecCodesList()
        {
            // сначала очистим List
            StaticData.SecCodes.Clear();
            // и наполним снова
            _commonRepo.FillStaticSecCodes();
        }
    }
}
