class_name GdUnitOrphanNodesMonitor
extends GdUnitMonitor

var _initial_count := 0
var _orphan_count := 0
var _orphan_detection_enabled :bool


func _init(name :String = "") -> void:
	super("OrphanNodesMonitor:" + name)
	_orphan_detection_enabled = GdUnitSettings.is_verbose_orphans()


func start() -> void:
	_initial_count = _orphans()


func stop() -> void:
	_orphan_count = max(0, _orphans() - _initial_count)


func _orphans() -> int:
	return Performance.get_monitor(Performance.OBJECT_ORPHAN_NODE_COUNT) as int


func orphan_nodes() -> int:
	return _orphan_count if _orphan_detection_enabled else 0
