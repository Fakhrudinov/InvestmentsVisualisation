﻿SELECT * FROM deals 
	where seccode = @secCode
	ORDER BY id desc
	LIMIT @lines_count OFFSET @page_number   ;