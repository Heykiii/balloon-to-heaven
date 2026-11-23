extends SceneTree
## Captures screenshots of every screen and of gameplay in several worlds (needs a display:
## run under xvfb-run). Usage: godot --path . -s res://tests/screenshots.gd -- <out_dir> [levels...]

var out := "user://shots"
var levels := [1, 6, 16, 22, 24, 31, 40, 48, 55, 60]

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() > 0: out = args[0]
	if args.size() > 1:
		levels = []
		for a in args.slice(1): levels.append(int(a))
	DirAccess.make_dir_recursive_absolute(out)
	change_scene_to_file("res://main.tscn")
	_run.call_deferred()

func _shot(n: String) -> void:
	await process_frame
	await process_frame
	var img := root.get_viewport().get_texture().get_image()
	img.save_png(out + "/" + n + ".png")
	print("shot ", n)

func _wait_screen(n: String, max_frames := 3000) -> bool:
	var g = root.get_node("Game")
	for i in max_frames:
		await process_frame
		if g.main and g.main.screen_name == n and not g.main._busy: return true
	return false

func _frames(n: int) -> void:
	for i in n: await process_frame

func _run() -> void:
	var g = root.get_node("Game")
	g.autopilot = true
	await _wait_screen("menu")
	g.save.data.coins = 250000
	g.save.data.gems = 120
	g.save.data.highest_unlocked = 60
	for i in 40: g.save.data.stars[i] = 1 + i % 3
	g.shop.try_purchase("phoenix")
	g.shop.try_upgrade("phoenix"); g.shop.try_upgrade("phoenix"); g.shop.try_upgrade("phoenix"); g.shop.try_upgrade("phoenix")
	g.shop.try_equip("phoenix")
	g.workshop.try_buy_trail("rainbow")
	await _frames(40)
	g.goto("menu"); await _wait_screen("menu"); await _frames(50); await _shot("01_menu")
	g.map_world = 1
	g.goto("map"); await _wait_screen("map"); await _frames(40); await _shot("02_map_w1")
	g.main.screen._popup(3); await _frames(30); await _shot("03_level_popup")
	g.map_world = 7
	g.goto("map"); await _wait_screen("map"); await _frames(40); await _shot("04_map_w7")
	g.goto("shop"); await _wait_screen("shop"); await _frames(40); await _shot("05_shop")
	g.goto("workshop"); await _wait_screen("workshop"); await _frames(40); await _shot("06_workshop")
	Dialogs.settings(g.main.screen); await _frames(20); await _shot("07_settings")
	g.goto("menu"); await _wait_screen("menu")
	Dialogs.achievements(g.main.screen); await _frames(20); await _shot("08_achievements")
	for n in levels:
		g.start_level(n, "phoenix")
		await _wait_screen("play")
		await _frames(150)
		await _shot("play_%02d_a" % n)
		var lv: Dictionary = g.db.level(n)
		if lv.is_boss:
			# jump to the boss
			var ps = g.main.screen
			ps.ctx.distance = float(lv.length) - 1.0
			await _frames(300)
			await _shot("play_%02d_boss" % n)
			await _frames(240)
			await _shot("play_%02d_boss2" % n)
		else:
			await _frames(420)
			await _shot("play_%02d_b" % n)
		if n == levels[0]:
			g.main.screen.toggle_pause(); await _frames(20); await _shot("09_pause"); g.main.screen.toggle_pause()
		g.main.screen.ctx.distance = float(lv.length) + 1.0
		if lv.is_boss: g.main.screen.boss.health = 0.001
		await _wait_screen("results", 3000)
		await _frames(90)
		if n == levels[0]: await _shot("10_results")
	g.goto("ending"); await _wait_screen("ending"); await _frames(400); await _shot("11_ending")
	quit()
