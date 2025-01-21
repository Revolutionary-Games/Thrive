class_name GdUnitTestSuiteDto
extends GdUnitResourceDto


# Dictionary[String, GdUnitTestCaseDto]
var _test_cases_by_name := Dictionary()


static func of(test_suite :Node) -> GdUnitTestSuiteDto:
	var dto := GdUnitTestSuiteDto.new()
	return dto.deserialize(dto.serialize(test_suite))


func serialize(test_suite :Node) -> Dictionary:
	var serialized := super.serialize(test_suite)
	var test_cases_ := Array()
	serialized["test_cases"] = test_cases_
	for test_case in test_suite.get_children():
		test_cases_.append(GdUnitTestCaseDto.new().serialize(test_case))
	return serialized


func deserialize(data :Dictionary) -> GdUnitResourceDto:
	@warning_ignore("return_value_discarded")
	super.deserialize(data)
	var test_cases_ :Array = data.get("test_cases", [])
	for test_case :Dictionary in test_cases_:
		add_test_case(GdUnitTestCaseDto.new().deserialize(test_case))
	return self


func add_test_case(test_case :GdUnitTestCaseDto) -> void:
	_test_cases_by_name[test_case.name()] = test_case


func test_case_count() -> int:
	return _test_cases_by_name.size()


func test_cases() -> Array[GdUnitTestCaseDto]:
	var test_cases_ :Array[GdUnitTestCaseDto] = []
	test_cases_.append_array(_test_cases_by_name.values())
	return test_cases_
