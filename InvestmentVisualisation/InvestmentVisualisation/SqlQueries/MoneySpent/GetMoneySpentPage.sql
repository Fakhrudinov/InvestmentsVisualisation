SELECT `event_date`, `total`, `appartment`, `electricity`, `internet`, `phone`, `transport`, `supermarket`, `marketplaces`
	FROM money_spent_by_month
	ORDER BY event_date desc
LIMIT @lines_count OFFSET @page_number;