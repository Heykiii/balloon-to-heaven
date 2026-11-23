extends Control
## The level: builds the 3D scene and systems, runs the state machine
## (intro -> playing -> boss -> finishing / failed), pause menu and results.

enum State { INTRO, PLAYING, BOSS, FINISHING, FAILED, DONE }
const INTRO_TIME := 1.8
const FINISH_TIME := 2.4
const FAIL_TIME := 2.2

var ctx: LevelCtx
var world_root: Node3D
var env: SkyEnvironment
var hud: Hud
var boss: Boss
var state := State.INTRO
var state_time := 0.0
var paused := false
var boss_defeated := false
var _pause_menu: Control
var _model: BalloonModel

func backdrop_world() -> int: return 0

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	mouse_filter = Control.MOUSE_FILTER_STOP
	_build()

func _build() -> void:
	var db := Game.db
	var level := db.level(Game.selected_level)
	if level.is_empty(): level = db.level(1)
	var w := db.world(int(level.world))
	var b: Dictionary = {}
	if Game.selected_balloon != "" and Game.shop.is_owned(Game.selected_balloon): b = db.balloon(Game.selected_balloon)
	if b.is_empty(): b = Game.shop.equipped()
	ctx = LevelCtx.new()
	ctx.db = db
	ctx.level = level
	ctx.world = w
	ctx.balloon_data = b
	ctx.stats = Game.shop.stats(b)
	ctx.rng = RandomNumberGenerator.new()
	ctx.rng.seed = int(level.number) * 7919 + Time.get_ticks_msec() % 1000
	world_root = Node3D.new()
	world_root.name = "World"
	world_root.process_mode = Node.PROCESS_MODE_PAUSABLE
	add_child(world_root)
	var cam := Camera3D.new()
	cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.keep_aspect = Camera3D.KEEP_HEIGHT
	cam.position = Vector3(0, 0, 30)
	cam.far = 200
	world_root.add_child(cam)
	cam.current = true
	cam.size = ctx.fit_to_viewport(get_viewport().get_visible_rect().size)
	ctx.camera = cam
	ctx.shake = CameraShake.new()
	cam.add_child(ctx.shake)
	SkyEnvironment.lights(world_root, w)
	env = SkyEnvironment.build(w, ctx.top, ctx.visible_half_width, int(level.number) * 31)
	world_root.add_child(env)
	var gameplay := Kit.node("Gameplay", world_root)
	ctx.root = gameplay
	# Balloon (order matters: abilities need stats, health needs abilities, abilities.begin needs health)
	var trail := db.trail(Game.workshop.equipped_trail())
	_model = BalloonModel.build(b, Game.shop.stage(b.id), trail)
	gameplay.add_child(_model)
	ctx.abilities = Abilities.new(b)
	ctx.balloon = BalloonController.new(ctx, _model, float(ctx.stats.speed))
	ctx.balloon.input_enabled = false
	ctx.balloon.set_vertical(ctx.bottom - 2.0)
	ctx.lanes = LanePlanner.new(C.LANES, ctx.half_width, ctx.rng)
	ctx.powerups = PowerUps.new(ctx)
	ctx.obstacles = ObstacleSpawner.new(ctx, gameplay)
	ctx.coins = CoinSystem.new(ctx, gameplay)
	ctx.powerup_spawner = PowerUpSpawner.new(ctx, gameplay)
	ctx.effects = EnvEffects.new(ctx, gameplay)
	ctx.health = BalloonHealth.new(int(ctx.stats.hp), int(ctx.stats.armor), _model, ctx.abilities, ctx.rng)
	ctx.health.died.connect(_on_died)
	ctx.scroll_speed = 0.0
	hud = Hud.new()
	add_child(hud)
	hud.setup(level, w.name, ctx.abilities.active.label if ctx.abilities.active else "")
	hud.pause_requested.connect(toggle_pause)
	hud.ability_pressed.connect(func(): if ctx.abilities.active: ctx.abilities.active.activate())
	ctx.abilities.begin(ctx)
	Events.level_coins_changed.emit(0)
	Events.level_progress_changed.emit(0.0)
	Events.boss_health_changed.emit(-1.0)
	Events.health_changed.emit(ctx.health.current, ctx.health.maximum)
	Sound.music(Sound.Cue.WORLD, w.music)
	var subtitle: String = ("Boss ahead: " + db.boss(level.boss).name) if level.is_boss else level.hint
	hud.banner(level.name, subtitle, 2.4)
	get_tree().create_timer(2.6, false).timeout.connect(func():
		if is_inside_tree() and state <= State.BOSS: Events.toast.emit("Challenge: " + Progression.challenge_text(level)))
	Sound.play("whoosh", 0.6)

# ---------------------------------------------------------------- input

func _gui_input(event: InputEvent) -> void:
	if paused or ctx == null: return
	if event is InputEventMouseButton or event is InputEventMouseMotion:
		ctx.balloon.handle_input(event)

func _unhandled_key_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel") or (event is InputEventKey and event.pressed and event.keycode == KEY_P):
		toggle_pause()
		get_viewport().set_input_as_handled()
	elif event is InputEventKey and event.pressed and event.keycode == KEY_SPACE and ctx.abilities.active:
		ctx.abilities.active.activate()

func on_back() -> void:
	toggle_pause()

func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_PAUSED or what == NOTIFICATION_APPLICATION_FOCUS_OUT:
		if state == State.PLAYING or state == State.BOSS: _pause()

# ---------------------------------------------------------------- frame loop

func _process(dt: float) -> void:
	if ctx == null or paused or state == State.DONE: return
	dt = minf(dt, 0.05) # a long frame (tab switch) must never teleport hazards through the balloon
	state_time += dt
	if Game.autopilot: _autopilot()
	match state:
		State.INTRO: _tick_intro(dt)
		State.PLAYING: _tick_playing(dt)
		State.BOSS: _tick_boss(dt)
		State.FINISHING: _tick_finishing(dt)
		State.FAILED: _tick_failed(dt)

func _tick_intro(dt: float) -> void:
	var t := clampf(state_time / INTRO_TIME, 0.0, 1.0)
	ctx.balloon.set_vertical(lerpf(ctx.bottom - 2.0, ctx.balloon_y, U.ease_out_back(t)))
	ctx.balloon.tick(dt)
	ctx.health.tick(dt)
	ctx.scroll_speed = float(ctx.level.ascent_speed) * t
	env.scroll(ctx.scroll_speed * dt, dt)
	if state_time >= INTRO_TIME:
		ctx.balloon.set_vertical(ctx.balloon_y)
		ctx.balloon.input_enabled = true
		hud.set_ability_visible(ctx.abilities.active != null)
		_set_state(State.PLAYING)

func _tick_systems(dt: float, advance: bool, scroll_factor: float) -> void:
	ctx.elapsed += dt
	ctx.scroll_speed = float(ctx.level.ascent_speed) * ctx.powerups.scroll_multiplier() * scroll_factor
	if advance: ctx.distance += ctx.scroll_speed * dt
	env.scroll(ctx.scroll_speed * dt, dt)
	ctx.abilities.tick(dt)
	ctx.powerups.tick(dt)
	ctx.effects.tick(dt)
	ctx.balloon.tick(dt)
	ctx.health.tick(dt)
	ctx.obstacles.tick(dt)
	ctx.coins.tick(dt)
	ctx.powerup_spawner.tick(dt)
	if boss: boss.tick(dt)

func _tick_playing(dt: float) -> void:
	_tick_systems(dt, true, 1.0)
	Events.level_progress_changed.emit(ctx.progress)
	if state != State.PLAYING: return
	if ctx.distance >= float(ctx.level.length):
		if ctx.level.is_boss: _start_boss()
		else: _begin_finish()

func _start_boss() -> void:
	_set_state(State.BOSS)
	ctx.normal_spawning = false
	ctx.boss_active = true
	ctx.coins.patterns_enabled = false
	ctx.obstacles.clear_all(true)
	var d := ctx.db.boss(ctx.level.boss)
	boss = Boss.new(ctx, d, ctx.root)
	hud.set_boss_name(d.name)
	hud.banner(d.name, d.title, 2.2)
	Sound.music(Sound.Cue.BOSS, ctx.world.music)
	get_tree().create_timer(2.8, false).timeout.connect(func():
		if is_inside_tree() and state == State.BOSS: Events.toast.emit("Collect the glowing crystals to strike!"))

func _tick_boss(dt: float) -> void:
	# The background keeps drifting slowly while the boss holds the balloon in place.
	_tick_systems(dt, false, 0.55)
	if state != State.BOSS: return
	if boss.is_done():
		boss_defeated = true
		_begin_finish()

func _begin_finish() -> void:
	_set_state(State.FINISHING)
	ctx.balloon.input_enabled = false
	ctx.balloon.release()
	ctx.health.set_invulnerable(FINISH_TIME + 5.0)
	ctx.normal_spawning = false
	ctx.coins.patterns_enabled = false
	ctx.powerup_spawner.enabled = false
	ctx.obstacles.clear_all(true)
	Events.level_progress_changed.emit(1.0)
	Events.boss_health_changed.emit(-1.0)
	hud.set_ability_visible(false)
	hud.banner("Victory!" if boss_defeated else "Level complete!", ctx.level.name, FINISH_TIME)
	Sound.play("level_complete")
	Sound.music(Sound.Cue.VICTORY)
	FX.burst(Vector3(ctx.balloon.x, ctx.balloon_y + 1.5, 2), Color(1, 0.9, 0.4), 50, 6.0, 0.35, 1.2)

func _tick_finishing(dt: float) -> void:
	ctx.scroll_speed = lerpf(ctx.scroll_speed, float(ctx.level.ascent_speed) * 2.2, dt * 2.0)
	env.scroll(ctx.scroll_speed * dt, dt)
	ctx.coins.tick(dt)
	ctx.powerups.tick(dt)
	ctx.obstacles.tick(dt)
	ctx.health.tick(dt)
	if boss: boss.tick(dt)
	var rise := maxf(0.0, state_time - 0.8)
	ctx.balloon.set_vertical(ctx.balloon_y + rise * rise * 4.0)
	ctx.balloon.tick(dt)
	if state_time >= FINISH_TIME: _complete(true)

func _on_died() -> void:
	if state >= State.FINISHING: return
	_set_state(State.FAILED)
	ctx.balloon.input_enabled = false
	_model.deflate()
	_model.set_trails_emitting(false)
	hud.set_ability_visible(false)
	if ctx.shake: ctx.shake.add(0.7)
	Sound.play("fail")
	Sound.music(Sound.Cue.FAIL)
	FX.burst(Vector3(ctx.balloon.x, ctx.balloon_y, 2), ctx.balloon_data.c1, 40, 5.0, 0.3, 0.9)
	hud.banner("Balloon down!", "%d%% of the way up" % roundi(ctx.progress * 100.0), FAIL_TIME)

func _tick_failed(dt: float) -> void:
	ctx.scroll_speed = move_toward(ctx.scroll_speed, 0.0, dt * 4.0)
	env.scroll(ctx.scroll_speed * dt, dt)
	ctx.obstacles.tick(dt)
	ctx.coins.tick(0.0)
	if boss: boss.tick(dt)
	ctx.balloon.set_vertical(ctx.balloon_y - state_time * state_time * 3.0)
	if state_time >= FAIL_TIME: _complete(false)

func _set_state(s: int) -> void:
	state = s
	state_time = 0.0

# ---------------------------------------------------------------- results

func _complete(victory: bool) -> void:
	if state == State.DONE: return
	state = State.DONE
	var r := Progression.new_result(int(ctx.level.number))
	r.victory = victory
	r.coins = ctx.coins_collected
	r.special = ctx.special_collected
	r.special_total = ctx.special_total
	r.took_damage = ctx.health.took_damage
	r.powerups = ctx.powerups_collected
	r.progress = 1.0 if victory else ctx.progress
	r.time = ctx.elapsed
	r.boss_defeated = boss_defeated
	r.balloon = ctx.balloon_data.id
	r.reward_multiplier = ctx.abilities.reward_multiplier()
	for i in ctx.powerups_collected: Game.progression.record_powerup()
	Game.progression.record_result(r)
	Game.achievements.evaluate()
	Game.save.save_if_dirty()
	Game.last_result = r
	Game.show_ending = bool(r.campaign_completed_now)
	Game.goto("results")

# ---------------------------------------------------------------- pause

func toggle_pause() -> void:
	if state >= State.FINISHING: return
	if paused: _resume()
	else: _pause()

func _pause() -> void:
	if paused or state >= State.FINISHING: return
	paused = true
	get_tree().paused = true
	Sound.ducked = true
	_pause_menu = Control.new()
	UI.full(_pause_menu)
	add_child(_pause_menu)
	var m := UI.modal(_pause_menu, 440)
	var v: VBoxContainer = m[1]
	v.add_child(UI.label("Paused", 40))
	v.add_child(UI.label("World %d · Level %d" % [ctx.level.world, ctx.level.index], 22, UI.TEXT_DIM))
	v.add_child(UI.wrap(UI.label("Challenge: " + Progression.challenge_text(ctx.level), 20, UI.TEXT_DIM)))
	v.add_child(UI.button("Resume", UI.SUCCESS, _resume, 28, Vector2(0, 70)))
	v.add_child(UI.button("Restart", UI.SECONDARY, func():
		_resume()
		Game.start_level(int(ctx.level.number), ctx.balloon_data.id)))
	v.add_child(UI.button("Settings", UI.PANEL_LIGHT, func(): Dialogs.settings(_pause_menu)))
	v.add_child(UI.button("World Map", UI.PRIMARY, func():
		_resume()
		Game.map_world = int(ctx.level.world)
		Game.goto("map")))
	v.add_child(UI.button("Main Menu", UI.DANGER, func():
		_resume()
		Game.goto("menu")))

func _resume() -> void:
	if not paused: return
	paused = false
	get_tree().paused = false
	Sound.ducked = false
	if _pause_menu:
		_pause_menu.queue_free()
		_pause_menu = null

func _exit_tree() -> void:
	get_tree().paused = false
	Sound.ducked = false
	if boss: boss.dispose()
	Events.boss_health_changed.emit(-1.0)

# ---------------------------------------------------------------- autopilot (smoke tests)

## A simple dodging bot used by the smoke test: scores candidate positions by predicted
## hazard overlap over the next 2.5 seconds, and is drawn to coins and boss crystals.
func _autopilot() -> void:
	if not ctx.balloon.input_enabled: return
	var limit := ctx.half_width - C.BALLOON_RADIUS - C.EDGE_MARGIN
	var best_x := ctx.balloon.x
	var best := -INF
	var x := -limit
	while x <= limit + 0.01:
		var score := -absf(x - ctx.balloon.x) * 0.15
		for o in ctx.obstacles.active:
			if o.data.harmless: continue
			var dy := o.bottom() - (ctx.balloon_y + 0.9)
			if o.top() < ctx.balloon_y - 1.2: continue
			var closing := ctx.scroll_speed * o.scroll_factor
			var vx := 0.0
			if o.use_custom_velocity:
				closing -= o.custom_velocity.y
				vx = o.custom_velocity.x
			else:
				match int(o.data.move):
					C.Move.FALLING: closing += float(o.data.speed) * o.speed_mul
					C.Move.DIAGONAL:
						closing += float(o.data.speed) * o.speed_mul
						vx = o.direction * float(o.data.speed) * o.speed_mul * 0.35
					C.Move.CROSS: vx = o.direction * float(o.data.speed) * o.speed_mul
			var t := maxf(0.0, dy) / maxf(0.3, closing) if dy > 0.0 else 0.0
			if t > 2.5: continue
			var px := o.pos.x + vx * t
			var half: float = (o.box.x * 0.5 if o.is_box() else o.radius) + C.BALLOON_RADIUS + 0.35
			if o.data.timed and o._t_state == 0: continue
			if absf(x - px) < half: score -= 10.0 * (2.6 - t)
		for c in ctx.coins.active:
			if c.pos.y > ctx.balloon_y and c.pos.y < ctx.balloon_y + 6.0 and absf(c.pos.x - x) < 0.8:
				score += 4.0 if c.kind == Pickup.Kind.CRYSTAL else (1.0 if c.kind == Pickup.Kind.SPECIAL else 0.2)
		for p in ctx.powerup_spawner.active:
			if p.pos.y > ctx.balloon_y and p.pos.y < ctx.balloon_y + 6.0 and absf(p.pos.x - x) < 0.9: score += 0.8
		if score > best:
			best = score
			best_x = x
		x += 0.35
	ctx.balloon.steer_to(best_x)
	if ctx.abilities.active and ctx.abilities.active.can_activate() and best < -5.0:
		ctx.abilities.active.activate()
