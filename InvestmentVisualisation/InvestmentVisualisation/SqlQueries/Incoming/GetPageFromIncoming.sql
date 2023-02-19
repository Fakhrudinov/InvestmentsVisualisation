SELECT *, UNIX_TIMESTAMP(date) AS DATETOSORT FROM incoming 
	ORDER BY DATETOSORT desc
	LIMIT @lines_count OFFSET @page_number   ;