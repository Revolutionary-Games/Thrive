class_name GdUnitRunnerConfig
extends Resource

const GdUnitTools := preload("res://addons/gdUnit4/src/core/GdUnitTools.gd")

const CONFIG_VERSION = "1.0"
const VERSION = "version"
const INCLUDED = "included"
const SKIPPED = "skipped"
const SERVER_PORT = "server_port"
const EXIT_FAIL_FAST ="exit_on_first_fail"

const CONFIG_FILE = "res://addons/gdUnit4/GdUnitRunner.cfg"

var _config := {
		VERSION : CONFIG_VERSION,
		# a set of directories or testsuite paths as key and a optional set of testcases as values
		INCLUDED :  Dictionary(),
		# a set of skipped directories or testsuite paths
		SKIPPED : Dictionary(),
		# the port of running test server for this session
		SERVER_PORT : -1
	}


func clear() -> GdUnitRunnerConfig:
	_config[INCLUDED] = Dictionary()
	_config[SKIPPED] = Dictionary()
	return self


func set_server_port(port :int) -> GdUnitRunnerConfig:
	_config[SERVER_PORT] = port
	return self


func server_port() -> int:
	return _config.get(SERVER_PORT, -1)


@warning_ignore("return_value_discarded")
func self_test() -> GdUnitRunnerConfig:
	add_test_suite("res://addons/gdUnit4/test/")
	add_test_suite("res://addons/gdUnit4/mono/test/")
	return self


func add_test_suite(p_resource_path :String) -> GdUnitRunnerConfig:
	var to_execute_ := to_execute()
	to_execute_[p_resource_path] = to_execute_.get(p_resource_path, PackedStringArray())
	return self


func add_test_suites(resource_paths :PackedStringArray) -> GdUnitRunnerConfig:
	for resource_path_ in resource_paths:
		@warning_ignore("return_value_discarded")
		add_test_suite(resource_path_)
	return self


func add_test_case(p_resource_path :String, test_name :StringName, test_param_index :int = -1) -> GdUnitRunnerConfig:
	var to_execute_ := to_execute()
	var test_cases :PackedStringArray = to_execute_.get(p_resource_path, PackedStringArray())
	if test_param_index != -1:
		@warning_ignore("return_value_discarded")
		test_cases.append("%s:%d" % [test_name, test_param_index])
	else:
		@warning_ignore("return_value_discarded")
		test_cases.append(test_name)
	to_execute_[p_resource_path] = test_cases
	return self


# supports full path or suite name with optional test case name
# <test_suite_name|path>[:<test_case_name>]
# '/path/path', res://path/path', 'res://path/path/testsuite.gd' or 'testsuite'
# 'res://path/path/testsuite.gd:test_case' or 'testsuite:test_case'
func skip_test_suite(value :StringName) -> GdUnitRunnerConfig:
	var parts: PackedStringArray = GdUnitFileAccess.make_qualified_path(value).rsplit(":")
	if parts[0] == "res":
		parts.remove_at(0)
	parts[0] = GdUnitFileAccess.make_qualified_path(parts[0])
	match parts.size():
		1:
			skipped()[parts[0]] = PackedStringArray()
		2:
			@warning_ignore("return_value_discarded")
			skip_test_case(parts[0], parts[1])
	return self


func skip_test_suites(resource_paths :PackedStringArray) -> GdUnitRunnerConfig:
	for resource_path_ in resource_paths:
		@warning_ignore("return_value_discarded")
		skip_test_suite(resource_path_)
	return self


func skip_test_case(p_resource_path :String, test_name :StringName) -> GdUnitRunnerConfig:
	var to_ignore := skipped()
	var test_cases :PackedStringArray = to_ignore.get(p_resource_path, PackedStringArray())
	@warning_ignore("return_value_discarded")
	test_cases.append(test_name)
	to_ignore[p_resource_path] = test_cases
	return self


# Dictionary[String, Dictionary[String, PackedStringArray]]
func to_execute() -> Dictionary:
	return _config.get(INCLUDED, {"res://" : PackedStringArray()})


func skipped() -> Dictionary:
	return _config.get(SKIPPED, {})


func save_config(path :String = CONFIG_FILE) -> GdUnitResult:
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		var error := FileAccess.get_open_error()
		return GdUnitResult.error("Can't write test runner configuration '%s'! %s" % [path, error_string(error)])
	_config[VERSION] = CONFIG_VERSION
	file.store_string(JSON.stringify(_config))
	return GdUnitResult.success(path)


func load_config(path :String = CONFIG_FILE) -> GdUnitResult:
	if not FileAccess.file_exists(path):
		return GdUnitResult.error("Can't find test runner configuration '%s'! Please select a test to run." % path)
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		var error := FileAccess.get_open_error()
		return GdUnitResult.error("Can't load test runner configuration '%s'! ERROR: %s." % [path, error_string(error)])
	var content := file.get_as_text()
	if not content.is_empty() and content[0] == '{':
		# Parse as json
		var test_json_conv := JSON.new()
		var error := test_json_conv.parse(content)
		if error != OK:
			return GdUnitResult.error("The runner configuration '%s' is invalid! The format is changed please delete it manually and start a new test run." % path)
		_config = test_json_conv.get_data()
		if not _config.has(VERSION):
			return GdUnitResult.error("The runner configuration '%s' is invalid! The format is changed please delete it manually and start a new test run." % path)
		fix_value_types()
	return GdUnitResult.success(path)


@warning_ignore("unsafe_cast")
func fix_value_types() -> void:
	# fix float value to int json stores all numbers as float
	var server_port_ :int = _config.get(SERVER_PORT, -1)
	_config[SERVER_PORT] = server_port_
	convert_Array_to_PackedStringArray(_config[INCLUDED] as Dictionary)
	convert_Array_to_PackedStringArray(_config[SKIPPED] as Dictionary)


func convert_Array_to_PackedStringArray(data :Dictionary) -> void:
	for key in data.keys() as Array[String]:
		var values :Array = data[key]
		data[key] = PackedStringArray(values)


func _to_string() -> String:
	return str(_config)
