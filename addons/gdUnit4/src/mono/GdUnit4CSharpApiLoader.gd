extends RefCounted
class_name GdUnit4CSharpApiLoader


static func instance() -> Object:
	return GdUnitSingleton.instance("GdUnit4CSharpApi", func() -> Object:
		if not GdUnit4CSharpApiLoader.is_mono_supported():
			return null
		@warning_ignore("unsafe_method_access")
		return load("res://addons/gdUnit4/src/mono/GdUnit4CSharpApi.cs").new()
	)


static func is_engine_version_supported(engine_version :int = Engine.get_version_info().hex) -> bool:
	return engine_version >= 0x40200


# test is Godot mono running
static func is_mono_supported() -> bool:
	return ClassDB.class_exists("CSharpScript") and is_engine_version_supported()


static func version() -> String:
	if not GdUnit4CSharpApiLoader.is_mono_supported():
		return "unknown"
	@warning_ignore("unsafe_method_access")
	return instance().Version()


static func create_test_suite(source_path :String, line_number :int, test_suite_path :String) -> GdUnitResult:
	if not GdUnit4CSharpApiLoader.is_mono_supported():
		return  GdUnitResult.error("Can't create test suite. No C# support found.")
	@warning_ignore("unsafe_method_access")
	var result: Dictionary = instance().CreateTestSuite(source_path, line_number, test_suite_path)
	if result.has("error"):
		return GdUnitResult.error(str(result.get("error")))
	return  GdUnitResult.success(result)


static func is_test_suite(resource_path :String) -> bool:
	if not is_csharp_file(resource_path) or not GdUnit4CSharpApiLoader.is_mono_supported():
		return false

	if resource_path.is_empty():
		if GdUnitSettings.is_report_push_errors():
			push_error("Can't create test suite. Missing resource path.")
		return  false
	@warning_ignore("unsafe_method_access")
	return instance().IsTestSuite(resource_path)


static func parse_test_suite(source_path :String) -> Node:
	if not GdUnit4CSharpApiLoader.is_mono_supported():
		if GdUnitSettings.is_report_push_errors():
			push_error("Can't create test suite. No c# support found.")
		return null
	@warning_ignore("unsafe_method_access")
	return instance().ParseTestSuite(source_path)


static func create_executor(listener :Node) -> RefCounted:
	if not GdUnit4CSharpApiLoader.is_mono_supported():
		return null
	@warning_ignore("unsafe_method_access")
	return instance().Executor(listener)


static func is_csharp_file(resource_path :String) -> bool:
	var ext := resource_path.get_extension()
	return ext == "cs" and GdUnit4CSharpApiLoader.is_mono_supported()
