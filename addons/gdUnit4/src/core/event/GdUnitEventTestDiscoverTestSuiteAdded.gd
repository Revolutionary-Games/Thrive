class_name GdUnitEventTestDiscoverTestSuiteAdded
extends GdUnitEvent


var _dto: GdUnitTestSuiteDto


func _init(arg_resource_path: String, arg_suite_name: String, arg_dto: GdUnitTestSuiteDto) -> void:
	_event_type = DISCOVER_SUITE_ADDED
	_resource_path = arg_resource_path
	_suite_name  = arg_suite_name
	_dto = arg_dto


func suite_dto() -> GdUnitTestSuiteDto:
	return _dto
