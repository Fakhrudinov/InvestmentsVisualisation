using DataAbstraction.Models.WishList;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlWishListRepository
    {
        Task AddNewWish(CancellationToken cancellationToken, string seccode, int level);
        Task DeleteWishBySecCode(CancellationToken cancellationToken, string seccode);
        Task EditWishLevel(CancellationToken cancellationToken, string seccode, int level);
        Task<List<WishListItemModel>> GetFullWishList(CancellationToken cancellationToken);
    }
}
