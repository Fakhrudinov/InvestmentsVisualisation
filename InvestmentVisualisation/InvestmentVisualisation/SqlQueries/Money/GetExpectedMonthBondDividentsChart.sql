select  min(DAY(i.event_date)) as expect_day, min(i.value) as volume, si.name-- , i.seccode , si.full_name
	FROM incoming i 
    right join seccode_info si
    on i.seccode = si.seccode
		where 	i.secboard = 2 	
			and si.expired_date is null
           and i.event_date > DATE_ADD(sysdate(), INTERVAL -6 MONTH)
group by i.seccode
order by expect_day;