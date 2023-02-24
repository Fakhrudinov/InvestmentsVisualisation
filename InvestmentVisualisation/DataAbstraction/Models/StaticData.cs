using DataAbstraction.Models.BaseModels;

namespace DataAbstraction.Models
{
    public static class StaticData
    {
        public static List<IncomingMoneyCategory> Categories = new List<IncomingMoneyCategory>();
        public static List<SecBoardCategory> SecBoards = new List<SecBoardCategory>();
        public static List<StaticSecCode> SecCodes= new List<StaticSecCode>();
        public static List<MonthName> MonthNames = new List<MonthName>();
        public static string FreeMoney;
    }
}
