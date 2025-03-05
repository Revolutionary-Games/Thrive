class_name GdUnitSpyFunctionDoubler
extends GdFunctionDoubler


const TEMPLATE_RETURN_VARIANT = """
	var args__: Array = ["$(func_name)", $(arguments)]

	# verify block
	var __verifier := __get_verifier()
	if __verifier != null:
		if __verifier.is_verify_interactions():
			__verifier.verify_interactions(args__)
			return ${default_return_value}$(return_as)
		else:
			__verifier.save_function_interaction(args__)

	if __do_call_real_func("$(func_name)"):
		@warning_ignore("unsafe_call_argument")
		return $(await)super($(arguments))
	return ${default_return_value}

"""


const TEMPLATE_RETURN_VOID = """
	var args__: Array = ["$(func_name)", $(arguments)]

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


const TEMPLATE_RETURN_VOID_VARARG = """
	var varargs__: Array = __get_verifier().filter_vargs([$(varargs)])
	var args__: Array = ["$(func_name)", $(arguments)] + varargs__

	# verify block
	var __verifier := __get_verifier()
	if __verifier != null:
		if __verifier.is_verify_interactions():
			__verifier.verify_interactions(args__)
			return
		else:
			__verifier.save_function_interaction(args__)

	$(await)__call_func("$(func_name)", [$(arguments)] + varargs__)

"""


const TEMPLATE_RETURN_VARIANT_VARARG = """
	var varargs__: Array = __get_verifier().filter_vargs([$(varargs)])
	var args__: Array = ["$(func_name)", $(arguments)] + varargs__

	# verify block
	var __verifier := __get_verifier()
	if __verifier != null:
		if __verifier.is_verify_interactions():
			__verifier.verify_interactions(args__)
			return ${default_return_value}$(return_as)
		else:
			__verifier.save_function_interaction(args__)

	return $(await)__call_func("$(func_name)", [$(arguments)] + varargs__)

"""


const TEMPLATE_CALLABLE_CALL = """
	var used_arguments__ := __get_verifier().filter_vargs([$(arguments)])

	# verify block
	var __verifier := __get_verifier()
	if __verifier != null:
		if __verifier.is_verify_interactions():
			__verifier.verify_interactions(["call", used_arguments__])
			return ${default_return_value}$(return_as)
		else:
			var args__ := used_arguments__.duplicate()
			args__.append_array(super.get_bound_arguments())
			__verifier.save_function_interaction(["call", args__])

	if __do_call_real_func("call"):
		return _cb.callv(used_arguments__)

	return ${default_return_value}

"""


func _init(push_errors: bool = false) -> void:
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
