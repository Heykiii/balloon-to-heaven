class_name SkyEnvironment
extends Node3D
## Themed 2.5D backdrop for one world: sky gradient, three parallax layers of scenery,
## foreground cloud wisps, weather particles, lights and (for storms) lightning flashes.

var layers: Array = [] # {factor, items: Array[Node3D], drift: Array[float]}
var bottom := -14.0
var top := 18.0
var auto_scroll := 0.0
var _flasher: MeshInstance3D
var _flash_timer := 2.0
var _flash_t := -1.0
var _rng := RandomNumberGenerator.new()

static func build(w: Dictionary, half_h: float, half_w: float, seed_value: int) -> SkyEnvironment:
	var e := SkyEnvironment.new()
	e.name = "Environment"
	e._build(w, half_h, half_w, seed_value)
	return e

static func lights(parent: Node, w: Dictionary) -> void:
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = w.get("sky_bottom", Color(0.5, 0.7, 1))
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = w.get("ambient", Color(0.6, 0.65, 0.8))
	env.ambient_light_energy = 0.85
	env.tonemap_mode = Environment.TONE_MAPPER_LINEAR
	var we := WorldEnvironment.new()
	we.environment = env
	parent.add_child(we)
	var sun := DirectionalLight3D.new()
	sun.light_color = w.get("light", Color(1, 0.97, 0.9))
	sun.light_energy = 1.05
	sun.rotation_degrees = Vector3(-35, -30, 0)
	sun.shadow_enabled = false
	parent.add_child(sun)

func _build(w: Dictionary, half_h: float, half_w: float, seed_value: int) -> void:
	_rng.seed = seed_value
	var theme: String = w.get("theme", "valley")
	var sky_top: Color = w.get("sky_top", Color(0.3, 0.6, 1))
	var sky_bottom: Color = w.get("sky_bottom", Color(0.8, 0.9, 1))
	var cloud: Color = w.get("cloud", Color.WHITE)
	var ground: Color = w.get("ground", Color(0.4, 0.75, 0.4))
	var accent: Color = w.get("accent", Color.YELLOW)
	half_w += 1.0
	bottom = -half_h - 4.0
	top = half_h + 8.0
	var sky := Kit.unlit_instance(Color.WHITE, false, Kit.vertical_gradient(sky_top, sky_bottom))
	sky.transparency = BaseMaterial3D.TRANSPARENCY_DISABLED
	Kit.part(self, "quad", sky, Vector3(0, 0, -80), Vector3(half_w * 2.6 + 6.0, half_h * 2.4, 1))
	var glow_a := 0.5 if theme == "heaven" or theme == "golden" else 0.2
	Kit.part(self, "quad", Kit.unlit(Color(accent, glow_a), true, Kit.soft_circle()), Vector3(0, -half_h * 0.9, -79), Vector3(half_w * 3.2, half_h * 1.6, 1))
	var span := half_h * 2.0 + 12.0
	_layer(theme, 0, 7, 0.08, -55.0, span, half_h, half_w, sky_top, sky_bottom, cloud, ground, accent)
	_layer(theme, 1, 6, 0.25, -35.0, span, half_h, half_w, sky_top, sky_bottom, cloud, ground, accent)
	_layer(theme, 2, 9, 0.5, -18.0, span, half_h, half_w, sky_top, sky_bottom, cloud, ground, accent)
	_layer(theme, 3, 5, 1.35, 6.0, span, half_h, half_w, sky_top, sky_bottom, cloud, ground, accent)
	_weather(str(w.get("weather", "none")), half_w, half_h)
	if theme == "storm":
		_flasher = Kit.part(self, "quad", Kit.unlit(Color(0.8, 0.85, 1, 0.3), true), Vector3(0, 0, -50), Vector3(half_w * 3.0, half_h * 3.0, 1))
		_flasher.visible = false

func _layer(theme: String, index: int, count: int, factor: float, z: float, span: float, half_h: float, half_w: float,
		sky_top: Color, sky_bottom: Color, cloud: Color, ground: Color, accent: Color) -> void:
	var root := Kit.node("Layer%d" % index, self, Vector3(0, 0, z))
	var layer := {"factor": factor, "items": [], "drift": []}
	var haze := 0.62 if index == 0 else (0.32 if index == 1 else 0.0)
	var mid := sky_bottom.lerp(sky_top, 0.4)
	for i in count:
		var y := -half_h - 4.0 + (i + _rng.randf() * 0.6) / count * span
		var x := (_rng.randf() * 2.0 - 1.0) * half_w * (1.1 if index == 3 else 0.95)
		var item: Node3D
		if index == 3:
			item = _puff(root, cloud, 0.28, 4.0 + _rng.randf() * 3.0)
		elif index == 2 and i % 2 == 0:
			item = _cluster(root, cloud.lerp(mid, 0.1), 0.8 + _rng.randf() * 0.7)
		elif index == 1 and i % 3 == 0:
			item = _puff(root, cloud.lerp(mid, haze), 0.75, 5.0 + _rng.randf() * 4.0)
		else:
			item = _decor(theme, index, root, ground.lerp(mid, haze), accent.lerp(mid, haze * 0.7), cloud.lerp(mid, haze))
		item.position = Vector3(x, y, _rng.randf() * 2.0)
		layer.items.append(item)
		layer.drift.append((_rng.randf() - 0.5) * 0.3 if index >= 2 else 0.0)
	layers.append(layer)

func scroll(distance: float, dt: float) -> void:
	for layer in layers:
		var d: float = distance * layer.factor
		for i in layer.items.size():
			var t: Node3D = layer.items[i]
			var p := t.position
			p.y -= d
			p.x += layer.drift[i] * dt
			if p.y < bottom: p.y += top - bottom
			if p.x > 9.0: p.x = -9.0
			elif p.x < -9.0: p.x = 9.0
			t.position = p

func _process(dt: float) -> void:
	if auto_scroll > 0.0: scroll(auto_scroll * dt, dt)
	if _flasher:
		_flash_timer -= dt
		if _flash_timer <= 0.0:
			_flash_timer = _rng.randf_range(1.5, 4.0)
			_flash_t = 0.0
		if _flash_t >= 0.0:
			_flash_t += dt
			_flasher.visible = _flash_t < 0.08 or (_flash_t > 0.15 and _flash_t < 0.25)
			if _flash_t > 0.3: _flash_t = -1.0

# ---------------------------------------------------------------- scenery pieces

func _puff(parent: Node3D, color: Color, alpha: float, size: float) -> Node3D:
	return Kit.part(parent, "quad", Kit.unlit(Color(color, alpha), false, Kit.soft_circle()), Vector3.ZERO, Vector3(size * 1.6, size, 1))

func _cluster(parent: Node3D, color: Color, scale_v: float) -> Node3D:
	var n := Kit.node("Cloud", parent)
	var m := Kit.toon(color, 0.08, 0.05)
	var puffs := 4 + _rng.randi() % 3
	for i in puffs:
		var s := 0.9 + _rng.randf() * 0.8
		Kit.part(n, "low_sphere", m, Vector3((i - puffs * 0.5) * 0.65, sin(i * 1.7) * 0.25 + (i % 2) * 0.3, 0), Vector3(s * 1.2, s, s))
	n.scale = Vector3.ONE * scale_v
	return n

func _island(parent: Node3D, top_c: Color, earth: Color, size: float, trees: bool, tree_c: Color) -> Node3D:
	var n := Kit.node("Island", parent)
	Kit.part(n, "cone", Kit.toon(earth, 0.0, 0.1), Vector3(0, -0.45, 0), Vector3(1.6, 1.1, 1.2), Vector3(180, 0, 0))
	Kit.part(n, "cylinder", Kit.toon(top_c, 0.02, 0.15), Vector3(0, 0.1, 0), Vector3(1.7, 0.22, 1.3))
	if trees:
		for i in 1 + _rng.randi() % 3:
			var x := -0.5 + i * 0.5
			Kit.part(n, "cylinder", Kit.toon(Color(0.45, 0.3, 0.2)), Vector3(x, 0.4, 0.1), Vector3(0.1, 0.5, 0.1))
			Kit.part(n, "low_sphere", Kit.toon(tree_c, 0.02, 0.1), Vector3(x, 0.78, 0.1), Vector3.ONE * (0.45 + _rng.randf() * 0.2))
	n.scale = Vector3.ONE * size
	return n

func _decor(theme: String, layer: int, parent: Node3D, ground: Color, accent: Color, cloud: Color) -> Node3D:
	var size := 2.0 + _rng.randf() * 1.4 if layer == 0 else 1.0 + _rng.randf() * 0.8
	match theme:
		"valley":
			if layer == 0: return Kit.part(parent, "hemi", Kit.toon(ground, 0.0, 0.05), Vector3.ZERO, Vector3(size * 2.0, size * 2.0, size))
			return _island(parent, ground, Color(0.55, 0.38, 0.25), size, true, ground.lerp(Color(0.2, 0.5, 0.2), 0.5))
		"mountain":
			if layer == 0: return _peak(parent, ground, Color.WHITE, size)
			return _rocky(parent, ground, size * 0.8, Color.TRANSPARENT)
		"rainbow":
			if layer == 0: return _rainbow(parent, size * 1.3)
			var isl := _island(parent, ground, Color(0.9, 0.7, 0.85), size, true, accent)
			var wf := FX.emitter(isl, Color(0.75, 0.9, 1, 0.8), 14, 0.8, 0.2, Vector3(0, -2.5, 0), 5.0, Vector3(0.15, 0.03, 0.03), false)
			wf.local_coords = true
			wf.position = Vector3(0.75, 0.05, 0.3)
			return isl
		"storm":
			return _cluster(parent, cloud.lerp(Color.BLACK, 0.35 if layer == 0 else 0.2), size * (0.7 if layer == 0 else 0.9))
		"frozen":
			if layer == 0: return _peak(parent, Color(0.75, 0.85, 1), Color.WHITE, size)
			var isl2 := _island(parent, Color.WHITE, Color(0.6, 0.75, 0.9), size, false, Color.WHITE)
			_crystals(isl2, Color(0.7, 0.9, 1))
			return isl2
		"fire":
			if layer == 0: return _volcano(parent, ground, accent, size)
			return _rocky(parent, ground, size * 0.8, accent)
		"cosmic":
			if layer == 0: return _planet(parent, size * 0.6)
			return _rocky(parent, Color(0.35, 0.32, 0.4), size * 0.5, Color.TRANSPARENT)
		"golden":
			if layer == 0: return _cluster(parent, cloud.lerp(accent, 0.25), size * 0.8)
			return _temple(parent, ground, accent, size * 0.8)
		"celestial":
			if layer == 0: return _spire(parent, accent, size)
			var isl3 := _island(parent, ground, Color(0.5, 0.45, 0.8), size, false, Color.WHITE)
			_crystals(isl3, accent)
			return isl3
	# heaven
	if layer == 0: return _castle(parent, Color.WHITE, accent, size * 0.8)
	return _gate(parent, accent, size * 0.9)

func _peak(parent: Node3D, rock: Color, snow: Color, size: float) -> Node3D:
	var n := Kit.node("Peak", parent)
	Kit.part(n, "cone", Kit.toon(rock, 0.0, 0.1), Vector3.ZERO, Vector3(1.6, 2.0, 1.0))
	Kit.part(n, "cone", Kit.toon(snow, 0.05, 0.2), Vector3(0, 0.65, 0.05), Vector3(0.62, 0.72, 0.42))
	n.scale = Vector3.ONE * size
	return n

func _rocky(parent: Node3D, color: Color, size: float, glow: Color) -> Node3D:
	var n := Kit.node("Rock", parent)
	var m := Kit.toon(color, 0.0, 0.1)
	Kit.part(n, "octa", m, Vector3.ZERO, Vector3(1.3, 1.0, 1.2), Vector3(10, 20, 30))
	Kit.part(n, "octa", m, Vector3(0.6, -0.2, 0), Vector3(0.7, 0.6, 0.7), Vector3(40, 0, 10))
	if glow.a > 0.0:
		Kit.part(n, "low_sphere", Kit.toon(glow, 1.0), Vector3(0, -0.35, 0.3), Vector3(0.5, 0.2, 0.3))
		Kit.glow(n, Vector3(0, -0.4, 0.5), 1.4, Color(glow, 0.5))
	n.scale = Vector3.ONE * size
	return n

func _rainbow(parent: Node3D, size: float) -> Node3D:
	var n := Kit.node("Rainbow", parent)
	var cols := [Color(1, 0.35, 0.35), Color(1, 0.65, 0.25), Color(1, 0.92, 0.35), Color(0.4, 0.85, 0.45), Color(0.35, 0.6, 1), Color(0.65, 0.45, 1)]
	for i in cols.size():
		var r := 1.0 - i * 0.09
		Kit.part(n, "arc", Kit.unlit(Color(cols[i], 0.55)), Vector3(0, 0, i * 0.01), Vector3(r * 2.0, r * 2.0, 1))
	n.scale = Vector3.ONE * size
	return n

func _crystals(island: Node3D, color: Color) -> void:
	var m := Kit.toon(color, 0.35, 0.9)
	for i in 3:
		var h := 0.5 + _rng.randf() * 0.5
		Kit.part(island, "octa", m, Vector3(-0.5 + i * 0.5, 0.2 + h * 0.4, 0.1), Vector3(0.3, h, 0.3), Vector3(0, 0, (i - 1) * 12))

func _volcano(parent: Node3D, rock: Color, lava: Color, size: float) -> Node3D:
	var n := Kit.node("Volcano", parent)
	Kit.part(n, "cone", Kit.toon(rock, 0.0, 0.1), Vector3.ZERO, Vector3(1.8, 1.6, 1.0))
	Kit.part(n, "low_sphere", Kit.toon(lava, 1.2), Vector3(0, 0.75, 0.1), Vector3(0.35, 0.15, 0.2))
	Kit.glow(n, Vector3(0, 0.95, 0.3), 1.6, Color(lava, 0.55))
	var smoke := FX.emitter(n, Color(0.3, 0.25, 0.25, 0.45), 6, 2.5, 0.6, Vector3(0.2, 0.8, 0), 15.0, Vector3.ONE * 0.1, false)
	smoke.local_coords = true
	smoke.position = Vector3(0, 0.85, 0)
	n.scale = Vector3.ONE * size
	return n

func _planet(parent: Node3D, size: float) -> Node3D:
	var n := Kit.node("Planet", parent)
	var c := Color.from_hsv(_rng.randf(), 0.45, 0.8)
	Kit.part(n, "sphere", Kit.toon(c, 0.05, 0.3), Vector3.ZERO, Vector3.ONE * 2.0)
	if _rng.randf() > 0.4:
		Kit.part(n, "torus", Kit.toon(c.lerp(Color.WHITE, 0.5), 0.1, 0.4), Vector3.ZERO, Vector3(3.2, 0.4, 3.2), Vector3(15, 0, -18))
	Kit.glow(n, Vector3(0, 0, -1), 6.0, Color(c, 0.18))
	n.scale = Vector3.ONE * size
	return n

func _temple(parent: Node3D, stone: Color, gold: Color, size: float) -> Node3D:
	var n := Kit.node("Temple", parent)
	var sm := Kit.toon(stone.lerp(Color.WHITE, 0.4), 0.05, 0.3)
	var gm := Kit.toon(gold, 0.2, 0.8)
	Kit.part(n, "box", sm, Vector3(0, -0.5, 0), Vector3(1.8, 0.2, 1))
	for i in 4:
		Kit.part(n, "cylinder", sm, Vector3(-0.7 + i * 0.47, 0, 0.2), Vector3(0.18, 0.8, 0.18))
	Kit.part(n, "box", gm, Vector3(0, 0.5, 0), Vector3(1.9, 0.18, 1))
	Kit.part(n, "prism", gm, Vector3(0, 0.75, 0), Vector3(2.0, 0.35, 0.9))
	Kit.part(n, "low_sphere", Kit.toon(Color.WHITE, 0.1, 0.05), Vector3(0, -0.7, 0), Vector3(2.4, 0.5, 1.2))
	n.scale = Vector3.ONE * size
	return n

func _spire(parent: Node3D, color: Color, size: float) -> Node3D:
	var n := Kit.node("Spire", parent)
	var m := Kit.toon(color, 0.3, 0.9)
	Kit.part(n, "octa", m, Vector3.ZERO, Vector3(0.8, 3.0, 0.8))
	Kit.part(n, "octa", m, Vector3(0.5, -0.6, 0), Vector3(0.45, 1.6, 0.45), Vector3(0, 0, -15))
	Kit.glow(n, Vector3(0, 0, -0.5), 3.0, Color(color, 0.25))
	n.scale = Vector3.ONE * size * 0.6
	return n

func _castle(parent: Node3D, wall: Color, gold: Color, size: float) -> Node3D:
	var n := Kit.node("Castle", parent)
	var wm := Kit.toon(wall, 0.15, 0.4)
	var gm := Kit.toon(gold, 0.35, 0.9)
	Kit.part(n, "box", wm, Vector3.ZERO, Vector3(1.4, 1.2, 0.8))
	for i in [-1, 0, 1]:
		var h := 2.2 if i == 0 else 1.6
		Kit.part(n, "cylinder", wm, Vector3(i * 0.8, h * 0.5 - 0.3, 0.1), Vector3(0.45, h, 0.45))
		Kit.part(n, "cone", gm, Vector3(i * 0.8, h - 0.1, 0.1), Vector3(0.6, 0.6, 0.6))
	Kit.part(n, "low_sphere", Kit.toon(Color.WHITE, 0.2, 0.05), Vector3(0, -0.7, 0), Vector3(3, 0.6, 1.2))
	Kit.glow(n, Vector3(0, 0.6, -0.6), 4.0, Color(gold, 0.3))
	n.scale = Vector3.ONE * size
	return n

func _gate(parent: Node3D, gold: Color, size: float) -> Node3D:
	var n := Kit.node("Gate", parent)
	var gm := Kit.toon(gold, 0.3, 0.9)
	Kit.part(n, "cylinder", gm, Vector3(-0.7, 0, 0), Vector3(0.25, 1.6, 0.25))
	Kit.part(n, "cylinder", gm, Vector3(0.7, 0, 0), Vector3(0.25, 1.6, 0.25))
	Kit.part(n, "arc", Kit.unlit(gold), Vector3(0, 0.8, 0), Vector3(0.85, 0.7, 1))
	Kit.part(n, "low_sphere", Kit.toon(Color.WHITE, 0.2, 0.05), Vector3(0, -0.85, 0), Vector3(2.4, 0.5, 1))
	n.scale = Vector3.ONE * size
	return n

# ---------------------------------------------------------------- weather

func _weather(kind: String, half_w: float, half_h: float) -> void:
	if kind == "none": return
	var box := Vector3(half_w * 1.1, 0.25, 0.5)
	var at := Vector3(0, half_h + 1.0, -20)
	var color := Color.WHITE
	var vel := Vector3(0, -1.2, 0)
	var amount := 40
	var life := 12.0
	var size := 0.12
	var additive := true
	match kind:
		"rain":
			color = Color(0.75, 0.85, 1, 0.5); vel = Vector3(-1.5, -22, 0); amount = 90; life = 1.3; size = 0.14; additive = false
		"snow":
			color = Color(1, 1, 1, 0.9); vel = Vector3(0.4, -2.2, 0); amount = 120; life = 10.0; size = 0.2; additive = false
		"embers":
			color = Color(1, 0.6, 0.2); vel = Vector3(0.3, 1.8, 0); at = Vector3(0, -half_h - 1.0, -20); amount = 90; life = 11.0; size = 0.12
		"stars":
			color = Color(0.85, 0.9, 1); vel = Vector3(0, -0.4, 0); at = Vector3(0, 0, -70); box = Vector3(half_w * 1.2, half_h * 1.1, 0.5); amount = 60; life = 5.0; size = 0.18
		"gold":
			color = Color(1, 0.85, 0.4); amount = 60; life = 14.0
		"motes":
			color = Color(0.85, 0.8, 1); amount = 60; life = 14.0
		"pollen":
			color = Color(1, 1, 0.8, 0.8); vel = Vector3(0.5, -1.4, 0); amount = 45; life = 14.0; size = 0.1
		"petals":
			color = Color(1, 0.7, 0.85, 0.9); vel = Vector3(0.5, -1.4, 0); amount = 45; life = 14.0; size = 0.14; additive = false
	var p := FX.emitter(self, color, amount, life, size, vel, 8.0, box, additive)
	p.position = at
	p.local_coords = true
	p.preprocess = minf(10.0, life)
	if kind == "rain":
		p.scale_amount_curve = null
		var mm := (p.mesh as QuadMesh)
		mm.size = Vector2(0.25, 2.2)
		(mm.material as StandardMaterial3D).billboard_mode = BaseMaterial3D.BILLBOARD_DISABLED
