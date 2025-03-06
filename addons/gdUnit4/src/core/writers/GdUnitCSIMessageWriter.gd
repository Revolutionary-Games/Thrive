@tool
class_name GdUnitCSIMessageWriter
extends GdUnitMessageWritter
## A message writer implementation using ANSI/CSI escape codes for console output.[br]
## [br]
## This writer provides formatted message output using CSI (Control Sequence Introducer) codes.[br]
## It supports:[br]
## - Color using RGB values[br]
## - Text styles (bold, italic, underline)[br]
## - Cursor positioning and text alignment[br]
## [br]
## Used primarily for console-based test execution and CI/CD environments.


enum {
	COLOR_TABLE,
	COLOR_RGB
}

const CSI_BOLD = "[1m"
const CSI_ITALIC = "[3m"
const CSI_UNDERLINE = "[4m"

# Control Sequence Introducer
var _debug_show_color_codes := false
var _color_mode := COLOR_TABLE

## Current cursor position in the line
var _current_pos := 0


## Constructs CSI style codes based on flags.[br]
## [br]
## [param flags] The style flags to apply (BOLD, ITALIC, UNDERLINE).[br]
## Returns the corresponding CSI codes.
func _apply_style_flags(flags: int) -> String:
	var _style := ""
	if flags & BOLD:
		_style += CSI_BOLD
	if flags & ITALIC:
		_style += CSI_ITALIC
	if flags & UNDERLINE:
		_style += CSI_UNDERLINE
	return _style


## Implementation of basic message output with formatting.
func _print_message(_message: String, _color: Color, _indent: int, _flags: int) -> void:
	var indent_text := "".lpad(_indent * 2)
	var _style := _apply_style_flags(_flags)
	printraw("%s[38;2;%d;%d;%dm%s%s[0m" % [indent_text, _color.r8, _color.g8, _color.b8, _style, _message] )
	_current_pos += _indent * 2 + _message.length()


## Implementation of line-ending message output with formatting.
func _println_message(_message: String, _color: Color, _indent: int, _flags: int) -> void:
	_print_message(_message, _color, _indent, _flags)
	prints()
	_current_pos = 0


## Implementation of positioned message output with formatting.
func _print_at(_message: String, cursor_pos: int, _color: Color, _effect: Effect, _align: Align, _flags: int) -> void:
	if _align == Align.RIGHT:
		cursor_pos = cursor_pos - _message.length()

	if cursor_pos > _current_pos:
		printraw("[%dG" % cursor_pos)  # Move cursor to absolute position
	else:
		_message = " " + _message

	var _style := _apply_style_flags(_flags)
	printraw("[38;2;%d;%d;%dm%s%s[0m" % [_color.r8, _color.g8, _color.b8, _style, _message] )
	_current_pos = cursor_pos + _message.length()


## Writes a line break and returns self for chaining.
func new_line() -> GdUnitCSIMessageWriter:
	prints()
	return self


## Saves the current cursor position.[br]
## Returns self for chaining.
func save_cursor() -> GdUnitCSIMessageWriter:
	printraw("[s")
	return self


## Restores previously saved cursor position.[br]
## Returns self for chaining.
func restore_cursor() -> GdUnitCSIMessageWriter:
	printraw("[u")
	return self


## Clears screen content and resets cursor position.
func clear() -> void:
	printraw("[2J[H")  # Clear screen and move cursor to home
	_current_pos = 0


## Debug method to display the available color table.[br]
## Shows both 6x6x6 color cube and RGB color modes.
@warning_ignore("return_value_discarded")
func _print_color_table() -> void:
	color(Color.ANTIQUE_WHITE).println_message("Color Table 6x6x6")
	_debug_show_color_codes = true
	for green in range(0, 6):
		for red in range(0, 6):
			for blue in range(0, 6):
				color(Color8(red*42, green*42, blue*42)).println_message("████████ ")
			new_line()
		new_line()

	color(Color.ANTIQUE_WHITE).println_message("Color Table RGB")
	_color_mode = COLOR_RGB
	for green in range(0, 6):
		for red in range(0, 6):
			for blue in range(0, 6):
				color(Color8(red*42, green*42, blue*42)).println_message("████████ ")
			new_line()
		new_line()
	_color_mode = COLOR_TABLE
	_debug_show_color_codes = false
