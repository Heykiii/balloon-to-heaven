class_name FX
extends Node3D
## Pooled one-shot particle bursts and shared looping effects for the current 3D scene.

static var current: FX
var _pool: Array[CPUParticles3D] = []
var _next := 0
var density := 1.0

func _enter_tree() -> void:
	current = self

func _exit_tree() -> void:
	if current == self: current = null

func _ready() -> void:
	for i in 12:
		var p := _make()
		_pool.append(p)

func _make() -> CPUParticles3D:
	var p := CPUParticles3D.new()
	p.one_shot = true
	p.emitting = false
	p.explosiveness = 1.0
	p.amount = 24
	p.lifetime = 0.7
	p.direction = Vector3.UP
	p.spread = 180.0
	p.gravity = Vector3(0, -3, 0)
	p.local_coords = false
	var q := QuadMesh.new()
	q.size = Vector2.ONE
	var m := Kit.unlit_instance(Color.WHITE, true, Kit.soft_circle())
	m.billboard_mode = BaseMaterial3D.BILLBOARD_PARTICLES
	m.billboard_keep_scale = true
	m.vertex_color_use_as_albedo = true
	q.material = m
	p.mesh = q
	var curve := Curve.new()
	curve.add_point(Vector2(0, 1))
	curve.add_point(Vector2(1, 0))
	p.scale_amount_curve = curve
	add_child(p)
	return p

static func burst(pos: Vector3, color: Color, count := 16, speed := 3.0, size := 0.3, life := 0.6) -> void:
	if current == null: return
	current._burst(pos, color, count, speed, size, life)

func _burst(pos: Vector3, color: Color, count: int, speed: float, size: float, life: float) -> void:
	var p := _pool[_next]
	_next = (_next + 1) % _pool.size()
	p.emitting = false
	p.global_position = pos
	p.amount = maxi(2, int(count * density))
	p.lifetime = life
	p.initial_velocity_min = speed * 0.5
	p.initial_velocity_max = speed
	p.scale_amount_min = size * 0.6
	p.scale_amount_max = size * 1.3
	p.color = color
	p.restart()
	p.emitting = true

## Continuous emitter (trails, weather, auras).
static func emitter(parent: Node, color: Color, amount: int, life: float, size: float, velocity: Vector3, spread := 20.0, box := Vector3.ZERO, additive := true) -> CPUParticles3D:
	var p := CPUParticles3D.new()
	p.amount = maxi(1, amount)
	p.lifetime = life
	p.direction = velocity.normalized() if velocity.length() > 0.001 else Vector3.DOWN
	p.initial_velocity_min = velocity.length() * 0.7
	p.initial_velocity_max = velocity.length() * 1.2
	p.spread = spread
	p.gravity = Vector3.ZERO
	p.local_coords = false
	p.scale_amount_min = size * 0.6
	p.scale_amount_max = size * 1.2
	p.color = color
	if box != Vector3.ZERO:
		p.emission_shape = CPUParticles3D.EMISSION_SHAPE_BOX
		p.emission_box_extents = box
	var q := QuadMesh.new()
	var m := Kit.unlit_instance(Color.WHITE, additive, Kit.soft_circle())
	m.billboard_mode = BaseMaterial3D.BILLBOARD_PARTICLES
	m.billboard_keep_scale = true
	m.vertex_color_use_as_albedo = true
	q.material = m
	p.mesh = q
	var curve := Curve.new()
	curve.add_point(Vector2(0, 0.6))
	curve.add_point(Vector2(0.2, 1))
	curve.add_point(Vector2(1, 0))
	p.scale_amount_curve = curve
	parent.add_child(p)
	return p
