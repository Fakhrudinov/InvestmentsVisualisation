using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MySqlConnector;
using System.Text;

namespace DataBaseRepository
{
    public class CommonRepository : IMySqlCommonRepository
    {
        private ILogger<CommonRepository> _logger;
        private readonly string _connectionString;
        private readonly string _connectionDBName;

        public CommonRepository(IOptions<DataBaseConnectionSettings> connection, ILogger<CommonRepository> logger)
        {
            _logger = logger;

            _connectionString = $"" +
                $"Server={connection.Value.Server};" +
                $"User ID={connection.Value.UserId};" +
                $"Password={connection.Value.Password};" +
                $"Port={connection.Value.Port};" +
                $"Database={connection.Value.Database}";
            _connectionDBName = connection.Value.Database;
        }

        public async Task<int> GetTableCountBySqlQuery(CancellationToken cancellationToken, string query)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                $"GetTableCountBySqlQuery start with \r\n{query}");

            int result = 0;

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    try
                    {
                        await con.OpenAsync(cancellationToken);
                        result = (int)(long)await cmd.ExecuteScalarAsync(cancellationToken);
                        _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                            $"GetTableCountBySqlQuery result tableCount={result}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                            $"GetTableCountBySqlQuery Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;
        }

        public void FillStaticCategories()
        {

			string? query = GetQueryTextByFolderAndFilename("CommonRepository", "GetAllFromCategory.sql");
			if (query is not null)
			{
			    using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;

                        try
                        {
                            con.Open();
                            using (MySqlDataReader sdr = cmd.ExecuteReader())
                            {
                                while (sdr.Read())
                                {
                                    IncomingMoneyCategory newCategory = new IncomingMoneyCategory();

                                    newCategory.Id = sdr.GetInt32(0);
                                    newCategory.Name = sdr.GetString(1);

                                    StaticData.Categories.Add(newCategory);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                                $"FillStaticCategories Exception!\r\n{ex.Message}");
                        }
                        finally
                        {
                            con.Close();
                        }
                    }
                }
            }
        }

        public void FillStaticSecBoards()
        {
			string? query = GetQueryTextByFolderAndFilename("CommonRepository", "GetAllFromSecboard.sql");
			if (query is not null)
			{
				using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        try
                        {
                            con.Open();
                            using (MySqlDataReader sdr = cmd.ExecuteReader())
                            {
                                while (sdr.Read())
                                {
                                    SecBoardCategory newSecBoard = new SecBoardCategory();

                                    newSecBoard.Id = sdr.GetInt32(0);
                                    newSecBoard.Name = sdr.GetString(1);

                                    StaticData.SecBoards.Add(newSecBoard);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                                $"FillStaticSecBoards Exception!\r\n{ex.Message}");
                        }
                        finally
                        {
                            con.Close();
                        }
                    }
                }
            }
        }

        public void FillStaticSecCodes()
        {
			string? query = GetQueryTextByFolderAndFilename("CommonRepository", "GetActualSecCodeAndSecBoard.sql");
			if (query is not null)
			{
				using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        try
                        {
                            con.Open();

                            using (MySqlDataReader sdr = cmd.ExecuteReader())
                            {
                                while (sdr.Read())
                                {
                                    StaticSecCode newSecBoard = new StaticSecCode();

                                    newSecBoard.SecBoard = sdr.GetInt32("secboard");
                                    newSecBoard.SecCode = sdr.GetString("seccode");

                                    StaticData.SecCodes.Add(newSecBoard);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                                $"FillStaticSecCodes Exception!\r\n{ex.Message}");
                        }
                        finally
                        {
                            con.Close();
                        }
                    }
                }
            }
        }

        public async Task<string> ExecuteNonQueryAsyncByQueryText(CancellationToken cancellationToken, string query)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                $"ExecuteNonQueryAsyncByQueryText start execute query \r\n{query}");

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        //Return Int32 Number of rows affected
                        int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
                        _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                            $"ExecuteNonQueryAsyncByQueryText execution affected {insertResult} lines");

                        return insertResult.ToString();

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                            $"ExecuteNonQueryAsyncByQueryText Exception!\r\n{ex.Message}");
                        return ex.Message;
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }
        }

        public void FillFreeMoney()
        {
			string? query = GetQueryTextByFolderAndFilename("Money", "GetMoneyValueFromLastYearMonth.sql");
			if (query is not null)
			{
				query = query.Replace("@data_base", _connectionDBName);

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query))
                    {
                        cmd.Connection = con;
                        try
                        {
                            con.Open();

                            var money = cmd.ExecuteScalar();
                            StaticData.FreeMoney = money.ToString();
                            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                                $"FillFreeMoney result money={money}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
                                $"FillFreeMoney Exception!\r\n{ex.Message}");
                        }
                        finally
                        {
                            con.Close();
                        }
                    }
                }
            }
        }

		public string? GetQueryTextByFolderAndFilename(string folderName, string queryFileName)
		{
			string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", folderName, queryFileName);
			if (!File.Exists(filePath))
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository Error! " +
					$"File with SQL script not found at " + filePath);
				return null;
			}

			string query = File.ReadAllText(filePath);
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} CommonRepository " +
				$"GetQueryTextByFolderAndFilename query {queryFileName} text is:\r\n{query}");

            return query;
		}

		public string GetQueryParamsFromListOfStrigs(List<string> listOfSeccodes)
		{
			StringBuilder sb = new StringBuilder();
            foreach (string seccode in listOfSeccodes)
            {
                sb.Append(", '" + seccode + "'");
            }

            sb.Remove(0,2);

            return sb.ToString();
		}
	}
}
