SELECT seccode, volume_@year FROM sec_volume sv
	where sv.seccode in 
    (
		select seccode FROM seccode_info where secboard = 1 and expired_date is null
    )
;