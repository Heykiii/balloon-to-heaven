extends SceneTree
## Compiles every script in the project; exits 1 if any fails (catches typos in rarely used screens).

func _initialize() -> void:
	var failed := 0
	var files := _collect("res://scripts")
	for f in files:
		var s = load(f)
		if s == null or not (s as GDScript).can_instantiate():
			print("FAILED: ", f)
			failed += 1
	print("Checked %d scripts, %d failed" % [files.size(), failed])
	quit(1 if failed > 0 else 0)

func _collect(dir: String) -> Array:
	var out := []
	var d := DirAccess.open(dir)
	for sub in d.get_directories(): out.append_array(_collect(dir + "/" + sub))
	for f in d.get_files():
		if f.ends_with(".gd"): out.append(dir + "/" + f)
	return out
