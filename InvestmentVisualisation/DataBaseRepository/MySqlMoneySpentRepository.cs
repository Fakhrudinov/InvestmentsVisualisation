using DataAbstraction.Interfaces;
using DataAbstraction.Models.MoneySpent;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace DataBaseRepository
{
	public class MySqlMoneySpentRepository : IMySqlMoneySpentRepository
	{
		private ILogger<MySqlMoneySpentRepository> _logger;
		private readonly string _connectionString;
		private IMySqlCommonRepository _commonRepo;

		public MySqlMoneySpentRepository(
			IOptions<DataBaseConnectionSettings> connection,
			ILogger<MySqlMoneySpentRepository> logger,
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

		public async Task<List<MoneySpentAndIncomeModel>?> GetMoneySpentAndIncomeModelChartData(
			CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
				$"GetMoneySpentAndIncomeModelChartData start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename(
				"MoneySpent", 
				"GetMoneySpentAndIncomeModelChartData.sql");
			if (query is null)
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
					$"GetMoneySpentAndIncomeModelChartData SQL file not found: GetMoneySpentAndIncomeModelChartData.sql");
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
								newChartItem.Date = sdr.GetDateTimeOffset("event_date");
								newChartItem.Divident = sdr.GetInt32("div_round");
								newChartItem.AverageDivident = sdr.GetInt32("avrg_div_round");

								int checkForNull4 = sdr.GetOrdinal("spent_round");
								if (!sdr.IsDBNull(checkForNull4))
								{
									newChartItem.MoneySpent = sdr.GetInt32("spent_round");
								}

								сhartItems.Add(newChartItem);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
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

		public async Task<int> GetMoneySpentCount(CancellationToken cancellationToken)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("MoneySpent", "GetMoneySpentCount.sql");
			if (query is null)
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
					$"GetMoneySpentCount SQL file not found: GetMoneySpentCount.sql");
				return 0;
			}
			return await _commonRepo.GetTableCountBySqlQuery(cancellationToken, query);
		}

		public async Task<List<MoneySpentByMonthModel>?> GetPageFromMoneySpent(
			CancellationToken cancellationToken, 
			int itemsAtPage, 
			int pageNumber)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
				 $"GetMoneySpentAndIncomeModelChartData start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename(
				"MoneySpent",
				"GetMoneySpentPage.sql");
			if (query is null)
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
					$"GetTableCountBySqlQuery SQL file not found: GetMoneySpentPage.sql");
				return null;
			}

			List<MoneySpentByMonthModel> items = new List<MoneySpentByMonthModel>();

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
								MoneySpentByMonthModel newItem = new MoneySpentByMonthModel();
								newItem.Date = sdr.GetDateTime("event_date").ToString("yyyy-MM-dd");
								
								int checkForNull = sdr.GetOrdinal("total");
								if (!sdr.IsDBNull(checkForNull))
								{
									newItem.Total = sdr.GetDecimal("total").ToString();
								}

								int checkForNulla = sdr.GetOrdinal("appartment");
								if (!sdr.IsDBNull(checkForNulla))
								{
									newItem.Appartment = sdr.GetDecimal("appartment").ToString();
								}

								int checkForNulle = sdr.GetOrdinal("electricity");
								if (!sdr.IsDBNull(checkForNulle))
								{
									newItem.Electricity = sdr.GetDecimal("electricity").ToString();
								}

								int checkForNulli = sdr.GetOrdinal("internet");
								if (!sdr.IsDBNull(checkForNulli))
								{
									newItem.Internet = sdr.GetDecimal("internet").ToString();
								}

								int checkForNullp = sdr.GetOrdinal("phone");
								if (!sdr.IsDBNull(checkForNullp))
								{
									newItem.Phone = sdr.GetDecimal("phone").ToString();
								}

								int checkForNullt = sdr.GetOrdinal("transport");
								if (!sdr.IsDBNull(checkForNullt))
								{
									newItem.Transport = sdr.GetDecimal("transport").ToString();
								}

								int checkForNulls = sdr.GetOrdinal("supermarket");
								if (!sdr.IsDBNull(checkForNulls))
								{
									newItem.SuperMarkets = sdr.GetDecimal("supermarket").ToString();
								}

								int checkForNullm = sdr.GetOrdinal("marketplaces");
								if (!sdr.IsDBNull(checkForNullm))
								{
									newItem.MarketPlaces = sdr.GetDecimal("marketplaces").ToString();
								}


								items.Add(newItem);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
							$"GetMoneySpentAndIncomeModelChartData Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			if (items.Count == 0)
			{
				return null;
			}

			return items;
		}

		public async Task<MoneySpentByMonthModel ?> GetSingleRowByDateTime(CancellationToken cancellationToken, DateTime date)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
				 $"GetSingleRowByDateTime start for date={date}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename(
				"MoneySpent",
				"GetSingleRowByDateTime.sql");
			if (query is null)
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
					$"GetSingleRowByDateTime SQL file not found: GetSingleRowByDateTime.sql");
				return null;
			}

			MoneySpentByMonthModel item = new MoneySpentByMonthModel();

			using (MySqlConnection con = new MySqlConnection(_connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					string dateString = date.ToString("yyyy-MM-dd");
					cmd.Parameters.AddWithValue("@date", dateString);

					try
					{
						await con.OpenAsync(cancellationToken);

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
						{
							while (await sdr.ReadAsync(cancellationToken))
							{
								item.Date = sdr.GetDateTime("event_date").ToString("yyyy-MM-dd");

								int checkForNull = sdr.GetOrdinal("total");
								if (!sdr.IsDBNull(checkForNull))
								{
									item.Total = sdr.GetDecimal("total").ToString();
								}

								int checkForNulla = sdr.GetOrdinal("appartment");
								if (!sdr.IsDBNull(checkForNulla))
								{
									item.Appartment = sdr.GetDecimal("appartment").ToString();
								}

								int checkForNulle = sdr.GetOrdinal("electricity");
								if (!sdr.IsDBNull(checkForNulle))
								{
									item.Electricity = sdr.GetDecimal("electricity").ToString();
								}

								int checkForNulli = sdr.GetOrdinal("internet");
								if (!sdr.IsDBNull(checkForNulli))
								{
									item.Internet = sdr.GetDecimal("internet").ToString();
								}

								int checkForNullp = sdr.GetOrdinal("phone");
								if (!sdr.IsDBNull(checkForNullp))
								{
									item.Phone = sdr.GetDecimal("phone").ToString();
								}

								int checkForNullt = sdr.GetOrdinal("transport");
								if (!sdr.IsDBNull(checkForNullt))
								{
									item.Transport = sdr.GetDecimal("transport").ToString();
								}

								int checkForNulls = sdr.GetOrdinal("supermarket");
								if (!sdr.IsDBNull(checkForNulls))
								{
									item.SuperMarkets = sdr.GetDecimal("supermarket").ToString();
								}

								int checkForNullm = sdr.GetOrdinal("marketplaces");
								if (!sdr.IsDBNull(checkForNullm))
								{
									item.MarketPlaces = sdr.GetDecimal("marketplaces").ToString();
								}
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
							$"GetMoneySpentAndIncomeModelChartData Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return item;
		}

		public async Task<string> EditMoneySpentItem(CancellationToken cancellationToken, MoneySpentByMonthModel model)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
				$"EditMoneySpentItem start, newModel is\r\nDate={model.Date} " +
				$"Total={model.Total} Appartment={model.Appartment} Electricity={model.Electricity} " +
				$"Phone={model.Phone} Internet={model.Internet}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("MoneySpent", "EditMoneySpentItem.sql");
			if (query is null)
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
					$"EditMoneySpentItem SQL file not found: EditMoneySpentItem.sql");
				return "MySqlMoneySpentRepository Error! File with SQL script EditMoneySpentItem.sql not found";
			}

			using (MySqlConnection con = new MySqlConnection(_connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@date", model.Date);

					if (model.Total is not null)
					{
						cmd.Parameters.AddWithValue("@total", model.Total);
					}
					else
					{
						cmd.Parameters.AddWithValue("@total", null);
					}

					if (model.Appartment is not null)
					{
						cmd.Parameters.AddWithValue("@appartment", model.Appartment);
					}
					else
					{
						cmd.Parameters.AddWithValue("@appartment", null);
					}

					if (model.Electricity is not null)
					{
						cmd.Parameters.AddWithValue("@electricity", model.Electricity);
					}
					else
					{
						cmd.Parameters.AddWithValue("@electricity", null);
					}

					if (model.Internet is not null)
					{
						cmd.Parameters.AddWithValue("@internet", model.Internet);
					}
					else
					{
						cmd.Parameters.AddWithValue("@internet", null);
					}

					if (model.Phone is not null)
					{
						cmd.Parameters.AddWithValue("@phone", model.Phone);
					}
					else
					{
						cmd.Parameters.AddWithValue("@phone", null);
					}

					if (model.Transport is not null)
					{
						cmd.Parameters.AddWithValue("@transport", model.Transport);
					}
					else
					{
						cmd.Parameters.AddWithValue("@transport", null);
					}
					
					if (model.SuperMarkets is not null)
					{
						cmd.Parameters.AddWithValue("@supermarket", model.SuperMarkets);
					}
					else
					{
						cmd.Parameters.AddWithValue("@supermarket", null);
					}
					
					if (model.MarketPlaces is not null)
					{
						cmd.Parameters.AddWithValue("@marketplaces", model.MarketPlaces);
					}
					else
					{
						cmd.Parameters.AddWithValue("@marketplaces", null);
					}


					try
					{
						await con.OpenAsync(cancellationToken);

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
						_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
							$"EditMoneySpentItem execution affected {insertResult} lines");

						return insertResult.ToString();

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
							$"EditMoneySpentItem Exception!\r\n{ex.Message}");
						return ex.Message;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		public async Task<MoneySpentByMonthModel?> GetLastRow(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
				 $"GetLastRow start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename(
				"MoneySpent",
				"GetLastRow.sql");
			if (query is null)
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
					$"GetLastRow SQL file not found: GetLastRow.sql");
				return null;
			}

			MoneySpentByMonthModel item = new MoneySpentByMonthModel();

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
								item.Date = sdr.GetDateTime("event_date").ToString("yyyy-MM-dd");

								int checkForNulli = sdr.GetOrdinal("internet");
								if (!sdr.IsDBNull(checkForNulli))
								{
									item.Internet = sdr.GetDecimal("internet").ToString();
								}

								int checkForNullp = sdr.GetOrdinal("phone");
								if (!sdr.IsDBNull(checkForNullp))
								{
									item.Phone = sdr.GetDecimal("phone").ToString();
								}
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
							$"GetLastRow Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return item;
		}

		public async Task<string> CreateNewItem(CancellationToken cancellationToken, MoneySpentByMonthModel model)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
				$"CreateNewItem start, newModel is\r\nDate={model.Date} " +
				$"Total={model.Total} Appartment={model.Appartment} Electricity={model.Electricity} " +
				$"Phone={model.Phone} Internet={model.Internet}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("MoneySpent", "CreateNewItem.sql");
			if (query is null)
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
					$"CreateNewItem SQL file not found: CreateNewItem.sql");
				return "MySqlMoneySpentRepository Error! File with SQL script CreateNewItem.sql not found";
			}

			using (MySqlConnection con = new MySqlConnection(_connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@date", model.Date);

					if (model.Total is not null)
					{
						cmd.Parameters.AddWithValue("@total", model.Total);
					}
					else
					{
						cmd.Parameters.AddWithValue("@total", null);
					}

					if (model.Appartment is not null)
					{
						cmd.Parameters.AddWithValue("@appartment", model.Appartment);
					}
					else
					{
						cmd.Parameters.AddWithValue("@appartment", null);
					}

					if (model.Electricity is not null)
					{
						cmd.Parameters.AddWithValue("@electricity", model.Electricity);
					}
					else
					{
						cmd.Parameters.AddWithValue("@electricity", null);
					}

					if (model.Internet is not null)
					{
						cmd.Parameters.AddWithValue("@internet", model.Internet);
					}
					else
					{
						cmd.Parameters.AddWithValue("@internet", null);
					}

					if (model.Phone is not null)
					{
						cmd.Parameters.AddWithValue("@phone", model.Phone);
					}
					else
					{
						cmd.Parameters.AddWithValue("@phone", null);
					}

					if (model.Transport is not null)
					{
						cmd.Parameters.AddWithValue("@transport", model.Transport);
					}
					else
					{
						cmd.Parameters.AddWithValue("@transport", null);
					}

					if (model.SuperMarkets is not null)
					{
						cmd.Parameters.AddWithValue("@supermarket", model.SuperMarkets);
					}
					else
					{
						cmd.Parameters.AddWithValue("@supermarket", null);
					}

					if (model.MarketPlaces is not null)
					{
						cmd.Parameters.AddWithValue("@marketplaces", model.MarketPlaces);
					}
					else
					{
						cmd.Parameters.AddWithValue("@marketplaces", null);
					}



					try
					{
						await con.OpenAsync(cancellationToken);

						int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
						_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
							$"CreateNewItem execution affected {insertResult} lines");

						return insertResult.ToString();

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlMoneySpentRepository " +
							$"CreateNewItem Exception!\r\n{ex.Message}");
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
