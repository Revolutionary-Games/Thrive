# GdUnit generated TestSuite
class_name GdUnitUpdateTest
extends GdUnitTestSuite

# TestSuite generated from
const GdUnitUpdate = preload('res://addons/gdUnit4/src/update/GdUnitUpdate.gd')


func after_test() -> void:
	clean_temp_dir()


func test__prepare_update_deletes_old_content() -> void:
	var _update :GdUnitUpdate = auto_free(GdUnitUpdate.new())




