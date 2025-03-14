extends GdUnitTestSuite


func test_instance() -> void:
	var n :Variant = GdUnitSingleton.instance("singelton_test", func() -> Node: return Node.new() )
	assert_object(n).is_instanceof(Node)
	assert_bool(is_instance_valid(n)).is_true()

	# free the singleton
	GdUnitSingleton.unregister("singelton_test")
	assert_bool(is_instance_valid(n)).is_false()
