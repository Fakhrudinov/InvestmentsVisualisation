INSERT INTO seccode_info
  (`seccode`, `secboard`, `name`, `full_name`, `isin`, `expired_date`, `payments_per_year`)
VALUES
  (@seccode, @secboard, @name, @full_name, @isin, @expired_date, @payments_per_year);