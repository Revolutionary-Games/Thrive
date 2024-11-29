
var __expected_interactions :int = -1
var __saved_interactions := Dictionary()
var __verified_interactions := Array()


func __save_function_interaction(function_args :Array[Variant]) -> void:
	var __matcher := GdUnitArgumentMatchers.to_matcher(function_args, true)
	for __index in __saved_interactions.keys().size():
		var __key :Variant = __saved_interactions.keys()[__index]
		if __matcher.is_match(__key):
			__saved_interactions[__key] += 1
			return
	__saved_interactions[function_args] = 1


func __is_verify_interactions() -> bool:
	return __expected_interactions != -1


func __do_verify_interactions(interactions_times :int = 1) -> Object:
	__expected_interactions = interactions_times
	return self


func __verify_interactions(function_args :Array[Variant]) -> void:
	var __summary := Dictionary()
	var __total_interactions := 0
	var __matcher := GdUnitArgumentMatchers.to_matcher(function_args, true)
	for __index in __saved_interactions.keys().size():
		var __key :Variant = __saved_interactions.keys()[__index]
		if __matcher.is_match(__key):
			var __interactions :int = __saved_interactions.get(__key, 0)
			__total_interactions += __interactions
			__summary[__key] = __interactions
			# add as verified
			__verified_interactions.append(__key)

	var __gd_assert := GdUnitAssertImpl.new("")
	if __total_interactions != __expected_interactions:
		var __expected_summary := {function_args : __expected_interactions}
		var __error_message :String
		# if no __interactions macht collect not verified __interactions for failure report
		if __summary.is_empty():
			var __current_summary := __verify_no_more_interactions()
			__error_message = GdAssertMessages.error_validate_interactions(__current_summary, __expected_summary)
		else:
			__error_message = GdAssertMessages.error_validate_interactions(__summary, __expected_summary)
		@warning_ignore("return_value_discarded")
		__gd_assert.report_error(__error_message)
	else:
		@warning_ignore("return_value_discarded")
		__gd_assert.report_success()
	__expected_interactions = -1


func __verify_no_interactions() -> Dictionary:
	var __summary := Dictionary()
	if not __saved_interactions.is_empty():
		for __index in __saved_interactions.keys().size():
			var func_call :Variant = __saved_interactions.keys()[__index]
			__summary[func_call] = __saved_interactions[func_call]
	return __summary


func __verify_no_more_interactions() -> Dictionary:
	var __summary := Dictionary()
	var called_functions :Array[Variant] = __saved_interactions.keys()
	if called_functions != __verified_interactions:
		# collect the not verified functions
		var called_but_not_verified := called_functions.duplicate()
		for __index in __verified_interactions.size():
			called_but_not_verified.erase(__verified_interactions[__index])

		for __index in called_but_not_verified.size():
			var not_verified :Variant = called_but_not_verified[__index]
			__summary[not_verified] = __saved_interactions[not_verified]
	return __summary


func __reset_interactions() -> void:
	__saved_interactions.clear()


func __filter_vargs(arg_values :Array[Variant]) -> Array[Variant]:
	var filtered :Array[Variant] = []
	for __index in arg_values.size():
		var arg :Variant = arg_values[__index]
		if typeof(arg) == TYPE_STRING and arg == GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE:
			continue
		filtered.append(arg)
	return filtered
