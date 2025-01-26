SELECT * FROM seccode_info 
	where expired_date is null
	ORDER BY seccode asc
	LIMIT @lines_count OFFSET @offset ;