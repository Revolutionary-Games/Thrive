class_name GdUnitTestDiscoverer
extends RefCounted


static func run() -> Array[GdUnitTestCase]:
	console_log("Running test discovery ..")
	await (Engine.get_main_loop() as SceneTree).process_frame
	GdUnitSignals.instance().gdunit_event.emit(GdUnitEventTestDiscoverStart.new())

	# We run the test discovery in an extra thread so that the main thread is not blocked
	var t:= Thread.new()
	@warning_ignore("return_value_discarded")
	t.start(func () -> Array[GdUnitTestCase]:
		var test_suite_directories :PackedStringArray = GdUnitCommandHandler.scan_all_test_directories(GdUnitSettings.test_root_folder())
		var scanner := GdUnitTestSuiteScanner.new()

		var collected_tests: Array[GdUnitTestCase] = []
		var collected_test_suites: Array[Script] = []
		# collect test suites
		for test_suite_dir in test_suite_directories:
			collected_test_suites.append_array(scanner.scan_directory(test_suite_dir))

		# Do sync the main thread before emit the discovered test suites to the inspector
		await (Engine.get_main_loop() as SceneTree).process_frame
		for test_suites_script in collected_test_suites:
			discover_tests(test_suites_script, func(test_case: GdUnitTestCase) -> void:
				collected_tests.append(test_case)
				GdUnitTestDiscoverSink.discover(test_case)
			)

		console_log_discover_results(collected_tests)
		return collected_tests
	)
	# wait unblocked to the tread is finished
	while t.is_alive():
		await (Engine.get_main_loop() as SceneTree).process_frame
	# needs finally to wait for finish
	var test_to_execute: Array[GdUnitTestCase] = await t.wait_to_finish()
	GdUnitSignals.instance().gdunit_event.emit(GdUnitEventTestDiscoverEnd.new(0, 0))
	return test_to_execute


static func console_log_discover_results(tests: Array[GdUnitTestCase]) -> void:
	var grouped_by_suites := GdArrayTools.group_by(tests, func(test: GdUnitTestCase) -> String:
		return test.source_file
	)
	for suite_tests: Array in grouped_by_suites.values():
		var test_case: GdUnitTestCase = suite_tests[0]
		console_log("Discover: TestSuite %s with %d tests fount" % [test_case.source_file, suite_tests.size()])
	console_log("Discover tests done, %d TestSuites and total %d Tests found. " % [grouped_by_suites.size(), tests.size()])
	console_log("")


static func console_log(message: String) -> void:
	prints(message)
	#GdUnitSignals.instance().gdunit_message.emit(message)


static func filter_tests(method: Dictionary) -> bool:
	var method_name: String = method["name"]
	return method_name.begins_with("test_")


static func default_discover_sink(test_case: GdUnitTestCase) -> void:
	GdUnitTestDiscoverSink.discover(test_case)


static func discover_tests(source_script: Script, discover_sink := default_discover_sink) -> void:
	if source_script is GDScript:
		var test_names := source_script.get_script_method_list()\
			.filter(filter_tests)\
			.map(func(method: Dictionary) -> String: return method["name"])
		# no tests discovered?
		if test_names.is_empty():
			return

		var parser := GdScriptParser.new()
		var fds := parser.get_function_descriptors(source_script as GDScript, test_names)
		for fd in fds:
			var resolver := GdFunctionParameterSetResolver.new(fd)
			for test_case in resolver.resolve_test_cases(source_script as GDScript):
				discover_sink.call(test_case)
	elif source_script.get_class() == "CSharpScript":
		if not GdUnit4CSharpApiLoader.is_dotnet_supported():
			return
		for test_case in GdUnit4CSharpApiLoader.discover_tests(source_script):
			discover_sink.call(test_case)
