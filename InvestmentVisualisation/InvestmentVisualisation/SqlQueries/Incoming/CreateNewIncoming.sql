INSERT INTO incoming
	(`event_date`, `seccode`, `secboard`, `category`, `value`, `comission`)  
VALUES 
	(@date_time, @seccode, @secboard, @category, @value, @comission);