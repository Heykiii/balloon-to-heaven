extends Control
## World map: one page per world with its 6 levels along a winding sky path.
## Tapping a level opens a popup with its goal, records and balloon choice.

var world := 1
var _path_root: Control

func backdrop_world() -> int:
	if Game.map_world <= 0: Game.map_world = C.world_of(Game.progression.current_level())
	world = clampi(Game.map_world, 1, C.WORLDS)
	return world

func _ready() -> void:
	Sound.music(Sound.Cue.WORLD, Game.db.world(world).music)
	add_child(UI.top_bar("World Map", func(): Game.goto("menu")))
	var w := Game.db.world(world)
	var header := UI.vbox(4)
	header.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	header.offset_top = 86
	add_child(header)
	var nav := UI.hbox(10)
	nav.alignment = BoxContainer.ALIGNMENT_CENTER
	var prev := UI.button("", UI.PANEL_LIGHT, func(): _change(-1), 24, Vector2(58, 58))
	UI.attach_icon(prev, Icon.make("back", 26))
	prev.disabled = world <= 1
	var next := UI.button("", UI.PANEL_LIGHT, func(): _change(1), 24, Vector2(58, 58))
	var ni := Icon.make("back", 26)
	UI.attach_icon(next, ni)
	ni.pivot_offset = Vector2(13, 13)
	ni.scale = Vector2(-1, 1)
	next.disabled = world >= C.WORLDS
	var title := UI.label("World %d\n%s" % [world, w.name], 30)
	title.custom_minimum_size = Vector2(300, 0)
	nav.add_child(prev)
	nav.add_child(title)
	nav.add_child(next)
	header.add_child(nav)
	var stars := UI.hbox(6)
	stars.alignment = BoxContainer.ALIGNMENT_CENTER
	stars.add_child(Icon.make("star", 26, UI.GOLD))
	stars.add_child(UI.label("%d / %d" % [Game.progression.world_stars(world), C.LEVELS_PER_WORLD * 3], 22))
	header.add_child(stars)
	var desc := UI.wrap(UI.label(w.description, 19, UI.TEXT))
	desc.custom_minimum_size = Vector2(440, 0)
	header.add_child(UI.margin(desc, 40, 0, 40, 0))
	_path_root = Control.new()
	UI.full(_path_root)
	_path_root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_path_root)
	if not Game.progression.is_world_unlocked(world):
		var lock := UI.panel(Color(0.05, 0.08, 0.2, 0.85))
		lock.set_anchors_and_offsets_preset(Control.PRESET_CENTER)
		lock.grow_horizontal = Control.GROW_DIRECTION_BOTH
		var lv := UI.vbox(8)
		lv.add_child(_centered_box(Icon.make("lock", 60, UI.TEXT_DIM)))
		lv.add_child(UI.label("Defeat the boss of World %d\nto unlock this world" % (world - 1), 22))
		lock.add_child(lv)
		add_child(lock)
	resized.connect(_layout)
	_layout.call_deferred()

func _centered(c: Control) -> Control:
	c.set_anchors_and_offsets_preset(Control.PRESET_CENTER)
	return c

func _centered_box(c: Control) -> CenterContainer:
	var cc := CenterContainer.new()
	cc.add_child(c)
	return cc

func _change(d: int) -> void:
	Game.map_world = clampi(world + d, 1, C.WORLDS)
	Game.goto("map")

func _layout() -> void:
	for c in _path_root.get_children(): c.queue_free()
	var area := Rect2(Vector2(40, 340), Vector2(size.x - 80, size.y - 440))
	var pts: Array[Vector2] = []
	for i in C.LEVELS_PER_WORLD:
		var t := float(i) / (C.LEVELS_PER_WORLD - 1)
		var x := area.position.x + area.size.x * (0.5 + 0.32 * sin(t * PI * 2.2 + 0.4))
		var y := area.end.y - t * area.size.y
		pts.append(Vector2(x, y))
	var line := Line2D.new()
	line.width = 10
	line.default_color = Color(1, 1, 1, 0.45)
	line.joint_mode = Line2D.LINE_JOINT_ROUND
	line.begin_cap_mode = Line2D.LINE_CAP_ROUND
	line.end_cap_mode = Line2D.LINE_CAP_ROUND
	var curve := Curve2D.new()
	for i in pts.size():
		var tangent := Vector2(0, -60)
		curve.add_point(pts[i], -tangent if i > 0 else Vector2.ZERO, tangent if i < pts.size() - 1 else Vector2.ZERO)
	for p in curve.tessellate(5, 3): line.add_point(p)
	_path_root.add_child(line)
	for i in C.LEVELS_PER_WORLD:
		_path_root.add_child(_node(C.first_level_of(world) + i, pts[i]))

func _node(n: int, at: Vector2) -> Control:
	var lv := Game.db.level(n)
	var unlocked := Game.progression.is_level_unlocked(n)
	var current := n == Game.progression.current_level() and not Game.progression.is_level_completed(n)
	var boss: bool = lv.is_boss
	var size_px := 104.0 if boss else 86.0
	var color := UI.DANGER if boss else (UI.SUCCESS if Game.progression.is_level_completed(n) else UI.PRIMARY)
	if not unlocked: color = Color(0.35, 0.4, 0.5)
	var holder := Control.new()
	holder.position = at - Vector2(size_px, size_px) * 0.5
	holder.size = Vector2(size_px, size_px + 34)
	var b := Button.new()
	b.size = Vector2(size_px, size_px)
	b.add_theme_stylebox_override("normal", UI.box(color, int(size_px * 0.5), Color.WHITE, 5, 8))
	b.add_theme_stylebox_override("hover", UI.box(color.lightened(0.1), int(size_px * 0.5), Color.WHITE, 5, 8))
	b.add_theme_stylebox_override("pressed", UI.box(color.darkened(0.1), int(size_px * 0.5), Color.WHITE, 5, 8))
	b.add_theme_stylebox_override("focus", StyleBoxEmpty.new())
	b.add_theme_font_size_override("font_size", 34)
	b.text = "" if not unlocked else ("BOSS" if boss else str(C.index_in_world(n)))
	if boss and unlocked: b.add_theme_font_size_override("font_size", 24)
	if not unlocked:
		var lock := Icon.make("lock", 38, Color(1, 1, 1, 0.8))
		lock.position = Vector2(size_px, size_px) * 0.5 - Vector2(19, 19)
		b.add_child(lock)
	b.pressed.connect(func():
		Sound.play("click" if unlocked else "deny")
		if unlocked: _popup(n)
		else: Game.toast("Finish the previous level first"))
	holder.add_child(b)
	if unlocked:
		var stars := UI.hbox(2)
		stars.position = Vector2(size_px * 0.5 - 45, size_px - 8)
		for s in 3:
			stars.add_child(Icon.make("star", 28, UI.GOLD, s < Game.progression.stars(n)))
		holder.add_child(stars)
	if current:
		b.pivot_offset = b.size * 0.5
		var tw := b.create_tween().set_loops()
		tw.tween_property(b, "scale", Vector2(1.1, 1.1), 0.5).set_trans(Tween.TRANS_SINE)
		tw.tween_property(b, "scale", Vector2.ONE, 0.5).set_trans(Tween.TRANS_SINE)
	return holder

func _popup(n: int) -> void:
	var lv := Game.db.level(n)
	var m := UI.modal(self, 480)
	var root: Control = m[0]
	var v: VBoxContainer = m[1]
	v.add_child(UI.label("World %d - Level %d" % [lv.world, lv.index], 20, UI.TEXT_DIM))
	v.add_child(UI.label(lv.name, 34))
	var hint := UI.wrap(UI.label(lv.hint, 20, UI.TEXT))
	v.add_child(hint)
	if lv.is_boss:
		var bd := Game.db.boss(lv.boss)
		v.add_child(UI.label("Boss: %s, %s" % [bd.name, bd.title], 20, Color("#FF9F9F")))
	var stars := UI.hbox(6)
	stars.alignment = BoxContainer.ALIGNMENT_CENTER
	for s in 3: stars.add_child(Icon.make("star", 40, UI.GOLD, s < Game.progression.stars(n)))
	v.add_child(stars)
	var goals := UI.vbox(2)
	goals.add_child(UI.label("1. Reach the top", 19, UI.TEXT_DIM))
	goals.add_child(UI.label("2. Collect %d coins" % int(lv.coins_for_second_star), 19, UI.TEXT_DIM))
	goals.add_child(UI.label("3. " + Progression.challenge_text(lv), 19, UI.TEXT_DIM))
	v.add_child(goals)
	var best := int(Game.save.data.best_coins[n - 1])
	if best > 0: v.add_child(UI.label("Best: %d coins" % best, 19, UI.GOLD))
	v.add_child(UI.label("Choose your balloon", 22))
	var chosen := [str(Game.save.data.equipped)]
	var scroll := ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(0, 130)
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	var row := UI.hbox(8)
	scroll.add_child(row)
	var buttons := {}
	var stats_label := UI.label("", 18, UI.TEXT_DIM)
	var refresh := func():
		for id in buttons:
			var sel: bool = id == chosen[0]
			buttons[id].add_theme_stylebox_override("normal", UI.box(UI.PANEL_LIGHT if not sel else Color("#3B6FD1"), 16, UI.GOLD if sel else Color(1, 1, 1, 0.15), 4 if sel else 2))
		var b := Game.db.balloon(chosen[0])
		var st := Game.shop.stats(b)
		stats_label.text = "%s  ·  HP %d  ·  Armor %d  ·  %s" % [b.name, st.hp, st.armor, b.ability_name]
	for b in Game.db.balloons:
		if not Game.shop.is_owned(b.id): continue
		var btn := Button.new()
		btn.custom_minimum_size = Vector2(96, 118)
		btn.add_theme_stylebox_override("hover", UI.box(Color("#3B6FD1"), 16))
		btn.add_theme_stylebox_override("pressed", UI.box(Color("#3B6FD1"), 16))
		btn.add_theme_stylebox_override("focus", StyleBoxEmpty.new())
		var ic := Icon.make("balloon", 60, b.c1)
		ic.position = Vector2(18, 8)
		btn.add_child(ic)
		var nm := UI.label(b.name.replace(" Balloon", ""), 15)
		nm.position = Vector2(0, 74)
		nm.size = Vector2(96, 36)
		nm.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		btn.add_child(nm)
		var id: String = b.id
		btn.pressed.connect(func():
			chosen[0] = id
			Sound.play("click")
			refresh.call())
		buttons[id] = btn
		row.add_child(btn)
	v.add_child(scroll)
	v.add_child(stats_label)
	refresh.call()
	var h := UI.hbox(10)
	h.add_child(UI.expand(UI.button("Back", UI.PANEL_LIGHT, func(): root.queue_free(), 24)))
	var go := UI.button("Start", UI.SUCCESS, func():
		Game.shop.try_equip(chosen[0])
		Game.save.save_if_dirty()
		Game.start_level(n, chosen[0]), 28)
	go.size_flags_stretch_ratio = 1.6
	h.add_child(UI.expand(go))
	v.add_child(h)

func on_back() -> void:
	Game.goto("menu")
