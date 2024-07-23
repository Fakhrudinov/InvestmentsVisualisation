namespace DataAbstraction.Models.Settings
{
    public class WebDiviPageSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string StartWord { get; set; } = string.Empty;
        public string EndWord { get; set; } = string.Empty;
        public string TableRowSplitter { get; set; } = string.Empty;
        public int NumberOfRowToStartSearchData { get; set; }
        public string TableCellSplitter { get; set; } = string.Empty;
        public int NumberOfCellWithHref { get; set; }
        public int NumberOfCellWithDiscont { get; set; }
        public string [] ? CleanWordsFromCell { get; set; }
    }
}
