namespace DataAbstraction.Models.Settings
{
    public class SmartLabDiviPageSettings
    {
        public string BaseUrl { get; set; }
        public string StartWord { get; set; }
        public string EndWord { get; set; }
        public string TableRowSplitter { get; set; }
        public string TableCellSplitter { get; set; }
        public string [] CleanWordsFromCell { get; set; }
    }
}
