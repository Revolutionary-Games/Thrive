# GdUnit generated TestSuite
class_name StringFuzzerTest
extends GdUnitTestSuite

# TestSuite generated from
const __source = 'res://addons/gdUnit4/src/fuzzers/StringFuzzer.gd'


func test_extract_charset() -> void:
	assert_str(StringFuzzer.extract_charset("abc").get_string_from_utf8()).is_equal("abc")
	assert_str(StringFuzzer.extract_charset("abcDXG").get_string_from_utf8()).is_equal("abcDXG")
	assert_str(StringFuzzer.extract_charset("a-c").get_string_from_utf8()).is_equal("abc")
	assert_str(StringFuzzer.extract_charset("a-z").get_string_from_utf8()).is_equal("abcdefghijklmnopqrstuvwxyz")
	assert_str(StringFuzzer.extract_charset("A-Z").get_string_from_utf8()).is_equal("ABCDEFGHIJKLMNOPQRSTUVWXYZ")

	# range token at start
	assert_str(StringFuzzer.extract_charset("-a-dA-D2-8+_").get_string_from_utf8()).is_equal("-abcdABCD2345678+_")
	# range token at end
	assert_str(StringFuzzer.extract_charset("a-dA-D2-8+_-").get_string_from_utf8()).is_equal("abcdABCD2345678+_-")
	# range token in the middle
	assert_str(StringFuzzer.extract_charset("a-d-A-D2-8+_").get_string_from_utf8()).is_equal("abcd-ABCD2345678+_")


func test_next_value() -> void:
	var pattern := "a-cD-X+2-5"
	var fuzzer := StringFuzzer.new(4, 128, pattern)
	var r := RegEx.new()
	r.compile("[%s]+" % pattern)
	for i in 100:
		var value :String = fuzzer.next_value()
		# verify the generated value has a length in the configured min/max range
		assert_int(value.length()).is_between(4, 128)
		# using regex to remove_at all expected chars to verify the value only containing expected chars by is empty
		assert_str(r.sub(value, "")).is_empty()
