extends Control
## Balloon Shop: 20 balloons with a live 3D preview, stats, ability, five upgrade stages,
## buying, equipping and upgrading.

var selected := ""
var preview_stage := 0 # 0 = current stage
var _preview: ModelPreview
var _details: VBoxContainer
var _grid: GridContainer

func backdrop_world() -> int: return Game.progression.highest_world()

func _ready() -> void:
	selected = str(Game.save.data.equipped)
	add_child(UI.top_bar("Balloons", func(): Game.goto("menu")))
	var v := UI.vbox(8)
	v.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	v.offset_top = 80
	v.offset_left = 14
	v.offset_right = -14
	v.offset_bottom = -10
	add_child(v)
	var card := UI.panel(Color(0.08, 0.14, 0.3, 0.86))
	v.add_child(card)
	var cv := UI.vbox(6)
	card.add_child(cv)
	var top := UI.hbox(8)
	cv.add_child(top)
	_preview = ModelPreview.new()
	_preview.custom_minimum_size = Vector2(190, 250)
	top.add_child(_preview)
	_details = UI.vbox(4)
	UI.expand(_details)
	top.add_child(_details)
	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	v.add_child(scroll)
	_grid = GridContainer.new()
	_grid.columns = 4
	_grid.add_theme_constant_override("h_separation", 8)
	_grid.add_theme_constant_override("v_separation", 8)
	UI.expand(_grid)
	scroll.add_child(_grid)
	_refresh()

func _refresh() -> void:
	for c in _grid.get_children(): c.queue_free()
	for b in Game.db.balloons: _grid.add_child(_tile(b))
	_show_details()

func _tile(b: Dictionary) -> Button:
	var owned := Game.shop.is_owned(b.id)
	var unlocked := Game.shop.is_unlocked_by_progress(b)
	var sel: bool = b.id == selected
	var btn := Button.new()
	btn.custom_minimum_size = Vector2(118, 128)
	UI.expand(btn)
	var rc := UI.rarity_color(int(b.rarity))
	var bg := Color("#2B4A80") if owned else Color(0.12, 0.17, 0.3)
	btn.add_theme_stylebox_override("normal", UI.box(bg, 16, UI.GOLD if sel else rc.darkened(0.2), 4 if sel else 2))
	btn.add_theme_stylebox_override("hover", UI.box(bg.lightened(0.08), 16, UI.GOLD if sel else rc, 4 if sel else 2))
	btn.add_theme_stylebox_override("pressed", UI.box(bg.darkened(0.1), 16, UI.GOLD, 4))
	btn.add_theme_stylebox_override("focus", StyleBoxEmpty.new())
	var ic := Icon.make("balloon", 62, b.c1 if unlocked else Color(0.4, 0.45, 0.55))
	ic.position = Vector2(28, 6)
	btn.add_child(ic)
	var nm := UI.label(str(b.name).replace(" Balloon", ""), 15)
	nm.position = Vector2(2, 70)
	nm.size = Vector2(114, 22)
	nm.clip_text = true
	btn.add_child(nm)
	var info := UI.hbox(3)
	info.position = Vector2(8, 96)
	if Game.shop.is_equipped(b.id):
		info.add_child(Icon.make("check", 18, UI.SUCCESS))
		info.add_child(UI.label("Equipped", 14, UI.SUCCESS))
	elif owned:
		for s in Game.shop.stage(b.id): info.add_child(Icon.make("star", 14, UI.GOLD))
	elif not unlocked:
		info.add_child(Icon.make("lock", 16, UI.TEXT_DIM))
		info.add_child(UI.label("World %d" % int(b.unlock_world), 14, UI.TEXT_DIM))
	else:
		info.add_child(Icon.make("coin", 16, UI.GOLD))
		info.add_child(UI.label(U.fmt(int(b.price)), 14, UI.GOLD))
	btn.add_child(info)
	var id: String = b.id
	btn.pressed.connect(func():
		Sound.play("click")
		selected = id
		preview_stage = 0
		_refresh())
	return btn

func _show_details() -> void:
	for c in _details.get_children(): c.queue_free()
	var b := Game.db.balloon(selected)
	var owned := Game.shop.is_owned(b.id)
	var unlocked := Game.shop.is_unlocked_by_progress(b)
	var stage := Game.shop.stage(b.id)
	var shown_stage := preview_stage if preview_stage > 0 else stage
	var trail := Game.db.trail(Game.workshop.equipped_trail())
	_preview.show_balloon(b, shown_stage, trail)
	_details.add_child(UI.label(b.name, 26, UI.TEXT, HORIZONTAL_ALIGNMENT_LEFT))
	_details.add_child(UI.label(C.RARITY_NAMES[int(b.rarity)], 18, UI.rarity_color(int(b.rarity)), HORIZONTAL_ALIGNMENT_LEFT))
	var st := Game.shop.stats(b, shown_stage)
	_details.add_child(UI.stat_row("Health", float(st.hp), 10.0, str(st.hp), Color("#FF5A6E")))
	_details.add_child(UI.stat_row("Speed", float(st.speed), 1.6, "%.2f" % st.speed, Color("#7CF29C")))
	_details.add_child(UI.stat_row("Armor", float(st.armor), 8.0, str(st.armor), Color("#B2BEC3")))
	_details.add_child(UI.stat_row("Magnet", float(st.magnet), 4.0, "%.1f" % st.magnet, UI.GOLD))
	var ab := UI.wrap(UI.label("%s: %s" % [b.ability_name, b.ability_desc], 16, Color("#CFE0FF"), HORIZONTAL_ALIGNMENT_LEFT))
	_details.add_child(ab)
	# stage selector (preview any stage)
	var stages := UI.hbox(4)
	stages.add_child(UI.label("Stage", 16, UI.TEXT_DIM))
	for s in range(1, C.MAX_STAGE + 1):
		var sb := Button.new()
		sb.custom_minimum_size = Vector2(36, 36)
		sb.flat = true
		var ic := Icon.make("star", 30, UI.GOLD, s <= stage and owned)
		ic.position = Vector2(3, 3)
		sb.add_child(ic)
		if s == shown_stage:
			sb.add_theme_stylebox_override("normal", UI.box(Color(1, 1, 1, 0.18), 10))
		sb.pressed.connect(func():
			preview_stage = s
			_show_details())
		stages.add_child(sb)
	_details.add_child(stages)
	var actions := UI.hbox(6)
	_details.add_child(actions)
	if not owned:
		if not unlocked:
			actions.add_child(UI.expand(UI.label("Unlocks in World %d" % int(b.unlock_world), 18, UI.TEXT_DIM)))
		else:
			var buy := UI.button("Buy  %s" % U.fmt(int(b.price)), UI.SUCCESS, func(): _buy(b.id), 22, Vector2(0, 56))
			UI.expand(buy)
			actions.add_child(buy)
	else:
		if not Game.shop.is_equipped(b.id):
			actions.add_child(UI.expand(UI.button("Equip", UI.PRIMARY, func():
				Game.shop.try_equip(b.id)
				Sound.play("purchase", 0.6)
				_refresh(), 22, Vector2(0, 56))))
		if stage < C.MAX_STAGE:
			var cost := Game.shop.upgrade_cost(b.id)
			actions.add_child(UI.expand(UI.button("Upgrade %s" % U.fmt(cost), UI.WARNING, func(): _upgrade(b.id), 20, Vector2(0, 56))))
		else:
			actions.add_child(UI.expand(UI.label("Max stage!", 20, UI.GOLD)))
	_details.add_child(UI.wrap(UI.label(b.description, 15, UI.TEXT_DIM, HORIZONTAL_ALIGNMENT_LEFT)))

func _buy(id: String) -> void:
	var r := Game.shop.try_purchase(id)
	if r == Shop.Result.OK:
		Sound.play("purchase")
		Game.shop.try_equip(id)
		Game.toast(Game.db.balloon(id).name + " is yours!")
		Game.achievements.evaluate()
	else:
		Sound.play("deny")
		Game.toast(Shop.describe(r))
	_refresh()

func _upgrade(id: String) -> void:
	var r := Game.shop.try_upgrade(id)
	if r == Shop.Result.OK:
		Sound.play("upgrade")
		Game.toast("Upgraded to stage %d!" % Game.shop.stage(id))
		FX.burst(Vector3(0, 0, 5), UI.GOLD, 30, 5.0, 0.3)
		Game.achievements.evaluate()
	else:
		Sound.play("deny")
		Game.toast(Shop.describe(r))
	preview_stage = 0
	_refresh()

func on_back() -> void:
	Game.goto("menu")
