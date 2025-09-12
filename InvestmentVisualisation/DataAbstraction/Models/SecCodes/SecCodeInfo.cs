using DataAbstraction.Models.BaseModels;

namespace DataAbstraction.Models.SecCodes
{
    public class SecCodeInfo : SecCodeAndSecBoardModel
    {       
        public string Name { get; set; }
        public string FullName { get; set; }
        public string ISIN { get; set; }
        public DateTime ? ExpiredDate { get; set; }
		public int? PaysPerYear { get; set; }
	}
}
