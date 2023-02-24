SELECT money_sum FROM money_by_month
where year = year(curdate())
	and month = month(curdate());