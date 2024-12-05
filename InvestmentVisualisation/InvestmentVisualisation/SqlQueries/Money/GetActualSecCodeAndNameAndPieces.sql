SELECT si.seccode, si.name, sv.pieces_@year as pieces
	FROM seccode_info si 
    right join sec_volume sv
    on sv.seccode = si.seccode
		where si.secboard = 1 
        and si.expired_date is null;