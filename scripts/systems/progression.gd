class_name Progression
extends RefCounted
## Level unlocks, stars, records and level rewards.

var save: SaveManager
var economy: Economy
var db: GameDB

func _init(p_save: SaveManager, p_economy: Economy, p_db: GameDB) -> void:
	save = p_save
	economy = p_economy
	db = p_db

var data: Dictionary:
	get: return save.data

func is_level_unlocked(n: int) -> bool: return n >= 1 and n <= int(data.highest_unlocked)
func is_level_completed(n: int) -> bool: return n >= 1 and n <= C.TOTAL_LEVELS and bool(data.completed[n - 1])
func stars(n: int) -> int: return int(data.stars[n - 1]) if n >= 1 and n <= C.TOTAL_LEVELS else 0
func is_world_unlocked(w: int) -> bool: return is_level_unlocked(C.first_level_of(w))
func is_world_completed(w: int) -> bool: return is_level_completed(w * C.LEVELS_PER_WORLD)
func highest_world() -> int: return C.world_of(int(data.highest_unlocked))
func current_level() -> int: return clampi(int(data.highest_unlocked), 1, C.TOTAL_LEVELS)

func worlds_completed() -> int:
	var n := 0
	for w in range(1, C.WORLDS + 1):
		if is_world_completed(w): n += 1
	return n

func world_stars(w: int) -> int:
	var n := 0
	for i in C.LEVELS_PER_WORLD: n += stars(C.first_level_of(w) + i)
	return n

## Star rules: 1 = reach the top, 2 = coin target, 3 = the level's challenge.
static func calculate_stars(level: Dictionary, r: Dictionary) -> Array:
	var flags := [false, false, false]
	if level.is_empty() or not r.get("victory", false): return flags
	flags[0] = true
	flags[1] = int(r.coins) >= int(level.coins_for_second_star)
	match int(level.challenge):
		C.Challenge.NO_DAMAGE: flags[2] = not r.took_damage
		C.Challenge.ALL_SPECIAL: flags[2] = int(r.special_total) > 0 and int(r.special) >= int(r.special_total)
		C.Challenge.COIN_TARGET: flags[2] = int(r.coins) >= int(level.challenge_value)
		C.Challenge.POWERUPS: flags[2] = int(r.powerups) >= int(level.challenge_value)
	return flags

static func challenge_text(level: Dictionary) -> String:
	match int(level.challenge):
		C.Challenge.NO_DAMAGE: return "Finish without taking damage"
		C.Challenge.ALL_SPECIAL: return "Collect all %d star coins" % int(level.special_coins)
		C.Challenge.COIN_TARGET: return "Collect %d coins" % int(level.challenge_value)
		C.Challenge.POWERUPS: return "Collect %d power-ups" % int(level.challenge_value)
	return ""

static func new_result(level_number: int) -> Dictionary:
	return {"level": level_number, "victory": false, "coins": 0, "special": 0, "special_total": 0,
		"took_damage": false, "powerups": 0, "progress": 0.0, "time": 0.0, "boss_defeated": false,
		"balloon": C.DEFAULT_BALLOON, "reward_multiplier": 1.0,
		"stars": 0, "star_flags": [false, false, false], "lines": [], "unlocks": [],
		"new_best": false, "campaign_completed_now": false}

## Applies a finished run: stars, rewards (one-time rewards are protected), records, unlocks.
func record_result(r: Dictionary) -> void:
	var level := db.level(int(r.level))
	if level.is_empty(): return
	var i: int = int(r.level) - 1
	var lines: Array = r.lines
	data.stats.total_coins = int(data.stats.total_coins) + maxi(0, int(r.coins))
	var coins_to_add := 0
	if int(r.coins) > 0:
		lines.append({"label": "Coins collected", "coins": int(r.coins), "gems": 0})
		coins_to_add += int(r.coins)

	if not r.victory:
		data.stats.levels_failed = int(data.stats.levels_failed) + 1
		r.stars = 0
		economy.add_coins(coins_to_add, false)
		save.save_now()
		return

	r.star_flags = calculate_stars(level, r)
	r.stars = r.star_flags.count(true)
	var mult := maxf(1.0, float(r.reward_multiplier))
	var scaled := func(v: int) -> int: return int(round(v * mult))

	lines.append({"label": "Level complete", "coins": scaled.call(C.R_LEVEL_COMPLETE), "gems": 0})
	coins_to_add += scaled.call(C.R_LEVEL_COMPLETE)
	if not r.took_damage:
		lines.append({"label": "No damage bonus", "coins": scaled.call(C.R_NO_DAMAGE), "gems": 0})
		coins_to_add += scaled.call(C.R_NO_DAMAGE)
	if int(r.special_total) > 0 and int(r.special) >= int(r.special_total):
		lines.append({"label": "All star coins", "coins": scaled.call(C.R_ALL_SPECIAL), "gems": 0})
		coins_to_add += scaled.call(C.R_ALL_SPECIAL)
	var boss := db.boss(level.boss) if level.is_boss else {}
	if level.is_boss and r.boss_defeated:
		var boss_coins: int = maxi(C.R_BOSS, int(boss.get("reward_coins", 0)))
		lines.append({"label": "Boss defeated", "coins": scaled.call(boss_coins), "gems": 0})
		coins_to_add += scaled.call(boss_coins)
	if mult > 1.0:
		lines.append({"label": "Balloon bonus x%.2f applied" % mult, "coins": 0, "gems": 0})
	economy.add_coins(coins_to_add, false)

	if r.stars == 3 and economy.try_grant_once("three_star_%d" % int(r.level), C.R_THREE_STAR, C.R_FIRST_THREE_STAR_GEMS):
		lines.append({"label": "First three-star bonus", "coins": C.R_THREE_STAR, "gems": C.R_FIRST_THREE_STAR_GEMS})
	if level.is_boss and r.boss_defeated and not boss.is_empty():
		var gems: int = maxi(C.R_BOSS_GEMS, int(boss.reward_gems))
		if economy.try_grant_once("boss_" + boss.id, 0, gems):
			lines.append({"label": "First victory over " + boss.name, "coins": 0, "gems": gems})
		if not data.stats.bosses.has(boss.id): data.stats.bosses.append(boss.id)

	if int(r.stars) > int(data.stars[i]):
		data.stars[i] = r.stars
		r.new_best = true
	data.best_coins[i] = maxi(int(data.best_coins[i]), int(r.coins))
	var first_clear := not bool(data.completed[i])
	data.completed[i] = true
	data.stats.levels_completed = int(data.stats.levels_completed) + 1
	if not r.took_damage: data.stats.no_damage_clears = int(data.stats.no_damage_clears) + 1

	if level.is_boss:
		var w := db.world(int(level.world))
		if economy.try_grant_once("world_%d" % int(w.number), int(w.completion_coins), int(w.completion_gems)):
			lines.append({"label": w.name + " completed", "coins": int(w.completion_coins), "gems": int(w.completion_gems)})

	var next: int = int(r.level) + 1
	if next <= C.TOTAL_LEVELS and int(data.highest_unlocked) < next:
		data.highest_unlocked = next
		if C.index_in_world(next) == 1:
			var nw := C.world_of(next)
			r.unlocks.append("World %d: %s unlocked!" % [nw, db.world(nw).name])
			var names := []
			for b in db.balloons:
				if int(b.unlock_world) == nw: names.append(b.name)
			if names.size() > 0: r.unlocks.append("New balloons in the shop: " + ", ".join(names))
		else:
			r.unlocks.append("Level %d unlocked" % C.index_in_world(next))

	if int(r.level) == C.TOTAL_LEVELS and first_clear and not data.campaign_completed:
		data.campaign_completed = true
		r.campaign_completed_now = true
		r.unlocks.append("You reached Heaven's Gate!")
	save.save_now()

func record_powerup() -> void:
	data.stats.powerups = int(data.stats.powerups) + 1
	save.mark_dirty()

static func total_coins_of(r: Dictionary) -> int:
	var n := 0
	for l in r.lines: n += int(l.coins)
	return n

static func total_gems_of(r: Dictionary) -> int:
	var n := 0
	for l in r.lines: n += int(l.gems)
	return n
