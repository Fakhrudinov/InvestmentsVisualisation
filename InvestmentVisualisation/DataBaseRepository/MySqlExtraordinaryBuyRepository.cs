using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.ExtraordinaryBuy;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;


namespace DataBaseRepository
{
	public class MySqlExtraordinaryBuyRepository : IMySqlExtraordinaryBuyRepository
	{
		private ILogger<MySqlExtraordinaryBuyRepository> _logger;
		private readonly string _connectionString;
		private IMySqlCommonRepository _commonRepo;
		//private InputHelper _helper;

		public MySqlExtraordinaryBuyRepository(
			IOptions<DataBaseConnectionSettings> connection,
			ILogger<MySqlExtraordinaryBuyRepository> logger,
			IMySqlCommonRepository commonRepo
			//,InputHelper helper
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

		public async Task<int> GetExtraordinaryBuyCount(CancellationToken cancellationToken)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("ExtraordinaryBuy", "GetExtraordinaryBuyCount.sql");
			if (query is null)
			{
				return 0;
			}
			return await _commonRepo.GetTableCountBySqlQuery(cancellationToken, query);
		}

		public async Task<List<ExtraordinaryBuyModel>?> GetPageFromExtraordinaryBuy(
			CancellationToken cancellationToken, 
			int itemsAtPage, 
			int pageNumber)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlExtraordinaryBuyRepository " +
				$"GetPageFromExtraordinaryBuy start with itemsAtPage={itemsAtPage} page={pageNumber}");			

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("ExtraordinaryBuy", "GetPageFromExtraordinaryBuy.sql");
			if (query is null)
			{
				return null;
			}

			List<ExtraordinaryBuyModel> result = new List<ExtraordinaryBuyModel>();

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
								ExtraordinaryBuyModel newItem = new ExtraordinaryBuyModel();

								cancellationToken.ThrowIfCancellationRequested();
								
								newItem.SecCode = sdr.GetString("seccode");

								newItem.Volume = sdr.GetInt32("volume");

								int checkForNull = sdr.GetOrdinal("description");
								if (!sdr.IsDBNull(checkForNull))
								{
									newItem.Description = sdr.GetString("description");
								}

								result.Add(newItem);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlExtraordinaryBuyRepository " +
							$"GetPageFromExtraordinaryBuy Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return result;
		}

		public async Task<string> CreateNew(CancellationToken cancellationToken, string seccode, int volume, string ? description)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlExtraordinaryBuyRepository " +
						$"CreateNew start, newModel is\r\n" +
						$"{seccode} volume={volume} description='{description}'");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("ExtraordinaryBuy", "CreateNew.sql");
			if (query is null)
			{
				return "MySqlExtraordinaryBuyRepository Error! File with SQL script not found!";
			}
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlExtraordinaryBuyRepository " +
				$"CreateNew execute query \r\n{query}");

			using (MySqlConnection con = new MySqlConnection(_connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					//(@seccode, @volume, @description);
					cmd.Parameters.AddWithValue("@seccode", seccode);
					cmd.Parameters.AddWithValue("@volume", volume);
					if (description is not null)
					{
						cmd.Parameters.AddWithValue("@description", description);
					}
					else
					{
						cmd.Parameters.AddWithValue("@description", null);
					}

					try
					{
						await con.OpenAsync(cancellationToken);

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
						_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlExtraordinaryBuyRepository " +
							$"CreateNew execution affected {insertResult} lines");

						return insertResult.ToString();
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlExtraordinaryBuyRepository " +
							$"CreateNew Exception!\r\n{ex.Message}");
						return ex.Message;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		public async Task<string> Delete(CancellationToken cancellationToken, string seccode)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlExtraordinaryBuyRepository " +
				$"Delete seccode={seccode} start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("ExtraordinaryBuy", "Delete.sql");

			if (query is null)
			{
				return "MySqlExtraordinaryBuyRepository Error! File with SQL script not found";
			}

			query = query.Replace("@seccode", seccode);
			return await _commonRepo.ExecuteNonQueryAsyncByQueryText(cancellationToken, query);
		}

		public async Task<string> Edit(CancellationToken cancellationToken, string seccode, int volume, string ? description)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlExtraordinaryBuyRepository " +
							$"Edit start, newModel is {seccode} volume={volume} description='{description}'");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("ExtraordinaryBuy", "Edit.sql");
			if (query is null)
			{
				return "MySqlExtraordinaryBuyRepository Error! File with SQL script not found";
			}

			using (MySqlConnection con = new MySqlConnection(_connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					//(@seccode, @volume, @description);
					cmd.Parameters.AddWithValue("@seccode", seccode);
					cmd.Parameters.AddWithValue("@volume", volume);
					if (description is not null)
					{
						cmd.Parameters.AddWithValue("@description", description);
					}
					else
					{
						cmd.Parameters.AddWithValue("@description", null);
					}

					try
					{
						await con.OpenAsync(cancellationToken);

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
						_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlExtraordinaryBuyRepository " +
							$"Edit execution affected {insertResult} lines");

						return insertResult.ToString();

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlExtraordinaryBuyRepository " +
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
	}
}
