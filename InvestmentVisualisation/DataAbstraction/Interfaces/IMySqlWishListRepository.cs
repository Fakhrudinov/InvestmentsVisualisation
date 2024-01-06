using DataAbstraction.Models.WishList;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlWishListRepository
    {
        Task AddNewWish(string seccode, int level);
        Task DeleteWishBySecCode(string seccode);
        Task EditWishLevel(string seccode, int level);
        Task<List<WishListItemModel>> GetFullWishList();
    }
}
