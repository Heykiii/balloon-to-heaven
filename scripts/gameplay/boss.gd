class_name Boss
extends RefCounted
## Boss fight: enter -> fight through phases -> defeated. Health drains over the encounter and
## each sky crystal the player collects strikes the boss. Every attack leaves the safe corridor open.

enum State { ENTERING, FIGHTING, DEFEATED, DONE }

var ctx: LevelCtx
var data: Dictionary
var view: BossModel
var state := State.ENTERING
var health := 1.0
var phase_index := 0
var _timers: Array[float] = [0, 0, 0, 0, 0, 0, 0, 0]
var _pending_shots: Array[float] = []
var _shot_data: Dictionary = {}
var _shot_speed := 0.0
var _state_time := 0.0
var _move_time := 0.0
var _crystal_timer := 0.0
var _teleport_timer := 0.0
var pos := Vector3.ZERO
var _base_y := 0.0
var _sweep := 0.0

func _init(p_ctx: LevelCtx, d: Dictionary, parent: Node3D) -> void:
	ctx = p_ctx
	data = d
	view = BossModel.build(d)
	parent.add_child(view)
	_base_y = ctx.top - 4.8
	_sweep = ctx.half_width - 2.0
	pos = Vector3(0, ctx.top + 5.0, -1.0)
	view.position = pos
	_crystal_timer = float(d.crystal_interval) * 0.6
	ctx.coins.crystal_collected.connect(_on_crystal)
	Events.boss_health_changed.emit(1.0)
	if not d.phases.is_empty(): Events.boss_phase_started.emit(d.phases[0].name)
	Sound.play("boss_attack")
	if ctx.shake: ctx.shake.add(0.4)

func is_done() -> bool: return state == State.DONE
func is_defeated() -> bool: return state == State.DEFEATED or state == State.DONE

func phase() -> Dictionary:
	if data.phases.is_empty(): return {}
	return data.phases[clampi(phase_index, 0, data.phases.size() - 1)]

func tick(dt: float) -> void:
	if dt <= 0.0: return
	_state_time += dt
	match state:
		State.ENTERING:
			pos.y = lerpf(ctx.top + 5.0, _base_y, U.ease_out_cubic(clampf(_state_time / 2.5, 0.0, 1.0)))
			view.position = pos
			if _state_time >= 2.5:
				state = State.FIGHTING
				_state_time = 0.0
				_reset_timers(0.8)
		State.FIGHTING:
			_fight(dt)
		State.DEFEATED:
			pos.y -= dt * 1.2
			pos.x += sin(_state_time * 30.0) * 0.04
			view.position = pos
			view.scale = Vector3.ONE * lerpf(1.0, 0.2, clampf((_state_time - 1.5) / 1.2, 0.0, 1.0))
			if ctx.rng.randf() < 0.3:
				FX.burst(pos + Vector3(ctx.rng.randf_range(-1.5, 1.5), ctx.rng.randf_range(-1, 1), 2), data.glow, 10, 4.0, 0.4)
			if _state_time > 2.8:
				FX.burst(pos + Vector3(0, 0, 2), Color.WHITE, 60, 8.0, 0.6, 1.0)
				view.visible = false
				state = State.DONE

func _reset_timers(scale: float) -> void:
	var p := phase()
	if p.is_empty(): return
	for i in mini(p.attacks.size(), _timers.size()):
		_timers[i] = float(p.attacks[i].cooldown) * scale * (0.6 + 0.25 * i)

func _fight(dt: float) -> void:
	health -= dt / maxf(5.0, float(data.duration))
	_check_phase()
	Events.boss_health_changed.emit(maxf(0.0, health))
	if health <= 0.0:
		_defeat()
		return
	var p := phase()
	_move(p, dt)
	if not p.is_empty():
		for i in mini(p.attacks.size(), _timers.size()):
			_timers[i] -= dt
			if _timers[i] > 0.0: continue
			_timers[i] = float(p.attacks[i].cooldown)
			_execute(p.attacks[i], i)
	for i in range(_pending_shots.size() - 1, -1, -1):
		if _pending_shots[i] > _state_time: continue
		_pending_shots.remove_at(i)
		var target := Vector3(ctx.balloon.x, ctx.balloon_y, 0)
		var origin := pos + Vector3(0, -1.2, 0)
		origin.z = 0
		if not _shot_data.is_empty():
			ctx.obstacles.spawn_projectile(_shot_data, origin, (target - origin).normalized() * _shot_speed)
	_crystal_timer -= dt
	if _crystal_timer <= 0.0:
		_crystal_timer = float(data.crystal_interval)
		ctx.coins.spawn_crystal(ctx.lanes.corridor_center())

func _check_phase() -> void:
	while phase_index + 1 < data.phases.size() and health <= float(data.phases[phase_index + 1].threshold):
		phase_index += 1
		Events.boss_phase_started.emit(phase().name)
		view.intensity = 1.0 + phase_index * 0.25
		view.attack_pulse()
		if ctx.shake: ctx.shake.add(0.5)
		Sound.play("boss_attack")
		_reset_timers(0.5)

func _move(p: Dictionary, dt: float) -> void:
	if p.is_empty(): return
	_move_time += dt
	var speed := float(p.move_speed)
	match int(p.move):
		C.BossMove.SWEEP:
			pos.x = sin(_move_time * speed * 0.35) * _sweep
			pos.y = _base_y + sin(_move_time * 1.3) * 0.3
		C.BossMove.FIGURE8:
			pos.x = sin(_move_time * speed * 0.3) * _sweep
			pos.y = _base_y + sin(_move_time * speed * 0.6) * 0.8
		C.BossMove.TRACK:
			pos.x = move_toward(pos.x, clampf(ctx.balloon.x, -_sweep, _sweep), speed * dt)
			pos.y = _base_y + sin(_move_time * 1.5) * 0.25
		C.BossMove.HOVER:
			pos.x = sin(_move_time * 0.5) * 1.2
			pos.y = _base_y + sin(_move_time * 1.1) * 0.35
		C.BossMove.TELEPORT:
			_teleport_timer -= dt
			var period := maxf(2.0, 6.0 - speed)
			var k := 1.0
			if _teleport_timer < 0.4: k = clampf(_teleport_timer / 0.4, 0.0, 1.0)
			if _teleport_timer <= 0.0:
				pos.x = (ctx.rng.randf() * 2.0 - 1.0) * _sweep
				_teleport_timer = period
				FX.burst(pos + Vector3(0, 0, 2), data.glow, 30, 5.0, 0.4)
				Sound.play("teleport", 0.5)
			var appear := clampf((period - _teleport_timer) / 0.3, 0.0, 1.0)
			view.scale = Vector3.ONE * maxf(0.05, k if k < 1.0 else appear)
			pos.y = _base_y + sin(_move_time * 1.2) * 0.3
	view.position = pos

func _execute(atk: Dictionary, index: int) -> void:
	view.attack_pulse()
	Sound.play("boss_attack", 0.5)
	var lanes := ctx.lanes
	var proj := ctx.db.obstacle(str(atk.projectile))
	if proj.is_empty(): return
	var origin := Vector3(pos.x, pos.y - 1.2, 0)
	var speed := float(atk.speed)
	var count := int(atk.count)
	match int(atk.type):
		C.Attack.FAN:
			var n := maxi(2, count)
			var gap := 1 + ctx.rng.randi() % maxi(1, n - 2) # one missing ray: a guaranteed hole
			for i in n:
				if i == gap: continue
				var ang := deg_to_rad(lerpf(-55.0, 55.0, float(i) / (n - 1)))
				ctx.obstacles.spawn_projectile(proj, origin, Vector3(sin(ang), -cos(ang), 0) * speed)
		C.Attack.AIMED:
			_shot_data = proj
			_shot_speed = speed
			for i in maxi(1, count): _pending_shots.append(_state_time + i * 0.35)
		C.Attack.COLUMN:
			var chosen: Array[int] = []
			var tries := 0
			while chosen.size() < count and tries < 30:
				tries += 1
				var lane := ctx.rng.randi() % lanes.lane_count
				if lanes.is_corridor_lane(lane) or chosen.has(lane) or chosen.has(lane - 1) or chosen.has(lane + 1): continue
				chosen.append(lane)
			for lane in chosen:
				ctx.obstacles.spawn_column(proj, lanes.lane_center(lane), float(atk.telegraph))
		C.Attack.RAIN:
			for i in count:
				var lane := ctx.rng.randi() % lanes.lane_count
				if lanes.is_corridor_lane(lane): continue
				var x := lanes.lane_center(lane) + (ctx.rng.randf() - 0.5) * lanes.lane_width * 0.5
				var y := ctx.spawn_y + ctx.rng.randf() * 3.0
				ctx.obstacles.spawn_projectile(proj, Vector3(x, y, 0), Vector3.DOWN * speed)
				lanes.block(lane, 1, ctx.elapsed + 1.2)
			lanes.advance_corridor(ctx.elapsed)
		C.Attack.SUMMON:
			for i in maxi(1, count): ctx.obstacles.try_spawn(proj)
		C.Attack.WALL:
			if not ctx.obstacles.try_spawn_wall(proj): _timers[index] = minf(_timers[index], 0.5)
		C.Attack.SPIRAL:
			var n := maxi(4, count)
			var off := ctx.rng.randf() * TAU
			var gap := ctx.rng.randi() % n
			for i in n:
				if i == gap or i == (gap + 1) % n: continue
				var a := off + float(i) / n * TAU
				var dir := Vector3(cos(a), sin(a), 0)
				ctx.obstacles.spawn_projectile(proj, Vector3(pos.x, pos.y, 0) + dir * 1.5, dir * speed + Vector3.DOWN * speed * 0.6)

func _on_crystal() -> void:
	if state != State.FIGHTING: return
	health -= float(data.crystal_damage)
	view.hit_flash()
	if ctx.shake: ctx.shake.add(0.3)
	Events.toast.emit("Crystal strike!")
	_check_phase()

func _defeat() -> void:
	state = State.DEFEATED
	_state_time = 0.0
	health = 0.0
	Events.boss_health_changed.emit(0.0)
	ctx.obstacles.clear_all(true)
	if ctx.shake: ctx.shake.add(0.8)
	Sound.play("boss_defeat")
	Events.toast.emit(data.name + " defeated!")

func dispose() -> void:
	if ctx.coins.crystal_collected.is_connected(_on_crystal):
		ctx.coins.crystal_collected.disconnect(_on_crystal)
