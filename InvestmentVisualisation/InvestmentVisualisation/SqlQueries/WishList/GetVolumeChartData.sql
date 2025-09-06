select si.seccode, wl.wish_level as level, lv.weight as wish_volume, sv.volume_@year as volume
	FROM money_test.seccode_info si
    left join money_test.sec_volume sv on sv.seccode = si.seccode
    left join money_test.wish_list wl on wl.seccode = si.seccode
    left join money_test.wish_levels lv on lv.level = wl.wish_level
        where 	si.expired_date is null
			and si.secboard not in (0)
;