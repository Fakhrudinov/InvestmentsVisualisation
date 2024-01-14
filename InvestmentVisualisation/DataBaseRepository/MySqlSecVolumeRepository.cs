using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.SecVolume;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Text;


namespace DataBaseRepository
{
    public class MySqlSecVolumeRepository : IMySqlSecVolumeRepository
    {
        private ILogger<MySqlSecVolumeRepository> _logger;
        private readonly string _connectionString;
        private IMySqlCommonRepository _commonRepo;

        public MySqlSecVolumeRepository(
            IOptions<DataBaseConnectionSettings> connection,
            ILogger<MySqlSecVolumeRepository> logger,
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

            if (StaticData.FreeMoney is null)
            {
                _commonRepo.FillFreeMoney();
            }
        }

        public async Task<int> GetSecVolumeCountForYear(CancellationToken cancellationToken, int year)
        {
            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "SecVolume", 
                "GetSecVolumeCountForYear.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return 0;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                query = query.Replace("@name", "pieces_" + year);

                return await _commonRepo.GetTableCountBySqlQuery(query);
            }
        }

        public async Task<List<SecVolumeModel>> GetSecVolumePageForYear(
            CancellationToken cancellationToken, 
            int itemsAtPage, 
            int pageNumber, 
            int year)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                $"GetSecVolumePageForYear start with itemsAtPage={itemsAtPage} page={pageNumber} year={year}");

            List<SecVolumeModel> result = new List<SecVolumeModel>();

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "SecVolume", 
                "GetSecVolumePageForYear.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                query = query.Replace("@year", year.ToString());

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                    $"GetSecVolumePageForYear execute query \r\n{query}");

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
                                    SecVolumeModel model = new SecVolumeModel();

                                    model.SecCode = sdr.GetString("seccode");
                                    model.SecBoard = sdr.GetInt32("secboard");
                                    model.Pieces = sdr.GetInt32("pieces_" + year);
                                    model.AvPrice = sdr.GetDecimal("av_price_" + year);
                                    model.Volume = sdr.GetDecimal("volume_" + year);

                                    result.Add(model);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                                $"GetSecVolumePageForYear Exception!\r\n{ex.Message}");
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

        public async Task RecalculateSecVolumeForYear(CancellationToken cancellationToken, int year)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                $"RecalculateSecVolumeForYear {year} start");

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "SecVolume", 
                "RecalculateSecVolumeForYear.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository Error! " +
                    $"File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                    $"RecalculateSecVolumeForYear {year} execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        cmd.Parameters.AddWithValue("@year", year);

                        try
                        {
                            await con.OpenAsync(cancellationToken);
                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                                $"RecalculateSecVolumeForYear executed");

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                                $"RecalculateSecVolumeForYear Exception!\r\n{ex.Message}");
                        }
                        finally
                        {
                            await con.CloseAsync();
                        }
                    }
                }
            }
        }

        public async Task<List<SecVolumeLast2YearsDynamicModel>> GetSecVolumeLast3YearsDynamic(
            CancellationToken cancellationToken, 
            int year)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                $"GetSecVolumeLast3YearsDynamic start with year={year}");

            return await GetSecVolumeLast3YearsDynamicByQueryName(
                cancellationToken, 
                "GetSecVolumeLast3YearsDynamic.sql", 
                year);
        }
        public async Task<List<SecVolumeLast2YearsDynamicModel>> GetSecVolumeLast3YearsDynamicSortedByVolume(
            CancellationToken cancellationToken, 
            int year)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                $"GetSecVolumeLast3YearsDynamicSortedByVolume start with year={year}");

            return await GetSecVolumeLast3YearsDynamicByQueryName(
                cancellationToken, 
                "GetSecVolumeLast3YearsDynamicSortedByVolume.sql", 
                year);
        }
        private async Task<List<SecVolumeLast2YearsDynamicModel>> GetSecVolumeLast3YearsDynamicByQueryName(
            CancellationToken cancellationToken,
            string queryName, 
            int year)
        {
            List<SecVolumeLast2YearsDynamicModel> result = new List<SecVolumeLast2YearsDynamicModel>();

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "SqlQueries", 
                "SecVolume",
                queryName);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return result;
            }
            else
            {
                StringBuilder queryStringBuilder = new StringBuilder(File.ReadAllText(filePath));
                queryStringBuilder.Replace("@year", year.ToString());
                queryStringBuilder.Replace("@prev_year", (year - 1).ToString());
                queryStringBuilder.Replace("@prev_prev_year", (year - 2).ToString());
                string query = queryStringBuilder.ToString();

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                    $"GetSecVolumeLast3YearsDynamicByQueryName execute query \r\n{query}");

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
                                    SecVolumeLast2YearsDynamicModel model = new SecVolumeLast2YearsDynamicModel();

                                    model.SecCode = sdr.GetString("seccode");
                                    model.Name = sdr.GetString("name");

                                    int checkForNull1 = sdr.GetOrdinal("pieces_" + (year - 2));
                                    if (!sdr.IsDBNull(checkForNull1))
                                    {
                                        model.PreviousPreviousYearPieces = sdr.GetInt32("pieces_" + (year - 2));
                                    }

                                    int checkForNull2 = sdr.GetOrdinal("pieces_" + (year - 1));
                                    if (!sdr.IsDBNull(checkForNull2))
                                    {
                                        model.PreviousYearPieces = sdr.GetInt32("pieces_" + (year - 1));
                                    }

                                    model.LastYearPieces = sdr.GetInt32("pieces_" + year);
                                    model.LastYearVolume = sdr.GetDecimal("volume_" + year);

                                    result.Add(model);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                                $"GetSecVolumeLast3YearsDynamicByQueryName Exception!\r\n{ex.Message}");
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

		public async Task<List<ChartItemModel>> GetVolumeChartData(CancellationToken cancellationToken)
		{
			int year = DateTime.Now.Year;
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                $"GetVolumeChartData with year = {year} ");

			List<ChartItemModel> result = new List<ChartItemModel>();

			string filePath = Path.Combine(
				Directory.GetCurrentDirectory(),
				"SqlQueries",
				"SecVolume",
				"GetSecVolumeChartData.sql");
			if (!File.Exists(filePath))
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} GetVolumeChartData Error! " +
					$"File with SQL script not found at " + filePath);
				return result;
			}
			else
			{
				string query = File.ReadAllText(filePath);
				query = query.Replace("@year", year.ToString());

				_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
                    $"GetVolumeChartData execute query GetSecVolumeChartData.sql\r\n{query}");

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
									ChartItemModel model = new ChartItemModel
                                        (
                                            sdr.GetString("NAME"),
										    sdr.GetDecimal("VOLUME")
										);
									result.Add(model);
								}
							}
						}
						catch (Exception ex)
						{
							_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlSecVolumeRepository " +
								$"GetVolumeChartData Exception!\r\n{ex.Message}");
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
}
