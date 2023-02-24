using DataAbstraction.Interfaces;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models.YearView;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace DataBaseRepository
{
    public class MySqlYearViewRepository : IMySqlYearViewRepository
    {
        private ILogger<MySqlYearViewRepository> _logger;
        private readonly string _connectionString;

        public MySqlYearViewRepository(
            IOptions<DataBaseConnectionSettings> connection,
            ILogger<MySqlYearViewRepository> logger)
        {
            _logger = logger;
            _connectionString = $"" +
                $"Server={connection.Value.Server};" +
                $"User ID={connection.Value.UserId};" +
                $"Password={connection.Value.Password};" +
                $"Port={connection.Value.Port};" +
                $"Database={connection.Value.Database}";
        }

        public async Task<List<YearViewModel>> GetYearViewPage()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository GetYearViewPage start");
            
            List<YearViewModel> result = new List<YearViewModel>();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "YearView", "GetYearViewPage.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository Error! File with SQL script not found at " + filePath);
                return result;
            }
            else
            {
                string query = File.ReadAllText(filePath);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository GetYearViewPage execute query \r\n{query}");

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
                                    YearViewModel newLine = new YearViewModel();

                                    newLine.SecCode = sdr.GetString("seccode");
                                    newLine.Name = sdr.GetString("name");

                                    int checkForNull1 = sdr.GetOrdinal("isin");
                                    if (!sdr.IsDBNull(checkForNull1))
                                    {
                                        newLine.ISIN = sdr.GetString("isin");
                                    }

                                    int checkForNull2 = sdr.GetOrdinal("pieces");
                                    if (!sdr.IsDBNull(checkForNull2))
                                    {
                                        newLine.Pieces = sdr.GetInt32("pieces");
                                    }

                                    int checkForNull3 = sdr.GetOrdinal("av_price");
                                    if (!sdr.IsDBNull(checkForNull3))
                                    {
                                        newLine.AvPrice = sdr.GetDecimal("av_price");
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
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository GetYearViewPage Exception!" +
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

        public async Task RecalculateYearView(int year)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository RecalculateYearView {year} start");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "YearView", "RecalculateYearView.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository Error! File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);

                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository RecalculateYearView {year} execute query \r\n{query}");

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        cmd.Parameters.AddWithValue("@year", year);

                        try
                        {
                            await con.OpenAsync();
                            await cmd.ExecuteNonQueryAsync();
                            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository RecalculateYearView executed");

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlYearViewRepository RecalculateYearView Exception!" +
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
    }
}
