﻿UPDATE incoming
	SET `event_date` = @date_time, 
		`seccode` = @seccode, 
		`secboard` = @secboard, 
		`category` = @category, 
		`value` = @value, 
		`comission` = @comission
	WHERE (`id` = @id);