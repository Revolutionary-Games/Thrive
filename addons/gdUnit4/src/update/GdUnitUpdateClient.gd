@tool
extends Node

signal request_completed(response :HttpResponse)

class HttpResponse:
	var _code :int
	var _body :PackedByteArray


	func _init(code_ :int, body_ :PackedByteArray) -> void:
		_code = code_
		_body = body_

	func code() -> int:
		return _code

	func response() -> Variant:
		var test_json_conv := JSON.new()
		@warning_ignore("return_value_discarded")
		test_json_conv.parse(_body.get_string_from_utf8())
		return test_json_conv.get_data()

	func body() -> PackedByteArray:
		return _body

var _http_request :HTTPRequest = HTTPRequest.new()


func _ready() -> void:
	add_child(_http_request)
	@warning_ignore("return_value_discarded")
	_http_request.request_completed.connect(_on_request_completed)


func _notification(what :int) -> void:
	if what == NOTIFICATION_PREDELETE:
		if is_instance_valid(_http_request):
			_http_request.queue_free()


#func list_tags() -> void:
#	_http_request.connect("request_completed",Callable(self,"_response_request_tags"))
#	var error = _http_request.request("https://api.github.com/repos/MikeSchulze/gdUnit4/tags")
#	if error != OK:
#		push_error("An error occurred in the HTTP request.")


func request_latest_version() -> HttpResponse:
	var error := _http_request.request("https://api.github.com/repos/MikeSchulze/gdUnit4/tags")
	if error != OK:
		var message := "request_latest_version failed: %d" % error
		return HttpResponse.new(error, message.to_utf8_buffer())
	return await self.request_completed


func request_releases() -> HttpResponse:
	var error := _http_request.request("https://api.github.com/repos/MikeSchulze/gdUnit4/releases")
	if error != OK:
		var message := "request_releases failed: %d" % error
		return HttpResponse.new(error, message.to_utf8_buffer())
	return await self.request_completed


func request_image(url :String) -> HttpResponse:
	var error := _http_request.request(url)
	if error != OK:
		var message := "request_image failed: %d" % error
		return HttpResponse.new(error, message.to_utf8_buffer())
	return await self.request_completed


func request_zip_package(url :String, file :String) -> HttpResponse:
	_http_request.set_download_file(file)
	var error := _http_request.request(url)
	if error != OK:
		var message := "request_zip_package failed: %d" % error
		return HttpResponse.new(error, message.to_utf8_buffer())
	return await self.request_completed


func _on_request_completed(_result :int, response_code :int, _headers :PackedStringArray, body :PackedByteArray) -> void:
	if _http_request.get_http_client_status() != HTTPClient.STATUS_DISCONNECTED:
		_http_request.set_download_file("")
	request_completed.emit(HttpResponse.new(response_code, body))
