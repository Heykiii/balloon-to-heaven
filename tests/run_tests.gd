extends SceneTree
## Headless test runner:  godot --headless --path . -s res://tests/run_tests.gd
## Exits with code 1 if any test fails.

var failures := 0
var checks := 0
var current := ""

func _initialize() -> void:
	var suites := [load("res://tests/test_logic.gd").new()]
	for suite in suites:
		suite.runner = self
		for m in suite.get_method_list():
			var n: String = m.name
			if not n.begins_with("test_"): continue
			current = n
			var before := failures
			suite.call(n)
			print(("  PASS  " if failures == before else "  FAIL  ") + n)
	print("\n%d checks, %d failures" % [checks, failures])
	quit(1 if failures > 0 else 0)

func check(cond: bool, msg: String) -> void:
	checks += 1
	if not cond:
		failures += 1
		printerr("    [%s] %s" % [current, msg])
