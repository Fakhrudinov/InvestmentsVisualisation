namespace DataAbstraction.Models.BaseModels
{
    public class SecCodeAndTimeToCutOffModel
    {
        public string SecCode { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string DSIIndex { get; set; } = string.Empty;
    }
}
