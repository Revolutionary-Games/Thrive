## An assertion tool to verify for Godot runtime errors like assert() and push notifications like push_error().
class_name GdUnitGodotErrorAssert
extends GdUnitAssert


## Verifies if the executed code runs without any runtime errors
## Usage:
##     [codeblock]
##		await assert_error(<callable>).is_success()
##     [/codeblock]
func is_success() -> GdUnitGodotErrorAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Verifies if the executed code runs into a runtime error
## Usage:
##     [codeblock]
##		await assert_error(<callable>).is_runtime_error(<expected error message>)
##     [/codeblock]
@warning_ignore("unused_parameter")
func is_runtime_error(expected_error :String) -> GdUnitGodotErrorAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Verifies if the executed code has a push_warning() used
## Usage:
##     [codeblock]
##		await assert_error(<callable>).is_push_warning(<expected push warning message>)
##     [/codeblock]
@warning_ignore("unused_parameter")
func is_push_warning(expected_warning :String) -> GdUnitGodotErrorAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Verifies if the executed code has a push_error() used
## Usage:
##     [codeblock]
##		await assert_error(<callable>).is_push_error(<expected push error message>)
##     [/codeblock]
@warning_ignore("unused_parameter")
func is_push_error(expected_error :String) -> GdUnitGodotErrorAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self
