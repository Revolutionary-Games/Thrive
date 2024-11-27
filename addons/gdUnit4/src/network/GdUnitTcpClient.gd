class_name GdUnitTcpClient
extends Node

signal connection_succeeded(message :String)
signal connection_failed(message :String)


var _host :String
var _port :int
var _client_id :int
var _connected :bool
var _stream :StreamPeerTCP


func _ready() -> void:
	_connected = false
	_stream = StreamPeerTCP.new()
	_stream.set_big_endian(true)


func stop() -> void:
	console("Client: disconnect from server")
	if _stream != null:
		rpc_send(RPCClientDisconnect.new().with_id(_client_id))
	if _stream != null:
		_stream.disconnect_from_host()
	_connected = false


func start(host :String, port :int) -> GdUnitResult:
	_host = host
	_port = port
	if _connected:
		return GdUnitResult.warn("Client already connected ... %s:%d" % [_host, _port])

	# Connect client to server
	if _stream.get_status() != StreamPeerTCP.STATUS_CONNECTED:
		var err := _stream.connect_to_host(host, port)
		#prints("connect_to_host", host, port, err)
		if err != OK:
			return GdUnitResult.error("GdUnit3: Can't establish client, error code: %s" % err)
	return GdUnitResult.success("GdUnit3: Client connected checked port %d" % port)


func _process(_delta :float) -> void:
	match _stream.get_status():
		StreamPeerTCP.STATUS_NONE:
			return

		StreamPeerTCP.STATUS_CONNECTING:
			set_process(false)
			# wait until client is connected to server
			for retry in 10:
				@warning_ignore("return_value_discarded")
				_stream.poll()
				console("wait to connect ..")
				if _stream.get_status() == StreamPeerTCP.STATUS_CONNECTING:
					await get_tree().create_timer(0.500).timeout
				if _stream.get_status() == StreamPeerTCP.STATUS_CONNECTED:
					set_process(true)
					return
			set_process(true)
			_stream.disconnect_from_host()
			console("connection failed")
			connection_failed.emit("Connect to TCP Server %s:%d faild!" % [_host, _port])

		StreamPeerTCP.STATUS_CONNECTED:
			if not _connected:
				var rpc_ :RPC = null
				set_process(false)
				while rpc_ == null:
					await get_tree().create_timer(0.500).timeout
					rpc_ = rpc_receive()
				set_process(true)
				_client_id = (rpc_ as RPCClientConnect).client_id()
				console("Connected to Server: %d" % _client_id)
				connection_succeeded.emit("Connect to TCP Server %s:%d success." % [_host, _port])
				_connected = true
			process_rpc()

		StreamPeerTCP.STATUS_ERROR:
			console("connection failed")
			_stream.disconnect_from_host()
			connection_failed.emit("Connect to TCP Server %s:%d faild!" % [_host, _port])
			return


func is_client_connected() -> bool:
	return _connected


func process_rpc() -> void:
	if _stream.get_available_bytes() > 0:
		var rpc_ := rpc_receive()
		if rpc_ is RPCClientDisconnect:
			stop()


func rpc_send(p_rpc :RPC) -> void:
	if _stream != null:
		var data := GdUnitServerConstants.JSON_RESPONSE_DELIMITER + p_rpc.serialize() + GdUnitServerConstants.JSON_RESPONSE_DELIMITER
		@warning_ignore("return_value_discarded")
		_stream.put_data(data.to_utf8_buffer())


func rpc_receive() -> RPC:
	if _stream != null:
		while _stream.get_available_bytes() > 0:
			var available_bytes := _stream.get_available_bytes()
			var data := _stream.get_data(available_bytes)
			var received_data: PackedByteArray = data[1]
			# data send by Godot has this magic header of 12 bytes
			var header := Array(received_data.slice(0, 4))
			if header == [0, 0, 0, 124]:
				received_data = received_data.slice(12, available_bytes)
			var decoded := received_data.get_string_from_utf8()
			if decoded == "":
				#prints("decoded is empty", available_bytes, received_data.get_string_from_utf8())
				return null
			return RPC.deserialize(decoded)
	return null


func console(_message :String) -> void:
	#prints("TCP Client:", _message)
	pass


func _on_connection_failed(message :String) -> void:
	console("connection faild: " + message)


func _on_connection_succeeded(message :String) -> void:
	console("connected: " + message)
