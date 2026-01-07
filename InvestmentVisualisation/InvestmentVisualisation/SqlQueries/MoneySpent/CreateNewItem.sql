INSERT INTO `money_spent_by_month` 
	(`event_date`, `total`, `appartment`, `electricity`, `internet`, `phone`, `transport`, `supermarket`, `marketplaces`) 
VALUES 
	(@date, @total, @appartment, @electricity, @internet, @phone, @transport, @supermarket, @marketplaces);