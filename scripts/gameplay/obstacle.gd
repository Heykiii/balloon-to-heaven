class_name Obstacle
extends Node3D
## One hazard: data, movement behaviour, timed telegraph state, collision and its 3D look.

enum HazardState { IDLE, WARNING, ACTIVE }

var data: Dictionary
var pos := Vector3.ZERO
var anchor := Vector3.ZERO
var offset := Vector3.ZERO
var box := Vector2.ZERO
var radius := 0.5
var age := 0.0
var speed_mul := 1.0
var scroll_factor := 1.0
var direction := 1.0
var phase := 0.0
var custom_velocity := Vector3.ZERO
var use_custom_velocity := false
var collision_enabled := true
var alive := false
var despawn_after_active := false
var warning_override := -1.0

# timed hazard
var _t_state := 0 # 0 idle, 1 warning, 2 active, 3 cooldown
var _t_timer := 0.0
var _homing_vx := 0.0

# view
var body: Node3D
var warn_node: Node3D
var active_node: Node3D
var sized: Node3D
var spinner: Node3D
var wings: Array = []
var spinners: Array = [] # [node, deg/s Vector3]
var trail: CPUParticles3D
var _view_time := 0.0
var _vstate := HazardState.ACTIVE

func setup(p_data: Dictionary) -> void:
	data = p_data
	name = "Obstacle_" + str(data.id)
	ObstacleModel.build(self)

func is_box() -> bool:
	return data.box != Vector2.ZERO

func reset_parameters() -> void:
	box = data.box
	offset = Vector3.ZERO
	speed_mul = 1.0
	scroll_factor = 1.0
	direction = 1.0
	custom_velocity = Vector3.ZERO
	use_custom_velocity = false
	despawn_after_active = false
	warning_override = -1.0
	_homing_vx = 0.0

func spawn(p: Vector3, ctx: LevelCtx) -> void:
	pos = p
	anchor = p
	age = 0.0
	alive = true
	visible = true
	collision_enabled = not data.timed
	radius = data.radius
	phase = ctx.rng.randf() * TAU
	position = pos
	set_facing(direction)
	if is_box(): set_size(box)
	if trail: trail.restart()
	if data.timed:
		_t_state = 0
		_t_timer = 0.0 if warning_override >= 0.0 else ctx.rng.randf() * 0.6
		collision_enabled = false
		set_hazard_state(HazardState.IDLE)
	if data.harmless:
		direction = -1.0 if ctx.rng.randf() < 0.5 else 1.0
		set_facing(direction)
		set_hazard_state(HazardState.ACTIVE)

func top() -> float: return pos.y + (box.y * 0.5 if is_box() else radius)
func bottom() -> float: return pos.y - (box.y * 0.5 if is_box() else radius)

func overlaps(center: Vector2, r: float) -> bool:
	if not is_box():
		var rr := radius + r
		return Vector2(center.x - pos.x, center.y - pos.y).length_squared() <= rr * rr
	var hx := box.x * 0.5
	var hy := box.y * 0.5
	var cx := clampf(center.x, pos.x - hx, pos.x + hx)
	var cy := clampf(center.y, pos.y - hy, pos.y + hy)
	return Vector2(center.x - cx, center.y - cy).length_squared() <= r * r

func tick(ctx: LevelCtx, dt: float) -> void:
	age += dt
	var scroll := ctx.scroll_speed * scroll_factor * dt
	pos.y -= scroll
	anchor.y -= scroll
	if use_custom_velocity:
		pos += custom_velocity * dt
	else:
		_behave(ctx, dt)
	if data.timed: _tick_timed(ctx, dt)
	position = pos

func _behave(ctx: LevelCtx, dt: float) -> void:
	var s: float = data.speed * speed_mul
	match int(data.move):
		C.Move.CROSS:
			pos.x += direction * s * dt
			pos.y = anchor.y + sin(age * 2.5 + phase) * 0.25
		C.Move.PATROL:
			var amp: float = maxf(0.1, data.amplitude)
			pos.x = anchor.x + sin(age * (s / amp) + phase) * amp
		C.Move.FALLING:
			pos.y -= s * dt
		C.Move.DIAGONAL:
			pos.x += direction * s * 0.35 * dt
			pos.y -= s * dt
		C.Move.CIRCULAR:
			var r: float = maxf(0.2, data.amplitude)
			var a := age * s * direction + phase
			pos.x = anchor.x + cos(a) * r
			pos.y = anchor.y + sin(a) * r
		C.Move.HOMING:
			# Tracks only while well above the balloon, then commits: a late sidestep always dodges.
			var can_track: bool = pos.y - ctx.balloon_y > 4.0 and not ctx.abilities.is_invisible()
			if can_track:
				var desired := clampf(ctx.balloon.x - pos.x, -1.0, 1.0) * s * 0.8
				_homing_vx = move_toward(_homing_vx, desired, float(data.homing) * dt * 2.0)
			pos.x += _homing_vx * dt
			pos.y -= s * 0.45 * dt
			if absf(_homing_vx) > 0.2: set_facing(_homing_vx)
		C.Move.SWARM:
			anchor.x += direction * s * 0.35 * dt
			var limit := ctx.half_width - 1.0
			if anchor.x > limit: direction = -1.0
			elif anchor.x < -limit: direction = 1.0
			var wob: float = data.amplitude * 0.35
			pos.x = anchor.x + offset.x + sin(age * 3.1 + phase) * wob
			pos.y = anchor.y + offset.y + cos(age * 2.7 + phase) * wob
			set_facing(direction)
		C.Move.BOB:
			pos.y = anchor.y + sin(age * 2.0 + phase) * float(data.amplitude)

## idle -> warning (telegraph, harmless) -> active (damaging) -> cooldown.
## Warning only starts once fully on screen, so the player always sees the telegraph.
func _tick_timed(ctx: LevelCtx, dt: float) -> void:
	_t_timer -= dt
	match _t_state:
		0, 3:
			var on_screen := top() < ctx.top + 0.5 or scroll_factor == 0.0
			if _t_timer <= 0.0 and on_screen:
				_t_state = 1
				_t_timer = warning_override if warning_override >= 0.0 else float(data.warn)
				set_hazard_state(HazardState.WARNING)
		1:
			if _t_timer <= 0.0:
				_t_state = 2
				_t_timer = float(data.active)
				collision_enabled = true
				set_hazard_state(HazardState.ACTIVE)
				Sound.play("thunder" if int(data.dmg) == C.Dmg.ELECTRIC else "boss_attack", 0.6)
		2:
			if _t_timer <= 0.0:
				collision_enabled = false
				set_hazard_state(HazardState.IDLE)
				if despawn_after_active:
					alive = false
					return
				_t_state = 3
				_t_timer = float(data.cooldown)

# ---------------------------------------------------------------- view

func set_facing(dir: float) -> void:
	if body: body.scale.x = absf(body.scale.x) * (-1.0 if dir < 0.0 else 1.0)

func set_size(size: Vector2) -> void:
	if sized: sized.scale = Vector3(size.x, size.y, sized.scale.z)

func set_hazard_state(s: int) -> void:
	_vstate = s
	if warn_node: warn_node.visible = s == HazardState.WARNING
	if active_node: active_node.visible = s == HazardState.ACTIVE
	if body and data.timed: body.visible = true

func _process(dt: float) -> void:
	_view_time += dt
	if spinner and float(data.spin) != 0.0:
		spinner.rotation_degrees += Vector3(0.3, 1.0, 0.6) * float(data.spin) * dt
	for s in spinners:
		s[0].rotation_degrees += s[1] * dt
	# wings: [node, axis (0 x / 1 y), base_deg, amp_deg, speed]
	for w in wings:
		var r: Vector3 = w[0].rotation_degrees
		r[w[1]] = w[2] + w[3] * sin((_view_time + phase) * w[4])
		w[0].rotation_degrees = r
	if warn_node and _vstate == HazardState.WARNING:
		warn_node.visible = fmod(_view_time, 0.24) < 0.16
	if active_node and _vstate == HazardState.ACTIVE and data.timed:
		active_node.scale.x = 1.0 + sin(_view_time * 40.0) * 0.08
