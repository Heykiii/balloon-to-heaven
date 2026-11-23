extends Control
## Main menu: title, the equipped balloon floating in the sky of the furthest world, and navigation.

func backdrop_world() -> int: return Game.progression.highest_world()

func _ready() -> void:
	Sound.music(Sound.Cue.MENU)
	var bar := UI.top_bar("", Callable())
	add_child(bar)
	var v := UI.vbox(14)
	v.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	v.offset_top = 90
	v.offset_left = 40
	v.offset_right = -40
	v.offset_bottom = -30
	add_child(v)
	var title := UI.label("Balloon\nto Heaven", 58, Color.WHITE)
	title.add_theme_constant_override("outline_size", 14)
	title.add_theme_color_override("font_outline_color", Color("#2A3E8F"))
	title.add_theme_constant_override("line_spacing", -12)
	v.add_child(title)
	var w := Game.db.world(Game.progression.highest_world())
	var sub := UI.label("World %d: %s   ·   %d / %d stars" % [w.number, w.name, Game.save.total_stars(), C.TOTAL_STARS], 22, UI.TEXT)
	v.add_child(sub)
	var gap := UI.spacer()
	gap.size_flags_vertical = Control.SIZE_EXPAND_FILL
	v.add_child(gap)
	var play := UI.button("PLAY", UI.SUCCESS, func(): Game.goto("map"), 40, Vector2(0, 92))
	v.add_child(play)
	_pulse(play)
	var row := UI.hbox(12)
	row.add_child(UI.expand(_icon_button("Balloons", "balloon", UI.SECONDARY, func(): Game.goto("shop"))))
	row.add_child(UI.expand(_icon_button("Workshop", "wrench", UI.WARNING, func(): Game.goto("workshop"))))
	v.add_child(row)
	var row2 := UI.hbox(12)
	row2.add_child(UI.expand(_icon_button("Awards", "trophy", UI.PRIMARY, func(): Dialogs.achievements(self))))
	row2.add_child(UI.expand(_icon_button("Settings", "gear", UI.PANEL_LIGHT, func(): Dialogs.settings(self))))
	v.add_child(row2)
	if Game.save.data.campaign_completed:
		v.add_child(UI.button("Heaven's Gate", Color("#D4A017"), func(): Game.goto("ending"), 24, Vector2(0, 56)))
	var b := Game.shop.equipped()
	Game.main.show_backdrop_balloon(b, Game.shop.stage(b.id), Vector3(0, 0.6, 0), 1.9)

func _icon_button(text: String, icon: String, color: Color, cb: Callable) -> Button:
	var b := UI.button("      " + text, color, cb, 24, Vector2(0, 66))
	UI.attach_icon(b, Icon.make(icon, 30, Color.WHITE), 18)
	return b

func _pulse(c: Control) -> void:
	await get_tree().process_frame
	c.pivot_offset = c.size * 0.5
	var tw := c.create_tween().set_loops()
	tw.tween_property(c, "scale", Vector2(1.04, 1.04), 0.6).set_trans(Tween.TRANS_SINE)
	tw.tween_property(c, "scale", Vector2.ONE, 0.6).set_trans(Tween.TRANS_SINE)

func on_back() -> void:
	pass
