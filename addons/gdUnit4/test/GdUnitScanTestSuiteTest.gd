# This test verifys only the scanner functionallity
# to find all given tests by pattern 'func test_<testname>():'
extends GdUnitTestSuite


func test_example() -> void:
	assert_that("This is a example message").has_length(25)
	assert_that("This is a example message").starts_with("This is a ex")


func test_b() -> void:
	pass


# test name starts with same name e.g. test_b vs test_b2
func test_b2() -> void:
	pass


# test scanning success with invalid formatting
func  	 test_b21 		 ( 	 ) 	 	  -> void:
	pass


# finally verify all tests are found
func after() -> void:
	assert_array(get_children())\
		.extract("get_name")\
		.contains_exactly(["test_example", "test_b", "test_b2", "test_b21"])
