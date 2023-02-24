using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Deals;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

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

            if (StaticData.SecBoards.Count == 0)
            {
                _commonRepo.FillStaticSecBoards();
            }

            if (StaticData.SecCodes.Count == 0)
            {
                _commonRepo.FillStaticSecCodes();
            }
        }

        public async Task<int> GetDealsCount()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Deals", "GetDealsCount.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository Error! File with SQL script not found at " + filePath);
                return 0;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                return await _commonRepo.GetTableCountBySqlQuery(query);
            }
        }

        public async Task<List<DealModel>> GetPageFromDeals(int itemsAtPage, int pageNumber)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository GetPageFromDeals start " +
                $"with itemsAtPage={itemsAtPage} page={pageNumber}");

            List<DealModel> result = new List<DealModel>();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Deals", "GetPageFromDeals.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository Error! File with SQL script not found at " + filePath);
                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository GetPageFromDeals execute query \r\n{query}");

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
                                    DealModel newDeal = new DealModel();

                                    newDeal.Id = sdr.GetInt32("id");
                                    newDeal.Date = sdr.GetDateTime("date");
                                    newDeal.SecCode = sdr.GetString("seccode");
                                    newDeal.SecBoard = sdr.GetInt32("secboard");

                                    newDeal.AvPrice = sdr.GetDecimal("av_price").ToString();
                                    newDeal.Pieces = sdr.GetInt32("pieces");

                                    int checkForNull = sdr.GetOrdinal("comission");
                                    if (!sdr.IsDBNull(checkForNull))
                                    {
                                        newDeal.Comission = sdr.GetDecimal("comission").ToString();
                                    }

                                    int checkForNullNkd = sdr.GetOrdinal("nkd");
                                    if (!sdr.IsDBNull(checkForNullNkd))
                                    {
                                        newDeal.NKD = sdr.GetDecimal("nkd").ToString();
                                    }

                                    result.Add(newDeal);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository GetPageFromDeals Exception!" +
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

        public async Task<string> CreateNewDeal(CreateDealsModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository CreateNewDeal start, newModel is\r\n" +
                $"{model.Date} {model.SecCode} SecBoard={model.SecBoard} AvPrice={model.AvPrice} Pieces={model.Pieces} " +
                $"Comission={model.Comission} NKD={model.NKD}");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Deals", "CreateNewDeal.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository Error! File with SQL script not found at " + filePath);
                return "MySqlRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                if (model.Comission is null)
                {
                    query = query.Replace(", `comission`", "");
                    query = query.Replace(", @comission", "");
                }
                if (model.NKD is null)
                {
                    query = query.Replace(", `nkd`", "");
                    query = query.Replace(", @nkd", "");
                }

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository CreateNewDeal execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        //(@date_time, @seccode, @secboard, @av_price, @pieces, @comission, @nkd);
                        cmd.Parameters.AddWithValue("@date_time", model.Date);
                        cmd.Parameters.AddWithValue("@seccode", model.SecCode);
                        cmd.Parameters.AddWithValue("@secboard", model.SecBoard);
                        cmd.Parameters.AddWithValue("@av_price", model.AvPrice);
                        cmd.Parameters.AddWithValue("@pieces", model.Pieces);
                        if (model.Comission is not null)
                        {
                            cmd.Parameters.AddWithValue("@comission", model.Comission);
                        }
                        if (model.NKD is not null)
                        {
                            cmd.Parameters.AddWithValue("@nkd", model.NKD);
                        }

                        try
                        {
                            await con.OpenAsync();

                            //Return Int32 Number of rows affected
                            var insertResult = await cmd.ExecuteNonQueryAsync();
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository CreateNewDeal execution " +
                                $"affected {insertResult} lines");

                            return insertResult.ToString();

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository CreateNewDeal Exception!" +
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
            //INSERT INTO deals
            //  (`date`, `seccode`, `secboard`, `av_price`, `pieces`, `comission`, `nkd`) 
            //VALUES
            //  (@date_time, @seccode, @secboard, @av_price, @pieces, @comission, @nkd);
        }

        public async Task<string> EditSingleDeal(DealModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository EditSingleDeal start, newModel is\r\n" +
                $"Id={model.Id} {model.Date} {model.SecCode} SecBoard={model.SecBoard} AvPrice={model.AvPrice} Pieces={model.Pieces} " +
                $"Comission={model.Comission} nkd={model.NKD}");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Deals", "EditSingleDeal.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository Error! File with SQL script not found at " + filePath);
                return "MySqlRepository Error! File with SQL script not found at " + filePath;
            }
            else
            {
                string query = File.ReadAllText(filePath);

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository EditSingleDeal execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        //(@date_time, @seccode, @secboard, @av_price, @pieces, @comission, @nkd);
                        cmd.Parameters.AddWithValue("@id", model.Id);
                        cmd.Parameters.AddWithValue("@date_time", model.Date);
                        cmd.Parameters.AddWithValue("@seccode", model.SecCode);
                        cmd.Parameters.AddWithValue("@secboard", model.SecBoard);
                        cmd.Parameters.AddWithValue("@av_price", model.AvPrice);
                        cmd.Parameters.AddWithValue("@pieces", model.Pieces);
                        if (model.Comission is not null)
                        {
                            cmd.Parameters.AddWithValue("@comission", model.Comission);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@comission", null);
                        }

                        if (model.NKD is not null)
                        {
                            cmd.Parameters.AddWithValue("@nkd", model.NKD);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@nkd", null);
                        }

                        try
                        {
                            await con.OpenAsync();

                            //Return Int32 Number of rows affected
                            int insertResult = await cmd.ExecuteNonQueryAsync();
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository EditSingleDeal execution " +
                                $"affected {insertResult} lines");

                            return insertResult.ToString();

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository EditSingleDeal Exception!" +
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

        public async Task<DealModel> GetSingleDealById(int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository GetSingleDealById={id} start");

            DealModel result = new DealModel();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Deals", "GetSingleDealById.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository Error! File with SQL script not found at " + filePath);

                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository GetSingleDealById={id} execute query \r\n{query}");

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
                                    result.AvPrice= sdr.GetDecimal("av_price").ToString();
                                    result.Pieces = sdr.GetInt32("pieces");

                                    int checkForNull = sdr.GetOrdinal("comission");
                                    if (!sdr.IsDBNull(checkForNull))
                                    {
                                        result.Comission = sdr.GetDecimal("comission").ToString();
                                    }

                                    int checkForNull2 = sdr.GetOrdinal("nkd");
                                    if (!sdr.IsDBNull(checkForNull2))
                                    {
                                        result.NKD = sdr.GetDecimal("nkd").ToString();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository GetSingleDealById={id} Exception!" +
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

        public async Task<string> DeleteSingleDeal(int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository DeleteSingleDeal id={id} start");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Deals", "DeleteSingleDeal.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlRepository Error! File with SQL script not found at " + filePath);
                return "MySqlDealsRepository Error! File with SQL script not found at " + filePath;
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
