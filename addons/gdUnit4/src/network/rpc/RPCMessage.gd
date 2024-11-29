class_name RPCMessage
extends RPC

var _message :String


static func of(p_message :String) -> RPCMessage:
	var rpc := RPCMessage.new()
	rpc._message = p_message
	return rpc


func message() -> String:
	return _message


func _to_string() -> String:
	return "RPCMessage: " + _message
