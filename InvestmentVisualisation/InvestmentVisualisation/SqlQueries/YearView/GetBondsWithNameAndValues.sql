SELECT si.seccode, si.name, si.full_name, sv.pieces_@year as pieces, sv.volume_@year as volume, si.payments_per_year
	FROM seccode_info si 
    right join sec_volume sv on si.seccode = sv.seccode
		where 	si.secboard = 2 	
				and expired_date is null;
