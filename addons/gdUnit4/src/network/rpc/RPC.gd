class_name RPC
extends RefCounted


func serialize() -> String:
	return JSON.stringify(inst_to_dict(self))


# using untyped version see comments below
static func deserialize(json_value :String) -> Object:
	var json := JSON.new()
	var err := json.parse(json_value)
	if err != OK:
		push_error("Can't deserialize JSON, error at line %d: %s \n json: '%s'" % [json.get_error_line(), json.get_error_message(), json_value])
		return null
	var result :Dictionary = json.get_data()
	if not typeof(result) == TYPE_DICTIONARY:
		push_error("Can't deserialize JSON, error at line %d: %s \n json: '%s'" % [result.error_line, result.error_string, json_value])
		return null
	return dict_to_inst(result)

# this results in orpan node, for more details https://github.com/godotengine/godot/issues/50069
#func deserialize2(data :Dictionary) -> RPC:
#	return  dict_to_inst(data) as RPC
