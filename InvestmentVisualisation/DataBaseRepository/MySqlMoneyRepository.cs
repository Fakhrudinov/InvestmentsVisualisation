using DataAbstraction.Interfaces;
using DataAbstraction.Models.BaseModels;
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

        public async Task<List<MoneyModel>> GetMoneyLastYearPage(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                $"GetMoneyLastYearPage start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Money", "GetMoneyLastYearPage.sql");
			if (query is null)
			{
				return _result;
			}

			return await GetMoneyPageByQuery(cancellationToken, query);
        }

        public async Task<List<MoneyModel>> GetMoneyYearPage(CancellationToken cancellationToken, int year)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneyRepository " +
                $"GetMoneyYearPage {year} start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Money", "GetMoneyYearPage.sql");
			if (query is null)
			{
				return _result;
			}

			query = query.Replace("@year", year.ToString());
			return await GetMoneyPageByQuery(cancellationToken, query);
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

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Money", "RecalculateMoney.sql");
			if (query is not null)
			{
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

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Money", "GetBankDepoChartData.sql");
			if (query is null)
			{
				return null;
			}

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

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Money", "GetMoneySpentAndIncomeModelChartData.sql");
			if (query is null)
			{
				return null;
			}

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

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Money", "GetActualSecCodeAndNameAndPieces.sql");
			if (query is null)
			{
				return null;
			}
			query = query.Replace("@year", year.ToString());

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

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Money", "GetBankDepositsEndedAfterDate.sql");
			if (query is null)
			{
				return null;
			}

			List<BankDepoDBPaymentData> result = new List<BankDepoDBPaymentData>();

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
