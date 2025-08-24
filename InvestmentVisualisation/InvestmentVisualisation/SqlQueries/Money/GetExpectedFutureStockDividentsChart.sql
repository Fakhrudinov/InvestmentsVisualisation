select  i.event_date, i.seccode, i.value
	FROM incoming i 
    right join seccode_info si
    on i.seccode = si.seccode
		where 	i.secboard = 1 	
			and si.expired_date is null
            and i.event_date 
				between 
					DATE_ADD(DATE_ADD(sysdate(), INTERVAL -1 WEEK), INTERVAL -1 YEAR) 
					and 
					DATE_ADD(sysdate(), INTERVAL -6 MONTH);