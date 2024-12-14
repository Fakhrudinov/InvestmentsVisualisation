using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Deals;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Text;
using UserInputService;

namespace DataBaseRepository
{
    public class MySqlDealsRepository : IMySqlDealsRepository
    {
        private ILogger<MySqlDealsRepository> _logger;
        private readonly string _connectionString;
        private IMySqlCommonRepository _commonRepo;
        private InputHelper _helper;

        public MySqlDealsRepository(
            IOptions<DataBaseConnectionSettings> connection, 
            ILogger<MySqlDealsRepository> logger,
            IMySqlCommonRepository commonRepo,
            InputHelper helper)
        {
            _logger = logger;
            _commonRepo=commonRepo;
            _helper=helper;

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

        public async Task<int> GetDealsCount(CancellationToken cancellationToken)
        {
            string? query = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "GetDealsCount.sql");
            if (query is null)
            {
				return 0;
			}
			return await _commonRepo.GetTableCountBySqlQuery(cancellationToken, query);
		}

        public async Task<List<DealModel>> GetPageFromDeals(
            CancellationToken cancellationToken, 
            int itemsAtPage, 
            int pageNumber)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository GetPageFromDeals start " +
                $"with itemsAtPage={itemsAtPage} page={pageNumber}");

            List<DealModel> result = new List<DealModel>();

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "GetPageFromDeals.sql");
			if (query is null)
			{
				return result;
			}

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
                                DealModel newDeal = new DealModel();

                                cancellationToken.ThrowIfCancellationRequested();
                                newDeal.Id = sdr.GetInt32("id");
                                newDeal.Date = sdr.GetDateTime("date");
                                newDeal.SecCode = sdr.GetString("seccode");
                                newDeal.SecBoard = sdr.GetInt32("secboard");

                                newDeal.AvPrice = sdr.GetDecimal("av_price").ToString();
                                newDeal.Pieces = sdr.GetInt32("pieces");

                                int checkForNull = sdr.GetOrdinal("comission");
                                if (!sdr.IsDBNull(checkForNull))
                                {
                                    newDeal.Comission = sdr.GetDecimal("comission").ToString();
                                }

                                int checkForNullNkd = sdr.GetOrdinal("nkd");
                                if (!sdr.IsDBNull(checkForNullNkd))
                                {
                                    newDeal.NKD = sdr.GetDecimal("nkd").ToString();
                                }

                                result.Add(newDeal);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                            $"GetPageFromDeals Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;
        }

        public async Task<string> CreateNewDeal(CancellationToken cancellationToken, CreateDealsModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                $"CreateNewDeal start, newModel is\r\n" +
                $"{model.Date} {model.SecCode} SecBoard={model.SecBoard} AvPrice={model.AvPrice} Pieces={model.Pieces} " +
                $"Comission={model.Comission} NKD={model.NKD}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "CreateNewDeal.sql");
			if (query is null)
			{
				return "MySqlRepository Error! File with SQL script not found!"; 
			}

			if (model.Comission is null)
                {
                    query = query.Replace(", `comission`", "");
                    query = query.Replace(", @comission", "");
            }
            if (model.NKD is null)
            {
                query = query.Replace(", `nkd`", "");
                query = query.Replace(", @nkd", "");
            }

            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                $"CreateNewDeal execute query \r\n{query}");

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    //(@date_time, @seccode, @secboard, @av_price, @pieces, @comission, @nkd);
                    cmd.Parameters.AddWithValue("@date_time", model.Date);
                    cmd.Parameters.AddWithValue("@seccode", model.SecCode);
                    cmd.Parameters.AddWithValue("@secboard", model.SecBoard);
                    cmd.Parameters.AddWithValue("@av_price", model.AvPrice);
                    cmd.Parameters.AddWithValue("@pieces", model.Pieces);
                    if (model.Comission is not null)
                    {
                        cmd.Parameters.AddWithValue("@comission", model.Comission);
                    }
                    if (model.NKD is not null)
                    {
                        cmd.Parameters.AddWithValue("@nkd", model.NKD);
                    }

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        //Return Int32 Number of rows affected
                        var insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
                        _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                            $"CreateNewDeal execution affected {insertResult} lines");

                        return insertResult.ToString();

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                            $"CreateNewDeal Exception!\r\n{ex.Message}");
                        return ex.Message;
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }
            //}
            //INSERT INTO deals
            //  (`date`, `seccode`, `secboard`, `av_price`, `pieces`, `comission`, `nkd`) 
            //VALUES
            //  (@date_time, @seccode, @secboard, @av_price, @pieces, @comission, @nkd);
        }

        public async Task<string> EditSingleDeal(CancellationToken cancellationToken, DealModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                $"EditSingleDeal start, newModel is\r\n" +
                $"Id={model.Id} {model.Date} {model.SecCode} SecBoard={model.SecBoard} AvPrice={model.AvPrice} " +
                $"Pieces={model.Pieces} Comission={model.Comission} nkd={model.NKD}");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "EditSingleDeal.sql");
			if (query is null)
			{
				return "MySqlDealsRepository Error! File with SQL script not found";
			}

			using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    //(@date_time, @seccode, @secboard, @av_price, @pieces, @comission, @nkd);
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@date_time", model.Date);
                    cmd.Parameters.AddWithValue("@seccode", model.SecCode);
                    cmd.Parameters.AddWithValue("@secboard", model.SecBoard);
                    cmd.Parameters.AddWithValue("@av_price", model.AvPrice);
                    cmd.Parameters.AddWithValue("@pieces", model.Pieces);
                    if (model.Comission is not null)
                    {
                        cmd.Parameters.AddWithValue("@comission", model.Comission);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@comission", null);
                    }

                    if (model.NKD is not null)
                    {
                        cmd.Parameters.AddWithValue("@nkd", model.NKD);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@nkd", null);
                    }

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        //Return Int32 Number of rows affected
                        int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
                        _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                            $"EditSingleDeal execution affected {insertResult} lines");

                        return insertResult.ToString();

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                            $"EditSingleDeal Exception!\r\n{ex.Message}");
                        return ex.Message;
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }
        }

        public async Task<DealModel> GetSingleDealById(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                $"GetSingleDealById={id} start");

            DealModel result = new DealModel();
			
            string? query = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "GetSingleDealById.sql");
			if (query is null)
			{
				return result;
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
                                result.Id = sdr.GetInt32("id");
                                result.Date = sdr.GetDateTime("date");
                                result.SecCode = sdr.GetString("seccode");
                                result.SecBoard = sdr.GetInt32("secboard");
                                result.AvPrice= sdr.GetDecimal("av_price").ToString();
                                result.Pieces = sdr.GetInt32("pieces");

                                int checkForNull = sdr.GetOrdinal("comission");
                                if (!sdr.IsDBNull(checkForNull))
                                {
                                    result.Comission = sdr.GetDecimal("comission").ToString();
                                }

                                int checkForNull2 = sdr.GetOrdinal("nkd");
                                if (!sdr.IsDBNull(checkForNull2))
                                {
                                    result.NKD = sdr.GetDecimal("nkd").ToString();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                            $"GetSingleDealById={id} Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;
        }

        public async Task<string> DeleteSingleDeal(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                $"DeleteSingleDeal id={id} start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "DeleteSingleDeal.sql");
			if (query is null)
			{
				return "MySqlDealsRepository Error! File with SQL script not found";
			}

            query = query.Replace("@id", id.ToString());
            return await _commonRepo.ExecuteNonQueryAsyncByQueryText(cancellationToken, query);
        }

		public async Task<int> GetDealsSpecificSecCodeCount(CancellationToken cancellationToken, string secCode)
        {
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "GetDealsSpecificSecCodeCount.sql");
			if (query is null)
			{
				return 0;
			}

            query = query.Replace("@secCode", secCode);
            return await _commonRepo.GetTableCountBySqlQuery(cancellationToken, query);
        }

		public async Task<List<DealModel>> GetPageFromDealsSpecificSecCode(
            CancellationToken cancellationToken, 
            string secCode, int itemsAtPage, 
            int pageNumber)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                $"GetPageFromDealsSpecificSecCode start with secCode={secCode} itemsAtPage={itemsAtPage} page={pageNumber}");

            List<DealModel> result = new List<DealModel>();

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "GetPageFromDealsSpecificSecCode.sql");
			if (query is null)
			{
				return result;
			}

			using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    cmd.Parameters.AddWithValue("@lines_count", itemsAtPage);
                    cmd.Parameters.AddWithValue("@page_number", pageNumber);
                    cmd.Parameters.AddWithValue("@secCode", secCode);

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            while (await sdr.ReadAsync(cancellationToken))
                            {
                                DealModel newDeal = new DealModel();

                                newDeal.Id = sdr.GetInt32("id");
                                newDeal.Date = sdr.GetDateTime("date");
                                newDeal.SecCode = sdr.GetString("seccode");
                                newDeal.SecBoard = sdr.GetInt32("secboard");

                                newDeal.AvPrice = sdr.GetDecimal("av_price").ToString();
                                newDeal.Pieces = sdr.GetInt32("pieces");

                                int checkForNull = sdr.GetOrdinal("comission");
                                if (!sdr.IsDBNull(checkForNull))
                                {
                                    newDeal.Comission = sdr.GetDecimal("comission").ToString();
                                }

                                int checkForNullNkd = sdr.GetOrdinal("nkd");
                                if (!sdr.IsDBNull(checkForNullNkd))
                                {
                                    newDeal.NKD = sdr.GetDecimal("nkd").ToString();
                                }

                                result.Add(newDeal);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                            $"GetPageFromDealsSpecificSecCode Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;
        }

        public async Task<string> CreateNewDealsFromList(CancellationToken cancellationToken, List<IndexedDealModel> model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                $"CreateNewDealsFromList start");

            string query = GetSqlRequestForNewDeals(model.Count);
            if (! query.ToUpper().StartsWith("INSERT"))
            {
                return query;
            }

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query))
                {
                    cmd.Connection = con;

                    ////(@date_time, @seccode, @secboard, @av_price, @pieces, @comission, @nkd);
                    for (int i = 0; i < model.Count(); i++)
                    {
                        cmd.Parameters.AddWithValue("@date_time" + i, model[i].Date);
                        cmd.Parameters.AddWithValue("@seccode" + i, model[i].SecCode);
                        cmd.Parameters.AddWithValue("@secboard" + i, model[i].SecBoard);
                        cmd.Parameters.AddWithValue("@av_price" + i, _helper.CleanPossibleNumber(model[i].AvPrice));
                        cmd.Parameters.AddWithValue("@pieces" + i, model[i].Pieces);
                        
                        if (model[i].Comission is not null && !model[i].Comission.Equals(""))
                        {
                            cmd.Parameters.AddWithValue("@comission" + i, _helper.CleanPossibleNumber( model[i].Comission));
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@comission" + i, DBNull.Value);
                        }

                        if (model[i].NKD is not null && !model[i].NKD.Equals(""))
                        {
                            cmd.Parameters.AddWithValue("@nkd" + i, _helper.CleanPossibleNumber(model[i].NKD));
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@nkd" + i, DBNull.Value);
                        }
                    }

                    try
                    {
                        await con.OpenAsync(cancellationToken);

                        //Return Int32 Number of rows affected
                        int insertResult = await cmd.ExecuteNonQueryAsync(cancellationToken);
                        _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                            $"CreateNewDealsFromList execution affected {insertResult} lines");

                        return insertResult.ToString();

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                            $"CreateNewDealsFromList Exception!\r\n{ex.Message}");
                        return ex.Message;
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }
        }

        private string GetSqlRequestForNewDeals(int count)
        {
			/// INSERT INTO `deals` 
			///     (`date`, `seccode`, `secboard`, `av_price`, `pieces`, `comission`, `nkd`) 
			/// VALUES
			///     (
			///          @date_time, @seccode, @secboard, @av_price, @pieces, @comission, @nkd
			///                 ),(
			///          '2024-10-07 12:00:36', 'BSPB', '1', '365.0000', '20', null, '1.55'
			///     );
			string? queryStr = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "CreateNewDealsFromList.sql");
			if (queryStr is null)
			{
				return "MySqlRepository Error! File with SQL script not found";
			}

			StringBuilder query = new StringBuilder(queryStr);
            StringBuilder parameters = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                parameters.Append($"),\r\n(" +
                    $"@date_time{i}, @seccode{i}, @secboard{i}, @av_price{i}, @pieces{i}, @comission{i}, @nkd{i}");
            }
            parameters.Remove(0, 5);
            string parametersStr = parameters.ToString();

			query.Replace("@values", parametersStr);

            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlDealsRepository " +
                $"GetSqlRequestForNewDeals execute query CreateNewDealsFromList.sql\r\n{query}");

            return query.ToString();
        }
    }
}
