class_name GdUnitTestDiscoverer
extends RefCounted


static func run() -> void:
	prints("Running test discovery ..")
	GdUnitSignals.instance().gdunit_event.emit(GdUnitEventTestDiscoverStart.new())
	await (Engine.get_main_loop() as SceneTree).create_timer(.5).timeout

	# We run the test discovery in an extra thread so that the main thread is not blocked
	var t:= Thread.new()
	@warning_ignore("return_value_discarded")
	t.start(func () -> void:
		var test_suite_directories :PackedStringArray = GdUnitCommandHandler.scan_test_directorys("res://" , GdUnitSettings.test_root_folder(), [])
		var scanner := GdUnitTestSuiteScanner.new()
		var _test_suites_to_process :Array[Node] = []

		for test_suite_dir in test_suite_directories:
			_test_suites_to_process.append_array(scanner.scan(test_suite_dir))

		# Do sync the main thread before emit the discovered test suites to the inspector
		await (Engine.get_main_loop() as SceneTree).process_frame
		var test_case_count :int = 0
		for test_suite in _test_suites_to_process:
			test_case_count += test_suite.get_child_count()
			var ts_dto := GdUnitTestSuiteDto.of(test_suite)
			GdUnitSignals.instance().gdunit_add_test_suite.emit(ts_dto)
			test_suite.free()

		prints("%d test suites discovered." % _test_suites_to_process.size())
		GdUnitSignals.instance().gdunit_event.emit(GdUnitEventTestDiscoverEnd.new(_test_suites_to_process.size(), test_case_count))
		_test_suites_to_process.clear()
	)
	# wait unblocked to the tread is finished
	while t.is_alive():
		await (Engine.get_main_loop() as SceneTree).process_frame
	# needs finally to wait for finish
	await t.wait_to_finish()
