class_name RPCClientDisconnect
extends RPC

var _client_id :int


func with_id(p_client_id :int) -> RPCClientDisconnect:
	_client_id = p_client_id
	return self


func client_id() -> int:
	return _client_id
