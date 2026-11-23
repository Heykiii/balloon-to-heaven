class_name ObstacleModel
## Builds a recognisable low-poly model for every obstacle visual type and wires the
## view hooks (body, warn/active telegraph nodes, sized part, wings, spinners, trail) on the Obstacle.
## Camera looks down -z, so +z is toward the player.

static func build(o: Obstacle) -> void:
	var d: Dictionary = o.data
	var a: Color = d.c1
	var b: Color = d.c2
	var r: float = d.radius
	var body := Kit.node("Body", o)
	body.scale = Vector3.ONE * float(d.scale)
	o.body = body
	match str(d.visual):
		"small_bird": _bird(o, body, a, b, 0.9, 0.0)
		"eagle": _bird(o, body, a, b, 1.35, 0.0)
		"electric_bird":
			_bird(o, body, a, b, 1.0, 0.35)
			_sparks(body, b, 0.5)
		"frozen_bird": _bird(o, body, a, b, 0.9, 0.1)
		"burning_bird":
			_bird(o, body, a, b, 1.0, 0.45)
			_fire(body, 0.4, false)
		"celestial_bird":
			_bird(o, body, a, b, 1.0, 0.5)
			Kit.glow(body, Vector3(0, 0, -0.3), 1.6, Color(b, 0.5))
		"butterfly": _butterfly(o, body, a, b)
		"rock", "asteroid": _rock(o, body, a, b, r * 2.0)
		"snowball":
			Kit.part(body, "sphere", Kit.toon(a, 0.05, 0.3), Vector3.ZERO, Vector3.ONE * r * 2.1)
			Kit.part(body, "low_sphere", Kit.toon(b, 0.0, 0.2), Vector3(r * 0.4, r * 0.3, r * 0.6), Vector3.ONE * r * 0.7)
			o.spinners.append([body, Vector3(0, 0, float(d.spin))])
		"ice_crystal", "shard":
			Kit.part(body, "octa", Kit.toon(a, 0.25, 0.9), Vector3.ZERO, Vector3(r * 1.6, r * 2.6, r * 1.6))
			Kit.part(body, "octa", Kit.toon(b, 0.2, 0.9), Vector3(r * 0.6, -r * 0.4, 0), Vector3(r * 0.7, r * 1.2, r * 0.7), Vector3(0, 0, -30))
			o.spinners.append([body, Vector3(0, float(d.spin), 0)])
		"fireball":
			Kit.part(body, "sphere", Kit.toon(b, 0.9, 0.2), Vector3.ZERO, Vector3.ONE * r * 1.6)
			Kit.glow(body, Vector3(0, 0, 0.3), r * 4.0, Color(a, 0.8))
			o.trail = _fire(o, r, true)
		"meteor":
			_rock(o, body, a.darkened(0.4), a, r * 1.8)
			Kit.glow(body, Vector3(0, 0, 0.3), r * 3.4, Color(b, 0.6))
			o.trail = _fire(o, r, true)
		"drone": _drone(o, body, a, b, r)
		"orb":
			Kit.part(body, "sphere", Kit.toon(a, 0.7, 0.6), Vector3.ZERO, Vector3.ONE * r * 1.7)
			Kit.glow(body, Vector3(0, 0, 0.4), r * 4.2, Color(a, 0.65))
			Kit.glow(body, Vector3(0, 0, 0.45), r * 1.6, Color(b, 0.9))
		"guardian":
			Kit.part(body, "sphere", Kit.toon(a, 0.35, 0.9), Vector3.ZERO, Vector3.ONE * r * 1.5)
			var ring1 := Kit.part(body, "torus", Kit.toon(b, 0.5, 0.9), Vector3.ZERO, Vector3.ONE * r * 2.4, Vector3(70, 0, 0))
			var ring2 := Kit.part(body, "torus", Kit.toon(a, 0.4, 0.9), Vector3.ZERO, Vector3.ONE * r * 2.1, Vector3(0, 0, 70))
			o.spinners.append([ring1, Vector3(0, 120, 0)])
			o.spinners.append([ring2, Vector3(120, 0, 0)])
			Kit.part(body, "low_sphere", Kit.toon(Color(0.2, 0.1, 0.3)), Vector3(0, 0, r * 0.65), Vector3.ONE * r * 0.5)
		"feather":
			Kit.part(body, "low_sphere", Kit.toon(a, 0.1, 0.2), Vector3.ZERO, Vector3(r * 0.9, r * 2.6, r * 0.25))
			Kit.part(body, "cylinder", Kit.toon(b), Vector3.ZERO, Vector3(0.05, r * 2.8, 0.05))
			o.spinners.append([body, Vector3(0, 0, 200)])
		"dark_cloud":
			_cloud(body, a, b, r)
			_sparks(body, Color(1, 0.95, 0.3), r)
		"lightning": _lightning(o, d)
		"lava": _lava(o, d)
		"barrier": _barrier(o, d)
		"beam": _beam(o, d)
		"wind": _wind(o, d)
		"cliff", "gate": _wall(o, d)
		"platform":
			var sized := Kit.node("Sized", body)
			Kit.part(sized, "box", Kit.toon(Color(0.55, 0.4, 0.3)), Vector3(0, -0.1, 0), Vector3(1, 0.8, 1))
			Kit.part(sized, "box", Kit.toon(a, 0.05, 0.2), Vector3(0, 0.4, 0), Vector3(1.05, 0.25, 1.05))
			for i in 3:
				Kit.part(sized, "cone", Kit.toon(b.darkened(0.2)), Vector3(-0.3 + i * 0.3, 0.6, 0.3), Vector3(0.12, 0.3, 0.12))
			o.sized = sized
		"temple":
			var sized := Kit.node("Sized", body)
			var trim := Kit.toon(b, 0.15, 0.8)
			Kit.part(sized, "box", Kit.toon(a, 0.1, 0.7), Vector3.ZERO, Vector3.ONE)
			Kit.part(sized, "box", trim, Vector3(0, 0.5, 0), Vector3(1.1, 0.12, 1.1))
			Kit.part(sized, "box", trim, Vector3(0, -0.5, 0), Vector3(1.1, 0.12, 1.1))
			for i in [-1, 1]:
				Kit.part(sized, "cylinder", trim, Vector3(i * 0.32, 0, 0.5), Vector3(0.14, 0.9, 0.14))
			o.sized = sized
		"crystal_barrier":
			var sized := Kit.node("Sized", body)
			var m := Kit.toon(a, 0.35, 0.9)
			for i in 5:
				Kit.part(sized, "octa", m, Vector3(-0.4 + i * 0.2, 0, 0), Vector3(0.25, 1.6, 0.5), Vector3(0, 0, 90 + (10 if i % 2 == 0 else -10)))
			o.sized = sized
		_:
			Kit.part(body, "sphere", Kit.toon(a), Vector3.ZERO, Vector3.ONE * r * 2.0)
	if o.sized and o.is_box():
		o.sized.scale = Vector3(d.box.x, d.box.y, 1.0)

# ---------------------------------------------------------------- creatures

static func _bird(o: Obstacle, body: Node3D, a: Color, b: Color, s: float, emission: float) -> void:
	var body_mat := Kit.toon(a, emission, 0.3)
	var belly := Kit.toon(b, emission * 0.5, 0.2)
	Kit.part(body, "low_sphere", body_mat, Vector3.ZERO, Vector3(0.9, 0.5, 0.5) * s)
	Kit.part(body, "low_sphere", belly, Vector3(0.05, -0.08, 0.08) * s, Vector3(0.6, 0.32, 0.4) * s)
	Kit.part(body, "low_sphere", belly, Vector3(0.45, 0.12, 0) * s, Vector3.ONE * 0.38 * s)
	Kit.part(body, "cone", Kit.toon(Color(1, 0.7, 0.2)), Vector3(0.68, 0.1, 0) * s, Vector3(0.12, 0.22, 0.12) * s, Vector3(0, 0, -90))
	Kit.part(body, "low_sphere", Kit.toon(Color(0.08, 0.08, 0.1)), Vector3(0.55, 0.18, 0.15) * s, Vector3.ONE * 0.08 * s)
	Kit.part(body, "cone", body_mat, Vector3(-0.5, 0.05, 0) * s, Vector3(0.25, 0.35, 0.1) * s, Vector3(0, 0, 90))
	# Near wing extends toward the camera (+z), far wing away; flapping around x swings them up and down.
	for side in [1, -1]:
		var pivot := Kit.node("Wing", body, Vector3(-0.05, 0.12, 0.1 * side) * s)
		Kit.part(pivot, "low_sphere", body_mat, Vector3(0, 0, 0.32 * side) * s, Vector3(0.45, 0.08, 0.7) * s)
		o.wings.append([pivot, 0, -30.0 * side, -50.0 * side, 10.0])

static func _butterfly(o: Obstacle, body: Node3D, a: Color, b: Color) -> void:
	Kit.part(body, "cylinder", Kit.toon(Color(0.2, 0.15, 0.2)), Vector3.ZERO, Vector3(0.08, 0.9, 0.08))
	var ma := Kit.toon(a, 0.25, 0.4)
	var mb := Kit.toon(b, 0.25, 0.4)
	for side in [-1, 1]:
		var pivot := Kit.node("Wing", body)
		Kit.part(pivot, "low_sphere", ma, Vector3(0.25 * side, 0.1, 0), Vector3(0.45, 0.4, 0.05))
		Kit.part(pivot, "low_sphere", mb, Vector3(0.18 * side, -0.15, 0), Vector3(0.3, 0.28, 0.05))
		o.wings.append([pivot, 1, 0.0, 55.0 * side, 14.0])

static func _rock(o: Obstacle, body: Node3D, a: Color, b: Color, size: float) -> void:
	Kit.part(body, "octa", Kit.toon(a, 0.0, 0.15), Vector3.ZERO, Vector3(size * 1.1, size * 0.95, size * 1.05), Vector3(15, 30, 40))
	Kit.part(body, "octa", Kit.toon(b, 0.0, 0.15), Vector3(size * 0.2, size * 0.15, size * 0.2), Vector3(size * 0.65, size * 0.55, size * 0.65), Vector3(60, 10, 0))
	o.spinners.append([body, Vector3(20, 0, 70) if float(o.data.spin) == 0.0 else Vector3(0.3, 0.4, 1.0) * float(o.data.spin)])

static func _drone(o: Obstacle, body: Node3D, a: Color, b: Color, r: float) -> void:
	Kit.part(body, "sphere", Kit.toon(Color(0.75, 0.78, 0.82), 0.0, 0.8), Vector3.ZERO, Vector3(r * 2.4, r * 0.7, r * 2.4))
	Kit.part(body, "hemi", Kit.toon(a, 0.4, 0.9), Vector3(0, r * 0.15, 0), Vector3.ONE * r * 1.2)
	var ring := Kit.node("Lights", body)
	for i in 6:
		var ang := i / 6.0 * TAU
		Kit.part(ring, "low_sphere", Kit.toon(b, 0.9), Vector3(cos(ang) * r * 1.05, -r * 0.05, sin(ang) * r * 1.05), Vector3.ONE * r * 0.22)
	o.spinners.append([ring, Vector3(0, 160, 0)])
	Kit.glow(body, Vector3(0, -r * 0.8, 0.2), r * 2.0, Color(b, 0.35))

static func _cloud(body: Node3D, a: Color, b: Color, radius: float) -> void:
	var m := Kit.toon(a, 0.0, 0.05)
	var m2 := Kit.toon(b, 0.0, 0.05)
	Kit.part(body, "low_sphere", m, Vector3.ZERO, Vector3.ONE * radius * 1.7)
	Kit.part(body, "low_sphere", m2, Vector3(-radius * 0.8, -radius * 0.2, -0.1), Vector3.ONE * radius * 1.25)
	Kit.part(body, "low_sphere", m2, Vector3(radius * 0.85, -radius * 0.15, -0.1), Vector3.ONE * radius * 1.3)
	Kit.part(body, "low_sphere", m, Vector3(radius * 0.3, radius * 0.45, -0.15), Vector3.ONE * radius * 1.1)

static func _sparks(parent: Node3D, color: Color, radius: float) -> CPUParticles3D:
	var p := FX.emitter(parent, color, 8, 0.3, 0.16, Vector3(0, 1.0, 0), 180.0, Vector3.ONE * radius * 0.8)
	p.local_coords = true
	return p

static func _fire(parent: Node3D, radius: float, trail: bool) -> CPUParticles3D:
	var p := FX.emitter(parent, Color(1, 0.6, 0.15), 30 if trail else 12, 0.45, radius * 1.1,
		Vector3(0, 2.5 if trail else 0.8, 0), 25.0, Vector3.ONE * radius * 0.5)
	var g := Gradient.new()
	g.set_color(0, Color(1, 0.9, 0.4))
	g.set_color(1, Color(1, 0.3, 0.05, 0))
	p.color_ramp = g
	p.color = Color.WHITE
	p.local_coords = not trail
	return p

# ---------------------------------------------------------------- timed / zone hazards

static func _states(o: Obstacle) -> Array:
	var warn := Kit.node("Warning", o)
	var act := Kit.node("Active", o)
	o.warn_node = warn
	o.active_node = act
	return [warn, act]

static func _lightning(o: Obstacle, d: Dictionary) -> void:
	var h: float = d.box.y
	var w: float = d.box.x
	var cloud := Kit.node("Cloud", o, Vector3(0, h * 0.5 + 0.4, -0.5))
	_cloud(cloud, d.c2, d.c2.darkened(0.2), 0.9)
	var s := _states(o)
	Kit.part(s[0], "quad", Kit.unlit(Color(1, 1, 0.5, 0.35), true, Kit.streak()), Vector3.ZERO, Vector3(h, 0.35, 1), Vector3(0, 0, 90))
	Kit.glow(s[0], Vector3(0, h * 0.5, 0), 2.2, Color(1, 1, 0.4, 0.5))
	Kit.part(s[1], "quad", Kit.unlit(Color(1, 1, 0.75), true, Kit.streak()), Vector3.ZERO, Vector3(h, w * 1.6, 1), Vector3(0, 0, 90))
	Kit.part(s[1], "quad", Kit.unlit(Color.WHITE, true, Kit.streak()), Vector3(0, 0, 0.05), Vector3(h, w * 0.6, 1), Vector3(0, 0, 90))
	Kit.glow(s[1], Vector3(0, h * 0.5, 0.1), 3.5, Color(1, 1, 0.6, 0.8))

static func _lava(o: Obstacle, d: Dictionary) -> void:
	var h: float = d.box.y
	var w: float = d.box.x
	Kit.part(o, "cone", Kit.toon(Color(0.25, 0.15, 0.12)), Vector3(0, -h * 0.5 - 0.3, -0.3), Vector3(w * 2.2, 0.9, w * 1.4))
	var s := _states(o)
	Kit.glow(s[0], Vector3(0, -h * 0.5 + 0.2, 0.2), w * 2.4, Color(1, 0.4, 0.1, 0.8))
	Kit.part(s[0], "quad", Kit.unlit(Color(1, 0.5, 0.1, 0.25), true, Kit.streak()), Vector3.ZERO, Vector3(h, w * 0.7, 1), Vector3(0, 0, 90))
	Kit.part(s[1], "cylinder", Kit.toon(d.c1, 1.1, 0.3), Vector3.ZERO, Vector3(w, h, w * 0.6))
	Kit.part(s[1], "quad", Kit.unlit(Color(1, 0.6, 0.2, 0.6), true, Kit.soft_circle()), Vector3(0, 0, 0.5), Vector3(w * 2.6, h * 1.1, 1))

static func _barrier(o: Obstacle, d: Dictionary) -> void:
	var w: float = d.box.x
	var pylon := Kit.toon(d.c2, 0.2, 0.8)
	var gem := Kit.toon(d.c1, 0.7, 0.9)
	for side in [-1, 1]:
		Kit.part(o, "cylinder", pylon, Vector3(side * (w * 0.5 + 0.15), 0, 0), Vector3(0.28, 2.0, 0.28))
		Kit.part(o, "octa", gem, Vector3(side * (w * 0.5 + 0.15), 1.1, 0), Vector3(0.32, 0.45, 0.32))
	var s := _states(o)
	Kit.part(s[0], "quad", Kit.unlit(Color(d.c1, 0.3), true, Kit.streak()), Vector3.ZERO, Vector3(w, 0.25, 1))
	Kit.part(s[1], "quad", Kit.unlit(d.c1, true, Kit.streak()), Vector3.ZERO, Vector3(w, d.box.y * 2.2, 1))
	Kit.part(s[1], "quad", Kit.unlit(Color.WHITE, true, Kit.streak()), Vector3(0, 0, 0.05), Vector3(w, d.box.y * 0.8, 1))

static func _beam(o: Obstacle, d: Dictionary) -> void:
	var h: float = d.box.y
	var w: float = d.box.x
	var c: Color = d.c1
	var s := _states(o)
	Kit.part(s[0], "quad", Kit.unlit(Color(c, 0.22)), Vector3.ZERO, Vector3(w, h, 1))
	Kit.part(s[0], "quad", Kit.unlit(Color(c, 0.75)), Vector3(-w * 0.5, 0, 0.01), Vector3(0.07, h, 1))
	Kit.part(s[0], "quad", Kit.unlit(Color(c, 0.75)), Vector3(w * 0.5, 0, 0.01), Vector3(0.07, h, 1))
	Kit.part(s[1], "quad", Kit.unlit(c, true, Kit.streak()), Vector3.ZERO, Vector3(h, w * 2.2, 1), Vector3(0, 0, 90))
	Kit.part(s[1], "quad", Kit.unlit(Color.WHITE, true, Kit.streak()), Vector3(0, 0, 0.05), Vector3(h, w * 0.9, 1), Vector3(0, 0, 90))

static func _wind(o: Obstacle, d: Dictionary) -> void:
	var flow := Kit.node("Flow", o)
	o.body = flow
	var p := FX.emitter(flow, Color(1, 1, 1, 0.75), 18, 0.8, 0.45, Vector3(4.5, 0, 0), 4.0, Vector3(d.box.x * 0.5, d.box.y * 0.5, 0.05))
	p.local_coords = true
	p.scale_amount_min = 0.3
	p.scale_amount_max = 0.6
	o.trail = p
	Kit.part(o, "quad", Kit.unlit(Color(0.85, 0.95, 1, 0.16), false, Kit.soft_circle()), Vector3(0, 0, -0.5), Vector3(d.box.x * 1.3, d.box.y * 1.2, 1))
	# Chevrons show which way it pushes.
	var chev := Kit.unlit(Color(1, 1, 1, 0.45), true)
	for i in 3:
		Kit.part(flow, "prism", chev, Vector3(-0.8 + i * 0.8, 0, -0.2), Vector3(0.35, 0.4, 0.02), Vector3(0, 0, -90))

static func _wall(o: Obstacle, d: Dictionary) -> void:
	var sized := Kit.node("Sized", o.body)
	var gate: bool = d.visual == "gate"
	var m := Kit.toon(d.c1, 0.15 if gate else 0.0, 0.8 if gate else 0.15)
	var m2 := Kit.toon(d.c2, 0.3 if gate else 0.0, 0.9 if gate else 0.15)
	Kit.part(sized, "box", m, Vector3.ZERO, Vector3.ONE)
	Kit.part(sized, "box", m2, Vector3(0, 0.45, 0.05), Vector3(1.02, 0.2, 1.1))
	if gate:
		for i in 4:
			Kit.part(sized, "cylinder", m2, Vector3(-0.36 + i * 0.24, 0, 0.5), Vector3(0.07, 1.0, 0.07))
	else:
		Kit.part(sized, "octa", m2, Vector3(-0.3, -0.3, 0.4), Vector3(0.35, 0.5, 0.4))
		Kit.part(sized, "octa", m, Vector3(0.25, 0.1, 0.45), Vector3(0.3, 0.45, 0.4))
	o.sized = sized
