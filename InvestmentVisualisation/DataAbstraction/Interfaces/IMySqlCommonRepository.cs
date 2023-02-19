namespace DataAbstraction.Interfaces
{
    public interface IMySqlCommonRepository
    {
        Task<string> DeleteSingleRecordByQuery(string query);
        void FillStaticCategories();
        void FillStaticSecBoards();
        void FillStaticSecCodes();
        Task<int> GetTableCountBySqlQuery(string query);
    }
}
