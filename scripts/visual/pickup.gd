class_name Pickup
extends Node3D
## Coins, star coins, sky crystals and power-up bubbles. Spins and bobs; logic lives in the systems.

enum Kind { COIN, SPECIAL, CRYSTAL, POWERUP }
const GOLD := Color(1, 0.8, 0.2)

var kind := Kind.COIN
var pu_type := -1
var pos := Vector3.ZERO
var magnetized := false
var pull_speed := 0.0
var spin: Node3D
var spin_speed := 180.0
var _phase := 0.0
var _t := 0.0

static func coin(special: bool) -> Pickup:
	var p := Pickup.new()
	p.kind = Kind.SPECIAL if special else Kind.COIN
	p.spin = Kit.node("Spin", p)
	var size := 0.9 if special else 0.5
	var gold := Kit.toon(GOLD, 0.35 if special else 0.15, 0.9)
	Kit.part(p.spin, "cylinder", gold, Vector3.ZERO, Vector3(size, 0.08, size), Vector3(90, 0, 0))
	Kit.part(p.spin, "torus", Kit.toon(Color(1, 0.65, 0.1), 0.1, 0.9), Vector3.ZERO, Vector3(size, size * 0.6, size), Vector3(90, 0, 0))
	if special:
		Kit.part(p.spin, "prism", Kit.toon(Color.WHITE, 0.6, 0.9), Vector3(0, 0.03, 0.06), Vector3(0.4, 0.38, 0.05))
		Kit.part(p.spin, "prism", Kit.toon(Color.WHITE, 0.6, 0.9), Vector3(0, -0.05, 0.065), Vector3(0.4, 0.38, 0.05), Vector3(0, 0, 180))
		Kit.glow(p, Vector3(0, 0, -0.3), 2.0, Color(1, 0.9, 0.4, 0.55))
	p.spin_speed = 120.0 if special else 220.0
	return p

static func crystal() -> Pickup:
	var p := Pickup.new()
	p.kind = Kind.CRYSTAL
	p.spin = Kit.node("Spin", p)
	Kit.part(p.spin, "octa", Kit.toon(Color(0.55, 0.95, 1), 0.6, 0.95), Vector3.ZERO, Vector3(0.7, 1.1, 0.7))
	Kit.glow(p, Vector3(0, 0, -0.3), 2.6, Color(0.5, 0.95, 1, 0.65))
	p.spin_speed = 140.0
	return p

static func powerup(type: int, color: Color) -> Pickup:
	var p := Pickup.new()
	p.kind = Kind.POWERUP
	p.pu_type = type
	p.spin = Kit.node("Spin", p)
	var bubble := Kit.unlit(Color(color, 0.22), false)
	Kit.part(p, "sphere", bubble, Vector3.ZERO, Vector3.ONE * 1.25)
	Kit.part(p, "thin_torus", Kit.unlit(Color(color.lightened(0.4), 0.8), true), Vector3(0, 0, 0.1), Vector3.ONE * 1.28, Vector3(90, 0, 0))
	Kit.glow(p, Vector3(0, 0, -0.4), 2.2, Color(color, 0.45))
	var m := Kit.toon(color, 0.4, 0.8)
	var white := Kit.toon(Color.WHITE, 0.3, 0.8)
	match type:
		C.PU.MAGNET:
			Kit.part(p.spin, "torus", m, Vector3(0, 0.0, 0), Vector3(0.6, 0.9, 0.6), Vector3(90, 0, 0))
			Kit.part(p.spin, "box", white, Vector3(-0.27, 0.12, 0), Vector3(0.14, 0.2, 0.14))
			Kit.part(p.spin, "box", white, Vector3(0.27, 0.12, 0), Vector3(0.14, 0.2, 0.14))
			Kit.part(p.spin, "box", Kit.toon(Color(0.95, 0.95, 1), 0.0), Vector3(0, 0.22, 0), Vector3(0.42, 0.22, 0.3))
		C.PU.SHIELD:
			Kit.part(p.spin, "hemi", m, Vector3.ZERO, Vector3(0.6, 0.9, 0.6), Vector3(90, 0, 0))
			Kit.part(p.spin, "low_sphere", white, Vector3(0, 0, 0.18), Vector3.ONE * 0.2)
		C.PU.ROCKET:
			Kit.part(p.spin, "cylinder", white, Vector3.ZERO, Vector3(0.22, 0.5, 0.22))
			Kit.part(p.spin, "cone", m, Vector3(0, 0.36, 0), Vector3(0.22, 0.22, 0.22))
			Kit.part(p.spin, "box", m, Vector3(-0.15, -0.22, 0), Vector3(0.12, 0.18, 0.04))
			Kit.part(p.spin, "box", m, Vector3(0.15, -0.22, 0), Vector3(0.12, 0.18, 0.04))
		C.PU.LIFE:
			Kit.part(p.spin, "low_sphere", m, Vector3(-0.13, 0.08, 0), Vector3.ONE * 0.32)
			Kit.part(p.spin, "low_sphere", m, Vector3(0.13, 0.08, 0), Vector3.ONE * 0.32)
			Kit.part(p.spin, "cone", m, Vector3(0, -0.15, 0), Vector3(0.42, 0.35, 0.25), Vector3(180, 0, 0))
		C.PU.DOUBLE:
			var gold := Kit.toon(GOLD, 0.3, 0.9)
			Kit.part(p.spin, "cylinder", gold, Vector3(-0.12, 0.06, 0.05), Vector3(0.42, 0.06, 0.42), Vector3(90, 0, 0))
			Kit.part(p.spin, "cylinder", gold, Vector3(0.12, -0.06, -0.05), Vector3(0.42, 0.06, 0.42), Vector3(90, 0, 0))
	p.spin_speed = 90.0
	return p

func reset(p: Vector3) -> void:
	pos = p
	position = p
	magnetized = false
	pull_speed = 0.0
	visible = true
	_phase = randf() * 6.0

func _process(dt: float) -> void:
	_t += dt
	if spin:
		spin.rotation_degrees.y += spin_speed * dt
		spin.position.y = sin(_t * 3.0 + _phase) * 0.08
