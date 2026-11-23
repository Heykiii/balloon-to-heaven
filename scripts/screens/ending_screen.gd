extends Control
## Heaven's Gate: the campaign ending. The balloon rises through the golden gate, credits roll.

var _t := 0.0
var _gate: Node3D
var _text: VBoxContainer

func backdrop_world() -> int: return 10

func _ready() -> void:
	Sound.music(Sound.Cue.ENDING)
	Game.show_ending = false
	var bd: Node3D = Game.main.backdrop
	# The gate itself.
	_gate = Kit.node("HeavensGate", bd, Vector3(0, 4.5, -2))
	var gold := Kit.toon(Color("#F9CA24"), 0.4, 0.9)
	for side in [-1, 1]:
		Kit.part(_gate, "cylinder", gold, Vector3(side * 2.6, 0, 0), Vector3(0.6, 6.0, 0.6))
		Kit.part(_gate, "octa", Kit.toon(Color.WHITE, 0.8, 0.9), Vector3(side * 2.6, 3.4, 0), Vector3(0.6, 0.9, 0.6))
	Kit.part(_gate, "arc", Kit.unlit(Color("#FFE27A")), Vector3(0, 3.0, 0), Vector3(3.2, 2.2, 1))
	Kit.glow(_gate, Vector3(0, 1.0, -1), 12.0, Color(1, 0.95, 0.7, 0.7))
	var rays := Kit.node("Rays", _gate, Vector3(0, 1.0, -1.5))
	var rm := Kit.unlit(Color(1, 0.92, 0.6, 0.35), true, Kit.streak())
	for i in 12:
		var a := deg_to_rad(i * 30.0)
		Kit.part(rays, "quad", rm, Vector3(cos(a), sin(a), 0) * 4.0, Vector3(8, 0.9, 1), Vector3(0, 0, i * 30))
	var b := Game.shop.equipped()
	Game.main.show_backdrop_balloon(b, Game.shop.stage(b.id), Vector3(0, -12, 0), 1.6)
	_text = UI.vbox(10)
	_text.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_text.offset_left = 30
	_text.offset_right = -30
	_text.offset_top = 70
	add_child(_text)
	var title := UI.label("Heaven's Gate", 52, Color("#FFF3B0"))
	title.add_theme_color_override("font_outline_color", Color("#8A6A00"))
	title.add_theme_constant_override("outline_size", 14)
	_text.add_child(title)
	var msg := UI.wrap(UI.label("Through valleys and storms, past fire and the stars, your balloon has reached the gates of Heaven.\n\nThe guardians bow. The sky is yours.", 22))
	_text.add_child(msg)
	_text.modulate.a = 0.0
	var stats := UI.label("%d / %d stars   ·   %d balloons" % [Game.save.total_stars(), C.TOTAL_STARS, Game.save.data.owned.size()], 22, UI.GOLD)
	_text.add_child(stats)
	var gap := UI.spacer()
	gap.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_text.add_child(gap)
	_text.add_child(UI.label("Thank you for playing!", 26))
	_text.add_child(UI.button("Continue", UI.SUCCESS, func(): Game.goto("menu"), 28, Vector2(0, 76)))
	_text.add_child(UI.spacer(30))
	var tw := create_tween()
	tw.tween_interval(1.5)
	tw.tween_property(_text, "modulate:a", 1.0, 1.5)

func _process(dt: float) -> void:
	_t += dt
	var bal: BalloonModel = Game.main.backdrop_balloon
	if bal:
		bal.position.y = lerpf(-12.0, 2.5, U.ease_in_out_sine(clampf(_t / 7.0, 0.0, 1.0)))
	if _gate:
		_gate.get_node("Rays").rotation_degrees.z += dt * 10.0
	if fmod(_t, 0.9) < dt:
		FX.burst(Vector3(randf_range(-5, 5), randf_range(-6, 8), 2), Color(1, 0.9, 0.5), 14, 3.0, 0.25, 1.0)

func on_back() -> void:
	Game.goto("menu")
