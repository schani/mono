STATISTIC_TIME(stop_world)
STATISTIC_SIZE(bytes_marked_after_stop_world)
STATISTIC_SIZE(bytes_allocated_after_stop_world)
STATISTIC_TIME(stop_workers)
STATISTIC_TIME(init)
STATISTIC_TIME(fragment_clear)
STATISTIC_TIME(pinning)
STATISTIC_TIME(wait_for_workers)
STATISTIC_SIZE(bytes_marked_after_wait_for_workers)
STATISTIC_SIZE(bytes_allocated_after_wait_for_workers)
STATISTIC_TIME(scan_remsets)
STATISTIC_TIME(scan_roots)
STATISTIC_TIME(finish_gray_stack_drain)
STATISTIC_TIME(finish_gray_stack_finalize)
STATISTIC_TIME(finish_gray_stack_null_links)
STATISTIC_SIZE(bytes_marked_after_finish_gray_stack)
STATISTIC_SIZE(bytes_allocated_after_finish_gray_stack)
STATISTIC_TIME(minor_fragment_creation)
STATISTIC_TIME(los_sweep)
STATISTIC_TIME(major_sweep)
STATISTIC_TIME(clean_up)
STATISTIC_TIME(restart_world)
STATISTIC_TIME(count_bytes_marked)

#undef STATISTIC_TIME
#undef STATISTIC_SIZE
