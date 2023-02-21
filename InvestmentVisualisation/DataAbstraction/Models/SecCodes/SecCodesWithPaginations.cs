namespace DataAbstraction.Models.SecCodes
{
    public class SecCodesWithPaginations
    {
        public List<SecCodeInfo> SecCodes { get; set; }
        public PaginationPageViewModel PageViewModel { get; set; }
    }
}
