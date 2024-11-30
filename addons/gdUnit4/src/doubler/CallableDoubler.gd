## The helper class to allow to double Callable
## Is just a wrapper to the original callable with the same function signature.
##
## Due to interface conflicts between 'Callable' and 'Object',
## it is not possible to stub the 'call' and 'call_deferred' methods.
##
## The Callable interface and the Object class have overlapping method signatures,
## which causes conflicts when attempting to stub these methods.
## As a result, you cannot create stubs for 'call' and 'call_deferred' methods.

class_name CallableDoubler


const doubler_script :Script =  preload("res://addons/gdUnit4/src/doubler/CallableDoubler.gd")

var _cb: Callable


func _init(cb: Callable) -> void:
	assert(cb!=null, "Invalid argument <cb> must not be null")
	_cb = cb

## --- helpers -----------------------------------------------------------------------------------------------------------------------------
static func map_func_name(method_info: Dictionary) -> String:
	return method_info["name"]


## We do not want to double all functions based on Object for this class
## Is used on SpyBuilder to excluding functions to be doubled for Callable
static func excluded_functions() -> PackedStringArray:
	return ClassDB.class_get_method_list("Object")\
		.map(CallableDoubler.map_func_name)\
		.filter(func (name: String) -> bool:
			return !CallableDoubler.callable_functions().has(name))


static func non_callable_functions(name: String) -> bool:
	return ![
		# we allow "_init", is need to construct it,
		"excluded_functions",
		"non_callable_functions",
		"callable_functions",
		"map_func_name"
		].has(name)


## Returns the list of supported Callable functions
static func callable_functions() -> PackedStringArray:
	var supported_functions :Array = doubler_script.get_script_method_list()\
		.map(CallableDoubler.map_func_name)\
		.filter(CallableDoubler.non_callable_functions)
	# We manually add these functions that we cannot/may not overwrite in this class
	supported_functions.append_array(["call_deferred", "callv"])
	return supported_functions


## -----------------------------------------------------------------------------------------------------------------------------------------
## Callable functions stubing
## -----------------------------------------------------------------------------------------------------------------------------------------

@warning_ignore("untyped_declaration")
func bind(arg0=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg1=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg2=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg3=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg4=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg5=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg6=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg7=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg8=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg9=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE) -> Callable:
	# save
	var bind_values: Array = GdArrayTools.filter_value([arg0,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9], GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE)
	_cb = _cb.bindv(bind_values)
	return _cb


func bindv(caller_args: Array) -> Callable:
	_cb = _cb.bindv(caller_args)
	return _cb


@warning_ignore("untyped_declaration", "native_method_override", "unused_parameter")
func call(arg0=null,
	arg1=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg2=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg3=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg4=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg5=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg6=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg7=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg8=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg9=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE) -> Variant:

	# This is a placeholder function signanture without any functionallity!
	# It is used by the function doubler to double function signature of Callable:call()
	# The doubled function calls direct _cb.callv(<arguments>) see GdUnitSpyFunctionDoubler:TEMPLATE_CALLABLE_CALL template
	assert(false)
	return null


# Is not supported, see class description
#func call_deferred(a) -> void:
#	pass


# Is not supported, see class description
#func callv(a) -> void:
#	pass



func get_bound_arguments() -> Array:
	return _cb.get_bound_arguments()


func get_bound_arguments_count() -> int:
	return _cb.get_bound_arguments_count()


func get_method() -> StringName:
	return _cb.get_method()


func get_object() -> Object:
	return _cb.get_object()


func get_object_id() -> int:
	return _cb.get_object_id()


func hash() -> int:
	return _cb.hash()


func is_custom() -> bool:
	return _cb.is_custom()


func is_null() -> bool:
	return _cb.is_null()


func is_standard() -> bool:
	return _cb.is_standard()


func is_valid() -> bool:
	return _cb.is_valid()


@warning_ignore("untyped_declaration")
func rpc(arg0=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg1=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg2=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg3=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg4=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg5=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg6=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg7=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg8=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg9=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE) -> void:

	var args: Array = GdArrayTools.filter_value([arg0,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9], GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE)
	match args.size():
		0: _cb.rpc(0)
		1: _cb.rpc(args[0])
		2: _cb.rpc(args[0], args[1])
		3: _cb.rpc(args[0], args[1], args[2])
		4: _cb.rpc(args[0], args[1], args[2], args[3])
		5: _cb.rpc(args[0], args[1], args[2], args[3], args[4])
		6: _cb.rpc(args[0], args[1], args[2], args[3], args[4], args[5])
		7: _cb.rpc(args[0], args[1], args[2], args[3], args[4], args[5], args[6])
		8: _cb.rpc(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7])
		9: _cb.rpc(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8])
		10: _cb.rpc(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9])


@warning_ignore("untyped_declaration")
func rpc_id(peer_id: int,
	arg0=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg1=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg2=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg3=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg4=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg5=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg6=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg7=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg8=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE,
	arg9=GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE) -> void:

	var args: Array = GdArrayTools.filter_value([arg0,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9], GdObjects.TYPE_VARARG_PLACEHOLDER_VALUE)
	match args.size():
		0: _cb.rpc_id(peer_id)
		1: _cb.rpc_id(peer_id, args[0])
		2: _cb.rpc_id(peer_id, args[0], args[1])
		3: _cb.rpc_id(peer_id, args[0], args[1], args[2])
		4: _cb.rpc_id(peer_id, args[0], args[1], args[2], args[3])
		5: _cb.rpc_id(peer_id, args[0], args[1], args[2], args[3], args[4])
		6: _cb.rpc_id(peer_id, args[0], args[1], args[2], args[3], args[4], args[5])
		7: _cb.rpc_id(peer_id, args[0], args[1], args[2], args[3], args[4], args[5], args[6])
		8: _cb.rpc_id(peer_id, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7])
		9: _cb.rpc_id(peer_id, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8])
		10: _cb.rpc_id(peer_id, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9])


func unbind(argcount: int) -> Callable:
	_cb = _cb.unbind(argcount)
	return _cb
