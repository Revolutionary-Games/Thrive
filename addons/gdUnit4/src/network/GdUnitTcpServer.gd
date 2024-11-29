@tool
class_name GdUnitTcpServer
extends Node

signal client_connected(client_id :int)
signal client_disconnected(client_id :int)
@warning_ignore("unused_signal")
signal rpc_data(rpc_data: RPC)

var _server :TCPServer


class TcpConnection extends Node:
	var _id :int
	# we do use untyped here because we using a mock for testing and the static type is break the mock
	@warning_ignore("untyped_declaration")
	var _stream
	var _readBuffer :String = ""


	@warning_ignore("unsafe_method_access")
	func _init(p_server :Variant) -> void:
		assert(p_server is TCPServer)
		_stream = p_server.take_connection()
		_stream.set_big_endian(true)
		_id = _stream.get_instance_id()
		rpc_send(RPCClientConnect.new().with_id(_id))


	func _ready() -> void:
		server().client_connected.emit(_id)


	func close() -> void:
		if _stream != null:
			@warning_ignore("unsafe_method_access")
			_stream.disconnect_from_host()
			_readBuffer = ""
			_stream = null
			queue_free()


	func id() -> int:
		return _id


	func server() -> GdUnitTcpServer:
		return get_parent()


	func rpc_send(p_rpc: RPC) -> void:
		@warning_ignore("unsafe_method_access")
		_stream.put_var(p_rpc.serialize(), true)


	func _process(_delta: float) -> void:
		@warning_ignore("unsafe_method_access")
		if _stream == null or _stream.get_status() != StreamPeerTCP.STATUS_CONNECTED:
			return
		receive_packages()


	@warning_ignore("unsafe_method_access")
	func receive_packages() -> void:
		var available_bytes :int = _stream.get_available_bytes()
		if available_bytes > 0:
			var partial_data :Array = _stream.get_partial_data(available_bytes)
			# Check for read error.
			if partial_data[0] != OK:
				push_error("Error getting data from stream: %s " % partial_data[0])
				return
			else:
				var received_data: PackedByteArray = partial_data[1]
				for package in _read_next_data_packages(received_data):
					var rpc_ := RPC.deserialize(package)
					if rpc_ is RPCClientDisconnect:
						close()
					server().rpc_data.emit(rpc_)


	func _read_next_data_packages(data_package: PackedByteArray) -> PackedStringArray:
		_readBuffer += data_package.get_string_from_utf8()
		var json_array := _readBuffer.split(GdUnitServerConstants.JSON_RESPONSE_DELIMITER)
		# We need to check if the current data is terminated by the delemiter (data packets can be split unspecifically).
		# If not, store the last part in _readBuffer and complete it on the next data packet that is received
		if not _readBuffer.ends_with(GdUnitServerConstants.JSON_RESPONSE_DELIMITER):
			_readBuffer = json_array[-1]
			json_array.remove_at(json_array.size()-1)
		else:
		# Reset the buffer if a completely terminated packet was received
			_readBuffer = ""
		# remove empty packages
		for index in json_array.size():
			if index < json_array.size() and json_array[index].is_empty():
				json_array.remove_at(index)
		return json_array


	func console(_message :String) -> void:
		#print_debug("TCP Connection:", _message)
		pass


@warning_ignore("return_value_discarded")
func _ready() -> void:
	_server = TCPServer.new()
	client_connected.connect(_on_client_connected)
	client_disconnected.connect(_on_client_disconnected)


func _notification(what: int) -> void:
	if what == NOTIFICATION_PREDELETE:
		stop()


func start() -> GdUnitResult:
	var server_port := GdUnitServerConstants.GD_TEST_SERVER_PORT
	var err := OK
	for retry in GdUnitServerConstants.DEFAULT_SERVER_START_RETRY_TIMES:
		err = _server.listen(server_port, "127.0.0.1")
		if err != OK:
			prints("GdUnit4: Can't establish server checked port: %d, Error: %s" % [server_port, error_string(err)])
			server_port += 1
			prints("GdUnit4: Retry (%d) ..." % retry)
		else:
			break
	if err != OK:
		if err == ERR_ALREADY_IN_USE:
			return GdUnitResult.error("GdUnit3: Can't establish server, the server is already in use. Error: %s, " % error_string(err))
		return GdUnitResult.error("GdUnit3: Can't establish server. Error: %s." % error_string(err))
	prints("GdUnit4: Test server successfully started checked port: %d" % server_port)
	return GdUnitResult.success(server_port)


func stop() -> void:
	if _server:
		_server.stop()
	for connection in get_children():
		if connection is TcpConnection:
			@warning_ignore("unsafe_method_access")
			connection.close()
			remove_child(connection)
	_server = null


func disconnect_client(client_id: int) -> void:
	client_disconnected.emit(client_id)


func _process(_delta: float) -> void:
	if _server != null and not _server.is_listening():
		return
	# check if connection is ready to be used
	if _server.is_connection_available():
		add_child(TcpConnection.new(_server))


func _on_client_connected(client_id: int) -> void:
	console("Client connected %d" % client_id)


@warning_ignore("unsafe_method_access")
func _on_client_disconnected(client_id: int) -> void:
	for connection in get_children():
		if connection is TcpConnection and connection.id() == client_id:
			connection.close()
			remove_child(connection)


func console(_message: String) -> void:
	#print_debug("TCP Server:", _message)
	pass
