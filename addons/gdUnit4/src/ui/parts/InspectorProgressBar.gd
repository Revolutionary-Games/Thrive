@tool
extends ProgressBar


@onready var status: Label = $Label
@onready var style: StyleBoxFlat = get("theme_override_styles/fill")


func _ready() -> void:
	style.bg_color = Color.DARK_GREEN
	value = 0
	max_value = 0
	update_text()

func update_text() -> void:
	status.text = "%d:%d" % [value, max_value]


func _on_test_counter_changed(index: int, total: int, state: GdUnitInspectorTreeConstants.STATE) -> void:
	value = index
	max_value = total
	# inital state
	if index == 0:
		style.bg_color = Color.DARK_GREEN
	if is_flaky(state):
		style.bg_color = Color.WEB_GREEN
	if is_failed(state):
		style.bg_color = Color.DARK_RED
	update_text()


func is_failed(state: GdUnitInspectorTreeConstants.STATE) -> bool:
	return state in [
		GdUnitInspectorTreeConstants.STATE.FAILED,
		GdUnitInspectorTreeConstants.STATE.ERROR,
		GdUnitInspectorTreeConstants.STATE.ABORDED]


func is_flaky(state: GdUnitInspectorTreeConstants.STATE) -> bool:
	return state == GdUnitInspectorTreeConstants.STATE.FLAKY
