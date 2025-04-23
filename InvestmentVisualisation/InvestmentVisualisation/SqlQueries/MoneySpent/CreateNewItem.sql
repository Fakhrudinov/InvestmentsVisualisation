INSERT INTO `money_spent_by_month` 
	(`date_year_month`, `total`, `appartment`, `electricity`, `internet`, `phone`) 
VALUES 
	(@date, @total, @appartment, @electricity, @internet, @phone);