class_name Vector3Fuzzer
extends Fuzzer


var _from :Vector3
var _to : Vector3


func _init(from: Vector3, to: Vector3) -> void:
	assert(from <= to, "Invalid range!")
	_from = from
	_to = to


func next_value() -> Vector3:
	var x := randf_range(_from.x, _to.x)
	var y := randf_range(_from.y, _to.y)
	var z := randf_range(_from.z, _to.z)
	return Vector3(x, y, z)
