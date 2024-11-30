
################################################################################
# internal mocking stuff
################################################################################
const __INSTANCE_ID = "${instance_id}"
const __SOURCE_CLASS = "${source_class}"

var __mock_working_mode := GdUnitMock.RETURN_DEFAULTS
var __excluded_methods :PackedStringArray = []
var __do_return_value :Variant = null
var __prepare_return_value := false

#{ <func_name> = {
#		<func_args> = <return_value>
#	}
#}
var __mocked_return_values := Dictionary()


static func __instance() -> Object:
	return Engine.get_meta(__INSTANCE_ID)


func _notification(what :int) -> void:
	if what == NOTIFICATION_PREDELETE:
		if Engine.has_meta(__INSTANCE_ID):
			Engine.remove_meta(__INSTANCE_ID)


func __instance_id() -> String:
	return __INSTANCE_ID


func __set_singleton() -> void:
	# store self need to mock static functions
	Engine.set_meta(__INSTANCE_ID, self)


func __release_double() -> void:
	# we need to release the self reference manually to prevent orphan nodes
	Engine.remove_meta(__INSTANCE_ID)


func __is_prepare_return_value() -> bool:
	return __prepare_return_value


func __sort_by_argument_matcher(__left_args :Array, __right_args :Array) -> bool:
	for __index in __left_args.size():
		var __larg :Variant = __left_args[__index]
		if __larg is GdUnitArgumentMatcher:
			return false
	return true


# we need to sort by matcher arguments so that they are all at the end of the list
func __sort_dictionary(__unsorted_args :Dictionary) -> Dictionary:
	# only need to sort if contains more than one entry
	if __unsorted_args.size() <= 1:
		return __unsorted_args
	var __sorted_args := __unsorted_args.keys()
	__sorted_args.sort_custom(__sort_by_argument_matcher)
	var __sorted_result := {}
	for __index in __sorted_args.size():
		var key :Variant = __sorted_args[__index]
		__sorted_result[key] = __unsorted_args[key]
	return __sorted_result


func __save_function_return_value(__fuction_args :Array) -> void:
	var __func_name :String = __fuction_args[0]
	var __func_args :Array = __fuction_args.slice(1)
	var __mocked_return_value_by_args :Dictionary = __mocked_return_values.get(__func_name, {})
	__mocked_return_value_by_args[__func_args] = __do_return_value
	__mocked_return_values[__func_name] = __sort_dictionary(__mocked_return_value_by_args)
	__do_return_value = null
	__prepare_return_value = false


@warning_ignore("unsafe_method_access")
func __is_mocked_args_match(__func_args :Array, __mocked_args :Array) -> bool:
	var __is_matching := false
	for __index in __mocked_args.size():
		var __fuction_args :Variant = __mocked_args[__index]
		if __func_args.size() != __fuction_args.size():
			continue
		__is_matching = true
		for __arg_index in __func_args.size():
			var __func_arg :Variant = __func_args[__arg_index]
			var __mock_arg :Variant = __fuction_args[__arg_index]
			if __mock_arg is GdUnitArgumentMatcher:
				__is_matching = __is_matching and __mock_arg.is_match(__func_arg)
			else:
				__is_matching = __is_matching and typeof(__func_arg) == typeof(__mock_arg) and __func_arg == __mock_arg
			if not __is_matching:
				break
		if __is_matching:
			break
	return __is_matching


@warning_ignore("unsafe_method_access")
func __get_mocked_return_value_or_default(__fuction_args :Array, __default_return_value :Variant) -> Variant:
	var __func_name :String = __fuction_args[0]
	if not __mocked_return_values.has(__func_name):
		return __default_return_value
	var __func_args :Array = __fuction_args.slice(1)
	var __mocked_args :Array = __mocked_return_values.get(__func_name).keys()
	for __index in __mocked_args.size():
		var __margs :Variant = __mocked_args[__index]
		if __is_mocked_args_match(__func_args, [__margs]):
			return __mocked_return_values[__func_name][__margs]
	return __default_return_value


func __set_script(__script :GDScript) -> void:
	super.set_script(__script)


func __set_mode(mock_working_mode :String) -> Object:
	__mock_working_mode = mock_working_mode
	return self


@warning_ignore("unsafe_method_access")
func __do_call_real_func(__func_name :String, __func_args := []) -> bool:
	var __is_call_real_func := __mock_working_mode == GdUnitMock.CALL_REAL_FUNC  and not __excluded_methods.has(__func_name)
	# do not call real funcions for mocked functions
	if __is_call_real_func and __mocked_return_values.has(__func_name):
		var __fuction_args :Array = __func_args.slice(1)
		var __mocked_args :Array = __mocked_return_values.get(__func_name).keys()
		return not __is_mocked_args_match(__fuction_args, __mocked_args)
	return __is_call_real_func


func __exclude_method_call(exluded_methods :PackedStringArray) -> void:
	__excluded_methods.append_array(exluded_methods)


func __do_return(mock_do_return_value :Variant) -> Object:
	__do_return_value = mock_do_return_value
	__prepare_return_value = true
	return self
