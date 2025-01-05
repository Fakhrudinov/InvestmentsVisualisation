using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models.MoneyByMonth
{
    public class MoneyModel
    {
        [Display(Name = "Год")]
        public int Year { get; set; }

        [Display(Name = "Месяц")]
        public int Month { get; set; }

        [Display(Name = "Всего зачислил")]
        public decimal ? TotalIn { get; set; }

        [Display(Name = "За месяц зачислил")]
        public decimal ? MonthIn { get; set; }

        [Display(Name = "Получено дивидентов")]
        public decimal ? Divident { get; set; }

        [Display(Name = "Получено по досрочному погашению")]
        public decimal ? Dosrochnoe { get; set; }

        [Display(Name = "Сделок на сумму")]
        public decimal? DealsSum { get; set; }

        [Display(Name = "Уплачено брокерской комиссии")]
        public decimal ? BrokComission { get; set; }

        [Display(Name = "Итого остаток денег на конец месяца")]
        public decimal ? MoneySum { get; set; }
    }
}
