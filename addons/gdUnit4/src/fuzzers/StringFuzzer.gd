class_name StringFuzzer
extends Fuzzer


const DEFAULT_CHARSET = "a-zA-Z0-9+-_"

var _min_length :int
var _max_length :int
var _charset :PackedByteArray


func _init(min_length :int, max_length :int, pattern :String = DEFAULT_CHARSET) -> void:
	assert(min_length>0 and min_length < max_length)
	assert(not null or not pattern.is_empty())
	_min_length = min_length
	_max_length = max_length
	_charset = StringFuzzer.extract_charset(pattern)


static func extract_charset(pattern :String) -> PackedByteArray:
	var reg := RegEx.new()
	if reg.compile(pattern) != OK:
		push_error("Invalid pattern to generate Strings! Use e.g  'a-zA-Z0-9+-_'")
		return PackedByteArray()

	var charset := Array()
	var char_before := -1
	var index := 0
	while index < pattern.length():
		var char_current := pattern.unicode_at(index)
		# - range token at first or last pos?
		if char_current == 45 and (index == 0 or index == pattern.length()-1):
			charset.append(char_current)
			index += 1
			continue
		index += 1
		# range starts
		if char_current == 45 and char_before != -1:
			var char_next := pattern.unicode_at(index)
			var characters := build_chars(char_before, char_next)
			for character in characters:
				charset.append(character)
			char_before = -1
			index += 1
			continue
		char_before = char_current
		charset.append(char_current)
	return PackedByteArray(charset)


static func build_chars(from :int, to :int) -> Array[int]:
	var characters :Array[int] = []
	for character in range(from+1, to+1):
		characters.append(character)
	return characters


func next_value() -> String:
	var value := PackedByteArray()
	var max_char := len(_charset)
	var length :int = max(_min_length, randi() % _max_length)
	for i in length:
		@warning_ignore("return_value_discarded")
		value.append(_charset[randi() % max_char])
	return value.get_string_from_utf8()
