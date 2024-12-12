# prototype of console with CSI support
# https://notes.burke.libbey.me/ansi-escape-codes/
class_name CmdConsole
extends RefCounted

enum {
	COLOR_TABLE,
	COLOR_RGB
}

const BOLD = 0x1
const ITALIC = 0x2
const UNDERLINE = 0x4

const CSI_BOLD = "[1m"
const CSI_ITALIC = "[3m"
const CSI_UNDERLINE = "[4m"

# Control Sequence Introducer
var _debug_show_color_codes := false
var _color_mode := COLOR_TABLE


func color(p_color :Color) -> CmdConsole:
	# using color table 16 - 231 a  6 x 6 x 6 RGB color cube  (16 + R * 36 + G * 6 + B)
	#if _color_mode == COLOR_TABLE:
	#	@warning_ignore("integer_division")
	#	var c2 := 16 + (int(p_color.r8/42) * 36) + (int(p_color.g8/42) * 6) + int(p_color.b8/42)
	#	if _debug_show_color_codes:
	#		printraw("%6d" % [c2])
	#	printraw("[38;5;%dm" % c2 )
	#else:
	printraw("[38;2;%d;%d;%dm" % [p_color.r8, p_color.g8, p_color.b8] )
	return self


func save_cursor() -> CmdConsole:
	printraw("[s")
	return self


func restore_cursor() -> CmdConsole:
	printraw("[u")
	return self


func end_color() -> CmdConsole:
	printraw("[0m")
	return self


func row_pos(row :int) -> CmdConsole:
	printraw("[%d;0H" % row )
	return self


func scroll_area(from :int, to :int) -> CmdConsole:
	printraw("[%d;%dr" % [from ,to])
	return self


@warning_ignore("return_value_discarded")
func progress_bar(p_progress :int, p_color :Color = Color.POWDER_BLUE) -> CmdConsole:
	if p_progress < 0:
		p_progress = 0
	if p_progress > 100:
		p_progress = 100
	color(p_color)
	printraw("[%-50s] %-3d%%\r" % ["".lpad(int(p_progress/2.0), "■").rpad(50, "-"), p_progress])
	end_color()
	return self


func printl(value :String) -> CmdConsole:
	printraw(value)
	return self


func new_line() -> CmdConsole:
	prints()
	return self


func reset() -> CmdConsole:
	return self


func bold(enable :bool) -> CmdConsole:
	if enable:
		printraw(CSI_BOLD)
	return self


func italic(enable :bool) -> CmdConsole:
	if enable:
		printraw(CSI_ITALIC)
	return self


func underline(enable :bool) -> CmdConsole:
	if enable:
		printraw(CSI_UNDERLINE)
	return self


func prints_error(message :String) -> CmdConsole:
	return color(Color.CRIMSON).printl(message).end_color().new_line()


func prints_warning(message :String) -> CmdConsole:
	return color(Color.GOLDENROD).printl(message).end_color().new_line()


func prints_color(p_message :String, p_color :Color, p_flags := 0) -> CmdConsole:
	return print_color(p_message, p_color, p_flags).new_line()


func print_color(p_message :String, p_color :Color, p_flags := 0) -> CmdConsole:
	return color(p_color)\
		.bold(p_flags&BOLD == BOLD)\
		.italic(p_flags&ITALIC == ITALIC)\
		.underline(p_flags&UNDERLINE == UNDERLINE)\
		.printl(p_message)\
		.end_color()


@warning_ignore("return_value_discarded")
func print_color_table() -> void:
	prints_color("Color Table 6x6x6", Color.ANTIQUE_WHITE)
	_debug_show_color_codes = true
	for green in range(0, 6):
		for red in range(0, 6):
			for blue in range(0, 6):
				print_color("████████ ", Color8(red*42, green*42, blue*42))
			new_line()
		new_line()

	prints_color("Color Table RGB", Color.ANTIQUE_WHITE)
	_color_mode = COLOR_RGB
	for green in range(0, 6):
		for red in range(0, 6):
			for blue in range(0, 6):
				print_color("████████ ", Color8(red*42, green*42, blue*42))
			new_line()
		new_line()
	_color_mode = COLOR_TABLE
	_debug_show_color_codes = false
