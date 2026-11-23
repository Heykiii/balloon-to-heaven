class_name Hud
extends Control
## In-level HUD: hearts, shield, coins, progress, power-up timers, boss bar, ability button, banners.

signal pause_requested
signal ability_pressed

var _hearts: HBoxContainer
var _coins: Label
var _progress: ProgressBar
var _shield: Control
var _shield_label: Label
var _powerups: VBoxContainer
var _pu_rows := {}
var _boss_box: VBoxContainer
var _boss_bar: ProgressBar
var _boss_name: Label
var _boss_phase: Label
var _ability: Button
var _ability_fill: ColorRect
var _banner: VBoxContainer
var _banner_title: Label
var _banner_sub: Label
var _max_hp := 3

func _init() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE

func setup(level: Dictionary, world_name: String, ability_label: String) -> void:
	UI.full(self)
	theme = UI.theme()
	var top := PanelContainer.new()
	top.add_theme_stylebox_override("panel", UI.box(Color(0.03, 0.06, 0.16, 0.55), 0))
	top.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	top.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(top)
	var v := UI.vbox(6)
	top.add_child(v)
	var row := UI.hbox(10)
	v.add_child(row)
	var pause := UI.button("", UI.PANEL_LIGHT, func(): pause_requested.emit(), 20, Vector2(54, 50))
	UI.attach_icon(pause, Icon.make("pause", 24))
	row.add_child(pause)
	_hearts = UI.hbox(4)
	row.add_child(_hearts)
	_shield = UI.hbox(2)
	_shield.add_child(Icon.make("shield", 30, Color("#6FE3FF")))
	_shield_label = UI.label("", 18)
	_shield.add_child(_shield_label)
	_shield.visible = false
	row.add_child(_shield)
	row.add_child(UI.expand(UI.spacer()))
	row.add_child(Icon.make("coin", 32, UI.GOLD))
	_coins = UI.label("0", 28)
	_coins.custom_minimum_size = Vector2(60, 0)
	_coins.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	row.add_child(_coins)
	var prow := UI.hbox(8)
	v.add_child(prow)
	var lname := UI.label("%d-%d" % [level.world, level.index], 18, UI.TEXT_DIM)
	prow.add_child(lname)
	_progress = ProgressBar.new()
	_progress.show_percentage = false
	_progress.max_value = 1.0
	_progress.custom_minimum_size = Vector2(0, 14)
	_progress.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	_progress.add_theme_stylebox_override("background", UI.box(Color(0, 0, 0, 0.4), 7))
	_progress.add_theme_stylebox_override("fill", UI.box(Color("#7CF29C"), 7))
	UI.expand(_progress)
	prow.add_child(_progress)
	prow.add_child(Icon.make("star", 22, UI.GOLD))
	_powerups = UI.vbox(6)
	_powerups.position = Vector2(12, 130)
	_powerups.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_powerups)
	_boss_box = UI.vbox(2)
	_boss_box.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	_boss_box.offset_top = 112
	_boss_box.offset_left = 60
	_boss_box.offset_right = -60
	_boss_box.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_boss_name = UI.label("", 22, Color("#FFB3B3"))
	_boss_box.add_child(_boss_name)
	_boss_bar = ProgressBar.new()
	_boss_bar.show_percentage = false
	_boss_bar.max_value = 1.0
	_boss_bar.custom_minimum_size = Vector2(0, 18)
	_boss_bar.add_theme_stylebox_override("background", UI.box(Color(0, 0, 0, 0.5), 9, Color(1, 1, 1, 0.5), 2))
	_boss_bar.add_theme_stylebox_override("fill", UI.box(Color("#FF4D6D"), 9))
	_boss_box.add_child(_boss_bar)
	_boss_phase = UI.label("", 17, UI.TEXT_DIM)
	_boss_box.add_child(_boss_phase)
	_boss_box.visible = false
	add_child(_boss_box)
	if ability_label != "":
		_ability = UI.button(ability_label, UI.SECONDARY, func(): ability_pressed.emit(), 22, Vector2(118, 118))
		_ability.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_RIGHT)
		_ability.offset_left = -140
		_ability.offset_top = -150
		_ability.offset_right = -22
		_ability.offset_bottom = -32
		_ability_fill = ColorRect.new()
		_ability_fill.color = Color(0, 0, 0, 0.45)
		_ability_fill.mouse_filter = Control.MOUSE_FILTER_IGNORE
		_ability.add_child(_ability_fill)
		_ability.visible = false
		add_child(_ability)
	_banner = UI.vbox(4)
	_banner.set_anchors_and_offsets_preset(Control.PRESET_CENTER)
	_banner.grow_horizontal = Control.GROW_DIRECTION_BOTH
	_banner.grow_vertical = Control.GROW_DIRECTION_BOTH
	_banner.offset_top = -200
	_banner.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_banner_title = UI.label("", 44)
	_banner_title.add_theme_constant_override("outline_size", 12)
	_banner_sub = UI.wrap(UI.label("", 22))
	_banner_sub.custom_minimum_size = Vector2(460, 0)
	_banner.add_child(_banner_title)
	_banner.add_child(_banner_sub)
	_banner.modulate.a = 0.0
	add_child(_banner)
	Events.health_changed.connect(_on_health)
	Events.level_coins_changed.connect(func(c): _coins.text = U.fmt(c); _bump(_coins))
	Events.level_progress_changed.connect(func(p): _progress.value = p)
	Events.shield_changed.connect(_on_shield)
	Events.powerup_updated.connect(_on_powerup)
	Events.boss_health_changed.connect(_on_boss)
	Events.boss_phase_started.connect(func(n): _boss_phase.text = n)
	Events.ability_cooldown_changed.connect(_on_ability)

func _exit_tree() -> void:
	for sig in [Events.health_changed, Events.level_coins_changed, Events.level_progress_changed, Events.shield_changed,
			Events.powerup_updated, Events.boss_health_changed, Events.boss_phase_started, Events.ability_cooldown_changed]:
		for c in sig.get_connections():
			if c.callable.get_object() == self:
				sig.disconnect(c.callable)

func _bump(c: Control) -> void:
	c.pivot_offset = c.size * 0.5
	c.scale = Vector2(1.2, 1.2)
	c.create_tween().tween_property(c, "scale", Vector2.ONE, 0.15)

func _on_health(cur: int, mx: int) -> void:
	if _hearts.get_child_count() != mx:
		for c in _hearts.get_children(): c.free()
		for i in mx: _hearts.add_child(Icon.make("heart", 30, Color("#FF5A6E")))
	for i in _hearts.get_child_count():
		var ic: Icon = _hearts.get_child(i)
		var was := ic.filled
		ic.set_kind("heart", Color("#FF5A6E"), i < cur)
		if was and i >= cur: _bump(ic)
	_max_hp = mx

func _on_shield(v: float) -> void:
	_shield.visible = v != 0.0
	_shield_label.text = "" if v < 0.0 else "%d" % ceili(v)

func _on_powerup(t: int, remaining: float, duration: float) -> void:
	if not _pu_rows.has(t):
		var h := UI.hbox(6)
		var icons := ["magnet", "shield", "rocket", "heart", "double"]
		var d := Game.db.powerup(t)
		h.add_child(Icon.make(icons[t], 30, d.color if not d.is_empty() else Color.WHITE))
		var bar := ProgressBar.new()
		bar.show_percentage = false
		bar.custom_minimum_size = Vector2(90, 12)
		bar.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		bar.max_value = 1.0
		bar.add_theme_stylebox_override("background", UI.box(Color(0, 0, 0, 0.4), 6))
		bar.add_theme_stylebox_override("fill", UI.box(d.color if not d.is_empty() else Color.WHITE, 6))
		h.add_child(bar)
		_powerups.add_child(h)
		_pu_rows[t] = h
	var row: HBoxContainer = _pu_rows[t]
	row.visible = remaining > 0.0
	(row.get_child(1) as ProgressBar).value = remaining / maxf(0.01, duration)

func _on_boss(v: float) -> void:
	_boss_box.visible = v >= 0.0
	_boss_bar.value = maxf(0.0, v)

func set_boss_name(n: String) -> void:
	_boss_name.text = n

func _on_ability(r: float) -> void:
	if not _ability: return
	_ability.disabled = r < 1.0
	_ability_fill.position = Vector2.ZERO
	_ability_fill.size = Vector2(_ability.size.x, _ability.size.y * (1.0 - r))

func set_ability_visible(on: bool) -> void:
	if _ability: _ability.visible = on

func banner(title: String, sub: String, duration := 2.2) -> void:
	_banner_title.text = title
	_banner_sub.text = sub
	_banner.pivot_offset = _banner.size * 0.5
	_banner.scale = Vector2(0.7, 0.7)
	var tw := create_tween()
	tw.set_parallel()
	tw.tween_property(_banner, "modulate:a", 1.0, 0.25)
	tw.tween_property(_banner, "scale", Vector2.ONE, 0.35).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tw.set_parallel(false)
	tw.tween_interval(duration)
	tw.tween_property(_banner, "modulate:a", 0.0, 0.4)
