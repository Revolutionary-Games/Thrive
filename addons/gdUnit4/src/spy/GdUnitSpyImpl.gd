class_name DoubledSpyClassSourceClassName

const __INSTANCE_ID = "${instance_id}"
const __SOURCE_CLASS = "${source_class}"


class SpyState:
	var instance_delegator :Object
	var excluded_methods :PackedStringArray = []


	func call_func(func_name: String, arguments: Array) -> Variant:
		return instance_delegator.callv(func_name, arguments)


@warning_ignore("unused_private_class_variable")
var __verifier_instance := GdUnitObjectInteractionsVerifier.new()
var __spying_state := SpyState.new()


func __init(__delegator: Object, __exluded_methods := PackedStringArray()) -> void:
	# store self need to access static functions
	Engine.set_meta(__INSTANCE_ID, self)
	__spying_state.instance_delegator = __delegator
	__spying_state.excluded_methods = __exluded_methods


func __release_double() -> void:
	# we need to release the self reference manually to prevent orphan nodes
	Engine.remove_meta(__INSTANCE_ID)
	__spying_state.instance_delegator = null


func _notification(what: int) -> void:
	if what == NOTIFICATION_PREDELETE:
		if Engine.has_meta(__INSTANCE_ID):
			Engine.remove_meta(__INSTANCE_ID)


static func __get_verifier() -> GdUnitObjectInteractionsVerifier:
	var __instance := __get_instance()
	@warning_ignore("unsafe_property_access")
	return null if __instance == null else __instance.__verifier_instance


static func __spy_state() -> SpyState:
	@warning_ignore("unsafe_property_access")
	return __get_instance().__spying_state


static func __get_instance() -> Object:
	return null if not Engine.has_meta(__INSTANCE_ID) else Engine.get_meta(__INSTANCE_ID)


func __instance_id() -> String:
	return __INSTANCE_ID


static func __do_call_real_func(__func_name: String) -> bool:
	return not __spy_state().excluded_methods.has(__func_name)


static func __call_func(__func_name: String, __arguments: Array) -> Variant:
	return __spy_state().call_func(__func_name, __arguments)
