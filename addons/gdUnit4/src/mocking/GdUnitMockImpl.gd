class_name DoubledMockClassSourceClassName

################################################################################
# internal mocking stuff
################################################################################
const __INSTANCE_ID = "${instance_id}"
const __SOURCE_CLASS = "${source_class}"


class MockingState:
	var working_mode := GdUnitMock.RETURN_DEFAULTS
	var excluded_methods: PackedStringArray = []
	var return_value: Variant = null
	var is_prepare_return := false

		#{ <func_name> = {
	#		<func_args> = <return_value>
	#	}
	#}
	var return_values := Dictionary():
		get:
			return return_values


@warning_ignore("unused_private_class_variable")
var __verifier_instance := GdUnitObjectInteractionsVerifier.new()
var __mocking_state := MockingState.new()


func __init(__script: GDScript, mock_working_mode: String) -> void:
	# store self need to access static functions
	Engine.set_meta(__INSTANCE_ID, self)
	super.set_script(__script)
	__mocking_state.working_mode = mock_working_mode


func __release_double() -> void:
	# we need to release the self reference manually to prevent orphan nodes
	Engine.remove_meta(__INSTANCE_ID)


func _notification(what: int) -> void:
	if what == NOTIFICATION_PREDELETE:
		if Engine.has_meta(__INSTANCE_ID):
			Engine.remove_meta(__INSTANCE_ID)


static func __mock_state() -> MockingState:
	@warning_ignore("unsafe_property_access")
	return __get_instance().__mocking_state


static func __get_verifier() -> GdUnitObjectInteractionsVerifier:
	var __instance := __get_instance()
	@warning_ignore("unsafe_property_access")
	return null if __instance == null else __instance.__verifier_instance


static func __get_instance() -> Object:
	return null if not Engine.has_meta(__INSTANCE_ID) else Engine.get_meta(__INSTANCE_ID)


func __instance_id() -> String:
	return __INSTANCE_ID


static func __is_prepare_return_value() -> bool:
	return __mock_state().is_prepare_return


static func __sort_by_argument_matcher(__left_args: Array, __right_args: Array) -> bool:
	for __index in __left_args.size():
		var __larg: Variant = __left_args[__index]
		if __larg is GdUnitArgumentMatcher:
			return false
	return true


# we need to sort by matcher arguments so that they are all at the end of the list
static func __sort_dictionary(__unsorted_args: Dictionary) -> Dictionary:
	var __instance := __get_instance()
	# only need to sort if contains more than one entry
	if __unsorted_args.size() <= 1:
		return __unsorted_args
	var __sorted_args: Array = __unsorted_args.keys()
	__sorted_args.sort_custom(__sort_by_argument_matcher)
	var __sorted_result := {}
	for __index in __sorted_args.size():
		var key :Variant = __sorted_args[__index]
		__sorted_result[key] = __unsorted_args[key]
	return __sorted_result


static func __save_function_return_value(__fuction_args: Array) -> void:
	var __mock := __mock_state()
	var __func_name: String = __fuction_args[0]
	var __func_args: Array = __fuction_args.slice(1)
	var mocked_return_value_by_args: Dictionary = __mock.return_values.get(__func_name, {})

	mocked_return_value_by_args[__func_args] = __mock.return_value
	__mock.return_values[__func_name] = __sort_dictionary(mocked_return_value_by_args)
	__mock.return_value = null
	__mock.is_prepare_return = false


static func __is_mocked_args_match(__func_args: Array, __mocked_args: Array) -> bool:
	var __is_matching := false
	for __index in __mocked_args.size():
		var __fuction_args: Array = __mocked_args[__index]
		if __func_args.size() != __fuction_args.size():
			continue
		__is_matching = true
		for __arg_index in __func_args.size():
			var __func_arg: Variant = __func_args[__arg_index]
			var __mock_arg: Variant = __fuction_args[__arg_index]
			if __mock_arg is GdUnitArgumentMatcher:
				@warning_ignore("unsafe_method_access")
				__is_matching = __is_matching and __mock_arg.is_match(__func_arg)
			else:
				__is_matching = __is_matching and typeof(__func_arg) == typeof(__mock_arg) and __func_arg == __mock_arg
			if not __is_matching:
				break
		if __is_matching:
			break
	return __is_matching


static func __get_mocked_return_value_or_default(__fuction_args: Array, __default_return_value: Variant) -> Variant:
	var __mock := __mock_state()
	var __func_name: String = __fuction_args[0]
	if not __mock.return_values.has(__func_name):
		return __default_return_value
	var __func_args: Array = __fuction_args.slice(1)
	@warning_ignore("unsafe_method_access")
	var __mocked_args: Array = __mock.return_values.get(__func_name).keys()
	for __index in __mocked_args.size():
		var __margs: Variant = __mocked_args[__index]
		if __is_mocked_args_match(__func_args, [__margs]):
			return __mock.return_values[__func_name][__margs]
	return __default_return_value


static func __do_call_real_func(__func_name: String, __func_args := []) -> bool:
	var __mock := __mock_state()
	var __is_call_real_func: bool = __mock.working_mode == GdUnitMock.CALL_REAL_FUNC  and not __mock.excluded_methods.has(__func_name)
	# do not call real funcions for mocked functions
	if __is_call_real_func and __mock.return_values.has(__func_name):
		var __fuction_args: Array = __func_args.slice(1)
		@warning_ignore("unsafe_method_access")
		var __mocked_args: Array = __mock.return_values.get(__func_name).keys()
		return not __is_mocked_args_match(__fuction_args, __mocked_args)
	return __is_call_real_func


func __exclude_method_call(exluded_methods: PackedStringArray) -> void:
	__mocking_state.excluded_methods.append_array(exluded_methods)


func __do_return(mock_do_return_value: Variant) -> Object:
	__mocking_state.return_value = mock_do_return_value
	__mocking_state.is_prepare_return = true
	return self
