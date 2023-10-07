namespace DataAbstraction.Models.BaseModels
{
    public class DohodDivsAndDatesModel
    {
        public List<SecCodeAndDividentModel>? DohodDivs { get; set; }
        public List<SecCodeAndTimeToCutOffModel>? DohodDates { get; set; }
    }
}
