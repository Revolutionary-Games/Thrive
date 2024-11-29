class_name Vector2Fuzzer
extends Fuzzer


var _from :Vector2
var _to : Vector2


func _init(from: Vector2, to: Vector2) -> void:
	assert(from <= to, "Invalid range!")
	_from = from
	_to = to


func next_value() -> Vector2:
	var x := randf_range(_from.x, _to.x)
	var y := randf_range(_from.y, _to.y)
	return Vector2(x, y)
