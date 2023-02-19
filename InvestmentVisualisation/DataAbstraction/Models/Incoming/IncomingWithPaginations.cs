namespace DataAbstraction.Models.Incoming
{
    public class IncomingWithPaginations
    {
        public List<IncomingModel> Incomings { get; set; }
        public PaginationPageViewModel PageViewModel { get; set; }
    }
}
