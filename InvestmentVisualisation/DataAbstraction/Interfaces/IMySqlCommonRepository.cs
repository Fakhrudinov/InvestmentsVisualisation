namespace DataAbstraction.Interfaces
{
    public interface IMySqlCommonRepository
    {
        Task<string> ExecuteNonQueryAsyncByQueryText(CancellationToken cancellationToken, string query);
        void FillFreeMoney();
        void FillStaticCategories();
        void FillStaticSecBoards();
        void FillStaticSecCodes();
        Task<int> GetTableCountBySqlQuery(CancellationToken cancellationToken, string query);
    }
}
