class_name BalloonModel
extends Node3D
## Procedural hot air balloon. Every balloon has its own colors, pattern, shape and ornaments,
## and each of the 5 stages adds visible detail:
## 2 = reinforced basket and extra ropes, 3 = fabric bands and glossy finish,
## 4 = aura and sparkles, 5 = crown gem, signature trail and glowing fabric.

var data: Dictionary
var stage := 1
var sway: Node3D
var basket: Node3D
var flame: MeshInstance3D
var shield: MeshInstance3D
var _flash_mats: Array[StandardMaterial3D] = []
var _flappers: Array = []
var _spinners: Array = []
var _trails: Array[CPUParticles3D] = []
var _lateral := 0.0
var _tilt := 0.0
var _time := 0.0
var _flash := 0.0
var _blink := false
var _deflate := -1.0
var max_tilt := 14.0
var bob_amount := 0.12

static func build(b: Dictionary, p_stage: int, trail: Dictionary = {}) -> BalloonModel:
	var m := BalloonModel.new()
	m.name = "Balloon_" + b.id
	m.data = b
	m.stage = clampi(p_stage, 1, C.MAX_STAGE)
	m._build(trail)
	return m

func _build(trail: Dictionary) -> void:
	var b := data
	sway = Kit.node("Sway", self)
	var env := Kit.node("Envelope", sway, Vector3(0, 0.55, 0))
	var scale_xyz := Vector3(1.7, 1.75, 1.7)
	match str(b.shape):
		"tall": scale_xyz = Vector3(1.45, 2.05, 1.45)
		"wide": scale_xyz = Vector3(1.95, 1.6, 1.95)
		"teardrop": scale_xyz = Vector3(1.55, 1.95, 1.55)
		"classic": scale_xyz = Vector3(1.65, 1.85, 1.65)
	var gloss := 0.65 if stage >= 3 else 0.25
	var fabric := Kit.toon_instance(Color.WHITE, 0.0, gloss, Kit.balloon_pattern(b))
	if stage >= 5:
		fabric.emission_enabled = true
		fabric.emission = (b.glow as Color) * 0.35
	_flash_mats.append(fabric)
	# Upper dome + tapered lower half = classic hot air balloon silhouette
	Kit.part(env, "sphere", fabric, Vector3(0, 0.12 * scale_xyz.y, 0), scale_xyz)
	var lower := Kit.part(env, "funnel", fabric, Vector3(0, -0.36 * scale_xyz.y, 0), Vector3(scale_xyz.x * 0.93, scale_xyz.y * 0.5, scale_xyz.z * 0.93))
	lower.rotation_degrees.x = 0
	var mouth_y := -0.62 * scale_xyz.y
	var trim_color: Color = (b.c3 as Color).lerp(Color("#FFD54A"), 0.6) if stage >= 3 else b.c3
	var trim := Kit.toon(trim_color, 0.25 if stage >= 4 else 0.0, 0.6)
	Kit.part(env, "torus", trim, Vector3(0, mouth_y, 0), Vector3(0.55, 0.6, 0.55))
	if stage >= 3:
		var band := Kit.toon(Color("#FFD54A") if stage >= 5 else Color("#E4E6EB"), 0.3 if stage >= 5 else 0.0, 0.8)
		Kit.part(env, "thin_torus", band, Vector3(0, 0.12 * scale_xyz.y, 0), Vector3(scale_xyz.x * 1.01, 1.4, scale_xyz.z * 1.01))
		Kit.part(env, "thin_torus", band, Vector3(0, 0.45 * scale_xyz.y, 0), Vector3(scale_xyz.x * 0.8, 1.2, scale_xyz.z * 0.8))

	# Basket and ropes
	basket = Kit.node("Basket", sway, Vector3(0, 0.55 + mouth_y - 0.55, 0))
	var wicker := Kit.toon(b.basket, 0.0, 0.1)
	Kit.part(basket, "cylinder", wicker, Vector3(0, -0.15, 0), Vector3(0.62, 0.42, 0.62))
	var rim := Kit.toon((b.basket as Color).darkened(0.3), 0.0, 0.2)
	Kit.part(basket, "torus", rim, Vector3(0, 0.07, 0), Vector3(0.66, 0.5, 0.66))
	if stage >= 2:
		var metal := Kit.toon(Color("#C9CED6") if stage < 5 else Color("#FFD54A"), 0.0, 0.9)
		Kit.part(basket, "torus", metal, Vector3(0, -0.15, 0), Vector3(0.68, 0.4, 0.68))
		Kit.part(basket, "torus", metal, Vector3(0, -0.33, 0), Vector3(0.64, 0.4, 0.64))
	var rope_mat := Kit.toon(Color("#6D4C2F"), 0.0, 0.1)
	var ropes := 8 if stage >= 2 else 4
	var top := Vector3(0, 0.55 + mouth_y - basket.position.y, 0)
	for i in ropes:
		var a := TAU * i / ropes + PI / 4.0
		var r_top := Vector3(cos(a) * 0.32, top.y + 0.02, sin(a) * 0.32)
		var r_bot := Vector3(cos(a) * 0.28, 0.06, sin(a) * 0.28)
		Kit.rope(basket, r_bot, r_top, 0.025, rope_mat)
	flame = Kit.glow(basket, Vector3(0, 0.35, 0.05), 0.45, Color(1.0, 0.65, 0.2, 0.9))

	# Ornaments
	var orn: Array = b.ornaments
	var c3: Color = b.c3
	if orn.has("wings"):
		var wing_mat := Kit.toon(Color(1, 1, 1) if b.id != "phoenix" else Color("#FF9F43"), 0.15, 0.4)
		for side in [-1, 1]:
			var pivot := Kit.node("Wing", env, Vector3(side * scale_xyz.x * 0.45, 0.0, -0.1))
			Kit.part(pivot, "sphere", wing_mat, Vector3(side * 0.55, 0.15, 0), Vector3(1.1, 0.45, 0.12), Vector3(0, 0, side * 25))
			Kit.part(pivot, "sphere", wing_mat, Vector3(side * 0.45, -0.15, 0), Vector3(0.8, 0.3, 0.1), Vector3(0, 0, side * 10))
			_flappers.append([pivot, side])
	if orn.has("halo"):
		var halo := Kit.part(env, "thin_torus", Kit.toon(Color("#FFE066"), 0.9, 0.8), Vector3(0, scale_xyz.y * 0.75, 0), Vector3(0.8, 3, 0.8))
		_spinners.append([halo, Vector3(0, 40, 0)])
		Kit.glow(env, Vector3(0, scale_xyz.y * 0.75, 0.2), 1.3, Color(1, 0.95, 0.6, 0.5))
	if orn.has("crown") or stage >= 5:
		var gold := Kit.toon(Color("#FFD54A"), 0.3, 0.9)
		var crown := Kit.node("Crown", env, Vector3(0, scale_xyz.y * 0.62, 0))
		Kit.part(crown, "cylinder", gold, Vector3.ZERO, Vector3(0.45, 0.12, 0.45))
		for i in 5:
			var a := TAU * i / 5.0
			Kit.part(crown, "cone", gold, Vector3(cos(a) * 0.18, 0.12, sin(a) * 0.18), Vector3(0.1, 0.18, 0.1))
		if stage >= 5:
			Kit.part(crown, "octa", Kit.toon(b.glow, 1.0, 0.9), Vector3(0, 0.26, 0), Vector3(0.18, 0.28, 0.18))
	if orn.has("ring"):
		var ring := Kit.part(env, "thin_torus", Kit.toon(c3, 0.2, 0.6), Vector3(0, 0.1, 0), Vector3(scale_xyz.x * 1.5, 4.0, scale_xyz.z * 1.5), Vector3(14, 0, 8))
		_spinners.append([ring, Vector3(0, 25, 0)])
	if orn.has("orbit"):
		var orbit := Kit.node("Orbit", env)
		for i in 3:
			var a := TAU * i / 3.0
			Kit.part(orbit, "sphere", Kit.toon(c3, 0.8, 0.6), Vector3(cos(a) * scale_xyz.x * 0.75, sin(a * 2.0) * 0.3, sin(a) * scale_xyz.x * 0.75), Vector3.ONE * 0.18)
		_spinners.append([orbit, Vector3(0, 90, 0)])
	if orn.has("crystals"):
		for i in 4:
			var a := TAU * i / 4.0 + 0.4
			Kit.part(env, "octa", Kit.toon(Color("#DFF9FB"), 0.3, 0.9), Vector3(cos(a) * scale_xyz.x * 0.52, -0.2, sin(a) * scale_xyz.x * 0.52), Vector3(0.16, 0.3, 0.16))
	if orn.has("sunrays"):
		var rays := Kit.node("Rays", env, Vector3(0, 0.1, 0))
		for i in 10:
			var a := TAU * i / 10.0
			Kit.part(rays, "cone", Kit.toon(Color("#FFD23F"), 0.4, 0.5), Vector3(cos(a) * scale_xyz.x * 0.6, sin(a) * scale_xyz.y * 0.6, -0.25), Vector3(0.14, 0.32, 0.14), Vector3(0, 0, rad_to_deg(a) - 90))
		_spinners.append([rays, Vector3(0, 0, 12)])
	if orn.has("leaves"):
		for i in 6:
			var a := TAU * i / 6.0
			Kit.part(env, "sphere", Kit.toon(Color("#55A630"), 0.0, 0.2), Vector3(cos(a) * scale_xyz.x * 0.48, mouth_y + 0.25, sin(a) * scale_xyz.x * 0.48), Vector3(0.28, 0.12, 0.16), Vector3(0, -rad_to_deg(a), 30))
	if orn.has("flames"):
		var fl := FX.emitter(sway, Color(1.0, 0.55, 0.15, 0.9), 14, 0.5, 0.35, Vector3(0, -1.5, 0), 25.0, Vector3(0.3, 0.05, 0.1))
		fl.position = Vector3(0, basket.position.y - 0.35, 0)
		_trails.append(fl)
	if orn.has("stars"):
		var st := FX.emitter(sway, Color(1, 1, 0.8, 0.9), 8, 1.2, 0.22, Vector3(0, -0.6, 0), 60.0, Vector3(scale_xyz.x * 0.5, scale_xyz.y * 0.4, 0.2))
		st.position = env.position
		_trails.append(st)

	# Stage 4+: aura and sparkles
	if stage >= 4:
		Kit.glow(sway, Vector3(0, 0.55, -0.6), 4.0, Color((b.glow as Color).r, (b.glow as Color).g, (b.glow as Color).b, 0.35))
		var sp := FX.emitter(sway, Color(1, 1, 1, 0.9), 10, 1.0, 0.16, Vector3(0, 0.4, 0), 180.0, Vector3(scale_xyz.x * 0.6, scale_xyz.y * 0.6, 0.3))
		sp.position = env.position
		_trails.append(sp)
	# Stage 5 signature trail, or a bought cosmetic trail
	if stage >= 5 or not trail.is_empty():
		var col: Color = trail.get("a", b.glow)
		var tr := FX.emitter(sway, Color(col.r, col.g, col.b, 0.8), 26, 0.9, 0.3, Vector3(0, -2.2, 0), 12.0, Vector3(0.15, 0.05, 0.1))
		tr.position = Vector3(0, basket.position.y - 0.4, 0)
		if not trail.is_empty():
			var grad := Gradient.new()
			grad.set_color(0, trail.a)
			grad.set_color(1, Color(trail.b.r, trail.b.g, trail.b.b, 0.0))
			tr.color_ramp = grad
			tr.color = Color.WHITE
		_trails.append(tr)

	# Shield bubble (hidden until needed)
	var sm := Kit.unlit_instance(Color(0.4, 0.9, 1.0, 0.28), true, Kit.soft_circle())
	sm.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	sm.billboard_keep_scale = true
	shield = Kit.part(self, "quad", sm, Vector3(0, 0.3, 0.5), Vector3(4.2, 4.2, 1))
	var ring_m := Kit.unlit_instance(Color(0.55, 0.95, 1.0, 0.6), true)
	var ring := Kit.part(shield, "thin_torus", ring_m, Vector3.ZERO, Vector3(0.82, 0.82, 0.82), Vector3(90, 0, 0))
	ring.name = "Ring"
	shield.visible = false

func set_lateral_velocity(v: float) -> void: _lateral = v
func set_shield(on: bool) -> void: shield.visible = on
func flash() -> void: _flash = 0.25
func set_blinking(on: bool) -> void: _blink = on
func deflate() -> void: _deflate = 0.0

func set_invisible(on: bool) -> void:
	for m in _flash_mats:
		m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA if on else BaseMaterial3D.TRANSPARENCY_DISABLED
		m.albedo_color.a = 0.35 if on else 1.0

func set_trails_emitting(on: bool) -> void:
	for t in _trails:
		if is_instance_valid(t): t.emitting = on

func _process(dt: float) -> void:
	_time += dt
	var target := clampf(-_lateral * 1.6, -max_tilt, max_tilt)
	_tilt = lerpf(_tilt, target, 1.0 - exp(-dt * 6.0))
	sway.rotation_degrees.z = _tilt
	sway.position.y = sin(_time * 1.6) * bob_amount
	sway.rotation_degrees.y = sin(_time * 0.5) * 6.0
	basket.rotation_degrees.z = -_tilt * 0.5 + sin(_time * 2.1) * 2.0
	for f in _flappers:
		f[0].rotation_degrees.z = f[1] * (sin(_time * 7.0) * 18.0)
	for s in _spinners:
		s[0].rotation_degrees += (s[1] as Vector3) * dt
	if flame:
		var flick := 0.4 + 0.12 * sin(_time * 23.0) + 0.06 * sin(_time * 41.0)
		flame.scale = Vector3(flick, flick * 1.3, 1)
	if shield.visible:
		var pulse := 1.0 + sin(_time * 5.0) * 0.04
		shield.scale = Vector3(4.2 * pulse, 4.2 * pulse, 1)
	if _flash > 0.0:
		_flash -= dt
	for m in _flash_mats:
		if _flash > 0.0:
			m.albedo_color = Color(1, 0.55, 0.55, m.albedo_color.a)
		elif m.albedo_color.g < 0.99:
			m.albedo_color = Color(1, 1, 1, m.albedo_color.a)
	if _blink:
		visible = fmod(_time, 0.16) < 0.1
	elif _deflate < 0.0:
		visible = true
	if _deflate >= 0.0:
		_deflate += dt
		var k := clampf(_deflate / 1.2, 0.0, 1.0)
		sway.scale = Vector3(1.0 + k * 0.15, 1.0 - k * 0.45, 1.0 + k * 0.15)
		sway.rotation_degrees.z = _tilt + sin(_deflate * 18.0) * 12.0 * (1.0 - k)
