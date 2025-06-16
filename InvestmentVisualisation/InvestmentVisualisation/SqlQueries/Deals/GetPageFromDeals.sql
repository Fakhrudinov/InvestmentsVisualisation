SELECT *, UNIX_TIMESTAMP(event_date) AS DATETOSORT FROM deals 
	ORDER BY DATETOSORT desc
	LIMIT @lines_count OFFSET @page_number   ;