using DataAbstraction.Interfaces;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.BankDeposits;
using MySqlConnector;
using DataAbstraction.Models.MoneyByMonth;

namespace DataBaseRepository
{
	public class MySqlBankDepositsRepository : IMySqlBankDepositsRepository
	{
		private ILogger<MySqlBankDepositsRepository> _logger;
		private readonly string _connectionString;
		private IMySqlCommonRepository _commonRepo;
		//private InputHelper _helper;

		public MySqlBankDepositsRepository(
			IOptions<DataBaseConnectionSettings> connection,
			ILogger<MySqlBankDepositsRepository> logger,
			IMySqlCommonRepository commonRepo//,
			//InputHelper helper
			)
		{
			_logger = logger;
			_commonRepo=commonRepo;
			//_helper=helper;

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

		public async Task<int> GetActiveBankDepositsCount(CancellationToken cancellationToken)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("BankDeposits", "GetActiveBankDepositsCount.sql");
			if (query is null)
			{
				return 0;
			}
			return await _commonRepo.GetTableCountBySqlQuery(cancellationToken, query);
		}

		public async Task<int> GetAnyBankDepositsCount(CancellationToken cancellationToken)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("BankDeposits", "GetAnyBankDepositsCount.sql");
			if (query is null)
			{
				return 0;
			}
			return await _commonRepo.GetTableCountBySqlQuery(cancellationToken, query);
		}

		public async Task<List<BankDepositModel> ?>  GetPageWithActiveBankDeposits(
			int itemsAtPage, 
			int pageNumber,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
				$"GetPageWithActiveBankDeposits with itemsAtPage={itemsAtPage} page={pageNumber}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("BankDeposits", "GetPageWithActiveBankDeposits.sql");
			if (query is null)
			{
				return null;
			}

			return await GetPageWithBankDepositByQuery(query, itemsAtPage, pageNumber, cancellationToken);
		}
		public async Task<List<BankDepositModel> ?>  GetPageWithAnyBankDeposits(
			int itemsAtPage, 
			int pageNumber,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
				$"GetPageWithAnyBankDeposits with itemsAtPage={itemsAtPage} page={pageNumber}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("BankDeposits", "GetPageWithAnyBankDeposits.sql");
			if (query is null)
			{
				return null;
			}

			return await GetPageWithBankDepositByQuery(query, itemsAtPage, pageNumber, cancellationToken);
		}
		private async Task<List<BankDepositModel> ?> GetPageWithBankDepositByQuery(			 
			string query, 
			int itemsAtPage, 
			int pageNumber, 
			CancellationToken cancellationToken)
		{
			List<BankDepositModel> result = new List<BankDepositModel>();

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
								BankDepositModel newBankDepo = new BankDepositModel();

								cancellationToken.ThrowIfCancellationRequested();
								
								newBankDepo.Id = sdr.GetInt32("id");
								int isOpenInt = sdr.GetInt16("isopen");
								if (isOpenInt == 0)
								{
									newBankDepo.IsOpen = false;
								}

								newBankDepo.DateOpen = sdr.GetDateTime("event_date");
								newBankDepo.DateClose = sdr.GetDateTime("date_close");

								newBankDepo.Name = sdr.GetString("name");
								newBankDepo.PlaceNameSign = sdr.GetInt16("placed_name");

								newBankDepo.Percent = sdr.GetDecimal("percent").ToString();
								newBankDepo.Summ = sdr.GetDecimal("summ").ToString();

								int checkForNull = sdr.GetOrdinal("income_summ");
								if (!sdr.IsDBNull(checkForNull))
								{
									newBankDepo.SummIncome = sdr.GetDecimal("income_summ").ToString();
								}

								result.Add(newBankDepo);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
							$"GetPageWithBankDepositByQuery Exception!\r\n{ex.Message}");
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

		public async Task<string> Close(CloseBankDepositModel closeBankDeposit, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
				$"'Close' start, Id={closeBankDeposit.Id} SummIncome={closeBankDeposit.SummIncome}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("BankDeposits", "Close.sql");
			if (query is null)
			{
				return "MySqlBankDepositsRepository Error! File with SQL script not found!";
			}

			using (MySqlConnection con = new MySqlConnection(_connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@id", closeBankDeposit.Id);
					cmd.Parameters.AddWithValue("@income_summ", closeBankDeposit.SummIncome);

					try
					{
						await con.OpenAsync(cancellationToken);

						//Return Int32 Number of rows affected
						var insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
						_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
							$"'Close' execution affected {insertResult} lines");

						return insertResult.ToString();

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
							$"'Close' Exception!\r\n{ex.Message}");
						return ex.Message;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
			//UPDATE `bank_deposits` SET `isopen` = '0', `income_summ` = '12345.11' WHERE(`id` = '1');
		}

		public async Task<BankDepositModel?> GetBankDepositById(int id, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
				$"GetBankDepositById={id} start");

			BankDepositModel result = new BankDepositModel();

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("BankDeposits", "GetBankDepositById.sql");
			if (query is null)
			{
				return null;
			}

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
								cancellationToken.ThrowIfCancellationRequested();

								result.Id = sdr.GetInt32("id");
								int isOpenInt = sdr.GetInt16("isopen");
								if (isOpenInt == 0)
								{
									result.IsOpen = false;
								}

								result.DateOpen = sdr.GetDateTime("event_date");
								result.DateClose = sdr.GetDateTime("date_close");

								result.Name = sdr.GetString("name");
								result.PlaceNameSign = sdr.GetInt16("placed_name");

								result.Percent = sdr.GetDecimal("percent").ToString();
								result.Summ = sdr.GetDecimal("summ").ToString();

								int checkForNull = sdr.GetOrdinal("income_summ");
								if (!sdr.IsDBNull(checkForNull))
								{
									result.SummIncome = sdr.GetDecimal("income_summ").ToString();
								}
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
							$"GetBankDepositById={id} Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return result;
		}

		public async Task<string> Edit(BankDepositModel model, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
				$"Edit start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("BankDeposits", "Edit.sql");
			if (query is null)
			{
				return "MySqlDealsRepository Error! File with SQL script not found";
			}

			using (MySqlConnection con = new MySqlConnection(_connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@id", model.Id);

					cmd.Parameters.AddWithValue("@isopen", model.IsOpen);
					cmd.Parameters.AddWithValue("@date_open", model.DateOpen);
					cmd.Parameters.AddWithValue("@date_close", model.DateClose);
					cmd.Parameters.AddWithValue("@name", model.Name);
					cmd.Parameters.AddWithValue("@placed_name", model.PlaceNameSign);
					cmd.Parameters.AddWithValue("@percent", model.Percent);
					cmd.Parameters.AddWithValue("@summ", model.Summ);

					if (model.SummIncome is not null && !model.SummIncome.Equals(""))
					{
						cmd.Parameters.AddWithValue("@income_summ", model.SummIncome);
					}
					else
					{
						cmd.Parameters.AddWithValue("@income_summ", DBNull.Value);
					}

					try
					{
						await con.OpenAsync(cancellationToken);

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
						_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
							$"Edit execution affected {insertResult} lines");

						return insertResult.ToString();

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
							$"Edit Exception!\r\n{ex.Message}");
						return ex.Message;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		public async Task<string> Create(NewBankDepositModel model, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
				$"Create start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("BankDeposits", "Create.sql");
			if (query is null)
			{
				return "MySqlDealsRepository Error! File with SQL script not found";
			}

			using (MySqlConnection con = new MySqlConnection(_connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@date_open", model.DateOpen);
					cmd.Parameters.AddWithValue("@date_close", model.DateClose);
					cmd.Parameters.AddWithValue("@name", model.Name);
					cmd.Parameters.AddWithValue("@placed_name", model.PlaceNameSign);
					cmd.Parameters.AddWithValue("@percent", model.Percent);
					cmd.Parameters.AddWithValue("@summ", model.Summ);

					try
					{
						await con.OpenAsync(cancellationToken);

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
						_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
							$"Create execution affected {insertResult} lines");

						return insertResult.ToString();

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
							$"Create Exception!\r\n{ex.Message}");
						return ex.Message;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		public async Task<List<BankDepoDBModel>?> GetBankDepoChartData(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
				$"GetBankDepoChartData start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("BankDeposits", "GetBankDepoChartData.sql");
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

								newChartItem.DateOpen = sdr.GetDateTimeOffset("event_date");
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
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
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

		public async Task<List<BankDepoDBPaymentData>?> GetBankDepositsEndedAfterDate(
			CancellationToken cancellationToken,
			string date)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
				 $"GetBankDepositsEndedAfterDate {date} start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("BankDeposits", "GetBankDepositsEndedAfterDate.sql");
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
								if (isOpenInt == 0)
								{
									newChartItem.IsOpen = false;
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

						_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
							$"GetBankDepositsEndedAfterDate executed");
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlBankDepositsRepository " +
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
