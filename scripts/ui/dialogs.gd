class_name Dialogs
## Shared modal panels: settings, achievements, confirmation.

static func settings(parent: Node, on_close := Callable()) -> void:
	var m := UI.modal(parent)
	var root: Control = m[0]
	var v: VBoxContainer = m[1]
	v.add_child(UI.label("Settings", 36))
	var s: Dictionary = Game.save.data.settings
	v.add_child(_slider("Music", float(s.music), func(x):
		s.music = x
		Game.apply_settings()
		Game.save.mark_dirty()))
	v.add_child(_slider("Sound effects", float(s.sfx), func(x):
		s.sfx = x
		Game.apply_settings()
		Game.save.mark_dirty()
		Sound.play("coin", 0.8)))
	var shake := CheckButton.new()
	shake.text = "Screen shake"
	shake.button_pressed = bool(s.shake)
	shake.add_theme_font_size_override("font_size", 24)
	shake.toggled.connect(func(on):
		s.shake = on
		Game.apply_settings()
		Game.save.mark_dirty())
	v.add_child(shake)
	v.add_child(UI.label("Graphics quality", 22, UI.TEXT_DIM))
	var q := UI.hbox(8)
	var names := ["Low", "Medium", "High"]
	var buttons: Array[Button] = []
	for i in 3:
		var b := UI.button(names[i], UI.PRIMARY if int(s.quality) == i else UI.PANEL_LIGHT, func():
			s.quality = i
			Game.apply_settings()
			Game.save.mark_dirty()
			root.queue_free()
			settings(parent, on_close), 22, Vector2(0, 54))
		UI.expand(b)
		q.add_child(b)
		buttons.append(b)
	v.add_child(q)
	v.add_child(UI.spacer(6))
	var reset := UI.button("Reset progress", UI.DANGER, func():
		confirm(parent, "Reset all progress?", "Coins, balloons, stars and upgrades will be lost. This cannot be undone.", func():
			Game.save.reset_progress()
			Events.currency_changed.emit(Game.economy.coins, Game.economy.gems)
			Game.apply_settings()
			Game.toast("Progress reset")
			root.queue_free()
			Game.goto("menu")), 22, Vector2(0, 54))
	v.add_child(reset)
	v.add_child(UI.button("Close", UI.SUCCESS, func():
		Game.save.save_if_dirty()
		root.queue_free()
		if on_close.is_valid(): on_close.call()))

static func _slider(text: String, value: float, on_change: Callable) -> VBoxContainer:
	var v := UI.vbox(4)
	v.add_child(UI.label(text, 22, UI.TEXT_DIM, HORIZONTAL_ALIGNMENT_LEFT))
	var sl := HSlider.new()
	sl.min_value = 0.0
	sl.max_value = 1.0
	sl.step = 0.05
	sl.value = value
	sl.custom_minimum_size = Vector2(0, 36)
	sl.value_changed.connect(on_change)
	v.add_child(sl)
	return v

static func confirm(parent: Node, title: String, text: String, on_yes: Callable) -> void:
	var m := UI.modal(parent, 430)
	var root: Control = m[0]
	var v: VBoxContainer = m[1]
	v.add_child(UI.label(title, 30))
	v.add_child(UI.wrap(UI.label(text, 22, UI.TEXT_DIM)))
	var h := UI.hbox(10)
	h.add_child(UI.expand(UI.button("Cancel", UI.PANEL_LIGHT, func(): root.queue_free())))
	h.add_child(UI.expand(UI.button("Yes", UI.DANGER, func():
		root.queue_free()
		on_yes.call())))
	v.add_child(h)

static func achievements(parent: Node) -> void:
	var m := UI.modal(parent, 500)
	var root: Control = m[0]
	var v: VBoxContainer = m[1]
	var a := Game.achievements
	v.add_child(UI.label("Achievements", 34))
	v.add_child(UI.label("%d / %d unlocked" % [a.unlocked_count(), Game.db.achievements.size()], 22, UI.TEXT_DIM))
	var scroll := ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(0, 560)
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	var list := UI.vbox(8)
	UI.expand(list)
	scroll.add_child(list)
	for ach in Game.db.achievements:
		var done := a.is_unlocked(ach.id)
		var row := UI.panel(Color(0.2, 0.42, 0.3) if done else UI.PANEL_LIGHT, 16)
		var h := UI.hbox(10)
		row.add_child(h)
		h.add_child(Icon.make("trophy", 40, UI.GOLD if done else Color(1, 1, 1, 0.3)))
		var tv := UI.vbox(2)
		UI.expand(tv)
		tv.add_child(UI.label(ach.title, 22, UI.TEXT, HORIZONTAL_ALIGNMENT_LEFT))
		var prog := mini(a.value(ach.stat), int(ach.target))
		tv.add_child(UI.wrap(UI.label("%s  (%d/%d)" % [ach.desc, prog, int(ach.target)], 18, UI.TEXT_DIM, HORIZONTAL_ALIGNMENT_LEFT)))
		h.add_child(tv)
		var reward := UI.hbox(4)
		reward.add_child(Icon.make("gem", 22, UI.GEM))
		reward.add_child(UI.label(str(int(ach.gems)), 20))
		h.add_child(reward)
		list.add_child(row)
	v.add_child(scroll)
	v.add_child(UI.button("Close", UI.SUCCESS, func(): root.queue_free()))
