class_name BalloonController
extends RefCounted
## Horizontal steering: touch/mouse drag with smoothing and inertia, plus keyboard for desktop.
## The gameplay screen forwards input events; ticking happens in the level loop.

var ctx: LevelCtx
var model: BalloonModel
var x := 0.0
var y := 0.0
var velocity := 0.0
var input_enabled := true
var external_velocity := 0.0
var slipperiness := 0.0
var _target := 0.0
var _smoothed := 0.0
var _dragging := false
var _drag_offset := 0.0
var _speed_mul := 1.0
var _keyboard := false

func _init(p_ctx: LevelCtx, p_model: BalloonModel, speed_multiplier: float) -> void:
	ctx = p_ctx
	model = p_model
	_speed_mul = maxf(0.5, speed_multiplier)
	model.max_tilt = C.MAX_TILT

func set_vertical(p_y: float) -> void:
	y = p_y
	model.position = Vector3(x, y, 0)

func _screen_to_world_x(screen: Vector2) -> float:
	if ctx.camera == null: return x
	return ctx.camera.project_position(screen, 20.0).x - ctx.camera.position.x

## Forwarded from the gameplay screen (_unhandled_input), so presses on HUD buttons never steer.
func handle_input(event: InputEvent) -> void:
	if not input_enabled:
		_dragging = false
		return
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_dragging = true
			var wx := _screen_to_world_x(event.position)
			_drag_offset = (x - wx) * 0.35 # mostly direct, but no jump
			_target = wx + _drag_offset
			_smoothed = x
		else:
			_dragging = false
	elif event is InputEventMouseMotion and _dragging:
		_target = _screen_to_world_x(event.position) + _drag_offset

func tick(dt: float) -> void:
	if dt <= 0.0: return
	if not input_enabled: _dragging = false
	var axis := 0.0
	if input_enabled and not _dragging:
		axis = Input.get_axis("ui_left", "ui_right")
		if Input.is_key_pressed(KEY_A): axis -= 1.0
		if Input.is_key_pressed(KEY_D): axis += 1.0
		axis = clampf(axis, -1.0, 1.0)
	_keyboard = absf(axis) > 0.1
	if _keyboard:
		_target = x + axis * 2.5
		_smoothed = _target
	var mods := {"acceleration": 1.0, "max_speed": 1.0}
	if ctx.abilities: ctx.abilities.modify_movement(mods)
	var grip := lerpf(1.0, 0.35, clampf(slipperiness, 0.0, 1.0))
	var max_speed := C.MAX_VELOCITY * _speed_mul * float(mods.max_speed)
	var accel := C.H_ACCEL * _speed_mul * float(mods.acceleration) * grip
	var limit := ctx.half_width - C.BALLOON_RADIUS - C.EDGE_MARGIN
	_target = clampf(_target, -limit, limit)
	if _dragging or _keyboard:
		_smoothed = lerpf(_smoothed, _target, 1.0 - exp(-dt / maxf(0.01, C.SMOOTHING)))
		var desired := clampf((_smoothed - x) * C.H_SPEED * _speed_mul * float(mods.max_speed), -max_speed, max_speed)
		velocity = move_toward(velocity, desired, accel * dt)
	else:
		# Inertia: glide to a stop. Ice makes the glide longer.
		velocity *= exp(-C.RELEASE_DAMPING * grip * dt)
		_smoothed = x
	var nx := x + (velocity + external_velocity) * dt
	var clamped := ctx.clamp_x(nx, C.BALLOON_RADIUS, C.EDGE_MARGIN)
	if not is_equal_approx(clamped, nx): velocity = 0.0
	x = clamped
	model.position = Vector3(x, y, 0)
	model.set_lateral_velocity(velocity + external_velocity * 0.5)

## Used by tests and the autopilot: steer toward a world x as if dragging.
func steer_to(world_x: float) -> void:
	_dragging = true
	_target = world_x

func release() -> void:
	_dragging = false

func teleport(dx: float) -> void:
	var from := x
	x = ctx.clamp_x(x + dx, C.BALLOON_RADIUS, C.EDGE_MARGIN)
	_target = x
	_smoothed = x
	if _dragging: _drag_offset += x - from
	model.position = Vector3(x, y, 0)
	FX.burst(Vector3(from, y, 1), Color(0.4, 1, 1), 18, 4.0, 0.3)
	FX.burst(Vector3(x, y, 1), Color.WHITE, 18, 3.0, 0.3)
