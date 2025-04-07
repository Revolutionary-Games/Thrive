extends GdUnitTestSuite


func test_discover_many_test() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/discovery/resources/DiscoverExampleTestSuite.gd")

	var discovered_tests := []
	GdUnitTestDiscoverer.discover_tests(script,
		func discover(test_case: GdUnitTestCase) -> void:
			if test_case.test_name in ["test_case1", "test_case2", "test_parameterized_static"]:
				discovered_tests.append(test_case)
	)

	assert_array(discovered_tests)\
		.extractv(extr("test_name"), extr("display_name"))\
		.contains_exactly([
			tuple("test_case1", "test_case1"),
			tuple("test_case2", "test_case2"),
			tuple("test_parameterized_static", "test_parameterized_static:0 (1, 1)"),
			tuple("test_parameterized_static", "test_parameterized_static:1 (2, 2)"),
			tuple("test_parameterized_static", "test_parameterized_static:2 (3, 3)"),
		])


func test_discover_parameterized_test() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/discovery/resources/DiscoverExampleTestSuite.gd")

	var discovered_tests := []
	GdUnitTestDiscoverer.discover_tests(script,
		func discover(test_case: GdUnitTestCase) -> void:
			if test_case.test_name == "test_parameterized_static":
				discovered_tests.append(test_case)
	)

	assert_array(discovered_tests)\
		.extractv(extr("test_name"), extr("display_name"))\
		.contains_exactly([
			tuple("test_parameterized_static", "test_parameterized_static:0 (1, 1)"),
			tuple("test_parameterized_static", "test_parameterized_static:1 (2, 2)"),
			tuple("test_parameterized_static", "test_parameterized_static:2 (3, 3)"),
		])


func test_discover_tests() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/discovery/resources/DiscoverExampleTestSuite.gd")

	var discovered_tests := []
	GdUnitTestDiscoverer.discover_tests(script,\
		func discover(test_case: GdUnitTestCase) -> void:
			discovered_tests.append(test_case)
	)

	assert_array(discovered_tests)\
		.extractv(extr("test_name"), extr("display_name"))\
		.contains_exactly([
			tuple("test_case1", "test_case1"),
			tuple("test_case2", "test_case2"),
			tuple("test_parameterized_static", "test_parameterized_static:0 (1, 1)"),
			tuple("test_parameterized_static", "test_parameterized_static:1 (2, 2)"),
			tuple("test_parameterized_static", "test_parameterized_static:2 (3, 3)"),
			tuple("test_parameterized_static_external", "test_parameterized_static_external:0 (<null>)"),
			tuple("test_parameterized_static_external", "test_parameterized_static_external:1 (%s)" % Vector2.ONE),
			tuple("test_parameterized_static_external", "test_parameterized_static_external:2 (%s)" % Vector2i.ONE),
			tuple("test_parameterized_dynamic", "test_parameterized_dynamic:0 (<null>)"),
			tuple("test_parameterized_dynamic", "test_parameterized_dynamic:1 (%s)" % Vector2.ONE),
			tuple("test_parameterized_dynamic", "test_parameterized_dynamic:2 (%s)" % Vector2i.ONE),
		])


func test_discover_tests_on_GdUnitTestSuite() -> void:
	var script: GDScript = load("res://addons/gdUnit4/src/GdUnitTestSuite.gd")

	var discovered_tests := []
	GdUnitTestDiscoverer.discover_tests(script,\
		func discover(test_case: GdUnitTestCase) -> void:
			discovered_tests.append(test_case)
	)

	# we expect no test covered from the base implementaion of GdUnitTestSuite
	assert_array(discovered_tests).is_empty()
