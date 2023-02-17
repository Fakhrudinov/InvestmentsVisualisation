SELECT * FROM incoming 
	ORDER BY id desc
	LIMIT @lines_count OFFSET @page_number   ;