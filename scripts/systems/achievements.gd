class_name Achievements
extends RefCounted
## 17 achievements evaluated from saved statistics. Each pays Sky Gems once.

signal unlocked(title: String)

var save: SaveManager
var economy: Economy
var db: GameDB

func _init(p_save: SaveManager, p_economy: Economy, p_db: GameDB) -> void:
	save = p_save
	economy = p_economy
	db = p_db

func value(stat: String) -> int:
	var d := save.data
	match stat:
		"levels_completed": return int(d.stats.levels_completed)
		"total_coins": return int(d.stats.total_coins)
		"balloons_owned": return d.owned.size()
		"bosses_defeated": return d.stats.bosses.size()
		"no_damage_clears": return int(d.stats.no_damage_clears)
		"total_stars": return save.total_stars()
		"max_stage":
			var m := 1
			for id in d.stages: m = maxi(m, int(d.stages[id]))
			return m
		"workshop_levels":
			var n := 0
			for v in d.workshop: n += int(v)
			return n
		"powerups": return int(d.stats.powerups)
		"worlds_completed":
			var w := 0
			for i in range(1, C.WORLDS + 1):
				if bool(d.completed[i * C.LEVELS_PER_WORLD - 1]): w += 1
			return w
		"campaign": return 1 if d.campaign_completed else 0
	return 0

func is_unlocked(id: String) -> bool: return save.data.achievements.has(id)
func unlocked_count() -> int: return save.data.achievements.size()

func evaluate() -> void:
	for a in db.achievements:
		if is_unlocked(a.id): continue
		if value(a.stat) >= int(a.target):
			save.data.achievements.append(a.id)
			economy.try_grant_once("achievement_" + a.id, 0, int(a.gems))
			unlocked.emit(a.title)
