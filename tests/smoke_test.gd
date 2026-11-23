extends SceneTree
## Headless smoke test: boots the real game, visits every screen and lets the autopilot play
## a selection of levels (including bosses) end to end. Run with --fixed-fps 60.
## Exits 1 if a screen fails to appear or a level never finishes.

var levels := [1, 2, 4, 6, 9, 12, 18, 24, 30, 36, 42, 48, 54, 60]
var failures := 0

func _initialize() -> void:
	change_scene_to_file("res://main.tscn")
	_run.call_deferred()

func _wait_screen(n: String, max_frames := 2000) -> bool:
	for i in max_frames:
		await process_frame
		var g = root.get_node("Game")
		if g.main and g.main.screen_name == n and not g.main._busy: return true
	print("FAIL: screen '%s' never appeared" % n)
	failures += 1
	return false

func _run() -> void:
	var g = root.get_node("Game")
	g.autopilot = true
	await _wait_screen("menu")
	g.save.data.coins = 500000
	g.save.data.gems = 500
	for s in ["map", "shop", "workshop", "ending", "menu"]:
		g.goto(s)
		await _wait_screen(s)
		for i in 30: await process_frame
		print("screen ok: ", s)
	# buy and upgrade a few balloons to exercise abilities and stage visuals
	for id in ["phoenix", "teleport", "shadow", "angel"]:
		if not g.db.balloon(id).is_empty():
			g.save.data.highest_unlocked = 60
			print("buy ", id, " -> ", g.shop.try_purchase(id))
	var balloons := ["classic", "phoenix", "teleport", "angel"]
	var wins := 0
	for idx in levels.size():
		var n: int = levels[idx]
		var b: String = balloons[idx % balloons.size()]
		if not g.shop.is_owned(b): b = "classic"
		g.start_level(n, b)
		await _wait_screen("play")
		var ok := await _wait_screen("results", 60 * 200)
		if not ok:
			print("FAIL: level %d did not finish" % n)
			continue
		var r: Dictionary = g.last_result
		if r.victory: wins += 1
		print("level %2d (%s) victory=%s stars=%d coins=%d progress=%.2f boss=%s damaged=%s time=%.1f" % [n, b, r.victory, r.stars, r.coins, r.progress, r.boss_defeated, r.took_damage, r.time])
		for i in 20: await process_frame
	print("wins %d / %d" % [wins, levels.size()])
	print("SMOKE TEST ", "PASSED" if failures == 0 else "FAILED (%d)" % failures)
	quit(1 if failures > 0 else 0)
