extends GdUnitSignalAssert

const DEFAULT_TIMEOUT := 2000

var _signal_collector :GdUnitSignalCollector
var _emitter :Object
var _current_failure_message :String = ""
var _custom_failure_message :String = ""
var _additional_failure_message: String = ""
var _line_number := -1
var _timeout := DEFAULT_TIMEOUT
var _interrupted := false


func _init(emitter :Object) -> void:
	# save the actual assert instance on the current thread context
	var context := GdUnitThreadManager.get_current_context()
	context.set_assert(self)
	_signal_collector = context.get_signal_collector()
	_line_number = GdUnitAssertions.get_line_number()
	_emitter =  emitter
	GdAssertReports.reset_last_error_line_number()


func _notification(what :int) -> void:
	if what == NOTIFICATION_PREDELETE:
		_interrupted = true
		if is_instance_valid(_emitter):
			_signal_collector.unregister_emitter(_emitter)
		_emitter = null


func report_success() -> GdUnitAssert:
	GdAssertReports.report_success()
	return self


func report_warning(message :String) -> GdUnitAssert:
	GdAssertReports.report_warning(message, GdUnitAssertions.get_line_number())
	return self


func report_error(failure :String) -> GdUnitAssert:
	_current_failure_message = GdAssertMessages.build_failure_message(failure, _additional_failure_message, _custom_failure_message)
	GdAssertReports.report_error(_current_failure_message, _line_number)
	return self


func failure_message() -> String:
	return _current_failure_message


func override_failure_message(message :String) -> GdUnitSignalAssert:
	_custom_failure_message = message
	return self


func append_failure_message(message :String) -> GdUnitSignalAssert:
	_additional_failure_message = message
	return self


func wait_until(timeout := 2000) -> GdUnitSignalAssert:
	if timeout <= 0:
		@warning_ignore("return_value_discarded")
		report_warning("Invalid timeout parameter, allowed timeouts must be greater than 0, use default timeout instead!")
		_timeout = DEFAULT_TIMEOUT
	else:
		_timeout = timeout
	return self


# Verifies the signal exists checked the emitter
func is_signal_exists(signal_name :String) -> GdUnitSignalAssert:
	if not _emitter.has_signal(signal_name):
		@warning_ignore("return_value_discarded")
		report_error("The signal '%s' not exists checked object '%s'." % [signal_name, _emitter.get_class()])
	return self


# Verifies that given signal is emitted until waiting time
func is_emitted(name :String, args := []) -> GdUnitSignalAssert:
	_line_number = GdUnitAssertions.get_line_number()
	return await _wail_until_signal(name, args, false)


# Verifies that given signal is NOT emitted until waiting time
func is_not_emitted(name :String, args := []) -> GdUnitSignalAssert:
	_line_number = GdUnitAssertions.get_line_number()
	return await _wail_until_signal(name, args, true)


func _wail_until_signal(signal_name :String, expected_args :Array, expect_not_emitted: bool) -> GdUnitSignalAssert:
	if _emitter == null:
		return report_error("Can't wait for signal checked a NULL object.")
	# first verify the signal is defined
	if not _emitter.has_signal(signal_name):
		return report_error("Can't wait for non-existion signal '%s' checked object '%s'." % [signal_name,_emitter.get_class()])
	_signal_collector.register_emitter(_emitter)
	var time_scale := Engine.get_time_scale()
	var timer := Timer.new()
	(Engine.get_main_loop() as SceneTree).root.add_child(timer)
	timer.add_to_group("GdUnitTimers")
	timer.set_one_shot(true)
	@warning_ignore("return_value_discarded")
	timer.timeout.connect(func on_timeout() -> void: _interrupted = true)
	timer.start((_timeout/1000.0)*time_scale)
	var is_signal_emitted := false
	while not _interrupted and not is_signal_emitted:
		await (Engine.get_main_loop() as SceneTree).process_frame
		if is_instance_valid(_emitter):
			is_signal_emitted = _signal_collector.match(_emitter, signal_name, expected_args)
			if is_signal_emitted and expect_not_emitted:
				@warning_ignore("return_value_discarded")
				report_error(GdAssertMessages.error_signal_emitted(signal_name, expected_args, LocalTime.elapsed(int(_timeout-timer.time_left*1000))))

	if _interrupted and not expect_not_emitted:
		@warning_ignore("return_value_discarded")
		report_error(GdAssertMessages.error_wait_signal(signal_name, expected_args, LocalTime.elapsed(_timeout)))
	timer.free()
	if is_instance_valid(_emitter):
		_signal_collector.reset_received_signals(_emitter, signal_name, expected_args)
	return self
