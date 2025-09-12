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

        public async Task<List<SecCodeInfo>> GetPageFromSecCodes(
            CancellationToken cancellationToken, 
            int itemsAtPage, 
            int offset)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                $"GetPageFromSecCodes start with itemsAtPage={itemsAtPage} offset={offset}");            

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("SecCodes", "GetPageFromSecCodes.sql");
			if (query is null)
			{
				List<SecCodeInfo> result = new List<SecCodeInfo>();
				return result;
			}
			return await GetPageFromSecCodesByQuery(cancellationToken, itemsAtPage, offset, query);
		}

		public async Task<List<SecCodeInfo>> GetPageFromOnlyActiveSecCodes(
            CancellationToken cancellationToken, 
            int itemsAtPage, 
            int offset)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
				$"GetPageFromOnlyActiveSecCodes start with itemsAtPage={itemsAtPage} offset={offset}");			

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("SecCodes", "GetPageFromOnlyActiveSecCodes.sql");
			if (query is null)
			{
				List<SecCodeInfo> result = new List<SecCodeInfo>();
				return result;
			}

            return await GetPageFromSecCodesByQuery(cancellationToken, itemsAtPage, offset, query);
		}

		private async Task<List<SecCodeInfo>> GetPageFromSecCodesByQuery(
            CancellationToken cancellationToken, 
            int itemsAtPage, 
            int offset, 
            string query)
		{
			List<SecCodeInfo> result = new List<SecCodeInfo>();
			using (MySqlConnection con = new MySqlConnection(_connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@lines_count", itemsAtPage);
					cmd.Parameters.AddWithValue("@offset", offset);

					try
					{
						await con.OpenAsync(cancellationToken);

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
						{
							while (await sdr.ReadAsync(cancellationToken))
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

								int checkForNull2 = sdr.GetOrdinal("payments_per_year");
								if (!sdr.IsDBNull(checkForNull2))
								{
									secCode.PaysPerYear = sdr.GetInt16("payments_per_year");
								}

								result.Add(secCode);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
							$"GetPageFromSecCodes Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return result;
		}

		public async Task<int> GetSecCodesCount(CancellationToken cancellationToken)
        {
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("SecCodes", "GetSecCodesCount.sql");
			if (query is null)
			{
				return 0;
			}

			return await _commonRepo.GetTableCountBySqlQuery(cancellationToken, query);
		}
		public async Task<int> GetOnlyActiveSecCodesCount(CancellationToken cancellationToken)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("SecCodes", "GetOnlyActiveSecCodesCount.sql");
			if (query is null)
			{
				return 0;
			}

			return await _commonRepo.GetTableCountBySqlQuery(cancellationToken, query);
		}

		public async Task<string> CreateNewSecCode(CancellationToken cancellationToken, SecCodeInfo model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                $"CreateNewSecCode start, newModel is\r\n{model.SecCode} {model.Name} SecBoard={model.SecBoard} " +
                $"ISIN={model.ISIN} {model.FullName} ExpiredDate={model.ExpiredDate}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("SecCodes", "CreateNewSecCode.sql");
			if (query is null)
			{
				return "MySqlSecCodesRepository Error! File with SQL script not found!";
			}

            if (model.ExpiredDate is null)
            {
                query = query.Replace(", `expired_date`", "");
                query = query.Replace(", @expired_date", "");
            }

			if (model.PaysPerYear is null)
			{
				query = query.Replace(", `payments_per_year`", "");
				query = query.Replace(", @payments_per_year", "");
			}

			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                $"CreateNewSecCode execute query \r\n{query}");

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

					if (model.PaysPerYear is not null)
					{
						cmd.Parameters.AddWithValue("@payments_per_year", model.PaysPerYear);
					}

					try
                    {
                        await con.OpenAsync(cancellationToken);

                        //Return Int32 Number of rows affected
                        int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
                        _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                            $"CreateNewSecCode execution affected {insertResult} lines");

                        return insertResult.ToString();

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                            $"CreateNewSecCode Exception!\r\n{ex.Message}");
                        return ex.Message;
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }
			//INSERT INTO `seccode_info`
			//  (`seccode`, `secboard`, `name`, `full_name`, `isin`, `expired_date`, `payments_per_year`)
			//VALUES
			//  (@seccode, @secboard, @name, @full_name, @isin, @expired_date, @payments_per_year);
		}

		public async Task<string> EditSingleSecCode(CancellationToken cancellationToken, SecCodeInfo model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                $"EditSingleSecCode start, newModel is\r\n{model.SecCode} {model.Name} SecBoard={model.SecBoard} " +
                $"ISIN={model.ISIN} {model.FullName} ExpiredDate={model.ExpiredDate}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("SecCodes", "EditSingleSecCode.sql");
			if (query is null)
			{
				return "MySqlSecCodesRepository Error! File with SQL script not found!";
			}

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

					if (model.PaysPerYear is not null)
					{
						cmd.Parameters.AddWithValue("@payments_per_year", model.PaysPerYear);
					}
					else
					{
						cmd.Parameters.AddWithValue("@payments_per_year", null);
					}

					try
                    {
                        await con.OpenAsync(cancellationToken);

                        //Return Int32 Number of rows affected
                        int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
                        _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                            $"EditSingleSecCode execution affected {insertResult} lines");

                        return insertResult.ToString();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                            $"EditSingleSecCode Exception!\r\n{ex.Message}");
                        return ex.Message;
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }
        }

        public async Task<SecCodeInfo> GetSingleSecCodeBySecCode(CancellationToken cancellationToken, string secCode)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                $"GetSingleSecCodeBySecCode={secCode} start");

            SecCodeInfo result = new SecCodeInfo();

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("SecCodes", "GetSingleSecCodeBySecCode.sql");
			if (query is null)
			{
				return result;
			}

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;
                    cmd.Parameters.AddWithValue("@secCode", secCode);

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            while (await sdr.ReadAsync(cancellationToken))
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

								int checkForNull2 = sdr.GetOrdinal("payments_per_year");
								if (!sdr.IsDBNull(checkForNull2))
								{
									result.PaysPerYear = sdr.GetInt16("payments_per_year");
								}
							}
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                            $"GetSingleSecCodeBySecCode={secCode} Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;
        }

        public async Task<string> GetSecCodeByISIN(CancellationToken cancellationToken, string isin)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                $"GetSecCodeByISIN start with isin={isin}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("SecCodes", "GetSecCodeByISIN.sql");
			if (query is null)
			{
				return "MySqlSecCodesRepository Error! File with SQL script not found!";
			}

			using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;
                    cmd.Parameters.AddWithValue("@isin", isin);

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        return (string)await cmd.ExecuteScalarAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecCodesRepository " +
                            $"GetSecCodeByISIN Exception!\r\n{ex.Message}");
                        return $"GetPageFromIncoming Exception! {ex.Message}";
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }
        }

        public void RenewStaticSecCodesList(CancellationToken cancellationToken)
        {
            // сначала очистим List
            StaticData.SecCodes.Clear();
            // и наполним снова
            _commonRepo.FillStaticSecCodes();
        }
	}
}
