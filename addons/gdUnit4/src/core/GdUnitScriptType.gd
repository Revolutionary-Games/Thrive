class_name GdUnitScriptType
extends RefCounted

const UNKNOWN := ""
const CS := "cs"
const GD := "gd"


static func type_of(script :Script) -> String:
	if script == null:
		return UNKNOWN
	if GdObjects.is_gd_script(script):
		return GD
	if GdObjects.is_cs_script(script):
		return CS
	return UNKNOWN
