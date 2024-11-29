extends GdUnitStringAssert

var _base: GdUnitAssertImpl


func _init(current :Variant) -> void:
	_base = GdUnitAssertImpl.new(current)
	# save the actual assert instance on the current thread context
	GdUnitThreadManager.get_current_context().set_assert(self)
	if current != null and typeof(current) != TYPE_STRING and typeof(current) != TYPE_STRING_NAME:
		@warning_ignore("return_value_discarded")
		report_error("GdUnitStringAssert inital error, unexpected type <%s>" % GdObjects.typeof_as_string(current))


func _notification(event :int) -> void:
	if event == NOTIFICATION_PREDELETE:
		if _base != null:
			_base.notification(event)
			_base = null


func failure_message() -> String:
	return _base.failure_message()


func current_value() -> Variant:
	return _base.current_value()


func report_success() -> GdUnitStringAssert:
	_base.report_success()
	return self


func report_error(error :String) -> GdUnitStringAssert:
	_base.report_error(error)
	return self


func override_failure_message(message :String) -> GdUnitStringAssert:
	@warning_ignore("return_value_discarded")
	_base.override_failure_message(message)
	return self


func append_failure_message(message :String) -> GdUnitStringAssert:
	@warning_ignore("return_value_discarded")
	_base.append_failure_message(message)
	return self


func is_null() -> GdUnitStringAssert:
	@warning_ignore("return_value_discarded")
	_base.is_null()
	return self


func is_not_null() -> GdUnitStringAssert:
	@warning_ignore("return_value_discarded")
	_base.is_not_null()
	return self


func is_equal(expected :Variant) -> GdUnitStringAssert:
	var current :Variant = current_value()
	if current == null:
		return report_error(GdAssertMessages.error_equal(current, expected))
	if not GdObjects.equals(current, expected):
		var diffs := GdDiffTool.string_diff(current, expected)
		var formatted_current := GdAssertMessages.colored_array_div(diffs[1])
		return report_error(GdAssertMessages.error_equal(formatted_current, expected))
	return report_success()


func is_equal_ignoring_case(expected :Variant) -> GdUnitStringAssert:
	var current :Variant = current_value()
	if current == null:
		return report_error(GdAssertMessages.error_equal_ignoring_case(current, expected))
	if not GdObjects.equals(str(current), expected, true):
		var diffs := GdDiffTool.string_diff(current, expected)
		var formatted_current := GdAssertMessages.colored_array_div(diffs[1])
		return report_error(GdAssertMessages.error_equal_ignoring_case(formatted_current, expected))
	return report_success()


func is_not_equal(expected :Variant) -> GdUnitStringAssert:
	var current :Variant = current_value()
	if GdObjects.equals(current, expected):
		return report_error(GdAssertMessages.error_not_equal(current, expected))
	return report_success()


func is_not_equal_ignoring_case(expected :Variant) -> GdUnitStringAssert:
	var current :Variant = current_value()
	if GdObjects.equals(current, expected, true):
		return report_error(GdAssertMessages.error_not_equal(current, expected))
	return report_success()


func is_empty() -> GdUnitStringAssert:
	var current :Variant = current_value()
	@warning_ignore("unsafe_cast")
	if current == null or not (current as String).is_empty():
		return report_error(GdAssertMessages.error_is_empty(current))
	return report_success()


func is_not_empty() -> GdUnitStringAssert:
	var current :Variant = current_value()
	@warning_ignore("unsafe_cast")
	if current == null or (current as String).is_empty():
		return report_error(GdAssertMessages.error_is_not_empty())
	return report_success()


func contains(expected :String) -> GdUnitStringAssert:
	var current :Variant = current_value()
	@warning_ignore("unsafe_cast")
	if current == null or (current as String).find(expected) == -1:
		return report_error(GdAssertMessages.error_contains(current, expected))
	return report_success()


func not_contains(expected :String) -> GdUnitStringAssert:
	var current :Variant = current_value()
	@warning_ignore("unsafe_cast")
	if current != null and (current as String).find(expected) != -1:
		return report_error(GdAssertMessages.error_not_contains(current, expected))
	return report_success()


func contains_ignoring_case(expected :String) -> GdUnitStringAssert:
	var current :Variant = current_value()
	@warning_ignore("unsafe_cast")
	if current == null or (current as String).findn(expected) == -1:
		return report_error(GdAssertMessages.error_contains_ignoring_case(current, expected))
	return report_success()


func not_contains_ignoring_case(expected :String) -> GdUnitStringAssert:
	var current :Variant = current_value()
	@warning_ignore("unsafe_cast")
	if current != null and (current as String).findn(expected) != -1:
		return report_error(GdAssertMessages.error_not_contains_ignoring_case(current, expected))
	return report_success()


func starts_with(expected :String) -> GdUnitStringAssert:
	var current :Variant = current_value()
	@warning_ignore("unsafe_cast")
	if current == null or (current as String).find(expected) != 0:
		return report_error(GdAssertMessages.error_starts_with(current, expected))
	return report_success()


func ends_with(expected :String) -> GdUnitStringAssert:
	var current :Variant = current_value()
	if current == null:
		return report_error(GdAssertMessages.error_ends_with(current, expected))
	@warning_ignore("unsafe_cast")
	var find :int = (current as String).length() - expected.length()
	@warning_ignore("unsafe_cast")
	if (current as String).rfind(expected) != find:
		return report_error(GdAssertMessages.error_ends_with(current, expected))
	return report_success()


# gdlint:disable=max-returns
func has_length(expected :int, comparator := Comparator.EQUAL) -> GdUnitStringAssert:
	var current :Variant = current_value()
	if current == null:
		return report_error(GdAssertMessages.error_has_length(current, expected, comparator))
	var str_current: String = current
	match comparator:
		Comparator.EQUAL:
			if str_current.length() != expected:
				return report_error(GdAssertMessages.error_has_length(str_current, expected, comparator))
		Comparator.LESS_THAN:
			if str_current.length() >= expected:
				return report_error(GdAssertMessages.error_has_length(str_current, expected, comparator))
		Comparator.LESS_EQUAL:
			if str_current.length() > expected:
				return report_error(GdAssertMessages.error_has_length(str_current, expected, comparator))
		Comparator.GREATER_THAN:
			if str_current.length() <= expected:
				return report_error(GdAssertMessages.error_has_length(str_current, expected, comparator))
		Comparator.GREATER_EQUAL:
			if str_current.length() < expected:
				return report_error(GdAssertMessages.error_has_length(str_current, expected, comparator))
		_:
			return report_error("Comparator '%d' not implemented!" % comparator)
	return report_success()
