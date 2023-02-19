UPDATE deals 
	SET 
		`date` = @date_time,
		`seccode` = @seccode, 
		`secboard` = @secboard, 
		`av_price` = @av_price, 
		`pieces` = @pieces, 
		`comission` = @comission, 
		`nkd` = @nkd 
	WHERE (`id` = @id);
