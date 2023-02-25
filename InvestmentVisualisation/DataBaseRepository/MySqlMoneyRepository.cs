using DataAbstraction.Interfaces;
using DataAbstraction.Models.MoneyByMonth;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace DataBaseRepository
{
    public class MySqlMoneyRepository : IMySqlMoneyRepository
    {
        private ILogger<MySqlMoneyRepository> _logger;
        private readonly string _connectionString;
        private List<MoneyModel> _result = new List<MoneyModel>();
        private IMySqlCommonRepository _commonRepo;

        public MySqlMoneyRepository(
            IOptions<DataBaseConnectionSettings> connection,
            ILogger<MySqlMoneyRepository> logger,
            IMySqlCommonRepository commonRepo)
        {
            _logger = logger;
            _connectionString = $"" +
                $"Server={connection.Value.Server};" +
                $"User ID={connection.Value.UserId};" +
                $"Password={connection.Value.Password};" +
                $"Port={connection.Value.Port};" +
                $"Database={connection.Value.Database}";

            _commonRepo=commonRepo;
        }

        public async Task<List<MoneyModel>> GetMoneyLastYearPage()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository GetMoneyLastYearPage start");            

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Money", "GetMoneyLastYearPage.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository Error! File with SQL script not found at " + filePath);
                return _result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository GetMoneyLastYearPage execute query \r\n{query}");

                return await GetMoneyPageByQuery(query);
            }
        }

        public async Task<List<MoneyModel>> GetMoneyYearPage(int year)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository GetMoneyYearPage {year} start");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Money", "GetMoneyYearPage.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository Error! File with SQL script not found at " + filePath);
                return _result;
            }
            else
            {
                string query = File.ReadAllText(filePath);

                query = query.Replace("@year", year.ToString());

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository GetMoneyYearPage {year} execute query \r\n{query}");

                return await GetMoneyPageByQuery(query);
            }
        }

        private async Task<List<MoneyModel>> GetMoneyPageByQuery(string query)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    try
                    {
                        await con.OpenAsync();

                        using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await sdr.ReadAsync())
                            {
                                MoneyModel newMoney = new MoneyModel();

                                newMoney.Year = sdr.GetInt32("year");
                                newMoney.Month = sdr.GetInt32("month");

                                int checkForNull1 = sdr.GetOrdinal("total_in");
                                if (!sdr.IsDBNull(checkForNull1))
                                {
                                    newMoney.TotalIn = sdr.GetDecimal("total_in");
                                }

                                int checkForNull2 = sdr.GetOrdinal("month_in");
                                if (!sdr.IsDBNull(checkForNull2))
                                {
                                    newMoney.MonthIn = sdr.GetDecimal("month_in");
                                }

                                int checkForNull3 = sdr.GetOrdinal("dividend");
                                if (!sdr.IsDBNull(checkForNull3))
                                {
                                    newMoney.Divident = sdr.GetDecimal("dividend");
                                }

                                int checkForNull4 = sdr.GetOrdinal("dosrochnoe");
                                if (!sdr.IsDBNull(checkForNull4))
                                {
                                    newMoney.Dosrochnoe = sdr.GetDecimal("dosrochnoe");
                                }

                                int checkForNull5 = sdr.GetOrdinal("deals_sum");
                                if (!sdr.IsDBNull(checkForNull5))
                                {
                                    newMoney.DealsSum = sdr.GetDecimal("deals_sum");
                                }

                                int checkForNull6 = sdr.GetOrdinal("brok_comission");
                                if (!sdr.IsDBNull(checkForNull6))
                                {
                                    newMoney.BrokComission = sdr.GetDecimal("brok_comission");
                                }

                                int checkForNull7 = sdr.GetOrdinal("money_sum");
                                if (!sdr.IsDBNull(checkForNull7))
                                {
                                    newMoney.MoneySum = sdr.GetDecimal("money_sum");
                                }

                                _result.Add(newMoney);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository GetMoneyLastYearPage Exception!" +
                            $"\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return _result;
        }

        public async Task RecalculateMoney(string data)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository RecalculateMoney {data} start");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Money", "RecalculateMoney.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository Error! File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository RecalculateMoney {data} execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        cmd.Parameters.AddWithValue("@data", data);

                        try
                        {
                            await con.OpenAsync();
                            await cmd.ExecuteNonQueryAsync();
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository RecalculateMoney executed");

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository RecalculateMoney Exception!" +
                                $"\r\n{ex.Message}");
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }
            }
        }

        public void FillFreeMoney()
        {
            _commonRepo.FillFreeMoney();
        }
    }
}
