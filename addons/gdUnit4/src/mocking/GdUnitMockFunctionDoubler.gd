class_name GdUnitMockFunctionDoubler
extends GdFunctionDoubler


const TEMPLATE_FUNC_WITH_RETURN_VALUE = """
	var args__: Array = ["$(func_name)", $(arguments)]

	if $(instance)__is_prepare_return_value():
		$(instance)__save_function_return_value(args__)
		return ${default_return_value}
	if $(instance)__is_verify_interactions():
		$(instance)__verify_interactions(args__)
		return ${default_return_value}
	else:
		$(instance)__save_function_interaction(args__)

	if $(instance)__do_call_real_func("$(func_name)", args__):
		return $(await)super($(arguments))
	return $(instance)__get_mocked_return_value_or_default(args__, ${default_return_value})

"""


const TEMPLATE_FUNC_WITH_RETURN_VOID = """
	var args__: Array = ["$(func_name)", $(arguments)]

	if $(instance)__is_prepare_return_value():
		if $(push_errors):
			push_error(\"Mocking a void function '$(func_name)(<args>) -> void:' is not allowed.\")
		return
	if $(instance)__is_verify_interactions():
		$(instance)__verify_interactions(args__)
		return
	else:
		$(instance)__save_function_interaction(args__)

	if $(instance)__do_call_real_func("$(func_name)"):
		$(await)super($(arguments))

"""


const TEMPLATE_FUNC_VARARG_RETURN_VALUE = """
	var varargs__: Array = __filter_vargs([$(varargs)])
	var args__: Array = ["$(func_name)", $(arguments)] + varargs__

	if $(instance)__is_prepare_return_value():
		if $(push_errors):
			push_error(\"Mocking a void function '$(func_name)(<args>) -> void:' is not allowed.\")
		$(instance)__save_function_return_value(args__)
		return ${default_return_value}
	if $(instance)__is_verify_interactions():
		$(instance)__verify_interactions(args__)
		return ${default_return_value}
	else:
		$(instance)__save_function_interaction(args__)

	if $(instance)__do_call_real_func("$(func_name)", args__):
		match varargs__.size():
			0: return $(await)super($(arguments))
			1: return $(await)super($(arguments), varargs__[0])
			2: return $(await)super($(arguments), varargs__[0], varargs__[1])
			3: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2])
			4: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3])
			5: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4])
			6: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5])
			7: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6])
			8: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7])
			9: return $(await)super($(arguments), varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7], varargs__[8])
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
