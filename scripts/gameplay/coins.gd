class_name CoinSystem
extends RefCounted
## Coin patterns along the safe corridor, evenly spaced star coins, boss sky crystals and the magnet.

signal crystal_collected

var ctx: LevelCtx
var root: Node3D
var active: Array[Pickup] = []
var patterns_enabled := true
var _pools := {Pickup.Kind.COIN: [], Pickup.Kind.SPECIAL: [], Pickup.Kind.CRYSTAL: []}
var _pending: Array[float] = []
var _pattern_timer := 1.2
var _pending_timer := 0.0
var _special_spawned := 0

func _init(p_ctx: LevelCtx, parent: Node3D) -> void:
	ctx = p_ctx
	root = Kit.node("Coins", parent)
	ctx.special_total = int(ctx.level.special_coins)
	for i in 24: _release(_make(Pickup.Kind.COIN))
	for i in 2: _release(_make(Pickup.Kind.SPECIAL))

func _make(kind: int) -> Pickup:
	var p: Pickup
	match kind:
		Pickup.Kind.CRYSTAL: p = Pickup.crystal()
		Pickup.Kind.SPECIAL: p = Pickup.coin(true)
		_: p = Pickup.coin(false)
	root.add_child(p)
	return p

func _release(p: Pickup) -> void:
	p.visible = false
	p.set_process(false)
	_pools[p.kind].append(p)

func _spawn(kind: int, at: Vector3) -> Pickup:
	var pool: Array = _pools[kind]
	var p: Pickup = pool.pop_back() if not pool.is_empty() else _make(kind)
	p.set_process(true)
	p.reset(at)
	active.append(p)
	return p

func spawn_crystal(x: float) -> void:
	_spawn(Pickup.Kind.CRYSTAL, Vector3(x, ctx.spawn_y, 0.2))

func _queue_pattern() -> void:
	var lanes := ctx.lanes
	var count := maxi(3, int(ctx.level.coins_per_pattern))
	var base := lanes.corridor_center() + (ctx.rng.randf() - 0.5) * lanes.lane_width * 0.8
	var type := ctx.rng.randi() % 4
	for i in count:
		var x := base
		match type:
			1: x = base + sin(i * 0.9) * 1.2                 # wave
			2: x = base + (i - count * 0.5) * 0.45           # diagonal
			3: x = base + (-0.5 if i % 2 == 0 else 0.5)      # zigzag
		_pending.append(ctx.clamp_x(x, 0.3, 0.3))

func tick(dt: float) -> void:
	var progress := ctx.progress
	if dt > 0.0 and patterns_enabled and progress < 0.98:
		_pattern_timer -= dt
		if _pattern_timer <= 0.0 and _pending.is_empty():
			_queue_pattern()
			_pattern_timer = float(ctx.level.coin_interval)
		if _special_spawned < ctx.special_total and progress >= (_special_spawned + 0.5) / ctx.special_total * 0.92:
			_spawn(Pickup.Kind.SPECIAL, Vector3(ctx.lanes.corridor_center(), ctx.spawn_y + 0.5, 0.2))
			_special_spawned += 1
	if dt > 0.0 and not _pending.is_empty():
		_pending_timer -= dt
		if _pending_timer <= 0.0:
			_spawn(Pickup.Kind.COIN, Vector3(_pending.pop_front(), ctx.spawn_y, 0.2))
			_pending_timer = 0.85 / maxf(0.5, ctx.scroll_speed)
	var magnet := maxf(float(ctx.stats.magnet) if ctx.stats else C.BASE_MAGNET, ctx.powerups.magnet_radius())
	var center := ctx.balloon_center
	var collect_sq := pow(C.BALLOON_RADIUS + 0.35, 2)
	var magnet_sq := magnet * magnet
	var can_collect := ctx.health != null and not ctx.health.dead
	for i in range(active.size() - 1, -1, -1):
		var c := active[i]
		var p := c.pos
		p.y -= ctx.scroll_speed * dt
		var dx := center.x - p.x
		var dy := center.y - p.y
		var dsq := dx * dx + dy * dy
		if can_collect and (c.magnetized or dsq < magnet_sq):
			c.magnetized = true
			c.pull_speed = minf(C.MAGNET_PULL * 1.6, c.pull_speed + C.MAGNET_PULL * 3.0 * dt + 2.0 * dt)
			var dist := sqrt(dsq)
			if dist > 0.001:
				var step := minf(dist, c.pull_speed * dt)
				p.x += dx / dist * step
				p.y += dy / dist * step
			dx = center.x - p.x
			dy = center.y - p.y
			dsq = dx * dx + dy * dy
		c.pos = p
		c.position = p
		if can_collect and dsq <= collect_sq:
			_collect(c)
			active.remove_at(i)
			_release(c)
		elif p.y < ctx.despawn_y:
			active.remove_at(i)
			_release(c)

func _collect(c: Pickup) -> void:
	var at := c.pos + Vector3(0, 0, 1)
	match c.kind:
		Pickup.Kind.CRYSTAL:
			Sound.play("crystal")
			FX.burst(at, Color(0.5, 0.95, 1), 24, 5.0, 0.35)
			crystal_collected.emit()
		Pickup.Kind.SPECIAL:
			ctx.special_collected += 1
			_add(C.R_SPECIAL_COIN)
			Sound.play("special_coin")
			FX.burst(at, Color(1, 0.9, 0.4), 26, 5.0, 0.4)
			Events.toast.emit("Star coin %d/%d" % [ctx.special_collected, ctx.special_total])
		_:
			_add(C.R_COIN)
			Sound.play("coin", 0.55)
			FX.burst(at, Pickup.GOLD, 6, 2.5, 0.18, 0.4)

func _add(base: int) -> void:
	var v := ctx.abilities.modify_coin_value(base) if ctx.abilities else base
	v *= ctx.powerups.coin_multiplier()
	ctx.coins_collected += v
	Events.level_coins_changed.emit(ctx.coins_collected)

func clear_all() -> void:
	for i in range(active.size() - 1, -1, -1):
		_release(active[i])
	active.clear()
	_pending.clear()
