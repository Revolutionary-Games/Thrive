@tool
extends ConfirmationDialog

const GdUnitTools := preload("res://addons/gdUnit4/src/core/GdUnitTools.gd")
const GdUnitUpdateClient := preload("res://addons/gdUnit4/src/update/GdUnitUpdateClient.gd")

@onready var _progress_content :RichTextLabel = %message
@onready var _progress_bar :TextureProgressBar = %progress


var _debug_mode := false
var _update_client :GdUnitUpdateClient
var _download_url :String


func _ready() -> void:
	message_h4("Press 'Update' to start!", Color.GREEN)
	init_progress(5)


func _process(_delta :float) -> void:
	if _progress_content != null and _progress_content.is_visible_in_tree():
		_progress_content.queue_redraw()


func init_progress(max_value : int) -> void:
	_progress_bar.max_value = max_value
	_progress_bar.value = 1


func setup(update_client :GdUnitUpdateClient, download_url :String) -> void:
	_update_client = update_client
	_download_url = download_url


func update_progress(message :String) -> void:
	message_h4(message, Color.GREEN)
	_progress_bar.value += 1
	if _debug_mode:
		await get_tree().create_timer(3).timeout
	await get_tree().create_timer(.2).timeout


func _colored(message :String, color :Color) -> String:
	return "[color=#%s]%s[/color]" % [color.to_html(), message]


func message_h4(message :String, color :Color) -> void:
	_progress_content.clear()
	_progress_content.append_text("[font_size=16]%s[/font_size]" % _colored(message, color))


@warning_ignore("return_value_discarded")
func run_update() -> void:
	get_cancel_button().disabled = true
	get_ok_button().disabled = true

	await update_progress("Download Release ... [img=24x24]%s[/img]" % GdUnitUiTools.get_spinner())
	await download_release()
	await update_progress("Extract update ... [img=24x24]%s[/img]" % GdUnitUiTools.get_spinner())
	var zip_file := temp_dir() + "/update.zip"
	var tmp_path := create_temp_dir("update")
	var result :Variant = extract_zip(zip_file, tmp_path)
	if result == null:
		await update_progress("Update failed!")
		await get_tree().create_timer(3).timeout
		queue_free()
		return

	await update_progress("Uninstall GdUnit4 ... [img=24x24]%s[/img]" % GdUnitUiTools.get_spinner())
	disable_gdUnit()
	if not _debug_mode:
		delete_directory("res://addons/gdUnit4/")
	# give editor time to react on deleted files
	await get_tree().create_timer(1).timeout

	await update_progress("Install new GdUnit4 version ...")
	if _debug_mode:
		copy_directory(tmp_path, "res://debug")
	else:
		copy_directory(tmp_path, "res://")

	await update_progress("New GdUnit version successfully installed, Restarting Godot ...")
	await get_tree().create_timer(3).timeout
	enable_gdUnit()
	hide()
	delete_directory("res://addons/.gdunit_update")
	restart_godot()


func restart_godot() -> void:
	prints("Force restart Godot")
	EditorInterface.restart_editor(true)


@warning_ignore("return_value_discarded")
func enable_gdUnit() -> void:
	var enabled_plugins := PackedStringArray()
	if ProjectSettings.has_setting("editor_plugins/enabled"):
		enabled_plugins = ProjectSettings.get_setting("editor_plugins/enabled")
	if not enabled_plugins.has("res://addons/gdUnit4/plugin.cfg"):
		enabled_plugins.append("res://addons/gdUnit4/plugin.cfg")
	ProjectSettings.set_setting("editor_plugins/enabled", enabled_plugins)
	ProjectSettings.save()


func disable_gdUnit() -> void:
	EditorInterface.set_plugin_enabled("gdUnit4", false)


const GDUNIT_TEMP := "user://tmp"

func temp_dir() -> String:
	if not DirAccess.dir_exists_absolute(GDUNIT_TEMP):
		@warning_ignore("return_value_discarded")
		DirAccess.make_dir_recursive_absolute(GDUNIT_TEMP)
	return GDUNIT_TEMP


func create_temp_dir(folder_name :String) -> String:
	var new_folder := temp_dir() + "/" + folder_name
	delete_directory(new_folder)
	if not DirAccess.dir_exists_absolute(new_folder):
		@warning_ignore("return_value_discarded")
		DirAccess.make_dir_recursive_absolute(new_folder)
	return new_folder


func delete_directory(path :String, only_content := false) -> void:
	var dir := DirAccess.open(path)
	if dir != null:
		@warning_ignore("return_value_discarded")
		dir.list_dir_begin()
		var file_name := "."
		while file_name != "":
			file_name = dir.get_next()
			if file_name.is_empty() or file_name == "." or file_name == "..":
				continue
			var next := path + "/" +file_name
			if dir.current_is_dir():
				delete_directory(next)
			else:
				# delete file
				var err := dir.remove(next)
				if err:
					push_error("Delete %s failed: %s" % [next, error_string(err)])
		if not only_content:
			var err := dir.remove(path)
			if err:
				push_error("Delete %s failed: %s" % [path, error_string(err)])


func copy_directory(from_dir :String, to_dir :String) -> bool:
	if not DirAccess.dir_exists_absolute(from_dir):
		push_error("Source directory not found '%s'" % from_dir)
		return false
	# check if destination exists
	if not DirAccess.dir_exists_absolute(to_dir):
		# create it
		var err := DirAccess.make_dir_recursive_absolute(to_dir)
		if err != OK:
			push_error("Can't create directory '%s'. Error: %s" % [to_dir, error_string(err)])
			return false
	var source_dir := DirAccess.open(from_dir)
	var dest_dir := DirAccess.open(to_dir)
	if source_dir != null:
		@warning_ignore("return_value_discarded")
		source_dir.list_dir_begin()
		var next := "."

		while next != "":
			next = source_dir.get_next()
			if next == "" or next == "." or next == "..":
				continue
			var source := source_dir.get_current_dir() + "/" + next
			var dest := dest_dir.get_current_dir() + "/" + next
			if source_dir.current_is_dir():
				@warning_ignore("return_value_discarded")
				copy_directory(source + "/", dest)
				continue
			var err := source_dir.copy(source, dest)
			if err != OK:
				push_error("Error checked copy file '%s' to '%s'" % [source, dest])
				return false
		return true
	else:
		push_error("Directory not found: " + from_dir)
		return false


func extract_zip(zip_package :String, dest_path :String) -> Variant:
	var zip: ZIPReader = ZIPReader.new()
	var err := zip.open(zip_package)
	if err != OK:
		push_error("Extracting `%s` failed! Please collect the error log and report this. Error Code: %s" % [zip_package, err])
		return null
	var zip_entries: PackedStringArray = zip.get_files()
	# Get base path and step over archive folder
	var archive_path := zip_entries[0]
	zip_entries.remove_at(0)

	for zip_entry in zip_entries:
		var new_file_path: String = dest_path + "/" + zip_entry.replace(archive_path, "")
		if zip_entry.ends_with("/"):
			@warning_ignore("return_value_discarded")
			DirAccess.make_dir_recursive_absolute(new_file_path)
			continue
		var file: FileAccess = FileAccess.open(new_file_path, FileAccess.WRITE)
		file.store_buffer(zip.read_file(zip_entry))
	@warning_ignore("return_value_discarded")
	zip.close()
	return dest_path


func download_release() -> void:
	var zip_file := GdUnitFileAccess.temp_dir() + "/update.zip"
	var response :GdUnitUpdateClient.HttpResponse
	if _debug_mode:
		response = GdUnitUpdateClient.HttpResponse.new(200, PackedByteArray())
		zip_file = "res://update.zip"
	else:
		response = await _update_client.request_zip_package(_download_url, zip_file)
	_update_client.queue_free()
	if response.code() != 200:
		push_warning("Update information cannot be retrieved from GitHub! \n Error code: %d : %s" % [response.code(), response.response()])
		message_h4("Update failed! Try it later again.", Color.RED)
		await get_tree().create_timer(3).timeout
		return


func _on_confirmed() -> void:
	await run_update()
