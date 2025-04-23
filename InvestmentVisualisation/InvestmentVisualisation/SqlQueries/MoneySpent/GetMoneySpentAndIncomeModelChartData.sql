SELECT  
	s.`date_year_month`, 
    ROUND(s.dividend) as div_round,
    ROUND((SUM(s.dividend) OVER (order BY s.`date_year_month` ROWS BETWEEN 11 PRECEDING  AND CURRENT ROW))/12) as avrg_div_round,
    ROUND(m.total) as spent_round
FROM money_by_month s
join money_spent_by_month m
     on s.`date_year_month` = m.`date_year_month`
		where s.`date_year_month` <= DATE_SUB(curdate(), INTERVAL 1 MONTH)
	ORDER BY s.`date_year_month` desc
	limit 12
;