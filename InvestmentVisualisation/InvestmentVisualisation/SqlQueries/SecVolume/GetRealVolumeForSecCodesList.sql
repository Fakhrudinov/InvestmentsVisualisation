SELECT seccode, volume_@year as real_volume FROM sec_volume sv
	where sv.seccode in 
    (
		@seccodes
    )
;