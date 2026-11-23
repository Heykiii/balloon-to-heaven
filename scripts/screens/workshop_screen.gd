extends Control
## Workshop: four permanent upgrade tracks (coins) and cosmetic trails (Sky Gems).

const ICONS := ["heart", "magnet", "rocket", "shield"]
const COLORS := [Color("#FF5A6E"), Color("#FFD34D"), Color("#7CF29C"), Color("#6FE3FF")]

var _list: VBoxContainer
var _preview: ModelPreview

func backdrop_world() -> int: return Game.progression.highest_world()

func _ready() -> void:
	add_child(UI.top_bar("Workshop", func(): Game.goto("menu")))
	var scroll := ScrollContainer.new()
	scroll.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	scroll.offset_top = 80
	scroll.offset_left = 14
	scroll.offset_right = -14
	scroll.offset_bottom = -10
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	add_child(scroll)
	_list = UI.vbox(10)
	UI.expand(_list)
	scroll.add_child(_list)
	_refresh()

func _refresh() -> void:
	for c in _list.get_children(): c.queue_free()
	_list.add_child(UI.label("Upgrades for every balloon", 24, UI.TEXT))
	var ws := Game.workshop
	for c in C.WORKSHOP_CATEGORIES:
		var p := UI.panel(Color(0.08, 0.14, 0.3, 0.88), 20)
		var h := UI.hbox(10)
		p.add_child(h)
		h.add_child(Icon.make(ICONS[c], 52, COLORS[c]))
		var v := UI.vbox(4)
		UI.expand(v)
		h.add_child(v)
		var lvl := ws.level(c)
		v.add_child(UI.label(Game.db.workshop.names[c], 22, UI.TEXT, HORIZONTAL_ALIGNMENT_LEFT))
		var pips := UI.hbox(4)
		for i in C.MAX_WORKSHOP_LEVEL:
			var pip := Panel.new()
			pip.custom_minimum_size = Vector2(30, 12)
			pip.add_theme_stylebox_override("panel", UI.box(COLORS[c] if i < lvl else Color(0, 0, 0, 0.35), 6))
			pips.add_child(pip)
		v.add_child(pips)
		var now := ws.describe(c, lvl)
		var txt := "Now: " + now
		if lvl < C.MAX_WORKSHOP_LEVEL: txt += "\nNext: " + ws.describe(c, lvl + 1)
		v.add_child(UI.label(txt, 16, UI.TEXT_DIM, HORIZONTAL_ALIGNMENT_LEFT))
		if lvl < C.MAX_WORKSHOP_LEVEL:
			var cost := ws.next_cost(c)
			var cat := c
			var b := UI.button(U.fmt(cost), UI.SUCCESS if Game.economy.can_afford_coins(cost) else UI.PANEL_LIGHT, func(): _upgrade(cat), 20, Vector2(120, 60))
			b.text = "     " + b.text
			UI.attach_icon(b, Icon.make("coin", 22, UI.GOLD), 12)
			h.add_child(b)
		else:
			h.add_child(UI.label("MAX", 22, UI.GOLD))
		_list.add_child(p)
	_list.add_child(UI.spacer(6))
	_list.add_child(UI.label("Trails (Sky Gems)", 24))
	_preview = ModelPreview.new()
	_preview.custom_minimum_size = Vector2(0, 200)
	_list.add_child(_preview)
	var b := Game.shop.equipped()
	_preview.show_balloon(b, Game.shop.stage(b.id), Game.db.trail(ws.equipped_trail()))
	var none := UI.button("No trail" + ("  (on)" if ws.equipped_trail() == "" else ""), UI.PANEL_LIGHT, func():
		ws.try_equip_trail("")
		_refresh(), 20, Vector2(0, 54))
	_list.add_child(none)
	for t in Game.db.trails:
		var p2 := UI.panel(Color(0.08, 0.14, 0.3, 0.88), 18)
		var h2 := UI.hbox(10)
		p2.add_child(h2)
		var sw := Panel.new()
		sw.custom_minimum_size = Vector2(44, 44)
		var grad := UI.box(t.a, 22, t.b, 6)
		sw.add_theme_stylebox_override("panel", grad)
		h2.add_child(sw)
		h2.add_child(UI.expand(UI.label(t.name, 21, UI.TEXT, HORIZONTAL_ALIGNMENT_LEFT)))
		var id: String = t.id
		if ws.equipped_trail() == id:
			h2.add_child(UI.label("Equipped", 18, UI.SUCCESS))
		elif ws.owns_trail(id):
			h2.add_child(UI.button("Equip", UI.PRIMARY, func():
				ws.try_equip_trail(id)
				Sound.play("purchase", 0.6)
				_refresh(), 18, Vector2(110, 50)))
		else:
			var bb := UI.button("   %d" % int(t.gems), UI.SECONDARY, func(): _buy_trail(id), 18, Vector2(110, 50))
			UI.attach_icon(bb, Icon.make("gem", 22, UI.GEM), 14)
			h2.add_child(bb)
		_list.add_child(p2)
	_list.add_child(UI.wrap(UI.label("Earn Sky Gems by beating bosses, finishing worlds, first 3-star clears and achievements.", 16, UI.TEXT_DIM)))

func _upgrade(c: int) -> void:
	var r := Game.workshop.try_upgrade(c)
	if r == WorkshopUpgrades.Result.OK:
		Sound.play("upgrade")
		Game.toast("%s upgraded!" % Game.db.workshop.names[c])
		Game.achievements.evaluate()
	else:
		Sound.play("deny")
		Game.toast("Not enough coins" if r == WorkshopUpgrades.Result.NOT_ENOUGH else "Already at max level")
	_refresh()

func _buy_trail(id: String) -> void:
	var r := Game.workshop.try_buy_trail(id)
	if r == WorkshopUpgrades.Result.OK:
		Sound.play("purchase")
		Game.toast(Game.db.trail(id).name + " equipped!")
	else:
		Sound.play("deny")
		Game.toast("Not enough Sky Gems")
	_refresh()

func on_back() -> void:
	Game.goto("menu")
