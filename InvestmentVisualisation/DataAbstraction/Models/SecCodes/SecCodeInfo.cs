namespace DataAbstraction.Models.SecCodes
{
    public class SecCodeInfo
    {       
        public string SecCode { get; set; }
        public int SecBoard { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string ISIN { get; set; }
        public DateTime ? ExpiredDate { get; set; }
    }
}
