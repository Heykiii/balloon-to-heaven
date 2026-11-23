extends Control
## Victory / failure results with animated stars, reward breakdown and unlocks.

var r: Dictionary

func backdrop_world() -> int:
	r = Game.last_result
	return C.world_of(int(r.get("level", 1)))

func _ready() -> void:
	var lv := Game.db.level(int(r.get("level", 1)))
	var win: bool = r.get("victory", false)
	if not win: Sound.music(Sound.Cue.FAIL)
	var center := CenterContainer.new()
	UI.full(center)
	add_child(center)
	var p := UI.panel()
	p.custom_minimum_size = Vector2(480, 0)
	center.add_child(p)
	var v := UI.vbox(10)
	p.add_child(UI.margin(v, 10, 8, 10, 8))
	v.add_child(UI.label("Level complete!" if win else "Balloon down!", 40, UI.GOLD if win else Color("#FF9F9F")))
	v.add_child(UI.label("%d-%d  %s" % [lv.world, lv.index, lv.name], 22, UI.TEXT_DIM))
	var stars := UI.hbox(10)
	stars.alignment = BoxContainer.ALIGNMENT_CENTER
	var star_icons: Array[Icon] = []
	for i in 3:
		var s := Icon.make("star", 70, UI.GOLD, false)
		stars.add_child(s)
		star_icons.append(s)
	v.add_child(stars)
	if win:
		var flags: Array = r.star_flags
		var goals := ["Reached the top", "Collect %d coins" % int(lv.coins_for_second_star), Progression.challenge_text(lv)]
		for i in 3:
			var row := UI.hbox(6)
			row.add_child(Icon.make("check" if flags[i] else "close", 20, UI.SUCCESS if flags[i] else Color(1, 1, 1, 0.35)))
			row.add_child(UI.label(goals[i], 18, UI.TEXT if flags[i] else UI.TEXT_DIM, HORIZONTAL_ALIGNMENT_LEFT))
			v.add_child(row)
		_animate_stars(star_icons, int(r.stars))
	else:
		v.add_child(UI.label("You made it %d%% of the way up." % roundi(float(r.progress) * 100.0), 22))
		v.add_child(UI.wrap(UI.label(_tip(lv), 19, UI.TEXT_DIM)))
	v.add_child(UI.spacer(4))
	var lines := UI.vbox(4)
	for l in r.get("lines", []):
		var h := UI.hbox(6)
		h.add_child(UI.expand(UI.label(l.label, 19, UI.TEXT_DIM, HORIZONTAL_ALIGNMENT_LEFT)))
		if int(l.coins) > 0:
			h.add_child(UI.label("+" + U.fmt(int(l.coins)), 19, UI.GOLD))
			h.add_child(Icon.make("coin", 20, UI.GOLD))
		if int(l.gems) > 0:
			h.add_child(UI.label("+%d" % int(l.gems), 19, UI.GEM))
			h.add_child(Icon.make("gem", 20, UI.GEM))
		lines.add_child(h)
	v.add_child(lines)
	var total := UI.hbox(8)
	total.alignment = BoxContainer.ALIGNMENT_CENTER
	total.add_child(UI.label("Total", 24))
	total.add_child(UI.label("+" + U.fmt(Progression.total_coins_of(r)), 26, UI.GOLD))
	total.add_child(Icon.make("coin", 26, UI.GOLD))
	var gems := Progression.total_gems_of(r)
	if gems > 0:
		total.add_child(UI.label("+%d" % gems, 26, UI.GEM))
		total.add_child(Icon.make("gem", 26, UI.GEM))
	v.add_child(total)
	for u in r.get("unlocks", []):
		v.add_child(UI.wrap(UI.label(u, 20, Color("#9CFFB5"))))
	if r.get("new_best", false): v.add_child(UI.label("New best!", 22, UI.GOLD))
	var n := int(r.level)
	if Game.show_ending:
		v.add_child(UI.button("Enter Heaven's Gate", Color("#D4A017"), func(): Game.goto("ending"), 28, Vector2(0, 76)))
	elif win and n < C.TOTAL_LEVELS and Game.progression.is_level_unlocked(n + 1):
		v.add_child(UI.button("Next level", UI.SUCCESS, func(): Game.start_level(n + 1, str(r.balloon)), 28, Vector2(0, 76)))
	var h2 := UI.hbox(10)
	h2.add_child(UI.expand(UI.button("Retry", UI.SECONDARY if win else UI.SUCCESS, func(): Game.start_level(n, str(r.balloon)))))
	h2.add_child(UI.expand(UI.button("Map", UI.PRIMARY, func():
		Game.map_world = C.world_of(mini(n + (1 if win else 0), C.TOTAL_LEVELS))
		Game.goto("map"))))
	v.add_child(h2)
	if not win:
		v.add_child(UI.button("Upgrade balloon", UI.WARNING, func(): Game.goto("shop"), 22, Vector2(0, 56)))
	UI.pop_in.call_deferred(p)

func _animate_stars(icons: Array[Icon], count: int) -> void:
	for i in count:
		await get_tree().create_timer(0.35 + i * 0.3).timeout
		if not is_inside_tree(): return
		var ic := icons[i]
		ic.set_kind("star", UI.GOLD, true)
		ic.pivot_offset = ic.size * 0.5
		ic.scale = Vector2(1.6, 1.6)
		ic.create_tween().tween_property(ic, "scale", Vector2.ONE, 0.25).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		Sound.play("star")

func _tip(lv: Dictionary) -> String:
	var tips := [
		"Tip: hazards always leave a gap. Look for the open lane early.",
		"Tip: upgrade Durability in the Workshop for extra hearts.",
		"Tip: Energy Shield and Sky Rocket make you briefly untouchable.",
		"Tip: each balloon has its own ability. Try another one from the Balloon Shop.",
		"Tip: lightning, lava and barriers flash before they strike.",
	]
	if lv.is_boss: return "Tip: grab the glowing crystals to damage the boss faster."
	return tips[int(lv.number) % tips.size()]

func on_back() -> void:
	Game.goto("map")
