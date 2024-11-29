class_name GdUnitTestCaseDto
extends GdUnitResourceDto

var _line_number :int = -1
var _script_path: String
var _test_case_names :PackedStringArray = []


@warning_ignore("unsafe_method_access")
func serialize(test_case :Node) -> Dictionary:
	var serialized := super.serialize(test_case)
	if test_case.has_method("line_number"):
		serialized["line_number"] = test_case.line_number()
	else:
		serialized["line_number"] = test_case.get("LineNumber")
	if test_case.has_method("script_path"):
		serialized["script_path"] = test_case.script_path()
	else:
		# TODO 'script_path' needs to be implement in c# the the
		# serialized["script_path"] = test_case.get("ScriptPath")
		serialized["script_path"] = serialized["resource_path"]
	if test_case.has_method("test_case_names"):
		serialized["test_case_names"] = test_case.test_case_names()
	elif test_case.has_method("TestCaseNames"):
		serialized["test_case_names"] = test_case.TestCaseNames()
	return serialized


func deserialize(data :Dictionary) -> GdUnitTestCaseDto:
	@warning_ignore("return_value_discarded")
	super.deserialize(data)
	_line_number = data.get("line_number", -1)
	_script_path = data.get("script_path", data.get("resource_path", ""))
	_test_case_names = data.get("test_case_names", [])
	return self


func line_number() -> int:
	return _line_number


func script_path() -> String:
	return _script_path


func test_case_names() -> PackedStringArray:
	return _test_case_names
