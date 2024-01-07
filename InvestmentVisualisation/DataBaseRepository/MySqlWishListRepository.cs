using DataAbstraction.Interfaces;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.WishList;
using MySqlConnector;

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
        }

        public async Task<List<WishListItemModel>> GetFullWishList()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository GetFullWishList start");

            List<WishListItemModel> result = new List<WishListItemModel>();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "WishList", "GetFullWishList.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository Error! " +
                    $"File with SQL script not found at " + filePath);
                return result;
            }

            string query = File.ReadAllText(filePath);
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository " +
                $"GetFullWishList execute query \r\n{query}");

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
                                WishListItemModel newWishItem = new WishListItemModel();

                                newWishItem.SecCode = sdr.GetString("seccode");
                                
                                int checkForNull = sdr.GetOrdinal("wish_level");
                                if (!sdr.IsDBNull(checkForNull))
                                {
                                    newWishItem.Level = sdr.GetInt16("wish_level");
                                }
                                result.Add(newWishItem);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository " +
                            $"GetFullWishList Exception!\r\n{ex.Message}");
                    }
                    finally
                    {
                        await con.CloseAsync();
                    }
                }
            }

            return result;            
        }


        public async Task DeleteWishBySecCode(string seccode)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository DeleteWishBySecCode " +
                $"seccode={seccode} start");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "WishList", "DeleteWishBySecCode.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository Error! " +
                    $"File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);
                query = query.Replace("@seccode", seccode);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository " +
                    $"DeleteWishBySecCode execute query\r\n{query}");

                string result = await _commonRepo.DeleteSingleRecordByQuery(query);
            }
        }

        public async Task AddNewWish(string seccode, int level)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository AddNewWish " +
                $"seccode={seccode} level={level} start");
            //INSERT INTO `money_test`.`wish_list` (`seccode`, `wish_level`) VALUES ('PHOR', '5');

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "WishList", "AddNewWish.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository Error! " +
                    $"File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);
                query = query.Replace("@seccode", seccode);
                query = query.Replace("@level", level.ToString());
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository " +
                    $"AddNewWish execute query\r\n{query}");

                string result = await _commonRepo.DeleteSingleRecordByQuery(query);
            }
        }

        public async Task EditWishLevel(string seccode, int level)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository EditWishLevel " +
                $"seccode={seccode} level={level} start");
            //UPDATE `money_test`.`wish_list` SET `wish_level` = '3' WHERE (`seccode` = 'MGNT');

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", "WishList", "EditWishLevelBySecCode.sql");
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository Error! " +
                    $"File with SQL script not found at " + filePath);
            }
            else
            {
                string query = File.ReadAllText(filePath);
                query = query.Replace("@seccode", seccode);
                query = query.Replace("@level", level.ToString());
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MySqlWishListRepository " +
                    $"EditWishLevel execute query\r\n{query}");

                string result = await _commonRepo.DeleteSingleRecordByQuery(query);
            }
        }
    }
}
