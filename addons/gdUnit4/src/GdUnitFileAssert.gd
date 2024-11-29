class_name GdUnitFileAssert
extends GdUnitAssert


func is_file() -> GdUnitFileAssert:
	return self


func exists() -> GdUnitFileAssert:
	return self


func is_script() -> GdUnitFileAssert:
	return self


@warning_ignore("unused_parameter")
func contains_exactly(expected_rows :Array) -> GdUnitFileAssert:
	return self
