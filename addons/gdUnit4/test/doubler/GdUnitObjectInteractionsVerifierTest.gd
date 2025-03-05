# GdUnit generated TestSuite
extends GdUnitTestSuite



func test_filter_vargs() -> void:
	var template := GdUnitObjectInteractionsVerifier.new()

	var varags :Array = [
		GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
		GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
		GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
		GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE]
	assert_array(template.filter_vargs(varags)).is_empty()

	var object := RefCounted.new()
	varags = [
		"foo",
		"bar",
		null,
		true,
		1,
		object,
		GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
		GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
		GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
		GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE]
	assert_array(template.filter_vargs(varags)).contains_exactly([
		"foo",
		"bar",
		null,
		true,
		1,
		object])
