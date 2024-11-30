## A tuple implementation to hold two or many values
class_name GdUnitTuple
extends RefCounted

const NO_ARG :Variant = GdUnitConstants.NO_ARG

var __values :Array = Array()


func _init(arg0:Variant,
	arg1 :Variant=NO_ARG,
	arg2 :Variant=NO_ARG,
	arg3 :Variant=NO_ARG,
	arg4 :Variant=NO_ARG,
	arg5 :Variant=NO_ARG,
	arg6 :Variant=NO_ARG,
	arg7 :Variant=NO_ARG,
	arg8 :Variant=NO_ARG,
	arg9 :Variant=NO_ARG) -> void:
	__values = GdArrayTools.filter_value([arg0,arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9], NO_ARG)


func values() -> Array:
	return __values


func _to_string() -> String:
	return "tuple(%s)" % str(__values)
