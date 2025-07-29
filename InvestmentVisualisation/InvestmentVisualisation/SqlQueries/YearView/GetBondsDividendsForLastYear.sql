SELECT i.event_date, i.seccode, i.value 
FROM incoming i
	where 
		i.event_date >= DATE_ADD(DATE_ADD(LAST_DAY(sysdate()), INTERVAL 1 DAY), INTERVAL -12 MONTH)
		and i.seccode in (SELECT seccode FROM seccode_info where secboard = 2 and expired_date is null)
		and i.category = 1;
