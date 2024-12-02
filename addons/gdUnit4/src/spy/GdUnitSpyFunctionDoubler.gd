class_name GdUnitSpyFunctionDoubler
extends GdFunctionDoubler


const TEMPLATE_RETURN_VARIANT = """
	var args__: Array = ["$(func_name)", $(arguments)]

	if $(instance)__is_verify_interactions():
		$(instance)__verify_interactions(args__)
		return ${default_return_value}
	else:
		$(instance)__save_function_interaction(args__)

	if $(instance)__do_call_real_func("$(func_name)"):
		return $(await)super($(arguments))
	return ${default_return_value}

"""


const TEMPLATE_RETURN_VOID = """
	var args__: Array = ["$(func_name)", $(arguments)]

	if $(instance)__is_verify_interactions():
		$(instance)__verify_interactions(args__)
		return
	else:
		$(instance)__save_function_interaction(args__)

	if $(instance)__do_call_real_func("$(func_name)"):
		$(await)super($(arguments))

"""


const TEMPLATE_RETURN_VOID_VARARG = """
	var varargs__: Array = __filter_vargs([$(varargs)])
	var args__: Array = ["$(func_name)", $(arguments)] + varargs__

	if $(instance)__is_verify_interactions():
		$(instance)__verify_interactions(args__)
		return
	else:
		$(instance)__save_function_interaction(args__)

	$(await)$(instance)__call_func("$(func_name)", [$(arguments)] + varargs__)

"""


const TEMPLATE_RETURN_VARIANT_VARARG = """
	var varargs__: Array = __filter_vargs([$(varargs)])
	var args__: Array = ["$(func_name)", $(arguments)] + varargs__

	if $(instance)__is_verify_interactions():
		$(instance)__verify_interactions(args__)
		return ${default_return_value}
	else:
		$(instance)__save_function_interaction(args__)

	return $(await)$(instance)__call_func("$(func_name)", [$(arguments)] + varargs__)

"""


const TEMPLATE_CALLABLE_CALL = """
	var used_arguments__ := __filter_vargs([$(arguments)])

	if __is_verify_interactions():
		__verify_interactions(["call", used_arguments__])
		return ${default_return_value}
	else:
		# append possible binded values to complete the original argument list
		var args__ := used_arguments__.duplicate()
		args__.append_array(super.get_bound_arguments())
		__save_function_interaction(["call", args__])

	if __do_call_real_func("call"):
		return _cb.callv(used_arguments__)
	return ${default_return_value}

"""


func _init(push_errors :bool = false) -> void:
	super._init(push_errors)


func get_template(fd: GdFunctionDescriptor, is_callable: bool) -> String:
	if is_callable and  fd.name() == "call":
		return TEMPLATE_CALLABLE_CALL
	if  fd.is_vararg():
		return TEMPLATE_RETURN_VOID_VARARG if fd.return_type() == TYPE_NIL else TEMPLATE_RETURN_VARIANT_VARARG
	var return_type :Variant = fd.return_type()
	if return_type is StringName:
		return TEMPLATE_RETURN_VARIANT
	return TEMPLATE_RETURN_VOID if (return_type == TYPE_NIL or return_type == GdObjects.TYPE_VOID) else TEMPLATE_RETURN_VARIANT
