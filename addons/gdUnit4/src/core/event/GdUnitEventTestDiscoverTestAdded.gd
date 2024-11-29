class_name GdUnitEventTestDiscoverTestAdded
extends GdUnitEvent


var _test_case_dto: GdUnitTestCaseDto


func _init(arg_resource_path: String, arg_suite_name: String, arg_test_case_dto: GdUnitTestCaseDto) -> void:
	_event_type = DISCOVER_TEST_ADDED
	_resource_path = arg_resource_path
	_suite_name  = arg_suite_name
	_test_name = arg_test_case_dto.name()
	_test_case_dto = arg_test_case_dto


func test_case_dto() -> GdUnitTestCaseDto:
	return _test_case_dto
