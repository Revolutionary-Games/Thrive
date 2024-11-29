class_name GdUnitContextMenuItem

enum MENU_ID {
	UNDEFINED = 0,
	TEST_RUN = 1000,
	TEST_DEBUG = 1001,
	TEST_RERUN = 1002,
	CREATE_TEST = 1010,
}

var id: MENU_ID = MENU_ID.UNDEFINED:
	set(value):
		id = value
	get:
		return id

var name: StringName:
	set(value):
		name = value
	get:
		return name

var command: GdUnitCommand:
	set(value):
		command = value
	get:
		return command

var visible: Callable:
	set(value):
		visible = value
	get:
		return visible

var icon: String:
	set(value):
		icon = value
	get:
		return icon


func _init(p_id: MENU_ID, p_name: StringName, p_icon :String, p_is_visible: Callable, p_command: GdUnitCommand) -> void:
	assert(p_id != null, "(%s) missing parameter 'MENU_ID'" % p_name)
	assert(p_is_visible != null, "(%s) missing parameter 'GdUnitCommand'" % p_name)
	assert(p_command != null, "(%s) missing parameter 'GdUnitCommand'" % p_name)
	self.id = p_id
	self.name = p_name
	self.icon = p_icon
	self.command = p_command
	self.visible = p_is_visible


func shortcut() -> Shortcut:
	return GdUnitCommandHandler.instance().get_shortcut(command.shortcut)


func is_enabled(script: Script) -> bool:
	return command.is_enabled.call(script)


func is_visible(script: Script) -> bool:
	return visible.call(script)


func execute(arguments:=[]) -> void:
	if arguments.is_empty():
		command.runnable.call()
	else:
		command.runnable.callv(arguments)
