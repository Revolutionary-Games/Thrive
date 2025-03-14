# GdUnit generated TestSuite
extends GdUnitTestSuite

# TestSuite generated from
const __source = 'res://addons/gdUnit4/src/network/GdUnitTcpServer.gd'

var tcp_server: GdUnitTcpServer
var tcp_client: GdUnitTcpClient


## We start a custom test server for this suite
func before() -> void:
	tcp_server = GdUnitTcpServer.new("Test TCP Server")
	tcp_client = GdUnitTcpClient.new("Test TCP Client", true)
	add_child(tcp_server)
	add_child(tcp_client)

	var result := tcp_server.start(62222)
	if not result.is_success():
		return

	var server_port: int = result.value()
	tcp_client.start("127.0.0.1", server_port)

	# wait until the client is connected
	for n in 200:
		await await_idle_frame()
		if tcp_client.is_client_connected():
			break


## Shutdown the test server
func after() -> void:
	tcp_client.stop()
	tcp_client.queue_free()
	await await_idle_frame()
	tcp_server.stop()
	tcp_server.queue_free()
	await await_idle_frame()


func test_receive_single_message() -> void:
	var signal_collector_ := signal_collector(tcp_server)
	await await_idle_frame()

	# send a single test message
	tcp_client.send(RPCMessage.of("Test Message"))
	await await_idle_frame()

	# expect the RPCMessage is received and emitted
	assert_bool(signal_collector_.is_emitted("rpc_data", [RPCMessage.of("Test Message")])).is_true()


func test_receive_multy_message() -> void:
	var signal_collector_ := signal_collector(tcp_server)
	await await_idle_frame()

	# send a two test message
	tcp_client.send(RPCMessage.of("Test Message A"))
	tcp_client.send(RPCMessage.of("Test Message B"))
	await await_idle_frame()

	# expect the RPCMessage is received and emitted
	assert_bool(signal_collector_.is_emitted("rpc_data", [RPCMessage.of("Test Message A")])).is_true()
	assert_bool(signal_collector_.is_emitted("rpc_data", [RPCMessage.of("Test Message B")])).is_true()


func add_data(package: StreamPeerBuffer, rpc_data: RPC) -> int:
	var buffer := rpc_data.serialize().to_utf8_buffer()
	var package_size := buffer.size()
	package.put_u32(0xDEADBEEF)
	package.put_u32(buffer.size())
	package.put_data(buffer)
	return package_size



# TODO refactor out and provide as public interface to can be reuse on other tests
class TestGdUnitSignalCollector:
	var _signalCollector: GdUnitSignalCollector
	var _emitter: Object


	func _init(emitter: Object) -> void:
		_emitter = emitter
		_signalCollector = GdUnitSignalCollector.new()
		_signalCollector.register_emitter(emitter)


	func is_emitted(signal_name: String, expected_args: Array) -> bool:
		return _signalCollector.match(_emitter, signal_name, expected_args)


	func _notification(what: int) -> void:
		if what == NOTIFICATION_PREDELETE:
			_signalCollector.unregister_emitter(_emitter)


func signal_collector(instance: Object) -> TestGdUnitSignalCollector:
	return TestGdUnitSignalCollector.new(instance)
