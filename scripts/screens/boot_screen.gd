extends Control
## Boot: logo, loads the save (repairing it if needed) and synthesises all audio over a few frames.

var _bar: ProgressBar
var _step := 0
var _done := false
var _t := 0.0

func backdrop_world() -> int: return 1

func _ready() -> void:
	var v := UI.vbox(18)
	v.set_anchors_and_offsets_preset(Control.PRESET_CENTER)
	v.grow_horizontal = Control.GROW_DIRECTION_BOTH
	v.grow_vertical = Control.GROW_DIRECTION_BOTH
	v.custom_minimum_size = Vector2(420, 0)
	add_child(v)
	var icon := Icon.make("balloon", 120, Color("#FF6B6B"))
	icon.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	v.add_child(icon)
	v.add_child(UI.label("Balloon to Heaven", 46, Color.WHITE))
	v.add_child(UI.label("Rise above the clouds", 22, UI.TEXT_DIM))
	_bar = ProgressBar.new()
	_bar.show_percentage = false
	_bar.custom_minimum_size = Vector2(360, 18)
	_bar.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	_bar.add_theme_stylebox_override("background", UI.box(Color(0, 0, 0, 0.35), 9))
	_bar.add_theme_stylebox_override("fill", UI.box(UI.GOLD, 9))
	_bar.max_value = Sound.steps_total()
	v.add_child(UI.spacer(20))
	v.add_child(_bar)
	if Game.save.recovered_from_corruption:
		Game.toast("Save was damaged and has been restored" + (" from backup." if Game.save.loaded_from_backup else "."))

func _process(dt: float) -> void:
	_t += dt
	if _done: return
	# A few synthesis steps per frame keeps the page responsive on the web.
	for i in 3:
		if Sound.prepare_step(_step):
			_done = true
			break
		_step += 1
	_bar.value = _step
	if _done:
		await get_tree().create_timer(0.3).timeout
		Game.goto("menu")
