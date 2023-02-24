using DataAbstraction.Interfaces;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using DataAbstraction.Models.Incoming;

namespace DataBaseRepository
{
    public class CommonRepository : IMySqlCommonRepository
    {
        private ILogger<CommonRepository> _logger;
        private readonly string _connectionString;

        public CommonRepository(IOptions<DataBaseConnectionSettings> connection, ILogger<CommonRepository> logger)
        {
            _logger = logger;

            _connectionString = $"" +
                $"Server={connection.Value.Server};" +
                $"User ID={connection.Value.UserId};" +
                $"Password={connection.Value.Password};" +
                $"Port={connection.Value.Port};" +
                $"Database={connection.Value.Database}";
        }

        public async Task<int> GetTableCountBySqlQuery(string query)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository GetTableCountBySqlQuery start with \r\n{query}");

            int result = 0;

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    try
                    {
                        await con.OpenAsync();
                        result = (int)(long)await cmd.ExecuteScalarAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository GetTableCountBySqlQuery Exception!\r\n" +
                            $"{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;
        }

        public void FillStaticCategories()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "GetAllFromCategory.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository Error! File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository FillStaticCategories execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        try
                        {
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
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository FillStaticCategories Exception!\r\n" +
                                $"{ex.Message}");
                        }
                        finally
                        {
                            con.Close();
                        }
                    }
                }
            }
        }

        public void FillStaticSecBoards()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "GetAllFromSecboard.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository Error! File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository FillStaticSecBoards execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        try
                        {
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
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository FillStaticSecBoards Exception!\r\n" +
                                $"{ex.Message}");
                        }
                        finally
                        {
                            con.Close();
                        }
                    }
                }
            }
        }

        public void FillStaticSecCodes()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "GetActualSecCodeAndSecBoard.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository Error! File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository FillStaticSecCodes execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        try
                        {
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
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository FillStaticSecCodes Exception!\r\n" +
                                $"{ex.Message}");
                        }
                        finally
                        {
                            con.Close();
                        }
                    }
                }
            }
        }

        public async Task<string> DeleteSingleRecordByQuery(string query)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository DeleteSingleRecordByQuery " +
                $"start execute query \r\n{query}");

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    try
                    {
                        await con.OpenAsync();

                        //Return Int32 Number of rows affected
                        int insertResult = await cmd.ExecuteNonQueryAsync();
                        _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository DeleteSingleRecordByQuery execution " +
                            $"affected {insertResult} lines");

                        return insertResult.ToString();

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository DeleteSingleRecordByQuery Exception!" +
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
}
