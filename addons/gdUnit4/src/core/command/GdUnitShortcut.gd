class_name GdUnitShortcut
extends RefCounted


enum ShortCut {
	NONE,
	RUN_TESTS_OVERALL,
	RUN_TESTCASE,
	RUN_TESTCASE_DEBUG,
	RUN_TESTSUITE,
	RUN_TESTSUITE_DEBUG,
	RERUN_TESTS,
	RERUN_TESTS_DEBUG,
	STOP_TEST_RUN,
	CREATE_TEST,
}

const DEFAULTS_MACOS := {
	ShortCut.NONE : [],
	ShortCut.RUN_TESTCASE : [Key.KEY_META, Key.KEY_ALT, Key.KEY_F5],
	ShortCut.RUN_TESTCASE_DEBUG : [Key.KEY_META, Key.KEY_ALT, Key.KEY_F6],
	ShortCut.RUN_TESTSUITE : [Key.KEY_META, Key.KEY_ALT, Key.KEY_F5],
	ShortCut.RUN_TESTSUITE_DEBUG : [Key.KEY_META, Key.KEY_ALT, Key.KEY_F6],
	ShortCut.RUN_TESTS_OVERALL : [Key.KEY_ALT, Key.KEY_F7],
	ShortCut.STOP_TEST_RUN : [Key.KEY_ALT, Key.KEY_F8],
	ShortCut.RERUN_TESTS : [Key.KEY_ALT, Key.KEY_F5],
	ShortCut.RERUN_TESTS_DEBUG : [Key.KEY_ALT, Key.KEY_F6],
	ShortCut.CREATE_TEST : [Key.KEY_META, Key.KEY_ALT, Key.KEY_F10],
}

const DEFAULTS_WINDOWS := {
	ShortCut.NONE : [],
	ShortCut.RUN_TESTCASE : [Key.KEY_CTRL, Key.KEY_ALT, Key.KEY_F5],
	ShortCut.RUN_TESTCASE_DEBUG : [Key.KEY_CTRL,Key.KEY_ALT, Key.KEY_F6],
	ShortCut.RUN_TESTSUITE : [Key.KEY_CTRL, Key.KEY_ALT, Key.KEY_F5],
	ShortCut.RUN_TESTSUITE_DEBUG : [Key.KEY_CTRL,Key.KEY_ALT, Key.KEY_F6],
	ShortCut.RUN_TESTS_OVERALL : [Key.KEY_ALT, Key.KEY_F7],
	ShortCut.STOP_TEST_RUN : [Key.KEY_ALT, Key.KEY_F8],
	ShortCut.RERUN_TESTS : [Key.KEY_ALT, Key.KEY_F5],
	ShortCut.RERUN_TESTS_DEBUG : [Key.KEY_ALT, Key.KEY_F6],
	ShortCut.CREATE_TEST : [Key.KEY_CTRL, Key.KEY_ALT, Key.KEY_F10],
}


static func default_keys(shortcut :ShortCut) -> PackedInt32Array:
	match OS.get_name().to_lower():
		'windows':
			return DEFAULTS_WINDOWS[shortcut]
		'macos':
			return DEFAULTS_MACOS[shortcut]
		_:
			return DEFAULTS_WINDOWS[shortcut]
