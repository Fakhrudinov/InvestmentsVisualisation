using DataAbstraction.Interfaces;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.MoneyByMonth;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Collections.Generic;

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

        public async Task<List<MoneyModel>> GetMoneyLastYearPage(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                $"GetMoneyLastYearPage start");            

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Money", "GetMoneyLastYearPage.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return _result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                    $"GetMoneyLastYearPage execute query \r\n{query}");

                return await GetMoneyPageByQuery(cancellationToken, query);
            }
        }

        public async Task<List<MoneyModel>> GetMoneyYearPage(CancellationToken cancellationToken, int year)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                $"GetMoneyYearPage {year} start");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Money", "GetMoneyYearPage.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return _result;
            }
            else
            {
                string query = File.ReadAllText(filePath);

                query = query.Replace("@year", year.ToString());

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                    $"GetMoneyYearPage {year} execute query \r\n{query}");

                return await GetMoneyPageByQuery(cancellationToken, query);
            }
        }

        private async Task<List<MoneyModel>> GetMoneyPageByQuery(CancellationToken cancellationToken, string query)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            while (await sdr.ReadAsync(cancellationToken))
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
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                            $"GetMoneyLastYearPage Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return _result;
        }

        public async Task RecalculateMoney(CancellationToken cancellationToken, string data)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                $"RecalculateMoney {data} start");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Money", "RecalculateMoney.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository Error! " +
                    $"File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                    $"RecalculateMoney {data} execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        cmd.Parameters.AddWithValue("@data", data);

                        try
                        {
                            await con.OpenAsync(cancellationToken);
                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                                $"RecalculateMoney executed");

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                                $"RecalculateMoney Exception!\r\n{ex.Message}");
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }
            }
        }

        public void FillFreeMoney(CancellationToken cancellationToken)
        {
            _commonRepo.FillFreeMoney();
        }

		public async Task<List<BankDepoDBModel>?> GetBankDepoChartData(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
				$"GetBankDepoChartData start");

			string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "Money", "GetBankDepoChartData.sql");
			if (!File.Exists(filePath))
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository Error! " +
					$"File with SQL script not found at " + filePath);
				return null;
			}

			string query = File.ReadAllText(filePath);
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                $"GetBankDepoChartData query GetBankDepoChartData.sql text:\r\n{query}");

            List<BankDepoDBModel> bankDepoChartItems = new List<BankDepoDBModel>();

			using (MySqlConnection con = new MySqlConnection(_connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					try
					{
						await con.OpenAsync(cancellationToken);

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
						{
							while (await sdr.ReadAsync(cancellationToken))
							{
								BankDepoDBModel newChartItem = new BankDepoDBModel();

								newChartItem.DateOpen = sdr.GetDateTimeOffset("date_open");
								newChartItem.DateClose = sdr.GetDateTimeOffset("date_close");
								newChartItem.Name = sdr.GetString("name");
								newChartItem.PlaceName = sdr.GetInt16("placed_name");
                                newChartItem.Percent = sdr.GetDecimal("percent");
								newChartItem.SummAmount = sdr.GetDecimal("summ");

								bankDepoChartItems.Add(newChartItem);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
							$"GetBankDepoChartData Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

            if (bankDepoChartItems.Count == 0)
            {
                return null;
            }

            return bankDepoChartItems;
		}

        public async Task<List<MoneySpentAndIncomeModel>?> GetMoneySpentAndIncomeModelChartData(
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                $"GetMoneySpentAndIncomeModelChartData start");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "Money", 
                "GetMoneySpentAndIncomeModelChartData.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return null;
            }

            string query = File.ReadAllText(filePath);
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                $"GetMoneySpentAndIncomeModelChartData query GetMoneySpentAndIncomeModelChartData.sql text:\r\n{query}");

            List<MoneySpentAndIncomeModel> сhartItems = new List<MoneySpentAndIncomeModel>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            while (await sdr.ReadAsync(cancellationToken))
                            {
                                MoneySpentAndIncomeModel newChartItem = new MoneySpentAndIncomeModel();
                                newChartItem.Date = sdr.GetDateTimeOffset("date");
                                newChartItem.Divident = sdr.GetInt32("div_round");
                                newChartItem.AverageDivident = sdr.GetInt32("avrg_div_round");
                                newChartItem.MoneySpent = sdr.GetInt32("spent_round");

                                сhartItems.Add(newChartItem);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                            $"GetMoneySpentAndIncomeModelChartData Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            if (сhartItems.Count == 0)
            {
                return null;
            }

            return сhartItems;
        }

        public async Task<List<SecCodeAndNameAndPiecesModel>?> GetActualSecCodeAndNameAndPieces(
            CancellationToken cancellationToken, 
            int year)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                $"GetActualSecCodeAndNameAndPieces start with year={year}");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(),
                "SqlQueries",
                "Money",
                "GetActualSecCodeAndNameAndPieces.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return null;
            }

            string query = File.ReadAllText(filePath);
            query = query.Replace("@year", year.ToString());
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                $"GetActualSecCodeAndNameAndPieces query GetActualSecCodeAndNameAndPieces.sql text:\r\n{query}");

            List<SecCodeAndNameAndPiecesModel> сhartItems = new List<SecCodeAndNameAndPiecesModel>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            while (await sdr.ReadAsync(cancellationToken))
                            {
                                SecCodeAndNameAndPiecesModel newChartItem = new SecCodeAndNameAndPiecesModel();
                                newChartItem.SecCode = sdr.GetString("seccode");
                                newChartItem.Name = sdr.GetString("name");
                                newChartItem.Pieces = sdr.GetInt32("pieces");

                                сhartItems.Add(newChartItem);
                                /*
                                 SELECT si.seccode, si.name, sv.pieces_2024 as pieces-- , sv.volume_2024 
	                                FROM money_test.seccode_info si 
                                    right join money_test.sec_volume sv
                                    on sv.seccode = si.seccode
		                                where si.secboard = 1 
                                        and si.expired_date is null;
                                */
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                            $"GetActualSecCodeAndNameAndPieces Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            if (сhartItems.Count == 0)
            {
                return null;
            }

            return сhartItems;
        }

        public async Task<List<BankDepoDBPaymentData>?> GetBankDepositsEndedAfterDate(
            CancellationToken cancellationToken, 
            string date)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                 $"GetBankDepositsEndedAfterDate {date} start");

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "Money", 
                "GetBankDepositsEndedAfterDate.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return null;
            }

            List<BankDepoDBPaymentData> result = new List<BankDepoDBPaymentData>();
            string query = File.ReadAllText(filePath);

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                    $"GetBankDepositsEndedAfterDate {date} execute query \r\n{query}");

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;
                    cmd.Parameters.AddWithValue("@data", date);

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            while (await sdr.ReadAsync(cancellationToken))
                            {
                                BankDepoDBPaymentData newChartItem = new BankDepoDBPaymentData();
                                // select bd.name,
                                // bd.date_close,
                                // bd.summ,
                                // bd.percent,
                                // bd.isopen,
                                // bd.income_summ, NULL !
                                // DATEDIFF(bd.date_close,bd.date_open) as days
                                newChartItem.Name = sdr.GetString("name");
                                newChartItem.DateClose = sdr.GetDateTimeOffset("date_close");
                                newChartItem.Percent = sdr.GetDecimal("percent");
                                newChartItem.SummAmount = sdr.GetDecimal("summ");

                                int isOpenInt = sdr.GetInt16("isopen");
                                if (isOpenInt == 1)
                                {
                                    newChartItem.IsOpen = true;
                                }

                                int checkForNull1 = sdr.GetOrdinal("income_summ");
                                if (!sdr.IsDBNull(checkForNull1))
                                {
                                    newChartItem.IncomeSummAmount = sdr.GetDecimal("income_summ");
                                }

                                newChartItem.DaysOfDeposit = sdr.GetInt16("days");


                                result.Add(newChartItem);
                            }
                        }

                        _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                            $"GetBankDepositsEndedAfterDate executed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                            $"GetBankDepositsEndedAfterDate Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            if (result.Count == 0)
            {
                return null;
            }
            return result;
        }
    }
}
