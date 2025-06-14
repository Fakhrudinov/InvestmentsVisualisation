using DataAbstraction.Models.WishList;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlWishListRepository
    {
        Task AddNewWish(CancellationToken cancellationToken, string seccode, int level, string description);
        Task DeleteWishBySecCode(CancellationToken cancellationToken, string seccode);
        Task EditWishLevel(CancellationToken cancellationToken, string seccode, int level, string description);
		Task<string?> EditWishLevelWeight(int level, int weight, CancellationToken cancellationToken);
		Task<List<WishListItemModel>> GetFullWishList(CancellationToken cancellationToken, string sqlFileName);
		Task<int[]?> GetWishLevelsWeight(CancellationToken cancellationToken);
	}
}
