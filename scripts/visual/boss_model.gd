class_name BossModel
extends Node3D
## Procedural boss models (one per world) with wing flaps, spinners, chain sway, attack pulse and hit flash.

var body: Node3D
var flappers: Array = []   # [node, axis Vector3, amp_deg, speed, base_rot Vector3]
var spinners: Array = []   # [node, deg/s Vector3]
var chain: Array = []      # [node, index, base_pos]
var flash_mats: Array[StandardMaterial3D] = []
var base_scale := 1.0
var intensity := 1.0
var _punch := 0.0
var _flash := 0.0
var _t := 0.0
var _base_colors: Array[Color] = []

static func build(d: Dictionary) -> BossModel:
	var v := BossModel.new()
	v.name = "Boss_" + d.id
	v.body = Kit.node("Body", v)
	var b := v.body
	var a: Color = d.c1
	var s: Color = d.c2
	var g: Color = d.glow
	var main := Kit.toon_instance(a, 0.05, 0.4)
	v.flash_mats.append(main)
	var second := Kit.toon(s, 0.1, 0.6)
	var glow := Kit.toon(g, 0.9, 0.8)
	Kit.glow(b, Vector3(0, 0, -1.5), 7.0, Color(g, 0.35))
	match str(d.id):
		"valley_guardian", "fire_phoenix":
			var phoenix: bool = d.id == "fire_phoenix"
			Kit.part(b, "sphere", main, Vector3.ZERO, Vector3(1.6, 1.3, 1.1))
			Kit.part(b, "sphere", second, Vector3(0, -0.25, 0.35), Vector3(1.0, 0.9, 0.6))
			Kit.part(b, "sphere", main, Vector3(0, 0.95, 0.1), Vector3.ONE * 0.85)
			Kit.part(b, "cone", Kit.toon(Color(1, 0.75, 0.2), 0.1), Vector3(0, 0.8, 0.6), Vector3(0.25, 0.45, 0.25), Vector3(100, 0, 0))
			_eyes(b, Vector3(0, 1.05, 0.42), 0.2, Color(1, 0.9, 0.3) if phoenix else Color(0.1, 0.1, 0.1), 0.8 if phoenix else 0.0)
			for i in [-1, 0, 1]:
				Kit.part(b, "cone", second, Vector3(i * 0.18, 1.45, 0), Vector3(0.14, 0.5, 0.14), Vector3(0, 0, -i * 20))
			_wings(v, b, Kit.toon(s, 0.4, 0.4) if phoenix else main, 2.4, 0.4, 4.0, 30.0)
			for i in range(-2, 3):
				Kit.part(b, "low_sphere", second, Vector3(i * 0.22, -1.05, -0.2), Vector3(0.22, 0.8, 0.1), Vector3(0, 0, i * 14))
			if phoenix:
				_fire(b, 1.4)
		"mountain_king":
			Kit.part(b, "octa", main, Vector3.ZERO, Vector3(3, 3, 2.2))
			Kit.part(b, "box", main, Vector3(0, 1.45, 0.1), Vector3(1.1, 0.9, 0.9), Vector3(0, 0, 5))
			_eyes(b, Vector3(0, 1.5, 0.58), 0.18, g, 1.0)
			_crown(b, Vector3(0, 2.0, 0), 0.75)
			for side in [-1, 1]:
				var arm := Kit.node("Arm", b, Vector3(1.2 * side, 0.4, 0))
				Kit.part(arm, "octa", second, Vector3(0.4 * side, -0.9, 0), Vector3(0.9, 1.2, 0.9))
				v.flappers.append([arm, Vector3(0, 0, 1), 18.0 * side, 2.0, arm.rotation_degrees])
		"butterfly_queen":
			Kit.part(b, "sphere", main, Vector3(0, 0.2, 0), Vector3(0.6, 1.2, 0.6))
			Kit.part(b, "sphere", second, Vector3(0, -0.8, 0), Vector3(0.45, 1.2, 0.45))
			Kit.part(b, "sphere", main, Vector3(0, 0.95, 0), Vector3.ONE * 0.55)
			_eyes(b, Vector3(0, 1.0, 0.25), 0.12, g, 0.8)
			_crown(b, Vector3(0, 1.3, 0), 0.45)
			for side in [-1, 1]:
				var pivot := Kit.node("Wings", b)
				Kit.part(pivot, "low_sphere", Kit.toon(a, 0.3, 0.4), Vector3(1.2 * side, 0.6, -0.1), Vector3(2.2, 2.0, 0.08))
				Kit.part(pivot, "low_sphere", Kit.toon(g, 0.5, 0.4), Vector3(1.2 * side, 0.6, -0.05), Vector3(1.1, 1.0, 0.1))
				Kit.part(pivot, "low_sphere", Kit.toon(s, 0.3, 0.4), Vector3(0.9 * side, -0.7, -0.1), Vector3(1.5, 1.4, 0.08))
				v.flappers.append([pivot, Vector3(0, 1, 0), 35.0 * side, 6.0, Vector3.ZERO])
			for side in [-1, 1]:
				Kit.rope(b, Vector3(0.1 * side, 1.15, 0), Vector3(0.45 * side, 1.8, 0), 0.05, second)
			_sparkles(b, g, 2.0)
		"thunder_dragon":
			Kit.part(b, "sphere", main, Vector3(0, 0.4, 0.2), Vector3(1.3, 1.0, 1.2))
			Kit.part(b, "sphere", main, Vector3(0, 0.15, 0.85), Vector3(0.8, 0.55, 0.9))
			_eyes(b, Vector3(0, 0.6, 0.75), 0.16, g, 1.0)
			for side in [-1, 1]:
				Kit.part(b, "cone", second, Vector3(0.45 * side, 1.0, 0), Vector3(0.2, 0.8, 0.2), Vector3(0, 0, -25 * side))
			for i in 7:
				var side := -1.0 if i % 2 == 0 else 1.0
				var seg := Kit.part(b, "sphere", main if i % 2 == 0 else second, Vector3(side * (0.9 + i * 0.35), -0.2 - i * 0.12, -0.4), Vector3.ONE * (0.9 - i * 0.08))
				v.chain.append([seg, i, seg.position])
			_wings(v, b, Kit.toon(s.darkened(0.2), 0.2, 0.4), 2.2, 0.6, 3.0, 25.0)
			_sparkles(b, g, 1.6)
		"ice_guardian":
			var ice := Kit.toon(a, 0.25, 0.95)
			Kit.part(b, "octa", ice, Vector3.ZERO, Vector3(2.4, 3.2, 2))
			Kit.part(b, "octa", ice, Vector3(0, 1.6, 0), Vector3(1, 1.2, 1))
			_eyes(b, Vector3(0, 1.65, 0.45), 0.15, g, 1.0)
			var orbit := Kit.node("Shards", b)
			for i in 6:
				var ang := i / 6.0 * TAU
				Kit.part(orbit, "octa", Kit.toon(s, 0.4, 0.95), Vector3(cos(ang) * 2.0, 0, sin(ang) * 2.0), Vector3(0.35, 0.8, 0.35))
			orbit.rotation_degrees = Vector3(15, 0, 0)
			v.spinners.append([orbit, Vector3(0, 60, 0)])
			_sparkles(b, Color.WHITE, 2.0)
		"cosmic_guardian":
			Kit.part(b, "sphere", Kit.toon(Color(0.95, 0.95, 1), 0.1, 0.8), Vector3.ZERO, Vector3.ONE * 2.4)
			Kit.part(b, "cylinder", Kit.unlit(s), Vector3(0, 0, 1.18), Vector3(1.2, 0.02, 1.2), Vector3(90, 0, 0))
			Kit.part(b, "cylinder", Kit.unlit(Color(0.05, 0.02, 0.1)), Vector3(0, 0, 1.21), Vector3(0.55, 0.02, 0.55), Vector3(90, 0, 0))
			Kit.part(b, "hemi", main, Vector3(0, 0.15, 0), Vector3(2.6, 1.6, 2.6))
			for i in 3:
				var ring := Kit.part(b, "torus", Kit.toon(g, 0.6, 0.9), Vector3.ZERO, Vector3.ONE * (3.3 + i * 0.5), Vector3(60 + i * 30, i * 40, 0))
				v.spinners.append([ring, Vector3(0, 40 + i * 25, 0)])
			_sparkles(b, g, 3.0)
		"golden_titan":
			Kit.part(b, "box", main, Vector3.ZERO, Vector3(2.2, 2.2, 1.4))
			Kit.part(b, "box", second, Vector3(0, -0.9, 0), Vector3(2.3, 0.35, 1.5))
			Kit.part(b, "box", main, Vector3(0, 1.6, 0), Vector3(0.9, 1, 0.9))
			Kit.part(b, "cone", second, Vector3(0, 2.3, 0), Vector3(0.9, 0.6, 0.9))
			_eyes(b, Vector3(0, 1.65, 0.48), 0.15, Color(0.4, 0.9, 1), 1.0)
			Kit.part(b, "octa", glow, Vector3(0, 0.3, 0.75), Vector3(0.6, 0.8, 0.4))
			for side in [-1, 1]:
				Kit.part(b, "sphere", second, Vector3(1.35 * side, 0.9, 0), Vector3.ONE * 0.9)
				var arm := Kit.node("Arm", b, Vector3(1.4 * side, 0.6, 0))
				Kit.part(arm, "box", main, Vector3(0.2 * side, -1.0, 0), Vector3(0.6, 1.6, 0.6))
				v.flappers.append([arm, Vector3(0, 0, 1), 12.0 * side, 1.5, Vector3.ZERO])
		_:
			# celestial_guardian and heavens_guardian
			var final: bool = d.id == "heavens_guardian"
			var crystal := Kit.toon(a, 0.3, 0.95)
			Kit.part(b, "cone", crystal, Vector3(0, -0.2, 0), Vector3(1.6, 2.8, 1.6), Vector3(180, 0, 0))
			Kit.part(b, "sphere", Kit.toon(Color(1, 0.97, 0.9) if final else s, 0.3, 0.8), Vector3(0, 1.55, 0), Vector3.ONE * 0.85)
			_eyes(b, Vector3(0, 1.6, 0.38), 0.12, g, 1.0)
			var halo := Kit.part(b, "torus", Kit.toon(Color(1, 0.88, 0.4), 0.9, 0.9), Vector3(0, 2.25, 0), Vector3(1.3, 0.8, 1.3), Vector3(15, 0, 0))
			v.spinners.append([halo, Vector3(0, 50, 0)])
			for p in (3 if final else 2):
				var wm := Kit.toon(Color.WHITE if p % 2 == 0 else s, 0.25, 0.6)
				_wings(v, b, wm, 2.4 - p * 0.35, 0.5 - p * 0.45, 2.5 + p * 0.5, 20.0, -15.0 + p * 25.0)
			if final:
				var rays := Kit.node("Rays", b, Vector3(0, 0.6, -1.2))
				var rm := Kit.unlit(Color(1, 0.92, 0.6, 0.5), true, Kit.streak())
				for i in 12:
					var ang := deg_to_rad(i * 30.0)
					Kit.part(rays, "quad", rm, Vector3(cos(ang), sin(ang), 0) * 2.6, Vector3(5, 0.6, 1), Vector3(0, 0, i * 30))
				v.spinners.append([rays, Vector3(0, 0, 15)])
			_sparkles(b, g, 2.5)
	v.base_scale = float(d.scale)
	b.scale = Vector3.ONE * v.base_scale
	for m in v.flash_mats: v._base_colors.append(m.albedo_color)
	return v

static func _eyes(b: Node3D, center: Vector3, size: float, color: Color, emission: float) -> void:
	var m := Kit.toon(color, emission, 0.9)
	Kit.part(b, "low_sphere", m, center + Vector3(-size * 1.4, 0, 0), Vector3.ONE * size)
	Kit.part(b, "low_sphere", m, center + Vector3(size * 1.4, 0, 0), Vector3.ONE * size)

static func _crown(b: Node3D, p: Vector3, size: float) -> void:
	var gold := Kit.toon(Color(1, 0.8, 0.2), 0.35, 0.9)
	Kit.part(b, "cylinder", gold, p, Vector3(size, size * 0.25, size))
	for i in 5:
		var a := i / 5.0 * TAU
		Kit.part(b, "cone", gold, p + Vector3(cos(a) * size * 0.4, size * 0.3, sin(a) * size * 0.4), Vector3(size * 0.22, size * 0.45, size * 0.22))

static func _wings(v: BossModel, b: Node3D, m: Material, span: float, height: float, speed: float, amp: float, tilt := 10.0) -> void:
	for side in [-1, 1]:
		var pivot := Kit.node("Wing", b, Vector3(0.5 * side, height, -0.2))
		# Feather fan: three overlapping flattened ellipsoids sweeping outward and up.
		for k in 3:
			var ln := span * (1.0 - k * 0.18)
			var ang := deg_to_rad(tilt + 18.0 - k * 22.0)
			var dir := Vector3(cos(ang) * side, sin(ang), 0)
			var part := Kit.part(pivot, "low_sphere", m, dir * ln * 0.5, Vector3(ln, span * 0.32, 0.08))
			part.rotation.z = ang * side
		v.flappers.append([pivot, Vector3(0, 0, 1), amp * side, speed, Vector3.ZERO])

static func _fire(b: Node3D, radius: float) -> void:
	var p := FX.emitter(b, Color(1, 0.6, 0.15), 40, 0.6, 0.6, Vector3(0, 2, 0), 25.0, Vector3.ONE * radius * 0.6)
	var g := Gradient.new()
	g.set_color(0, Color(1, 0.9, 0.4))
	g.set_color(1, Color(1, 0.3, 0.05, 0))
	p.color_ramp = g
	p.color = Color.WHITE
	p.local_coords = true

static func _sparkles(b: Node3D, color: Color, radius: float) -> void:
	var p := FX.emitter(b, color, 14, 1.0, 0.3, Vector3(0, 0.5, 0), 180.0, Vector3.ONE * radius)
	p.local_coords = true

func attack_pulse() -> void: _punch = 1.0
func hit_flash() -> void: _flash = 0.3

func _process(dt: float) -> void:
	_t += dt
	for f in flappers:
		var axis: Vector3 = f[1]
		f[0].rotation_degrees = (f[4] as Vector3) + axis * sin(_t * f[3] * intensity) * f[2]
	for s in spinners:
		s[0].rotation_degrees += (s[1] as Vector3) * intensity * dt
	for c in chain:
		c[0].position = (c[2] as Vector3) + Vector3(0, sin(_t * 3.0 * intensity - c[1] * 0.7) * 0.25, 0)
	_punch = maxf(0.0, _punch - dt * 4.0)
	body.scale = Vector3.ONE * base_scale * (1.0 + sin(_punch * PI) * 0.08)
	_flash = maxf(0.0, _flash - dt)
	var k := _flash / 0.3
	for i in flash_mats.size():
		flash_mats[i].albedo_color = _base_colors[i].lerp(Color.WHITE, k * 0.8)
		flash_mats[i].emission_enabled = k > 0.0
		flash_mats[i].emission = Color.WHITE
		flash_mats[i].emission_energy_multiplier = k
