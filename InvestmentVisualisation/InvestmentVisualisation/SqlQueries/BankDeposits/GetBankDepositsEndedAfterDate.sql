select bd.name, bd.date_close, bd.summ, bd.percent, bd.isopen, bd.income_summ, DATEDIFF(bd.date_close,bd.event_date) as days
	from bank_deposits bd
		where bd.date_close >= @data
    order by bd.date_close