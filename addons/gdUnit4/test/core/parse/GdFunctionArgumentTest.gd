# GdUnit generated TestSuite
class_name GdFunctionArgumentTest
extends GdUnitTestSuite
@warning_ignore('unused_parameter')
@warning_ignore('return_value_discarded')

# TestSuite generated from
const __source = 'res://addons/gdUnit4/src/core/parse/GdFunctionArgument.gd'


func test__parse_argument_as_array_typ1() -> void:
	var test_parameters := """[
		[1, "flowchart TD\nid>This is a  flag shaped node]"],
		[
			2,
			"flowchart TD\nid(((This is a\tdouble circle node)))"
		],
		[3,
			"flowchart TD\nid((This is a circular node))"],
		[
			4, "flowchart TD\nid>This is a flag shaped node]"
		],
		[5, "flowchart TD\nid{'This is a rhombus node'}"],
		[6, 'flowchart TD\nid((This is a circular node))'],
		[7, 'flowchart TD\nid>This is a flag shaped node]'], [8, 'flowchart TD\nid{"This is a rhombus node"}'],
		[9, \"\"\"
			flowchart TD
			id{"This is a  rhombus node"}
			\"\"\"]
		]"""

	var fa := GdFunctionArgument.new(GdFunctionArgument.ARG_PARAMETERIZED_TEST, TYPE_STRING, test_parameters)
	assert_array(fa.parameter_sets()).contains_exactly([
		"""[1, "flowchart TDid>This is a  flag shaped node]"]""",
		"""[2, "flowchart TDid(((This is a\tdouble circle node)))"]""",
		"""[3, "flowchart TDid((This is a circular node))"]""",
		"""[4, "flowchart TDid>This is a flag shaped node]"]""",
		"""[5, "flowchart TDid{'This is a rhombus node'}"]""",
		"""[6, 'flowchart TDid((This is a circular node))']""",
		"""[7, 'flowchart TDid>This is a flag shaped node]']""",
		"""[8, 'flowchart TDid{"This is a rhombus node"}']""",
		"""[9, \"\"\"flowchart TDid{"This is a  rhombus node"}\"\"\"]"""
		]
	)


func test__parse_argument_as_array_typ2() -> void:
	var test_parameters := """[
		["test_a", null, "LOG", {}],
		[
			"test_b",
			Node2D,
			null,
			{Node2D: "ER,ROR"}
		],
		[
			"test_c",
			Node2D,
			"LOG",
			{Node2D: "LOG"}
		]
	]"""
	var fa := GdFunctionArgument.new(GdFunctionArgument.ARG_PARAMETERIZED_TEST, TYPE_STRING, test_parameters)
	assert_array(fa.parameter_sets()).contains_exactly([
		"""["test_a", null, "LOG", {}]""",
		"""["test_b", Node2D, null, {Node2D: "ER,ROR"}]""",
		"""["test_c", Node2D, "LOG", {Node2D: "LOG"}]"""
		]
	)


func test__parse_argument_as_array_bad_formatted() -> void:
	var test_parameters := """[
		["test_a", null, "LOG", {}],
		[
				"test_b",
			Node2D,
			null,
			{Node2D: "ER,ROR"}
		],
			[
			"test_c",
			Node2D,
			"LOG",
			{Node2D: "LOG 1"}
		]

		  ]"""
	var fa := GdFunctionArgument.new(GdFunctionArgument.ARG_PARAMETERIZED_TEST, TYPE_STRING, test_parameters)
	assert_array(fa.parameter_sets()).contains_exactly([
		"""["test_a", null, "LOG", {}]""",
		"""["test_b", Node2D, null, {Node2D: "ER,ROR"}]""",
		"""["test_c", Node2D, "LOG", {Node2D: "LOG 1"}]"""
		]
	)


func test_parse_argument_as_array_ends_with_additional_comma() -> void:
	var test_parameters := """
			[
			[true, 'bool'],
			[42, 'int'],
			['foo', 'String'],
		]"""
	var fa := GdFunctionArgument.new(GdFunctionArgument.ARG_PARAMETERIZED_TEST, TYPE_STRING, test_parameters)
	assert_array(fa.parameter_sets()).contains_exactly([
		"""[true, 'bool']""",
		"""[42, 'int']""",
		"""['foo', 'String']"""
		]
	)


func test__parse_argument_as_reference() -> void:
	var test_parameters := "_test_args()"

	var fa := GdFunctionArgument.new(GdFunctionArgument.ARG_PARAMETERIZED_TEST, TYPE_STRING, test_parameters)
	assert_array(fa.parameter_sets()).is_empty()


func test_parse_parameter_set_with_const_data_in_array() -> void:
	var test_parameters := "[_data1, _data2]"

	var fa := GdFunctionArgument.new(GdFunctionArgument.ARG_PARAMETERIZED_TEST, TYPE_STRING, test_parameters)
	assert_array(fa.parameter_sets()).contains_exactly(["_data1", "_data2"])
