INSERT INTO seccode_info
  (`seccode`, `secboard`, `name`, `full_name`, `isin`, `expired_date`)
VALUES
  (@seccode, @secboard, @name, @full_name, @isin, @expired_date);