class_name GdFunctionDoubler
extends RefCounted

const DEFAULT_TYPED_RETURN_VALUES := {
	TYPE_NIL: "null",
	TYPE_BOOL: "false",
	TYPE_INT: "0",
	TYPE_FLOAT: "0.0",
	TYPE_STRING: "\"\"",
	TYPE_STRING_NAME: "&\"\"",
	TYPE_VECTOR2: "Vector2.ZERO",
	TYPE_VECTOR2I: "Vector2i.ZERO",
	TYPE_RECT2: "Rect2()",
	TYPE_RECT2I: "Rect2i()",
	TYPE_VECTOR3: "Vector3.ZERO",
	TYPE_VECTOR3I: "Vector3i.ZERO",
	TYPE_VECTOR4: "Vector4.ZERO",
	TYPE_VECTOR4I: "Vector4i.ZERO",
	TYPE_TRANSFORM2D: "Transform2D()",
	TYPE_PLANE: "Plane()",
	TYPE_QUATERNION: "Quaternion()",
	TYPE_AABB: "AABB()",
	TYPE_BASIS: "Basis()",
	TYPE_TRANSFORM3D: "Transform3D()",
	TYPE_PROJECTION: "Projection()",
	TYPE_COLOR: "Color()",
	TYPE_NODE_PATH: "NodePath()",
	TYPE_RID: "RID()",
	TYPE_OBJECT: "null",
	TYPE_CALLABLE: "Callable()",
	TYPE_SIGNAL: "Signal()",
	TYPE_DICTIONARY: "Dictionary()",
	TYPE_ARRAY: "Array()",
	TYPE_PACKED_BYTE_ARRAY: "PackedByteArray()",
	TYPE_PACKED_INT32_ARRAY: "PackedInt32Array()",
	TYPE_PACKED_INT64_ARRAY: "PackedInt64Array()",
	TYPE_PACKED_FLOAT32_ARRAY: "PackedFloat32Array()",
	TYPE_PACKED_FLOAT64_ARRAY: "PackedFloat64Array()",
	TYPE_PACKED_STRING_ARRAY: "PackedStringArray()",
	TYPE_PACKED_VECTOR2_ARRAY: "PackedVector2Array()",
	TYPE_PACKED_VECTOR3_ARRAY: "PackedVector3Array()",
	# since Godot 4.3.beta1 TYPE_PACKED_VECTOR4_ARRAY = 38
	GdObjects.TYPE_PACKED_VECTOR4_ARRAY: "PackedVector4Array()",
	TYPE_PACKED_COLOR_ARRAY: "PackedColorArray()",
	GdObjects.TYPE_VARIANT: "null",
	GdObjects.TYPE_ENUM: "0"
}

# @GlobalScript enums
# needs to manually map because of https://github.com/godotengine/godot/issues/73835
const DEFAULT_ENUM_RETURN_VALUES = {
	"Side" : "SIDE_LEFT",
	"Corner" : "CORNER_TOP_LEFT",
	"Orientation" : "HORIZONTAL",
	"ClockDirection" : "CLOCKWISE",
	"HorizontalAlignment" : "HORIZONTAL_ALIGNMENT_LEFT",
	"VerticalAlignment" : "VERTICAL_ALIGNMENT_TOP",
	"InlineAlignment" : "INLINE_ALIGNMENT_TOP_TO",
	"EulerOrder" : "EULER_ORDER_XYZ",
	"Key" : "KEY_NONE",
	"KeyModifierMask" : "KEY_CODE_MASK",
	"MouseButton" : "MOUSE_BUTTON_NONE",
	"MouseButtonMask" : "MOUSE_BUTTON_MASK_LEFT",
	"JoyButton" : "JOY_BUTTON_INVALID",
	"JoyAxis" : "JOY_AXIS_INVALID",
	"MIDIMessage" : "MIDI_MESSAGE_NONE",
	"Error" : "OK",
	"PropertyHint" : "PROPERTY_HINT_NONE",
	"Variant.Type" : "TYPE_NIL",
}

var _push_errors :String


# Determine the enum default by reflection
static func get_enum_default(value :String) -> Variant:
	var script := GDScript.new()
	script.source_code = """
	extends Resource

	static func get_enum_default() -> Variant:
		return %s.values()[0]

	""".dedent() % value
	@warning_ignore("return_value_discarded")
	script.reload()
	@warning_ignore("unsafe_method_access")
	return script.new().call("get_enum_default")


static func default_return_value(func_descriptor :GdFunctionDescriptor) -> String:
	var return_type :Variant = func_descriptor.return_type()
	if return_type == GdObjects.TYPE_ENUM:
		var enum_class := func_descriptor._return_class
		var enum_path := enum_class.split(".")
		if enum_path.size() >= 2:
			var keys := ClassDB.class_get_enum_constants(enum_path[0], enum_path[1])
			if not keys.is_empty():
				return "%s.%s" % [enum_path[0], keys[0]]
			var enum_value :Variant = get_enum_default(enum_class)
			if enum_value != null:
				return str(enum_value)
		# we need fallback for @GlobalScript enums,
		return DEFAULT_ENUM_RETURN_VALUES.get(func_descriptor._return_class, "0")
	return DEFAULT_TYPED_RETURN_VALUES.get(return_type, "invalid")


func _init(push_errors :bool = false) -> void:
	_push_errors = "true" if push_errors else "false"
	for type_key in TYPE_MAX:
		if not DEFAULT_TYPED_RETURN_VALUES.has(type_key):
			push_error("missing default definitions! Expexting %d bud is %d" % [DEFAULT_TYPED_RETURN_VALUES.size(), TYPE_MAX])
			prints("missing default definition for type", type_key)
			assert(DEFAULT_TYPED_RETURN_VALUES.has(type_key), "Missing Type default definition!")


@warning_ignore("unused_parameter")
func get_template(return_type: GdFunctionDescriptor, is_callable: bool) -> String:
	assert(false, "'get_template' must be implemented!")
	return ""


func double(func_descriptor: GdFunctionDescriptor, is_callable: bool = false) -> PackedStringArray:
	var is_coroutine := func_descriptor.is_coroutine()
	var func_name := func_descriptor.name()
	var args := func_descriptor.args()
	var varargs := func_descriptor.varargs()
	var return_value := GdFunctionDoubler.default_return_value(func_descriptor)
	var arg_names := extract_arg_names(args, true)
	var vararg_names := extract_arg_names(varargs)

	# save original constructor arguments
	if func_name == "_init":
		var constructor_args := ",".join(GdFunctionDoubler.extract_constructor_args(args))
		var constructor := """
			func _init(%s) -> void:
				Engine.set_meta(__INSTANCE_ID, self)
				@warning_ignore("unsafe_call_argument")
				super(%s)

			""".dedent() % [constructor_args, ", ".join(arg_names)]
		return constructor.split("\n")

	var double_src := "@warning_ignore('shadowed_variable', 'untyped_declaration')\n"
	if func_descriptor.is_engine():
		double_src += '@warning_ignore("native_method_override")\n'
	double_src += GdFunctionDoubler.extract_func_signature(func_descriptor)
	# fix to  unix format, this is need when the template is edited under windows than the template is stored with \r\n
	var func_template := get_template(func_descriptor, is_callable).replace("\r\n", "\n")
	double_src += func_template\
		.replace("$(arguments)", ", ".join(arg_names))\
		.replace("$(varargs)", ", ".join(vararg_names))\
		.replace("$(await)", GdFunctionDoubler.await_is_coroutine(is_coroutine)) \
		.replace("$(func_name)", func_name )\
		.replace("${default_return_value}", return_value)\
		.replace("$(push_errors)", _push_errors)

	if func_descriptor.return_type() == GdObjects.TYPE_ENUM:
		double_src = double_src.replace("$(return_as)", " as " + func_descriptor.return_type_as_string())
	else:
		double_src = double_src.replace("$(return_as)", "")

	return double_src.split("\n")


func extract_arg_names(argument_signatures: Array[GdFunctionArgument], add_suffix := false) -> PackedStringArray:
	var arg_names := PackedStringArray()
	for arg in argument_signatures:
		@warning_ignore("return_value_discarded")
		arg_names.append(arg._name + ("_" if add_suffix else ""))
	return arg_names


static func extract_constructor_args(args :Array[GdFunctionArgument]) -> PackedStringArray:
	var constructor_args := PackedStringArray()
	for arg in args:
		var arg_name := arg._name + "_"
		var default_value := get_default(arg)
		if default_value == "null":
			@warning_ignore("return_value_discarded")
			constructor_args.append(arg_name + ":Variant=" + default_value)
		else:
			@warning_ignore("return_value_discarded")
			constructor_args.append(arg_name + ":=" + default_value)
	return constructor_args


static func extract_func_signature(descriptor: GdFunctionDescriptor) -> String:
	var func_signature := ""
	if descriptor._return_type == TYPE_NIL:
		func_signature = "func %s(%s) -> void:" % [descriptor.name(), typeless_args(descriptor)]
	elif descriptor._return_type == GdObjects.TYPE_VARIANT:
		func_signature = "func %s(%s):" % [descriptor.name(), typeless_args(descriptor)]
	else:
		func_signature = "func %s(%s) -> %s:" % [descriptor.name(), typeless_args(descriptor), descriptor.return_type_as_string()]
	return "static " + func_signature if descriptor.is_static() else func_signature


static func typeless_args(descriptor: GdFunctionDescriptor) -> String:
	var collect := PackedStringArray()
	for arg in descriptor.args():
		if arg.has_default():
			# For Variant types we need to enforce the type in the signature
			if arg.type() == GdObjects.TYPE_VARIANT:
				collect.push_back("%s_:%s=%s" % [arg.name(), GdObjects.type_as_string(arg.type()), arg.value_as_string()])
			else:
				@warning_ignore("return_value_discarded")
				collect.push_back("%s_=%s" % [arg.name(), arg.value_as_string()])
		else:
			@warning_ignore("return_value_discarded")
			collect.push_back(arg.name() + "_")
	for arg in descriptor.varargs():
		@warning_ignore("return_value_discarded")
		collect.push_back(arg.name() + "=" + arg.value_as_string())
	return ", ".join(collect)


static func get_default(arg :GdFunctionArgument) -> String:
	if arg.has_default():
		return arg.value_as_string()
	else:
		return DEFAULT_TYPED_RETURN_VALUES.get(arg.type(), "null")


static func await_is_coroutine(is_coroutine :bool) -> String:
	return "await " if is_coroutine else ""
