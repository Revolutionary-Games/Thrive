class_name GdUnitEventTestDiscoverTestRemoved
extends GdUnitEvent


func _init(arg_resource_path: String, arg_suite_name: String, arg_test_name: String) -> void:
	_event_type = DISCOVER_TEST_REMOVED
	_resource_path = arg_resource_path
	_suite_name  = arg_suite_name
	_test_name = arg_test_name
