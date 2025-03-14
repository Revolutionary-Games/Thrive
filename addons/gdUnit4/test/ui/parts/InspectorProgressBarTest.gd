extends GdUnitTestSuite


const InspectorProgressBar := preload("res://addons/gdUnit4/src/ui/parts/InspectorProgressBar.gd")


var _progress: InspectorProgressBar
var _status: Label
var _style: StyleBoxFlat


func before_test() -> void:
	@warning_ignore("unsafe_method_access")
	_progress = load('res://addons/gdUnit4/src/ui/parts/InspectorProgressBar.tscn').instantiate()
	add_child(_progress)

	_status = _progress.status
	_style = _progress.style


func test_progress_init() -> void:
	assert_that(_progress.value).is_equal(0.000000)
	assert_that(_progress.max_value).is_equal(0.000000)
	assert_that(_status.text).is_equal("0:0")


@warning_ignore("unused_parameter")
func test_progress_on_test_counter_changed(index: int, total_count: int, state: GdUnitInspectorTreeConstants.STATE, expected_color :Color, test_parameters := [
	[0, 0, GdUnitInspectorTreeConstants.STATE.INITIAL, Color.DARK_GREEN],
	[1, 2, GdUnitInspectorTreeConstants.STATE.SUCCESS, Color.DARK_GREEN],
	[2, 2, GdUnitInspectorTreeConstants.STATE.WARNING, Color.DARK_GREEN],
	[3, 5, GdUnitInspectorTreeConstants.STATE.RUNNING, Color.DARK_GREEN],
	[4, 5, GdUnitInspectorTreeConstants.STATE.FAILED, Color.DARK_RED],
	[5, 5, GdUnitInspectorTreeConstants.STATE.ERROR, Color.DARK_RED],
]) -> void:

	_progress._on_test_counter_changed(index, total_count, state)
	assert_float(_progress.value).is_equal(index as float)
	assert_float(_progress.max_value).is_equal(total_count as float)
	assert_str(_status.text).is_equal("%d:%d" % [index, total_count])
	assert_that(_style.bg_color).is_equal(expected_color)
