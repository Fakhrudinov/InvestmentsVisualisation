SELECT sum(sv.volume_@year) FROM sec_volume sv
	WHERE sv.seccode IN 
	(
		SELECT seccode FROM seccode_info
			WHERE secboard = @secBoardType
			AND expired_date IS NULL
	);