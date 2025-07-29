class_name GdScriptParser
extends RefCounted

const GdUnitTools := preload("res://addons/gdUnit4/src/core/GdUnitTools.gd")

const ALLOWED_CHARACTERS := "0123456789_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ\""

var TOKEN_NOT_MATCH := Token.new("")
var TOKEN_SPACE := SkippableToken.new(" ")
var TOKEN_TABULATOR := SkippableToken.new("\t")
var TOKEN_NEW_LINE := SkippableToken.new("\n")
var TOKEN_COMMENT := SkippableToken.new("#")
var TOKEN_CLASS_NAME := Token.new("class_name")
var TOKEN_INNER_CLASS := Token.new("class")
var TOKEN_EXTENDS := Token.new("extends")
var TOKEN_ENUM := Token.new("enum")
var TOKEN_FUNCTION_STATIC_DECLARATION := Token.new("static func")
var TOKEN_FUNCTION_DECLARATION := Token.new("func")
var TOKEN_FUNCTION := Token.new(".")
var TOKEN_FUNCTION_RETURN_TYPE := Token.new("->")
var TOKEN_FUNCTION_END := Token.new("):")
var TOKEN_ARGUMENT_ASIGNMENT := Token.new("=")
var TOKEN_ARGUMENT_TYPE_ASIGNMENT := Token.new(":=")
var TOKEN_ARGUMENT_FUZZER := FuzzerToken.new(GdUnitTools.to_regex("((?!(fuzzer_(seed|iterations)))fuzzer?\\w+)( ?+= ?+| ?+:= ?+| ?+:Fuzzer ?+= ?+|)"))
var TOKEN_ARGUMENT_TYPE := Token.new(":")
var TOKEN_ARGUMENT_SEPARATOR := Token.new(",")
var TOKEN_BRACKET_ROUND_OPEN := Token.new("(")
var TOKEN_BRACKET_ROUND_CLOSE := Token.new(")")
var TOKEN_BRACKET_SQUARE_OPEN := Token.new("[")
var TOKEN_BRACKET_SQUARE_CLOSE := Token.new("]")
var TOKEN_BRACKET_CURLY_OPEN := Token.new("{")
var TOKEN_BRACKET_CURLY_CLOSE := Token.new("}")



var OPERATOR_ADD := Operator.new("+")
var OPERATOR_SUB := Operator.new("-")
var OPERATOR_MUL := Operator.new("*")
var OPERATOR_DIV := Operator.new("/")
var OPERATOR_REMAINDER := Operator.new("%")

var TOKENS :Array[Token] = [
	TOKEN_SPACE,
	TOKEN_TABULATOR,
	TOKEN_NEW_LINE,
	TOKEN_COMMENT,
	TOKEN_BRACKET_ROUND_OPEN,
	TOKEN_BRACKET_ROUND_CLOSE,
	TOKEN_BRACKET_SQUARE_OPEN,
	TOKEN_BRACKET_SQUARE_CLOSE,
	TOKEN_BRACKET_CURLY_OPEN,
	TOKEN_BRACKET_CURLY_CLOSE,
	TOKEN_CLASS_NAME,
	TOKEN_INNER_CLASS,
	TOKEN_EXTENDS,
	TOKEN_ENUM,
	TOKEN_FUNCTION_STATIC_DECLARATION,
	TOKEN_FUNCTION_DECLARATION,
	TOKEN_ARGUMENT_FUZZER,
	TOKEN_ARGUMENT_TYPE_ASIGNMENT,
	TOKEN_ARGUMENT_ASIGNMENT,
	TOKEN_ARGUMENT_TYPE,
	TOKEN_FUNCTION,
	TOKEN_ARGUMENT_SEPARATOR,
	TOKEN_FUNCTION_RETURN_TYPE,
	OPERATOR_ADD,
	OPERATOR_SUB,
	OPERATOR_MUL,
	OPERATOR_DIV,
	OPERATOR_REMAINDER,
]

var _regex_clazz_name := GdUnitTools.to_regex("(class) ([a-zA-Z0-9_]+) (extends[a-zA-Z]+:)|(class) ([a-zA-Z0-9_]+)")
var _regex_strip_comments := GdUnitTools.to_regex("^([^#\"']|'[^']*'|\"[^\"]*\")*\\K#.*")
var _scanned_inner_classes := PackedStringArray()
var _script_constants := {}
var _is_awaiting := GdUnitTools.to_regex("\\bawait\\s+(?![^\"]*\"[^\"]*$)(?!.*#.*await)")


static func to_unix_format(input :String) -> String:
	return input.replace("\r\n", "\n")


class Token extends RefCounted:
	var _token: String
	var _consumed: int
	var _is_operator: bool
	var _regex :RegEx


	func _init(p_token: String, p_is_operator := false, p_regex :RegEx = null) -> void:
		_token = p_token
		_is_operator = p_is_operator
		_consumed = p_token.length()
		_regex = p_regex

	func match(input: String, pos: int) -> bool:
		if _regex:
			var result := _regex.search(input, pos)
			if result == null:
				return false
			_consumed = result.get_end() - result.get_start()
			return pos == result.get_start()
		return input.findn(_token, pos) == pos

	func is_operator() -> bool:
		return _is_operator

	func is_inner_class() -> bool:
		return _token == "class"

	func is_variable() -> bool:
		return false

	func is_token(token_name :String) -> bool:
		return _token == token_name

	func is_skippable() -> bool:
		return false

	func _to_string() -> String:
		return "Token{" + _token + "}"


class Operator extends Token:
	func _init(value: String) -> void:
		super(value, true)

	func _to_string() -> String:
		return "OperatorToken{%s}" % [_token]


# A skippable token, is just a placeholder like space or tabs
class SkippableToken extends Token:

	func _init(p_token: String) -> void:
		super(p_token)

	func is_skippable() -> bool:
		return true


# Token to parse Fuzzers
class FuzzerToken extends Token:
	var _name: String


	func _init(regex: RegEx) -> void:
		super("", false, regex)


	func match(input: String, pos: int) -> bool:
		if _regex:
			var result := _regex.search(input, pos)
			if result == null:
				return false
			_name = result.strings[1]
			_consumed = result.get_end() - result.get_start()
			return pos == result.get_start()
		return input.findn(_token, pos) == pos


	func name() -> String:
		return _name


	func type() -> int:
		return GdObjects.TYPE_FUZZER


	func _to_string() -> String:
		return "FuzzerToken{%s: '%s'}" % [_name, _token]


# Token to parse function arguments
class Variable extends Token:
	var _plain_value :String
	var _typed_value :Variant
	var _type :int = TYPE_NIL


	func _init(p_value: String) -> void:
		super(p_value)
		_type = _scan_type(p_value)
		_plain_value = p_value
		_typed_value = _cast_to_type(p_value, _type)


	func _scan_type(p_value: String) -> int:
		if p_value.begins_with("\"") and p_value.ends_with("\""):
			return TYPE_STRING
		var type_ := GdObjects.string_to_type(p_value)
		if type_ != TYPE_NIL:
			return type_
		if p_value.is_valid_int():
			return TYPE_INT
		if p_value.is_valid_float():
			return TYPE_FLOAT
		if p_value.is_valid_hex_number():
			return TYPE_INT
		return TYPE_OBJECT


	func _cast_to_type(p_value :String, p_type: int) -> Variant:
		match p_type:
			TYPE_STRING:
				return p_value#.substr(1, p_value.length() - 2)
			TYPE_INT:
				return p_value.to_int()
			TYPE_FLOAT:
				return p_value.to_float()
		return p_value


	func is_variable() -> bool:
		return true


	func type() -> int:
		return _type


	func value() -> Variant:
		return _typed_value


	func plain_value() -> String:
		return _plain_value


	func _to_string() -> String:
		return "Variable{%s: %s : '%s'}" % [_plain_value, GdObjects.type_as_string(_type), _token]


class TokenInnerClass extends Token:
	var _clazz_name :String
	var _content := PackedStringArray()


	static func _strip_leading_spaces(input :String) -> String:
		var characters := input.to_utf8_buffer()
		while not characters.is_empty():
			if characters[0] != 0x20:
				break
			characters.remove_at(0)
		return characters.get_string_from_utf8()


	static func _consumed_bytes(row :String) -> int:
		return row.replace(" ", "").replace("	", "").length()


	func _init(clazz_name :String) -> void:
		super("class")
		_clazz_name = clazz_name


	func is_class_name(clazz_name :String) -> bool:
		return _clazz_name == clazz_name


	func content() -> PackedStringArray:
		return _content


	func parse(source_rows :PackedStringArray, offset :int) -> void:
		# add class signature
		@warning_ignore("return_value_discarded")
		_content.append(source_rows[offset])
		# parse class content
		for row_index in range(offset+1, source_rows.size()):
			# scan until next non tab
			var source_row := source_rows[row_index]
			var row := TokenInnerClass._strip_leading_spaces(source_row)
			if row.is_empty() or row.begins_with("\t") or row.begins_with("#"):
				# fold all line to left by removing leading tabs and spaces
				if source_row.begins_with("\t"):
					source_row = source_row.trim_prefix("\t")
				# refomat invalid empty lines
				if source_row.dedent().is_empty():
					@warning_ignore("return_value_discarded")
					_content.append("")
				else:
					@warning_ignore("return_value_discarded")
					_content.append(source_row)
				continue
			break
		_consumed += TokenInnerClass._consumed_bytes("".join(_content))


	func _to_string() -> String:
		return "TokenInnerClass{%s}" % [_clazz_name]



func get_token(input :String, current_index :int) -> Token:
	for t in TOKENS:
		if t.match(input, current_index):
			return t
	return TOKEN_NOT_MATCH


func next_token(input: String, current_index: int, ignore_tokens :Array[Token] = []) -> Token:
	var token := TOKEN_NOT_MATCH
	for t :Token in TOKENS.filter(func(t :Token) -> bool: return not ignore_tokens.has(t)):
		if t.match(input, current_index):
			token = t
			break
	if token == OPERATOR_SUB:
		token = tokenize_value(input, current_index, token)
	if token == TOKEN_INNER_CLASS:
		token = tokenize_inner_class(input, current_index, token)
	if token == TOKEN_NOT_MATCH:
		return tokenize_value(input, current_index, token, ignore_tokens.has(TOKEN_FUNCTION))
	return token


func tokenize_value(input: String, current: int, token: Token, ignore_dots := false) -> Token:
	var next := 0
	var current_token := ""
	# test for '--', '+-', '*-', '/-', '%-', or at least '-x'
	var test_for_sign := (token == null or token.is_operator()) and input[current] == "-"
	while current + next < len(input):
		var character := input[current + next] as String
		# if first charater a sign
		# or allowend charset
		# or is a float value
		if (test_for_sign and next==0) \
			or character in ALLOWED_CHARACTERS \
			or (character == "." and (ignore_dots or current_token.is_valid_int())):
			current_token += character
			next += 1
			continue
		break
	if current_token != "":
		return Variable.new(current_token)
	return TOKEN_NOT_MATCH


func extract_clazz_name(value :String) -> String:
	var result := _regex_clazz_name.search(value)
	if result == null:
		push_error("Can't extract class name from '%s'" % value)
		return ""
	if result.get_string(2).is_empty():
		return result.get_string(5)
	else:
		return result.get_string(2)


@warning_ignore("unused_parameter")
func tokenize_inner_class(source_code: String, current: int, token: Token) -> Token:
	var clazz_name := extract_clazz_name(source_code.substr(current, 64))
	return TokenInnerClass.new(clazz_name)


func parse_return_token(input: String) -> Variable:
	var index := input.rfind(TOKEN_FUNCTION_RETURN_TYPE._token)
	if index == -1:
		return TOKEN_NOT_MATCH
	index += TOKEN_FUNCTION_RETURN_TYPE._consumed
	# We scan for the return value exclusive '.' token because it could be referenced to a
	# external or internal class e.g.  'func foo() -> InnerClass.Bar:'
	var token := next_token(input, index, [TOKEN_FUNCTION])
	while !token.is_variable() and token != TOKEN_NOT_MATCH:
		index += token._consumed
		token = next_token(input, index, [TOKEN_FUNCTION])
	return token


func get_function_descriptors(script: GDScript, included_functions: PackedStringArray = []) -> Array[GdFunctionDescriptor]:
	var fds: Array[GdFunctionDescriptor] = []
	for method_descriptor in script.get_script_method_list():
		var func_name: String = method_descriptor["name"]
		if included_functions.is_empty() or func_name in included_functions:
			# exclude type set/geters
			if is_getter_or_setter(func_name):
				continue
			if not fds.any(func(fd: GdFunctionDescriptor) -> bool: return fd.name() == func_name):
				fds.append(GdFunctionDescriptor.extract_from(method_descriptor, false))

	# we need to enrich it by default arguments and line number by parsing the script
	# the engine core functions has no valid methods to get this info
	_prescan_script(script)
	_enrich_function_descriptor(script, fds)
	return fds


func is_getter_or_setter(func_name: String) -> bool:
	return func_name.begins_with("@") and (func_name.ends_with("getter") or func_name.ends_with("setter"))


func _parse_function_arguments(input: String) -> Dictionary:
	var arguments := {}
	var current_index := 0
	var token :Token = null
	var bracket := 0
	var in_function := false
	while current_index < len(input):
		token = next_token(input, current_index)
		# fallback to not end in a endless loop
		if token == TOKEN_NOT_MATCH:
			var error : = """
				Parsing Error: Invalid token at pos %d found.
				Please report this error!
				source_code:
				--------------------------------------------------------------
				%s
				--------------------------------------------------------------
			""".dedent() % [current_index, input]
			push_error(error)
			current_index += 1
			continue
		current_index += token._consumed
		if token.is_skippable():
			continue
		if token == TOKEN_BRACKET_ROUND_OPEN :
			in_function = true
			bracket += 1
			continue
		if token == TOKEN_BRACKET_ROUND_CLOSE:
			bracket -= 1
		# if function end?
		if in_function and bracket == 0:
			return arguments
		# is function
		if token == TOKEN_FUNCTION_DECLARATION:
			token = next_token(input, current_index)
			current_index += token._consumed
			continue
		# is fuzzer argument
		if token is FuzzerToken:
			var arg_value := _parse_end_function(input.substr(current_index), true)
			current_index += arg_value.length()
			var arg_name :String = (token as FuzzerToken).name()
			arguments[arg_name] = arg_value.lstrip(" ")
			continue
		# is value argument
		if in_function and token.is_variable():
			var arg_name: String = (token as Variable).plain_value()
			var arg_value: String = GdFunctionArgument.UNDEFINED
			# parse type and default value
			while current_index < len(input):
				token = next_token(input, current_index)
				current_index += token._consumed
				if token.is_skippable():
					continue

				match token:
					TOKEN_ARGUMENT_TYPE:
						token = next_token(input, current_index)
						if token == TOKEN_SPACE:
							current_index += token._consumed
							token = next_token(input, current_index)
					TOKEN_ARGUMENT_TYPE_ASIGNMENT:
						arg_value = _parse_end_function(input.substr(current_index), true)
						current_index += arg_value.length()
					TOKEN_ARGUMENT_ASIGNMENT:
						token = next_token(input, current_index)
						arg_value = _parse_end_function(input.substr(current_index), true)
						current_index += arg_value.length()
					TOKEN_BRACKET_SQUARE_OPEN:
						bracket += 1
					TOKEN_BRACKET_CURLY_OPEN:
						bracket += 1
					TOKEN_BRACKET_ROUND_OPEN :
						bracket += 1
						# if value a function?
						if bracket > 1:
							# complete the argument value
							var func_begin := input.substr(current_index-TOKEN_BRACKET_ROUND_OPEN ._consumed)
							var func_body := _parse_end_function(func_begin)
							arg_value += func_body
							# fix parse index to end of value
							current_index += func_body.length() - TOKEN_BRACKET_ROUND_OPEN ._consumed - TOKEN_BRACKET_ROUND_CLOSE._consumed
					TOKEN_BRACKET_SQUARE_CLOSE:
						bracket -= 1
					TOKEN_BRACKET_CURLY_CLOSE:
						bracket -= 1
					TOKEN_BRACKET_ROUND_CLOSE:
						bracket -= 1
						# end of function
						if bracket == 0:
							break
					TOKEN_ARGUMENT_SEPARATOR:
						if bracket <= 1:
							break
			arguments[arg_name] = arg_value.lstrip(" ")
	return arguments


func _parse_end_function(input: String, remove_trailing_char := false) -> String:
	# find end of function
	var current_index := 0
	var bracket_count := 0
	var in_array := 0
	var in_dict := 0
	var end_of_func := false

	while current_index < len(input) and not end_of_func:
		var character := input[current_index]
		# step over strings
		if character == "'" :
			current_index = input.find("'", current_index+1) + 1
			if current_index == 0:
				push_error("Parsing error on '%s', can't evaluate end of string." % input)
				return ""
			continue
		if character == '"' :
			# test for string blocks
			if input.find('"""', current_index) == current_index:
				current_index = input.find('"""', current_index+3) + 3
			else:
				current_index = input.find('"', current_index+1) + 1
			if current_index == 0:
				push_error("Parsing error on '%s', can't evaluate end of string." % input)
				return ""
			continue

		match character:
			# count if inside an array
			"[": in_array += 1
			"]": in_array -= 1
			# count if inside an dictionary
			"{": in_dict += 1
			"}": in_dict -= 1
			# count if inside a function
			"(": bracket_count += 1
			")":
				bracket_count -= 1
				if bracket_count < 0 and in_array <= 0 and in_dict <= 0:
					end_of_func = true
			",":
				if bracket_count == 0 and in_array == 0 and in_dict <= 0:
					end_of_func = true
		current_index += 1
	if remove_trailing_char:
		# check if the parsed value ends with comma or end of doubled breaked
		# `<value>,` or `<function>())`
		var trailing_char := input[current_index-1]
		if trailing_char == ',' or (bracket_count < 0 and trailing_char == ')'):
			return input.substr(0, current_index-1)
	return input.substr(0, current_index)


func extract_inner_class(source_rows: PackedStringArray, clazz_name :String) -> PackedStringArray:
	for row_index in source_rows.size():
		var input := source_rows[row_index]
		var token := next_token(input, 0)
		if token.is_inner_class():
			@warning_ignore("unsafe_method_access")
			if token.is_class_name(clazz_name):
				@warning_ignore("unsafe_method_access")
				token.parse(source_rows, row_index)
				@warning_ignore("unsafe_method_access")
				return token.content()
	return PackedStringArray()


func extract_func_signature(rows :PackedStringArray, index :int) -> String:
	var signature := ""

	for rowIndex in range(index, rows.size()):
		var row := rows[rowIndex]
		row = _regex_strip_comments.sub(row, "").strip_edges(false)
		if row.is_empty():
			continue
		signature += row + "\n"
		if is_func_end(row):
			return signature.strip_edges()
	push_error("Can't fully extract function signature of '%s'" % rows[index])
	return ""


func get_class_name(script :GDScript) -> String:
	var source_code := GdScriptParser.to_unix_format(script.source_code)
	var source_rows := source_code.split("\n")

	for index :int in min(10, source_rows.size()):
		var input := source_rows[index]
		var token := next_token(input, 0)
		if token == TOKEN_CLASS_NAME:
			var current_index := token._consumed
			token = next_token(input, current_index)
			current_index += token._consumed
			token = tokenize_value(input, current_index, token)
			return (token as Variable).value()
	# if no class_name found extract from file name
	return GdObjects.to_pascal_case(script.resource_path.get_basename().get_file())


func parse_func_name(input :String) -> String:
	var current_index := 0
	var token := next_token(input, current_index)
	current_index += token._consumed
	if token != TOKEN_FUNCTION_STATIC_DECLARATION and token != TOKEN_FUNCTION_DECLARATION:
		return ""
	while not token is Variable:
		token = next_token(input, current_index)
		current_index += token._consumed
	return token._token


## Enriches the function descriptor by line number and argument default values
## - enrich all function descriptors form current script up to all inherited scrips
func _enrich_function_descriptor(script: GDScript, fds: Array[GdFunctionDescriptor]) -> void:
	var enriched_functions := PackedStringArray()
	var script_to_scan := script
	while script_to_scan != null:
		# do not scan the test suite base class itself
		if script_to_scan.resource_path == "res://addons/gdUnit4/src/GdUnitTestSuite.gd":
			break

		var rows := script_to_scan.source_code.split("\n")
		for rowIndex in rows.size():
			var input := rows[rowIndex]
			# step over inner class functions
			if input.begins_with("\t"):
				continue
			# skip comments and empty lines
			if input.begins_with("#") or input.length() == 0:
				continue
			var token := next_token(input, 0)
			if token == TOKEN_FUNCTION_STATIC_DECLARATION or token == TOKEN_FUNCTION_DECLARATION:
				var function_name := parse_func_name(input)
				var fd: GdFunctionDescriptor = fds.filter(func(element: GdFunctionDescriptor) -> bool:
					# is same function name and not already enriched
					return function_name == element.name() and not enriched_functions.has(element.name())
				).pop_front()
				if fd != null:
					# add as enriched function to exclude from next iteration (could be inherited)
					@warning_ignore("return_value_discarded")
					enriched_functions.append(fd.name())
					var func_signature := extract_func_signature(rows, rowIndex)
					var func_arguments := _parse_function_arguments(func_signature)
					# enrich missing default values
					for arg_name: String in func_arguments.keys():
						var func_argument: String = func_arguments[arg_name]
						fd.set_argument_value(arg_name, func_argument)
					fd.enrich_file_info(script_to_scan.resource_path, rowIndex + 1)
					fd._is_coroutine = is_func_coroutine(rows, rowIndex)
					# enrich return class name if not set
					if fd.return_type() == TYPE_OBJECT and fd._return_class in ["", "Resource", "RefCounted"]:
						var var_token := parse_return_token(func_signature)
						if var_token != TOKEN_NOT_MATCH and var_token.type() == TYPE_OBJECT:
							fd._return_class = _patch_inner_class_names(var_token.plain_value(), "")
		# if the script ihnerits we need to scan this also
		script_to_scan = script_to_scan.get_base_script()


func is_func_coroutine(rows :PackedStringArray, index :int) -> bool:
	var is_coroutine := false
	for rowIndex in range(index+1, rows.size()):
		var input := rows[rowIndex].strip_edges()
		# skip empty lines
		if input.is_empty():
			continue
		var token := next_token(input, 0)
		# scan until next function
		if token == TOKEN_FUNCTION_STATIC_DECLARATION or token == TOKEN_FUNCTION_DECLARATION:
			break

		if _is_awaiting.search(input):
			return true
	return is_coroutine


func is_inner_class(clazz_path :PackedStringArray) -> bool:
	return clazz_path.size() > 1


func is_func_end(row :String) -> bool:
	return row.strip_edges(false, true).ends_with(":")


func _patch_inner_class_names(clazz :String, clazz_name :String = "") -> String:
	var inner_clazz_name := clazz.split(".")[0]
	if _scanned_inner_classes.has(inner_clazz_name):
		return inner_clazz_name
		#var base_clazz := clazz_name.split(".")[0]
		#return base_clazz + "." + clazz
	if _script_constants.has(clazz):
		return clazz_name + "." + clazz
	return clazz


func _prescan_script(script: GDScript) -> void:
	_script_constants = script.get_script_constant_map()
	for key :String in _script_constants.keys():
		var value :Variant = _script_constants.get(key)
		if value is GDScript:
			@warning_ignore("return_value_discarded")
			_scanned_inner_classes.append(key)


func parse(clazz_name :String, clazz_path :PackedStringArray) -> GdUnitResult:
	if clazz_path.is_empty():
		return GdUnitResult.error("Invalid script path '%s'" % clazz_path)
	var is_inner_class_ := is_inner_class(clazz_path)
	var script :GDScript = load(clazz_path[0])
	_prescan_script(script)

	if is_inner_class_:
		var inner_class_name := clazz_path[1]
		if _scanned_inner_classes.has(inner_class_name):
			# do load only on inner class source code and enrich the stored script instance
			var source_code := _load_inner_class(script, inner_class_name)
			script = _script_constants.get(inner_class_name)
			script.source_code = source_code
	var function_descriptors := get_function_descriptors(script)
	var gd_class := GdClassDescriptor.new(clazz_name, is_inner_class_, function_descriptors)
	return GdUnitResult.success(gd_class)


func _load_inner_class(script: GDScript, inner_clazz: String) -> String:
	var source_rows := GdScriptParser.to_unix_format(script.source_code).split("\n")
	# extract all inner class names
	var inner_class_code := extract_inner_class(source_rows, inner_clazz)
	return "\n".join(inner_class_code)
