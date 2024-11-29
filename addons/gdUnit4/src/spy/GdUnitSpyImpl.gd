
const __INSTANCE_ID = "${instance_id}"
const __SOURCE_CLASS = "${source_class}"

var __instance_delegator :Object
var __excluded_methods :PackedStringArray = []


static func __instance() -> Variant:
	return Engine.get_meta(__INSTANCE_ID)


func _notification(what :int) -> void:
	if what == NOTIFICATION_PREDELETE:
		if Engine.has_meta(__INSTANCE_ID):
			Engine.remove_meta(__INSTANCE_ID)


func __instance_id() -> String:
	return __INSTANCE_ID


func __set_singleton(delegator :Object) -> void:
	# store self need to mock static functions
	Engine.set_meta(__INSTANCE_ID, self)
	__instance_delegator = delegator


func __release_double() -> void:
	# we need to release the self reference manually to prevent orphan nodes
	Engine.remove_meta(__INSTANCE_ID)
	__instance_delegator = null


func __do_call_real_func(func_name :String) -> bool:
	return not __excluded_methods.has(func_name)


func __exclude_method_call(exluded_methods :PackedStringArray) -> void:
	__excluded_methods.append_array(exluded_methods)


func __call_func(func_name :String, arguments :Array) -> Variant:
	return __instance_delegator.callv(func_name, arguments)
