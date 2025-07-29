using DataAbstraction.Interfaces;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models.YearView;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Threading;

namespace DataBaseRepository
{
    public class MySqlYearViewRepository : IMySqlYearViewRepository
    {
        private ILogger<MySqlYearViewRepository> _logger;
        private readonly string _connectionString;
		private IMySqlCommonRepository _commonRepo;

		public MySqlYearViewRepository(
            IOptions<DataBaseConnectionSettings> connection,
            ILogger<MySqlYearViewRepository> logger,
			IMySqlCommonRepository commonRepo)
        {
            _logger = logger;
            _connectionString = $"" +
                $"Server={connection.Value.Server};" +
                $"User ID={connection.Value.UserId};" +
                $"Password={connection.Value.Password};" +
                $"Port={connection.Value.Port};" +
                $"Database={connection.Value.Database}";
            _commonRepo = commonRepo;
        }

        public async Task<List<YearViewModel>> GetYearViewPage(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository GetYearViewPage start");
            
            List<YearViewModel> result = new List<YearViewModel>();

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("YearView", "GetYearViewPage.sql");
			if (query is null)
			{
				return result;
			}

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
                                YearViewModel newLine = new YearViewModel();

                                newLine.SecCode = sdr.GetString("seccode");
                                newLine.Name = sdr.GetString("name");

                                int checkForNull1 = sdr.GetOrdinal("isin");
                                if (!sdr.IsDBNull(checkForNull1))
                                {
                                    newLine.ISIN = sdr.GetString("isin");
                                }

                                newLine.Volume = sdr.GetDecimal("volume");

                                int checkForNull4 = sdr.GetOrdinal("jan");
                                if (!sdr.IsDBNull(checkForNull4))
                                {
                                    newLine.Jan = sdr.GetDecimal("jan");
                                }

                                int checkForNull5 = sdr.GetOrdinal("feb");
                                if (!sdr.IsDBNull(checkForNull5))
                                {
                                    newLine.Feb = sdr.GetDecimal("feb");
                                }

                                int checkForNull6 = sdr.GetOrdinal("mar");
                                if (!sdr.IsDBNull(checkForNull6))
                                {
                                    newLine.Mar = sdr.GetDecimal("mar");
                                }

                                int checkForNull7 = sdr.GetOrdinal("apr");
                                if (!sdr.IsDBNull(checkForNull7))
                                {
                                    newLine.Apr = sdr.GetDecimal("apr");
                                }


                                int checkForNull8 = sdr.GetOrdinal("may");
                                if (!sdr.IsDBNull(checkForNull8))
                                {
                                    newLine.May = sdr.GetDecimal("may");
                                }

                                int checkForNull9 = sdr.GetOrdinal("jun");
                                if (!sdr.IsDBNull(checkForNull9))
                                {
                                    newLine.Jun = sdr.GetDecimal("jun");
                                }

                                int checkForNull10 = sdr.GetOrdinal("jul");
                                if (!sdr.IsDBNull(checkForNull10))
                                {
                                    newLine.Jul = sdr.GetDecimal("jul");
                                }

                                int checkForNull11 = sdr.GetOrdinal("aug");
                                if (!sdr.IsDBNull(checkForNull11))
                                {
                                    newLine.Aug = sdr.GetDecimal("aug");
                                }

                                int checkForNull12 = sdr.GetOrdinal("sep");
                                if (!sdr.IsDBNull(checkForNull12))
                                {
                                    newLine.Sep = sdr.GetDecimal("sep");
                                }

                                int checkForNull13 = sdr.GetOrdinal("okt");
                                if (!sdr.IsDBNull(checkForNull13))
                                {
                                    newLine.Okt = sdr.GetDecimal("okt");
                                }

                                int checkForNull14 = sdr.GetOrdinal("nov");
                                if (!sdr.IsDBNull(checkForNull14))
                                {
                                    newLine.Nov = sdr.GetDecimal("nov");
                                }

                                int checkForNull15 = sdr.GetOrdinal("dec");
                                if (!sdr.IsDBNull(checkForNull15))
                                {
                                    newLine.Dec = sdr.GetDecimal("dec");
                                }


                                int checkForNull16 = sdr.GetOrdinal("summ");
                                if (!sdr.IsDBNull(checkForNull16))
                                {
                                    newLine.Summ = sdr.GetDecimal("summ");
                                }

                                int checkForNull17 = sdr.GetOrdinal("vol%");
                                if (!sdr.IsDBNull(checkForNull17))
                                {
                                    newLine.VolPercent = sdr.GetDecimal("vol%");
                                }

                                int checkForNull18 = sdr.GetOrdinal("%");
                                if (!sdr.IsDBNull(checkForNull18))
                                {
                                    newLine.DivPercent = sdr.GetDecimal("%");
                                }

                                newLine.FullName = sdr.GetString("full_name");

                                result.Add(newLine);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository GetYearViewPage " +
                            $"Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;
        }

        public async Task RecalculateYearView(CancellationToken cancellationToken, int year, bool sortedByVolume)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository RecalculateYearView " +
                $"year={year} sortedByVolume={sortedByVolume} start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("YearView", "RecalculateYearView.sql");
			if (query is not null)
			{
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        cmd.Parameters.AddWithValue("@year", year);
                        cmd.Parameters.AddWithValue("@sortedByVolume", sortedByVolume);

                        try
                        {
                            await con.OpenAsync(cancellationToken);
                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
                                $"RecalculateYearView executed");

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
                                $"RecalculateYearView Exception!\r\n{ex.Message}");
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }
            }
        }

        public async Task CallFillViewShowLast12Month(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
                $"CallFillViewShowLast12Month start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("YearView", "CallFillViewShowLast12Month.sql");
			if (query is not null)
			{
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        try
                        {
                            await con.OpenAsync(cancellationToken);
                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
                                $"CallFillViewShowLast12Month executed");

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
                                $"CallFillViewShowLast12Month Exception!\r\n{ex.Message}");
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }
            }
        }

        public async Task DropTableLast12MonthView(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
                $"DropTableLast12MonthView start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("YearView", "DropTableLast12MonthView.sql");
			if (query is not null)
			{
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        try
                        {
                            await con.OpenAsync(cancellationToken);
                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
                                $"DropTableLast12MonthView executed");

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
                                $"DropTableLast12MonthView Exception!\r\n{ex.Message}");
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }
            }
        }

        public async Task<List<YearViewModel>> GetLast12MonthViewPage(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
                $"GetLast12MonthViewPage start");

            List<YearViewModel> result = new List<YearViewModel>();

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("YearView", "GetLast12MonthViewPage.sql");
			if (query is null)
			{
				return result;
			}

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
                                YearViewModel newLine = new YearViewModel();

                                newLine.SecCode = sdr.GetString("seccode");
                                newLine.Name = sdr.GetString("name");

                                newLine.Volume = sdr.GetDecimal("volume");

                                int checkForNull4 = sdr.GetOrdinal("m-11");
                                if (!sdr.IsDBNull(checkForNull4))
                                {
                                    newLine.Jan = sdr.GetDecimal("m-11");
                                }

                                int checkForNull5 = sdr.GetOrdinal("m-10");
                                if (!sdr.IsDBNull(checkForNull5))
                                {
                                    newLine.Feb = sdr.GetDecimal("m-10");
                                }

                                int checkForNull6 = sdr.GetOrdinal("m-09");
                                if (!sdr.IsDBNull(checkForNull6))
                                {
                                    newLine.Mar = sdr.GetDecimal("m-09");
                                }

                                int checkForNull7 = sdr.GetOrdinal("m-08");
                                if (!sdr.IsDBNull(checkForNull7))
                                {
                                    newLine.Apr = sdr.GetDecimal("m-08");
                                }

                                int checkForNull8 = sdr.GetOrdinal("m-07");
                                if (!sdr.IsDBNull(checkForNull8))
                                {
                                    newLine.May = sdr.GetDecimal("m-07");
                                }

                                int checkForNull9 = sdr.GetOrdinal("m-06");
                                if (!sdr.IsDBNull(checkForNull9))
                                {
                                    newLine.Jun = sdr.GetDecimal("m-06");
                                }

                                int checkForNull10 = sdr.GetOrdinal("m-05");
                                if (!sdr.IsDBNull(checkForNull10))
                                {
                                    newLine.Jul = sdr.GetDecimal("m-05");
                                }

                                int checkForNull11 = sdr.GetOrdinal("m-04");
                                if (!sdr.IsDBNull(checkForNull11))
                                {
                                    newLine.Aug = sdr.GetDecimal("m-04");
                                }

                                int checkForNull12 = sdr.GetOrdinal("m-03");
                                if (!sdr.IsDBNull(checkForNull12))
                                {
                                    newLine.Sep = sdr.GetDecimal("m-03");
                                }

                                int checkForNull13 = sdr.GetOrdinal("m-02");
                                if (!sdr.IsDBNull(checkForNull13))
                                {
                                    newLine.Okt = sdr.GetDecimal("m-02");
                                }

                                int checkForNull14 = sdr.GetOrdinal("m-01");
                                if (!sdr.IsDBNull(checkForNull14))
                                {
                                    newLine.Nov = sdr.GetDecimal("m-01");
                                }

                                int checkForNull15 = sdr.GetOrdinal("m-00");
                                if (!sdr.IsDBNull(checkForNull15))
                                {
                                    newLine.Dec = sdr.GetDecimal("m-00");
                                }

                                newLine.FullName = sdr.GetString("full_name");

                                result.Add(newLine);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
                            $"GetYearViewPage Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;
        }

		public async Task<List<SecCodeAndDividentAndDateModel>?> GetBondsDividendsForLastYear(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
				$"GetBondsDividendsForLastYear start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("YearView", "GetBondsDividendsForLastYear.sql");
			if (query is null)
			{
				return null;
			}

			//int year = DateTime.Now.Year;
			//query = query.Replace("@year", year.ToString());

			List<SecCodeAndDividentAndDateModel> result = new List<SecCodeAndDividentAndDateModel>();

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
								SecCodeAndDividentAndDateModel newLine = new SecCodeAndDividentAndDateModel();

								newLine.SecCode = sdr.GetString("seccode");
								newLine.EventDate = sdr.GetDateTime("event_date");
								newLine.Divident = sdr.GetDecimal("value").ToString();

								result.Add(newLine);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
							$"GetBondsDividendsForLastYear Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return result;
		}

		public async Task<List<NameAndPiecesAndValueModel>?> GetBondsWithNameAndValues(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
				$"GetBondsWithNameAndValues start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("YearView", "GetBondsWithNameAndValues.sql");
			if (query is null)
			{
				return null;
			}

            int year = DateTime.Now.Year;
            query = query.Replace("@year", year.ToString());

			List<NameAndPiecesAndValueModel> result = new List<NameAndPiecesAndValueModel>();

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
								NameAndPiecesAndValueModel newLine = new NameAndPiecesAndValueModel();

								newLine.SecCode = sdr.GetString("seccode");
								newLine.Name = sdr.GetString("name");
								newLine.FullName = sdr.GetString("full_name");
								newLine.Pieces = sdr.GetInt32("pieces");
								newLine.Volume = sdr.GetDecimal("volume");

								result.Add(newLine);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository " +
							$"GetBondsWithNameAndValues Exception!\r\n{ex.Message}");
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
}
