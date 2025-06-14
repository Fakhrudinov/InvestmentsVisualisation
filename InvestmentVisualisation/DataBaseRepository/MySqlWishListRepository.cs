using DataAbstraction.Interfaces;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.WishList;
using MySqlConnector;
using System.Text;

namespace DataBaseRepository
{
    public class MySqlWishListRepository : IMySqlWishListRepository
    {
        private ILogger<MySqlWishListRepository> _logger;
        private readonly string _connectionString;
        private IMySqlCommonRepository _commonRepo;

        public MySqlWishListRepository(
            IOptions<DataBaseConnectionSettings> connection,
            ILogger<MySqlWishListRepository> logger,
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

            if (StaticData.Categories.Count == 0)
            {
                _commonRepo.FillStaticCategories();
            }

            if (StaticData.FreeMoney is null)
            {
                _commonRepo.FillFreeMoney();
            }
        }

        public async Task<List<WishListItemModel>> GetFullWishList(CancellationToken cancellationToken, string sqlFileName)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository " +
                $"GetFullWishList start");

            List<WishListItemModel> result = new List<WishListItemModel>();

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("WishList", sqlFileName);
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
                                WishListItemModel newWishItem = new WishListItemModel();

                                newWishItem.SecCode = sdr.GetString("seccode");
                                
                                int checkForNull = sdr.GetOrdinal("wish_level");
                                if (!sdr.IsDBNull(checkForNull))
                                {
                                    newWishItem.Level = sdr.GetInt16("wish_level");
                                }

                                int checkForNull2 = sdr.GetOrdinal("description");
                                if (!sdr.IsDBNull(checkForNull2))
                                {
                                    newWishItem.Description = sdr.GetString("description");
                                }
                                
                                result.Add(newWishItem);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository " +
                            $"GetFullWishList {sqlFileName} Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;            
        }


        public async Task DeleteWishBySecCode(CancellationToken cancellationToken, string seccode)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository " +
                $"DeleteWishBySecCode seccode={seccode} start");

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("WishList", "DeleteWishBySecCode.sql");
			if (query is not null)
			{
                query = query.Replace("@seccode", seccode);

                string result = await _commonRepo.ExecuteNonQueryAsyncByQueryText(cancellationToken, query);
            }
        }

        public async Task AddNewWish(CancellationToken cancellationToken, string seccode, int level, string description)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository AddNewWish " +
                $"seccode={seccode} level={level} start");
			//INSERT INTO `money_test`.`wish_list` (`seccode`, `wish_level`) VALUES ('PHOR', '5');

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("WishList", "AddNewWish.sql");
			if (query is not null)
			{
                StringBuilder queryStringBuilder = new StringBuilder(query);
                queryStringBuilder.Replace("@seccode", seccode);
                queryStringBuilder.Replace("@level", level.ToString());
                queryStringBuilder.Replace("@description", description);
                query = queryStringBuilder.ToString();

                string result = await _commonRepo.ExecuteNonQueryAsyncByQueryText(cancellationToken, query);
            }
        }

        public async Task EditWishLevel(CancellationToken cancellationToken, string seccode, int level, string description)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository EditWishLevel " +
                $"seccode={seccode} level={level} start");
			//UPDATE `money_test`.`wish_list` SET `wish_level` = '3' WHERE (`seccode` = 'MGNT');
			
            string? query = _commonRepo.GetQueryTextByFolderAndFilename("WishList", "EditWishLevelBySecCode.sql");
			if (query is not null)
			{
				StringBuilder queryStringBuilder = new StringBuilder(query);
                queryStringBuilder.Replace("@seccode", seccode);
                queryStringBuilder.Replace("@level", level.ToString());
                queryStringBuilder.Replace("@description", description);
                query = queryStringBuilder.ToString();

                string result = await _commonRepo.ExecuteNonQueryAsyncByQueryText(cancellationToken, query);
            }
        }

		public async Task<int[]?> GetWishLevelsWeight(CancellationToken cancellationToken)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("WishList", "GetWishLevelsWeight.sql");
			if (query is null)
			{
				return null;
			}

			// it must be always 6 level array - from 0 to 5
			int[] wishListSettings = new int[6];

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
								int level = sdr.GetInt16("level");
                                wishListSettings[level] = sdr.GetInt32("weight");
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository " +
							$"GetWishLevelsWeight Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return wishListSettings;
		}

		public async Task<string?> EditWishLevelWeight(int level, int weight, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository EditWishLevelWeight " +
				$"level={level} weight={weight} start");
			//UPDATE `money_test`.`wish_list` SET `wish_level` = '3' WHERE (`seccode` = 'MGNT');

			string? query = _commonRepo.GetQueryTextByFolderAndFilename("WishList", "EditWishLevelWeight.sql");
			if (query is not null)
			{
                StringBuilder queryStringBuilder = new StringBuilder(query);
                queryStringBuilder.Replace("@level", level.ToString());
                queryStringBuilder.Replace("@weight", weight.ToString());
                query = queryStringBuilder.ToString();

                string result = await _commonRepo.ExecuteNonQueryAsyncByQueryText(cancellationToken, query);
                if (!result.Equals("1"))
                {
					return result;
				}
            }
            else
            {
                return "Error - query EditWishLevelWeight.sql not found.";
            }

            return null;
		}
	}
}
