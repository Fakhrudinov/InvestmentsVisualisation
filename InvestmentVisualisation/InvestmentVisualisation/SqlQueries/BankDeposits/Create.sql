INSERT INTO bank_deposits 
	(`isopen`, `date_open`, `date_close`, `name`, `placed_name`, `percent`, `summ`) 
VALUES 
	(1, @date_open, @date_close, @name, @placed_name, @percent, @summ);