SELECT 
			CASE
				WHEN i.secboard = 1 THEN i.seccode
				ELSE i.name
			END as NAME,
            ROUND(sv.volume_@year, 2) AS VOLUME
	FROM seccode_info i
    inner join sec_volume sv 
		on i.seccode = sv.seccode
	where i.expired_date is null;
