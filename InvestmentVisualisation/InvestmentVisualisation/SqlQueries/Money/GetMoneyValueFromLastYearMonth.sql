SELECT money_sum FROM @data_base.money_by_month
	where year = year(curdate())
		and month = month(curdate());