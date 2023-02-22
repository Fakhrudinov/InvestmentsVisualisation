SELECT seccode, secboard, pieces_@year, av_price_@year, volume_@year 
	FROM sec_volume
		where pieces_@year is not null
    ORDER BY seccode asc
	LIMIT @lines_count OFFSET @page_number   ;