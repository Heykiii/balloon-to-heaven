class_name ObstacleSpawner
extends RefCounted
## Spawns, pools, ticks and collides all obstacles. Spawning follows the level's configuration
## and is validated by the LanePlanner so that the player always has a reachable gap.
## Bosses use the same system for projectiles, columns, walls and summons.

const MIN_CROSSER_SPACING := 1.1

var ctx: LevelCtx
var root: Node3D
var active: Array[Obstacle] = []
var _pools := {}
var _spawn_timer := 1.5
var _last_crosser := -10.0
var _total_weight := 0.0

func _init(p_ctx: LevelCtx, parent: Node3D) -> void:
	ctx = p_ctx
	root = Kit.node("Obstacles", parent)
	for e in ctx.level.obstacles:
		_total_weight += maxf(0.0, float(e.weight))
		var d := ctx.db.obstacle(e.id)
		if not d.is_empty():
			for i in 2: _release(_create(d))

# ---------------------------------------------------------------- pooling

func _create(d: Dictionary) -> Obstacle:
	var o := Obstacle.new()
	root.add_child(o)
	o.setup(d)
	return o

func _take(d: Dictionary) -> Obstacle:
	var pool: Array = _pools.get(d.id, [])
	var o: Obstacle = pool.pop_back() if not pool.is_empty() else _create(d)
	o.reset_parameters()
	return o

func _release(o: Obstacle) -> void:
	o.alive = false
	o.visible = false
	o.set_process(false)
	if o.trail: o.trail.emitting = false
	if not _pools.has(o.data.id): _pools[o.data.id] = []
	_pools[o.data.id].append(o)

func _activate(o: Obstacle, p: Vector3) -> void:
	o.set_process(true)
	o.spawn(p, ctx)
	if o.trail: o.trail.emitting = true
	active.append(o)

func _despawn(i: int) -> void:
	var o := active[i]
	active.remove_at(i)
	_release(o)

func clear_all(with_bursts: bool) -> void:
	for i in range(active.size() - 1, -1, -1):
		if with_bursts and not active[i].data.harmless:
			FX.burst(active[i].pos + Vector3(0, 0, 1), Color.WHITE, 8, 2.5, 0.25)
		_despawn(i)

func hazard_count() -> int:
	var n := 0
	for o in active:
		if not o.data.harmless: n += 1
	return n

# ---------------------------------------------------------------- regular spawning

func _pick(progress: float) -> Dictionary:
	var available := 0.0
	for e in ctx.level.obstacles:
		if progress >= float(e.start): available += float(e.weight)
	if available <= 0.0: return {}
	var roll := ctx.rng.randf() * available
	for e in ctx.level.obstacles:
		if progress < float(e.start): continue
		roll -= float(e.weight)
		if roll <= 0.0: return ctx.db.obstacle(e.id)
	return ctx.db.obstacle(ctx.level.obstacles.back().id)

func tick(dt: float) -> void:
	if dt <= 0.0: return
	if ctx.normal_spawning and _total_weight > 0.0 and not ctx.powerups.spawn_paused():
		_spawn_timer -= dt
		if _spawn_timer <= 0.0:
			var progress := ctx.progress
			var interval := lerpf(float(ctx.level.interval_start), float(ctx.level.interval_end), progress)
			# Never spawn in the final stretch so the finish is always reachable.
			if progress < 0.96 and hazard_count() < int(ctx.level.max_obstacles):
				var d := _pick(progress)
				var ok := not d.is_empty() and try_spawn(d)
				_spawn_timer = interval * lerpf(0.85, 1.15, ctx.rng.randf()) if ok else 0.25
			else:
				_spawn_timer = 0.3
	_tick_active(dt)

func _block_duration(height: float) -> float:
	var clearance := C.BALLOON_RADIUS * 2.0 + 0.9
	return (height + clearance) / maxf(0.5, ctx.scroll_speed)

static func footprint_width(d: Dictionary) -> float:
	if d.box != Vector2.ZERO: return d.box.x
	var w: float = d.radius * 2.0
	if int(d.group_max) > 1: w += 1.2
	if int(d.move) == C.Move.PATROL or int(d.move) == C.Move.CIRCULAR: w += float(d.amplitude) * 2.0
	return w

static func footprint_height(d: Dictionary) -> float:
	if d.box != Vector2.ZERO: return d.box.y
	return d.radius * 2.0

## Attempts a fair spawn. Returns false when no fair placement exists right now.
func try_spawn(d: Dictionary) -> bool:
	var lanes := ctx.lanes
	var now := ctx.elapsed
	var y := ctx.spawn_y
	var speed_mul := float(ctx.level.obstacle_speed)
	if d.full_row:
		if not lanes.try_reserve_full_row(now, now + _block_duration(d.box.y) + 0.4): return false
		_spawn_full_row(d, y, lanes.corridor_center())
		lanes.advance_corridor(now)
		return true
	if d.harmless:
		var wind := _take(d)
		var x = (ctx.rng.randf() * 2.0 - 1.0) * (ctx.half_width - d.box.x * 0.5)
		_activate(wind, Vector3(x, y + d.box.y * 0.5, -0.5))
		return true
	if int(d.move) == C.Move.CROSS:
		if now - _last_crosser < MIN_CROSSER_SPACING or lanes.row_blocked_until > now: return false
		_last_crosser = now
		var bird := _take(d)
		bird.direction = 1.0 if ctx.rng.randf() < 0.5 else -1.0
		bird.speed_mul = speed_mul
		var sx := -bird.direction * (ctx.visible_half_width + 1.0)
		var sy := lerpf(1.0, ctx.top - 1.5, ctx.rng.randf())
		_activate(bird, Vector3(sx, sy, 0))
		lanes.advance_corridor(now)
		return true
	var count := lanes.lanes_for(footprint_width(d))
	var balloon_lane := lanes.lane_of(ctx.balloon.x)
	var mv := int(d.move)
	var fast := mv == C.Move.FALLING or mv == C.Move.DIAGONAL
	var impact := (y - ctx.balloon_y) / maxf(0.5, ctx.scroll_speed + (float(d.speed) * speed_mul if fast else 0.0))
	var avoid := balloon_lane if impact < C.MIN_REACTION_TIME + 0.4 else -1 # never drop fast objects onto the balloon
	var start := lanes.find_placement(count, now, int(ctx.level.min_free_lanes), avoid)
	if start < 0: return false
	var duration := _block_duration(footprint_height(d) + (1.5 if int(d.group_max) > 1 else 0.0))
	if fast or mv == C.Move.HOMING: duration *= 0.75
	lanes.block(start, count, now + duration)
	lanes.advance_corridor(now)
	var cx := (lanes.lane_center(start) + lanes.lane_center(start + count - 1)) * 0.5
	var group := ctx.rng.randi_range(int(d.group_min), int(d.group_max)) if int(d.group_max) > 1 else 1
	var dir := -1.0 if cx > 0.0 else 1.0
	for i in group:
		var o := _take(d)
		o.speed_mul = speed_mul
		o.direction = dir if mv == C.Move.DIAGONAL else (-1.0 if ctx.rng.randf() < 0.5 else 1.0)
		if group > 1:
			o.direction = dir
			var ang := float(i) / group * TAU
			o.offset = Vector3(cos(ang) * 0.75, sin(ang) * 0.55, 0)
		var p := Vector3(cx, y + footprint_height(d) * 0.5, 0)
		_activate(o, p)
	return true

func _spawn_full_row(d: Dictionary, y: float, gap_center: float) -> void:
	var gap := maxf(float(d.gap), C.BALLOON_RADIUS * 2.0 + 1.8)
	var left := -ctx.visible_half_width - 0.5
	var right := ctx.visible_half_width + 0.5
	var gl := gap_center - gap * 0.5
	var gr := gap_center + gap * 0.5
	var h: float = d.box.y
	if gl - left > 0.2:
		var w := _take(d)
		w.box = Vector2(gl - left, h)
		_activate(w, Vector3((left + gl) * 0.5, y + h * 0.5, 0))
	if right - gr > 0.2:
		var w2 := _take(d)
		w2.box = Vector2(right - gr, h)
		_activate(w2, Vector3((gr + right) * 0.5, y + h * 0.5, 0))

# ---------------------------------------------------------------- boss helpers

func spawn_projectile(d: Dictionary, p: Vector3, velocity: Vector3) -> Obstacle:
	var o := _take(d)
	o.use_custom_velocity = true
	o.custom_velocity = velocity
	o.scroll_factor = 0.0
	_activate(o, p)
	return o

func spawn_column(d: Dictionary, x: float, telegraph: float) -> Obstacle:
	var o := _take(d)
	o.scroll_factor = 0.0
	o.despawn_after_active = true
	o.warning_override = maxf(0.7, telegraph)
	_activate(o, Vector3(x, 0, -0.3))
	return o

func try_spawn_wall(d: Dictionary) -> bool:
	var now := ctx.elapsed
	if not ctx.lanes.try_reserve_full_row(now, now + _block_duration(d.box.y) + 0.4): return false
	_spawn_full_row(d, ctx.spawn_y, ctx.lanes.corridor_center())
	ctx.lanes.advance_corridor(now)
	return true

# ---------------------------------------------------------------- simulation and collisions

func _tick_active(dt: float) -> void:
	var wind := 0.0
	var center := ctx.balloon_center
	var invisible := ctx.abilities != null and ctx.abilities.is_invisible()
	var limit_x := ctx.visible_half_width + 4.0
	for i in range(active.size() - 1, -1, -1):
		var o := active[i]
		o.tick(ctx, dt)
		if not o.alive or o.top() < ctx.despawn_y or absf(o.pos.x) > limit_x or o.age > 60.0 or o.pos.y > ctx.spawn_y + 12.0:
			_despawn(i)
			continue
		if ctx.health == null or ctx.health.dead or not o.collision_enabled: continue
		if not o.overlaps(center, C.BALLOON_RADIUS): continue
		if o.data.harmless:
			wind += o.direction * float(o.data.push)
			continue
		if invisible: continue
		var damaged := ctx.health.try_hit(int(o.data.damage), int(o.data.dmg))
		if damaged:
			Sound.play("hit")
			if ctx.shake: ctx.shake.add(0.45)
			FX.burst(Vector3(center.x, center.y, 1), Color(1, 0.4, 0.35), 16, 4.0, 0.3)
		if o.data.destroy_on_hit and (damaged or ctx.health.has_shield() or ctx.health.is_invulnerable()):
			FX.burst(o.pos + Vector3(0, 0, 1), o.data.c1, 14, 3.5, 0.3)
			_despawn(i)
	if ctx.effects: ctx.effects.zone_wind = wind
