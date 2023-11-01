﻿SELECT 		i.seccode, 
			i.name, 
			sv.pieces_@prev_prev_year, 
			sv.pieces_@prev_year, 
			sv.pieces_@year, 
			sv.volume_@year
	FROM seccode_info i
inner join sec_volume sv 
		on i.seccode = sv.seccode
	where i.expired_date is null
		and i.secboard = 1
	order by sv.volume_@year desc;
