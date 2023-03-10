namespace DataAbstraction.Models.Settings
{
    public class WebDiviPageSettings
    {
        public string BaseUrl { get; set; }
        public string StartWord { get; set; }
        public string EndWord { get; set; }
        public string TableRowSplitter { get; set; }
        public int NumberOfRowToStartSearchData { get; set; }
        public string TableCellSplitter { get; set; }
        public int NumberOfCellWithHref { get; set; }
        public int NumberOfCellWithDiscont { get; set; }
        public string [] CleanWordsFromCell { get; set; }
    }
}
