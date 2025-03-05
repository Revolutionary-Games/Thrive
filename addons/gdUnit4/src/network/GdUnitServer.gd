@tool
extends Node

@onready var _server :GdUnitTcpServer = $TcpServer


@warning_ignore("return_value_discarded")
func _ready() -> void:
	var result := _server.start()
	if result.is_error():
		push_error(result.error_message())
		return
	var server_port :int = result.value()
	Engine.set_meta("gdunit_server_port", server_port)
	_server.client_connected.connect(_on_client_connected)
	_server.client_disconnected.connect(_on_client_disconnected)
	_server.rpc_data.connect(_receive_rpc_data)
	GdUnitCommandHandler.instance().gdunit_runner_stop.connect(_on_gdunit_runner_stop)


func _on_client_connected(client_id: int) -> void:
	GdUnitSignals.instance().gdunit_client_connected.emit(client_id)


func _on_client_disconnected(client_id: int) -> void:
	GdUnitSignals.instance().gdunit_client_disconnected.emit(client_id)


func _on_gdunit_runner_stop(client_id: int) -> void:
	if _server:
		_server.disconnect_client(client_id)


func _receive_rpc_data(p_rpc: RPC) -> void:
	if p_rpc is RPCMessage:
		var rpc_message: RPCMessage = p_rpc
		GdUnitSignals.instance().gdunit_message.emit(rpc_message.message())
		return
	if p_rpc is RPCGdUnitEvent:
		var rpc_event: RPCGdUnitEvent = p_rpc
		GdUnitSignals.instance().gdunit_event.emit(rpc_event.event())
		return
