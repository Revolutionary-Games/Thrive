class_name GdUnitMockFunctionDoubler
extends GdFunctionDoubler


const TEMPLATE_FUNC_WITH_RETURN_VALUE = """
	var args__: Array = ["$(func_name)", $(arguments)]

	if __is_prepare_return_value():
		__save_function_return_value(args__)
		return ${default_return_value}$(return_as)

	# verify block
	var __verifier := __get_verifier()
	if __verifier != null:
		if __verifier.is_verify_interactions():
			__verifier.verify_interactions(args__)
			return ${default_return_value}$(return_as)
		else:
			__verifier.save_function_interaction(args__)

	if __do_call_real_func("$(func_name)", args__):
		@warning_ignore("unsafe_call_argument")
		return $(await)super($(arguments))$(return_as)

	return __get_mocked_return_value_or_default(args__, ${default_return_value})

"""


const TEMPLATE_FUNC_WITH_RETURN_VOID = """
	var args__: Array = ["$(func_name)", $(arguments)]

	if __is_prepare_return_value():
		if $(push_errors):
			push_error(\"Mocking a void function '$(func_name)(<args>) -> void:' is not allowed.\")
		return

	# verify block
	var __verifier := __get_verifier()
	if __verifier != null:
		if __verifier.is_verify_interactions():
			__verifier.verify_interactions(args__)
			return
		else:
			__verifier.save_function_interaction(args__)

	if __do_call_real_func("$(func_name)"):
		@warning_ignore("unsafe_call_argument")
		$(await)super($(arguments))

"""


const TEMPLATE_FUNC_VARARG_RETURN_VALUE = """
	var varargs__: Array = __get_verifier().filter_vargs([$(varargs)])
	var args__: Array = ["$(func_name)", $(arguments)] + varargs__

	if __is_prepare_return_value():
		if $(push_errors):
			push_error(\"Mocking a void function '$(func_name)(<args>) -> void:' is not allowed.\")
		__save_function_return_value(args__)
		return ${default_return_value}$(return_as)

	# verify block
	var __verifier := __get_verifier()
	if __verifier != null:
		if __verifier.is_verify_interactions():
			__verifier.verify_interactions(args__)
			return ${default_return_value}$(return_as)
		else:
			__verifier.save_function_interaction(args__)

	if __do_call_real_func("$(func_name)", args__):
		match varargs__.size():
			@warning_ignore("unsafe_call_argument")
			0: return $(await)super($(arguments))
			@warning_ignore("unsafe_call_argument")
			1: return $(await)super($(arguments), varargs__[0])
			@warning_ignore("unsafe_call_argument")
			2: return $(await)super($(arguments), varargs__[0], varargs__[1])
			@warning_ignore("unsafe_call_argument")
			3: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2])
			@warning_ignore("unsafe_call_argument")
			4: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3])
			@warning_ignore("unsafe_call_argument")
			5: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4])
			@warning_ignore("unsafe_call_argument")
			6: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5])
			@warning_ignore("unsafe_call_argument")
			7: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6])
			@warning_ignore("unsafe_call_argument")
			8: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7])
			@warning_ignore("unsafe_call_argument")
			9: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7], varargs__[8])
			@warning_ignore("unsafe_call_argument")
			10: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7], varargs__[8], varargs__[9])

	return __get_mocked_return_value_or_default(args__, ${default_return_value})

"""


func _init(push_errors :bool = false) -> void:
	super._init(push_errors)


func get_template(fd: GdFunctionDescriptor, _is_callable: bool) -> String:
	if fd.is_vararg():
		return TEMPLATE_FUNC_VARARG_RETURN_VALUE
	var return_type :Variant = fd.return_type()
	if return_type is StringName:
		return TEMPLATE_FUNC_WITH_RETURN_VALUE
	return TEMPLATE_FUNC_WITH_RETURN_VOID if (return_type == TYPE_NIL or return_type == GdObjects.TYPE_VOID) else TEMPLATE_FUNC_WITH_RETURN_VALUE
