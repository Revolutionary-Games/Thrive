extends Node

@onready var _client :GdUnitTcpClient = $GdUnitTcpClient
@onready var _executor :GdUnitTestSuiteExecutor = GdUnitTestSuiteExecutor.new()

enum {
	INIT,
	RUN,
	STOP,
	EXIT
}

const GDUNIT_RUNNER = "GdUnitRunner"

var _config := GdUnitRunnerConfig.new()
var _test_suites_to_process :Array[Node]
var _state :int = INIT
var _cs_executor :RefCounted


func _init() -> void:
	# minimize scene window checked debug mode
	if OS.get_cmdline_args().size() == 1:
		DisplayServer.window_set_title("GdUnit4 Runner (Debug Mode)")
	else:
		DisplayServer.window_set_title("GdUnit4 Runner (Release Mode)")
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_MINIMIZED)
	# store current runner instance to engine meta data to can be access in as a singleton
	Engine.set_meta(GDUNIT_RUNNER, self)
	_cs_executor = GdUnit4CSharpApiLoader.create_executor(self)


func _ready() -> void:
	var config_result := _config.load_config()
	if config_result.is_error():
		push_error(config_result.error_message())
		_state = EXIT
		return
	@warning_ignore("return_value_discarded")
	_client.connect("connection_failed", _on_connection_failed)
	@warning_ignore("return_value_discarded")
	GdUnitSignals.instance().gdunit_event.connect(_on_gdunit_event)
	var result := _client.start("127.0.0.1", _config.server_port())
	if result.is_error():
		push_error(result.error_message())
		return
	_state = INIT


func _on_connection_failed(message :String) -> void:
	prints("_on_connection_failed", message, _test_suites_to_process)
	_state = STOP


func _notification(what :int) -> void:
	#prints("GdUnitRunner", self, GdObjects.notification_as_string(what))
	if what == NOTIFICATION_PREDELETE:
		Engine.remove_meta(GDUNIT_RUNNER)


func _process(_delta :float) -> void:
	match _state:
		INIT:
			# wait until client is connected to the GdUnitServer
			if _client.is_client_connected():
				var time := LocalTime.now()
				prints("Scan for test suites.")
				_test_suites_to_process = load_test_suits()
				prints("Scanning of %d test suites took" % _test_suites_to_process.size(), time.elapsed_since())
				gdUnitInit()
				_state = RUN
		RUN:
			# all test suites executed
			if _test_suites_to_process.is_empty():
				_state = STOP
			else:
				# process next test suite
				set_process(false)
				var test_suite :Node = _test_suites_to_process.pop_front()
				@warning_ignore("unsafe_method_access")
				if _cs_executor != null and _cs_executor.IsExecutable(test_suite):
					@warning_ignore("unsafe_method_access")
					_cs_executor.Execute(test_suite)
					@warning_ignore("unsafe_property_access")
					await _cs_executor.ExecutionCompleted
				else:
					await _executor.execute(test_suite as GdUnitTestSuite)
				set_process(true)
		STOP:
			_state = EXIT
			# give the engine small amount time to finish the rpc
			_on_gdunit_event(GdUnitStop.new())
			await get_tree().create_timer(0.1).timeout
			await get_tree().process_frame
			get_tree().quit(0)


func load_test_suits() -> Array[Node]:
	var to_execute := _config.to_execute()
	if to_execute.is_empty():
		prints("No tests selected to execute!")
		_state = EXIT
		return []
	# scan for the requested test suites
	var test_suites :Array[Node] = []
	var _scanner := GdUnitTestSuiteScanner.new()
	for resource_path :String in to_execute.keys():
		var selected_tests :PackedStringArray = to_execute.get(resource_path)
		var scaned_suites := _scanner.scan(resource_path)
		_filter_test_case(scaned_suites, selected_tests)
		test_suites += scaned_suites
	return test_suites


func gdUnitInit() -> void:
	#enable_manuall_polling()
	send_message("Scaned %d test suites" % _test_suites_to_process.size())
	var total_count := _collect_test_case_count(_test_suites_to_process)
	_on_gdunit_event(GdUnitInit.new(_test_suites_to_process.size(), total_count))
	if not GdUnitSettings.is_test_discover_enabled():
		for test_suite in _test_suites_to_process:
			send_test_suite(test_suite)


func _filter_test_case(test_suites :Array[Node], included_tests :PackedStringArray) -> void:
	if included_tests.is_empty():
		return
	for test_suite in test_suites:
		for test_case in test_suite.get_children():
			_do_filter_test_case(test_suite, test_case, included_tests)


func _do_filter_test_case(test_suite :Node, test_case :Node, included_tests :PackedStringArray) -> void:
	for included_test in included_tests:
		var test_meta :PackedStringArray = included_test.split(":")
		var test_name := test_meta[0]
		if test_case.get_name() == test_name:
			# we have a paremeterized test selection
			if test_meta.size() > 1:
				var test_param_index := test_meta[1]
				@warning_ignore("unsafe_method_access")
				test_case.set_test_parameter_index(test_param_index.to_int())
			return
	# the test is filtered out
	test_suite.remove_child(test_case)
	test_case.free()


func _collect_test_case_count(testSuites :Array[Node]) -> int:
	var total :int = 0
	for test_suite in testSuites:
		total += test_suite.get_child_count()
	return total


# RPC send functions
func send_message(message :String) -> void:
	_client.rpc_send(RPCMessage.of(message))


func send_test_suite(test_suite :Node) -> void:
	_client.rpc_send(RPCGdUnitTestSuite.of(test_suite))


func _on_gdunit_event(event :GdUnitEvent) -> void:
	_client.rpc_send(RPCGdUnitEvent.of(event))


# Event bridge from C# GdUnit4.ITestEventListener.cs
func PublishEvent(data :Dictionary) -> void:
	var event := GdUnitEvent.new().deserialize(data)
	_client.rpc_send(RPCGdUnitEvent.of(event))
