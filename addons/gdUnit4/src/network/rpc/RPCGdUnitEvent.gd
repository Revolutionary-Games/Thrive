class_name RPCGdUnitEvent
extends RPC

var _event :Dictionary


static func of(p_event :GdUnitEvent) -> RPCGdUnitEvent:
	var rpc := RPCGdUnitEvent.new()
	rpc._event = p_event.serialize()
	return rpc


func event() -> GdUnitEvent:
	return GdUnitEvent.new().deserialize(_event)


func _to_string() -> String:
	return "RPCGdUnitEvent: " + str(_event)
