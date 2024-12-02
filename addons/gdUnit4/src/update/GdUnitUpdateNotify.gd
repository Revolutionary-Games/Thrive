@tool
extends Window

#signal request_completed(response)

const GdMarkDownReader = preload("res://addons/gdUnit4/src/update/GdMarkDownReader.gd")
const GdUnitUpdateClient = preload("res://addons/gdUnit4/src/update/GdUnitUpdateClient.gd")

@onready var _md_reader :GdMarkDownReader = GdMarkDownReader.new()
@onready var _update_client :GdUnitUpdateClient = $GdUnitUpdateClient
@onready var _header :Label = $Panel/GridContainer/PanelContainer/header
@onready var _update_button :Button = $Panel/GridContainer/Panel/HBoxContainer/update
@onready var _content :RichTextLabel = $Panel/GridContainer/PanelContainer2/ScrollContainer/MarginContainer/content

var _debug_mode := false
var _patcher :GdUnitPatcher = GdUnitPatcher.new()
var _current_version := GdUnit4Version.current()
var _download_zip_url :String


func _ready() -> void:
	_update_button.disabled = true
	_md_reader.set_http_client(_update_client)
	@warning_ignore("return_value_discarded")
	GdUnitFonts.init_fonts(_content)
	await request_releases()


func request_releases() -> void:
	if _debug_mode:
		_header.text = "A new version 'v4.1.0_debug' is available"
		await show_update()
		return

	# wait 20s to allow the editor to initialize itself
	await get_tree().create_timer(20).timeout
	var response :GdUnitUpdateClient.HttpResponse = await _update_client.request_latest_version()
	if response.code() != 200:
		push_warning("Update information cannot be retrieved from GitHub! \n %s" % response.response())
		return
	var latest_version := extract_latest_version(response)
	# if same version exit here no update need
	if latest_version.is_greater(_current_version):
		_patcher.scan(_current_version)
		_header.text = "A new version '%s' is available" % latest_version
		_download_zip_url = extract_zip_url(response)
		await show_update()


func _colored(message_ :String, color :Color) -> String:
	return "[color=#%s]%s[/color]" % [color.to_html(), message_]


func message_h4(message_ :String, color :Color, clear := true) -> void:
	if clear:
		_content.clear()
	_content.append_text("[font_size=16]%s[/font_size]" % _colored(message_, color))


func message(message_ :String, color :Color) -> void:
	_content.clear()
	_content.append_text(_colored(message_, color))


func _process(_delta :float) -> void:
	if _content != null and _content.is_visible_in_tree():
		_content.queue_redraw()


func show_update() -> void:
	message_h4("\n\n\nRequest release infos ... [img=24x24]%s[/img]" % GdUnitUiTools.get_spinner(), Color.SNOW)
	popup_centered_ratio(.5)
	prints("Scan for GdUnit4 Update ...")
	var content :String
	if _debug_mode:
		var template := FileAccess.open("res://addons/gdUnit4/test/update/resources/markdown.txt", FileAccess.READ).get_as_text()
		content = await _md_reader.to_bbcode(template)
	else:
		var response :GdUnitUpdateClient.HttpResponse = await _update_client.request_releases()
		if response.code() == 200:
			content = await extract_releases(response, _current_version)
		else:
			message_h4("\n\n\nError checked request available releases!", Color.RED)
			return

	# finally force rescan to import images as textures
	if Engine.is_editor_hint():
		await rescan()
	message(content, Color.DODGER_BLUE)
	_update_button.set_disabled(false)


func extract_latest_version(response :GdUnitUpdateClient.HttpResponse) -> GdUnit4Version:
	var body :Array = response.response()
	return GdUnit4Version.parse(body[0]["name"] as String)


func extract_zip_url(response :GdUnitUpdateClient.HttpResponse) -> String:
	var body :Array = response.response()
	return body[0]["zipball_url"]


func extract_releases(response :GdUnitUpdateClient.HttpResponse, current_version :GdUnit4Version) -> String:
	await get_tree().process_frame
	var result := ""
	for release :Dictionary in response.response():
		if GdUnit4Version.parse(release["tag_name"] as String).equals(current_version):
			break
		var release_description :String = release["body"]
		result += await _md_reader.to_bbcode(release_description)
	return result


func rescan() -> void:
	if Engine.is_editor_hint():
		if OS.is_stdout_verbose():
			prints(".. reimport release resources")
		var fs := EditorInterface.get_resource_filesystem()
		fs.scan()
		while fs.is_scanning():
			if OS.is_stdout_verbose():
				progressBar(fs.get_scanning_progress() * 100 as int)
			await get_tree().process_frame
		await get_tree().process_frame
	await get_tree().create_timer(1).timeout


func progressBar(p_progress :int) -> void:
	if p_progress < 0:
		p_progress = 0
	if p_progress > 100:
		p_progress = 100
	printraw("scan [%-50s] %-3d%%\r" % ["".lpad(int(p_progress/2.0), "#").rpad(50, "-"), p_progress])


@warning_ignore("return_value_discarded")
func _on_update_pressed() -> void:
	_update_button.set_disabled(true)
	# close all opend scripts before start the update
	if not _debug_mode:
		ScriptEditorControls.close_open_editor_scripts()
	# copy update source to a temp because the update is deleting the whole gdUnit folder
	DirAccess.make_dir_absolute("res://addons/.gdunit_update")
	DirAccess.copy_absolute("res://addons/gdUnit4/src/update/GdUnitUpdate.tscn", "res://addons/.gdunit_update/GdUnitUpdate.tscn")
	DirAccess.copy_absolute("res://addons/gdUnit4/src/update/GdUnitUpdate.gd", "res://addons/.gdunit_update/GdUnitUpdate.gd")
	var source := FileAccess.open("res://addons/gdUnit4/src/update/GdUnitUpdate.tscn", FileAccess.READ)
	var content := source.get_as_text().replace("res://addons/gdUnit4/src/update/GdUnitUpdate.gd", "res://addons/.gdunit_update/GdUnitUpdate.gd")
	var dest := FileAccess.open("res://addons/.gdunit_update/GdUnitUpdate.tscn", FileAccess.WRITE)
	dest.store_string(content)
	hide()
	var update: Node = load("res://addons/.gdunit_update/GdUnitUpdate.tscn").instantiate()
	update.setup(_update_client, _download_zip_url)
	(Engine.get_main_loop() as SceneTree).root.add_child(update)
	update.popup_centered()


func _on_show_next_toggled(enabled :bool) -> void:
	GdUnitSettings.set_update_notification(enabled)


func _on_cancel_pressed() -> void:
	hide()


func _on_content_meta_clicked(meta :String) -> void:
	var properties :Variant = str_to_var(meta)
	if properties.has("url"):
		@warning_ignore("return_value_discarded")
		OS.shell_open(properties.get("url") as String)


func _on_content_meta_hover_started(meta :String) -> void:
	var properties :Variant = str_to_var(meta)
	if properties.has("tool_tip"):
		_content.set_tooltip_text(properties.get("tool_tip") as String)


@warning_ignore("unused_parameter")
func _on_content_meta_hover_ended(meta :String) -> void:
	_content.set_tooltip_text("")
