extends Node
## Screen host (main scene): swaps screens with a fade, keeps a live 3D sky backdrop behind
## the menus, and shows toasts and achievement pop-ups on top of everything.

var screen: Control
var screen_name := ""
var backdrop: Node3D
var backdrop_world := -1
var backdrop_balloon: BalloonModel
var _fade: ColorRect
var _toasts: VBoxContainer
var _busy := false

func _ready() -> void:
	Game.main = self
	var fx := FX.new()
	fx.name = "FX"
	add_child(fx)
	var top := CanvasLayer.new()
	top.layer = 10
	add_child(top)
	var toast_root := Control.new()
	UI.full(toast_root)
	toast_root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	top.add_child(toast_root)
	_toasts = UI.vbox(8)
	_toasts.set_anchors_and_offsets_preset(Control.PRESET_CENTER_TOP)
	_toasts.position.y = 210
	_toasts.alignment = BoxContainer.ALIGNMENT_BEGIN
	_toasts.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_toasts.grow_horizontal = Control.GROW_DIRECTION_BOTH
	toast_root.add_child(_toasts)
	var fade_layer := CanvasLayer.new()
	fade_layer.layer = 20
	add_child(fade_layer)
	_fade = ColorRect.new()
	_fade.color = Color(0.04, 0.07, 0.16, 1)
	UI.full(_fade)
	_fade.mouse_filter = Control.MOUSE_FILTER_IGNORE
	fade_layer.add_child(_fade)
	Events.toast.connect(_on_toast)
	Events.achievement_unlocked.connect(func(t): _on_toast("Achievement: " + t, UI.GOLD))
	get_tree().root.size_changed.connect(_on_resize)
	Game.apply_settings()
	show_screen("boot", true)

func show_screen(n: String, instant := false) -> void:
	if _busy: return
	_busy = true
	if not instant:
		var tw := create_tween()
		tw.tween_property(_fade, "color:a", 1.0, 0.18)
		await tw.finished
	if screen:
		screen.queue_free()
		await get_tree().process_frame
	get_tree().paused = false
	screen_name = n
	var script: GDScript = load(Game.SCREENS[n])
	screen = script.new()
	screen.name = n.capitalize() + "Screen"
	UI.full(screen)
	screen.theme = UI.theme()
	var w: int = screen.backdrop_world() if screen.has_method("backdrop_world") else 0
	set_backdrop(w)
	if backdrop_balloon:
		backdrop_balloon.queue_free()
		backdrop_balloon = null
	add_child(screen)
	_busy = false
	var tw2 := create_tween()
	tw2.tween_property(_fade, "color:a", 0.0, 0.25)

func back_pressed() -> void:
	if screen and screen.has_method("on_back"): screen.on_back()

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"): back_pressed()

# ---------------------------------------------------------------- 3D backdrop for menus

func set_backdrop(world_number: int) -> void:
	if world_number == backdrop_world: return
	if backdrop:
		backdrop.queue_free()
		backdrop = null
		backdrop_balloon = null
	backdrop_world = world_number
	if world_number <= 0: return
	var w := Game.db.world(world_number)
	backdrop = Node3D.new()
	backdrop.name = "Backdrop"
	add_child(backdrop)
	move_child(backdrop, 1)
	var cam := Camera3D.new()
	cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.size = C.CAMERA_HEIGHT
	cam.keep_aspect = Camera3D.KEEP_HEIGHT
	cam.position = Vector3(0, 0, 30)
	cam.far = 200
	backdrop.add_child(cam)
	cam.current = true
	_fit_camera(cam)
	SkyEnvironment.lights(backdrop, w)
	var env := SkyEnvironment.build(w, cam.size * 0.5, cam.size * 0.5 * _aspect(), world_number * 31)
	env.auto_scroll = 1.6
	backdrop.add_child(env)

func show_backdrop_balloon(b: Dictionary, stage: int, pos := Vector3(0, -1.5, 0), scale_v := 1.6) -> void:
	if not backdrop: return
	if backdrop_balloon: backdrop_balloon.queue_free()
	var trail := Game.db.trail(Game.workshop.equipped_trail())
	backdrop_balloon = BalloonModel.build(b, stage, trail)
	backdrop_balloon.position = pos
	backdrop_balloon.scale = Vector3.ONE * scale_v
	backdrop.add_child(backdrop_balloon)

func _aspect() -> float:
	var s := get_viewport().get_visible_rect().size
	return s.x / maxf(1.0, s.y)

func _fit_camera(cam: Camera3D) -> void:
	var aspect := maxf(0.3, _aspect())
	cam.size = maxf(C.CAMERA_HEIGHT, 2.0 * C.MAX_HALF_WIDTH / aspect)

func _on_resize() -> void:
	if backdrop:
		for c in backdrop.get_children():
			if c is Camera3D: _fit_camera(c)

# ---------------------------------------------------------------- toasts

func _on_toast(text: String, color := Color.WHITE) -> void:
	var p := UI.panel(Color(0.06, 0.1, 0.22, 0.88), 22)
	p.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var l := UI.label(text, 22, color)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	p.add_child(l)
	_toasts.add_child(p)
	if _toasts.get_child_count() > 3: _toasts.get_child(0).queue_free()
	p.modulate.a = 0.0
	var tw := p.create_tween()
	tw.tween_property(p, "modulate:a", 1.0, 0.2)
	tw.tween_interval(1.8)
	tw.tween_property(p, "modulate:a", 0.0, 0.4)
	tw.tween_callback(p.queue_free)
