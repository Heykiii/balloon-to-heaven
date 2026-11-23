class_name PowerUpSpawner
extends RefCounted
## Drops power-up bubbles in the safe corridor at the level's interval and weights.

var ctx: LevelCtx
var root: Node3D
var active: Array[Pickup] = []
var enabled := true
var _pools := {}
var _timer := 0.0

func _init(p_ctx: LevelCtx, parent: Node3D) -> void:
	ctx = p_ctx
	root = Kit.node("PowerUps", parent)
	_timer = float(ctx.level.powerup_interval) * 0.6

func _take(t: int) -> Pickup:
	var pool: Array = _pools.get(t, [])
	if not pool.is_empty(): return pool.pop_back()
	var d := ctx.db.powerup(t)
	var p := Pickup.powerup(t, d.color if not d.is_empty() else Color.WHITE)
	root.add_child(p)
	return p

func _release(p: Pickup) -> void:
	p.visible = false
	p.set_process(false)
	if not _pools.has(p.pu_type): _pools[p.pu_type] = []
	_pools[p.pu_type].append(p)

func _pick() -> int:
	var total := 0.0
	for e in ctx.level.powerups: total += maxf(0.0, float(e.weight))
	if total <= 0.0: return -1
	var roll := ctx.rng.randf() * total
	for e in ctx.level.powerups:
		roll -= maxf(0.0, float(e.weight))
		if roll <= 0.0:
			# Never waste an Extra Life at full health.
			if int(e.type) == C.PU.LIFE and ctx.health.current >= ctx.health.maximum: return C.PU.MAGNET
			return int(e.type)
	return int(ctx.level.powerups[0].type)

func spawn_at(t: int, x: float) -> void:
	var p := _take(t)
	p.set_process(true)
	p.reset(Vector3(x, ctx.spawn_y, 0.3))
	active.append(p)

func tick(dt: float) -> void:
	if dt <= 0.0: return
	if enabled and ctx.progress < 0.92:
		_timer -= dt
		if _timer <= 0.0:
			_timer = float(ctx.level.powerup_interval) * lerpf(0.85, 1.2, ctx.rng.randf())
			var t := _pick()
			if t >= 0:
				var x := ctx.lanes.corridor_center() + (ctx.rng.randf() - 0.5) * ctx.lanes.lane_width
				spawn_at(t, ctx.clamp_x(x, 0.6, 0.2))
	var center := ctx.balloon_center
	var r := C.BALLOON_RADIUS + 0.6
	for i in range(active.size() - 1, -1, -1):
		var p := active[i]
		p.pos.y -= ctx.scroll_speed * dt
		p.position = p.pos
		var dx := center.x - p.pos.x
		var dy := center.y - p.pos.y
		var got := not ctx.health.dead and dx * dx + dy * dy <= r * r
		if got:
			var d := ctx.db.powerup(p.pu_type)
			FX.burst(p.pos + Vector3(0, 0, 1), d.color if not d.is_empty() else Color.WHITE, 24, 4.5, 0.35)
			ctx.powerups.activate(p.pu_type)
		if got or p.pos.y < ctx.despawn_y:
			active.remove_at(i)
			_release(p)
