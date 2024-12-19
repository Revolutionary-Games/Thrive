class_name FloatFuzzer
extends Fuzzer

var _from: float = 0
var _to: float = 0

func _init(from: float, to: float) -> void:
	assert(from <= to, "Invalid range!")
	_from = from
	_to = to

func next_value() -> float:
	return randf_range(_from, _to)
