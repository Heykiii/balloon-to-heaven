class_name ModelPreview
extends SubViewportContainer
## A small 3D stage inside the UI (own world) that shows a slowly turning balloon.

var viewport: SubViewport
var holder: Node3D
var model: Node3D
var _t := 0.0

func _init() -> void:
	stretch = true
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	viewport = SubViewport.new()
	viewport.own_world_3d = true
	viewport.transparent_bg = true
	viewport.msaa_3d = Viewport.MSAA_2X
	add_child(viewport)
	var cam := Camera3D.new()
	cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.size = 6.2
	cam.position = Vector3(0, 0.3, 20)
	viewport.add_child(cam)
	SkyEnvironment.lights(viewport, {"sky_bottom": Color(0, 0, 0, 0), "ambient": Color(0.75, 0.78, 0.9), "light": Color(1, 0.97, 0.9)})
	for c in viewport.get_children():
		if c is WorldEnvironment: c.environment.background_mode = Environment.BG_CLEAR_COLOR
	holder = Node3D.new()
	viewport.add_child(holder)

func show_balloon(b: Dictionary, stage: int, trail: Dictionary = {}, dimmed := false) -> void:
	if model: model.queue_free()
	var m := BalloonModel.build(b, stage, trail)
	m.position = Vector3(0, -0.4, 0)
	m.scale = Vector3.ONE * 1.15
	holder.add_child(m)
	if dimmed: m.set_invisible(true)
	model = m

func _process(dt: float) -> void:
	_t += dt
	holder.rotation_degrees.y = sin(_t * 0.6) * 25.0
