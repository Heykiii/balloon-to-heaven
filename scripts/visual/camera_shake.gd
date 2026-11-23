class_name CameraShake
extends Node
## Trauma-based screen shake for the parent Camera3D (respects the settings toggle).

static var enabled := true
var trauma := 0.0
var base := Vector3.ZERO
var _t := 0.0
var _noise := FastNoiseLite.new()

func _ready() -> void:
	base = (get_parent() as Node3D).position
	_noise.frequency = 1.0

func add(amount: float) -> void:
	if not enabled: return
	trauma = clampf(maxf(trauma, amount), 0.0, 1.0)

func _process(dt: float) -> void:
	var cam := get_parent() as Node3D
	_t += dt * 30.0
	if trauma <= 0.0:
		cam.position = base
		return
	trauma = maxf(0.0, trauma - dt * 2.2)
	var s := trauma * trauma * 0.6
	cam.position = base + Vector3(_noise.get_noise_2d(_t, 0.0) * s, _noise.get_noise_2d(0.0, _t) * s, 0)
