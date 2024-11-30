SELECT  
	s.date, 
    ROUND(s.dividend) as div_round,
    ROUND((SUM(s.dividend) OVER (order BY s.date ROWS BETWEEN 11 PRECEDING  AND CURRENT ROW))/12) as avrg_div_round,
    ROUND(m.total) as spent_round
FROM money_by_month s
join money_spent_by_month m
     on s.date = m.date
		where s.date < DATE_SUB(curdate(), INTERVAL 1 MONTH)
	ORDER BY s.date desc
	limit 12
;