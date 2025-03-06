# GdUnit generated TestSuite
#warning-ignore-all:unused_argument
#warning-ignore-all:return_value_discarded
class_name GdUnitScriptTypeTest
extends GdUnitTestSuite

# TestSuite generated from
const __source = 'res://addons/gdUnit4/src/core/GdUnitScriptType.gd'


func test_type_of() -> void:
	assert_str(GdUnitScriptType.type_of(null)).is_equal(GdUnitScriptType.UNKNOWN)
	@warning_ignore("unsafe_cast")
	assert_str(GdUnitScriptType.type_of(ClassDB.instantiate("GDScript") as Script)).is_equal(GdUnitScriptType.GD)
	#if GdUnit4CSharpApiLoader.is_mono_supported():
	#	assert_str(GdUnitScriptType.type_of(ClassDB.instantiate("CSharpScript"))).is_equal(GdUnitScriptType.CS)
	#assert_str(GdUnitScriptType.type_of(ClassDB.instantiate("VisualScript"))).is_equal(GdUnitScriptType.VS)
	#assert_str(GdUnitScriptType.type_of(ClassDB.instantiate("NativeScript"))).is_equal(GdUnitScriptType.NATIVE)
