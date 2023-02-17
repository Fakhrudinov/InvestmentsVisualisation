namespace DataAbstraction.Models
{
    public class IncomingWithPaginations
    {
        public List<IncomingModel> Incomings { get; set; }
        public PaginationPageViewModel PageViewModel { get; set; }
    }
}
