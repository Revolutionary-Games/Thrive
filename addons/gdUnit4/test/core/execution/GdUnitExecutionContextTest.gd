# GdUnit generated TestSuite
class_name GdUnitExecutionContextTest
extends GdUnitTestSuite
@warning_ignore('unused_parameter')
@warning_ignore('return_value_discarded')


func test_report_collectors() -> void:
	# setup
	var ts :GdUnitTestSuite = auto_free(GdUnitTestSuite.new())
	var tc :_TestCase = auto_free(create_test_case("test_case1", 0, ""))
	ts.add_child(tc)
	var ec1 := GdUnitExecutionContext.of_test_suite(ts)
	var ec2 := GdUnitExecutionContext.of_test_case(ec1, tc)
	var ec3 := GdUnitExecutionContext.of(ec2)

	# add reports
	var failure11 := GdUnitReport.new().create(GdUnitReport.FAILURE, 1, "error_ec11")
	ec1.add_report(failure11)
	var failure21 := GdUnitReport.new().create(GdUnitReport.FAILURE, 3, "error_ec21")
	var failure22 := GdUnitReport.new().create(GdUnitReport.FAILURE, 3, "error_ec22")
	ec2.add_report(failure21)
	ec2.add_report(failure22)
	var failure31 := GdUnitReport.new().create(GdUnitReport.FAILURE, 3, "error_ec31")
	var failure32 := GdUnitReport.new().create(GdUnitReport.FAILURE, 3, "error_ec32")
	var failure33 := GdUnitReport.new().create(GdUnitReport.FAILURE, 3, "error_ec33")
	ec3.add_report(failure31)
	ec3.add_report(failure32)
	ec3.add_report(failure33)
	# verify
	assert_array(ec1.reports()).contains_exactly([failure11])
	assert_array(ec2.reports()).contains_exactly([failure21, failure22])
	assert_array(ec3.reports()).contains_exactly([failure31, failure32, failure33])
	ec1.dispose()


func test_has_and_count_failures() -> void:
	# setup
	var ts :GdUnitTestSuite = auto_free(GdUnitTestSuite.new())
	var tc :_TestCase = auto_free(create_test_case("test_case1", 0, ""))
	ts.add_child(tc)
	var ec1 := GdUnitExecutionContext.of_test_suite(ts)
	var ec2 := GdUnitExecutionContext.of_test_case(ec1, tc)
	var ec3 := GdUnitExecutionContext.of(ec2)

	# precheck
	assert_that(ec1.has_failures()).is_false()
	assert_that(ec1.count_failures(true)).is_equal(0)
	assert_that(ec2.has_failures()).is_false()
	assert_that(ec2.count_failures(true)).is_equal(0)
	assert_that(ec3.has_failures()).is_false()
	assert_that(ec3.count_failures(true)).is_equal(0)

	# add four failure report to test
	ec3.add_report(GdUnitReport.new().create(GdUnitReport.FAILURE, 42, "error_ec31"))
	ec3.add_report(GdUnitReport.new().create(GdUnitReport.FAILURE, 43, "error_ec32"))
	ec3.add_report(GdUnitReport.new().create(GdUnitReport.FAILURE, 44, "error_ec33"))
	ec3.add_report(GdUnitReport.new().create(GdUnitReport.FAILURE, 45, "error_ec34"))
	# verify
	assert_that(ec1.has_failures()).is_true()
	assert_that(ec1.count_failures(true)).is_equal(4)
	assert_that(ec2.has_failures()).is_true()
	assert_that(ec2.count_failures(true)).is_equal(4)
	assert_that(ec3.has_failures()).is_true()
	assert_that(ec3.count_failures(true)).is_equal(4)

	# add two failure report to test_case_stage
	ec2.add_report(GdUnitReport.new().create(GdUnitReport.FAILURE, 42, "error_ec21"))
	ec2.add_report(GdUnitReport.new().create(GdUnitReport.FAILURE, 43, "error_ec22"))
	# verify
	assert_that(ec1.has_failures()).is_true()
	assert_that(ec1.count_failures(true)).is_equal(6)
	assert_that(ec2.has_failures()).is_true()
	assert_that(ec2.count_failures(true)).is_equal(6)
	assert_that(ec3.has_failures()).is_true()
	assert_that(ec3.count_failures(true)).is_equal(4)

	# add one failure report to test_suite_stage
	ec1.add_report(GdUnitReport.new().create(GdUnitReport.FAILURE, 42, "error_ec1"))
	# verify
	assert_that(ec1.has_failures()).is_true()
	assert_that(ec1.count_failures(true)).is_equal(7)
	assert_that(ec2.has_failures()).is_true()
	assert_that(ec2.count_failures(true)).is_equal(6)
	assert_that(ec3.has_failures()).is_true()
	assert_that(ec3.count_failures(true)).is_equal(4)
	ec1.dispose()


static func create_test_case(p_name: String, p_line_number: int, p_script_path: String) -> _TestCase:
	var test_case := GdUnitTestCase.new()
	test_case.test_name = p_name
	test_case.line_number = p_line_number
	test_case.source_file = p_script_path
	var attribute := TestCaseAttribute.new()
	return _TestCase.new(test_case, attribute, null)
