SELECT event_date, seccode, pieces, av_price FROM deals 
	where event_date > DATE_ADD(sysdate(), INTERVAL -61 DAY)
		and secboard = 1
		and seccode in (@seccodes);
