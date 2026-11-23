class_name WorkshopUpgrades
extends RefCounted
## Permanent workshop upgrades (coins) and cosmetic trails (Sky Gems).

enum Result { OK, MAX_LEVEL, NOT_ENOUGH, ALREADY_OWNED, NOT_OWNED, UNKNOWN }

var save: SaveManager
var economy: Economy
var db: GameDB

func _init(p_save: SaveManager, p_economy: Economy, p_db: GameDB) -> void:
	save = p_save
	economy = p_economy
	db = p_db

func level(c: int) -> int: return int(save.data.workshop[c])
func total_levels() -> int:
	var n := 0
	for v in save.data.workshop: n += int(v)
	return n

func next_cost(c: int) -> int:
	var l := level(c)
	return int(db.workshop.costs[c][l]) if l < C.MAX_WORKSHOP_LEVEL else 0

func describe(c: int, lvl: int) -> String:
	var ws := db.workshop
	match c:
		C.Workshop.DURABILITY: return "+%d HP, +%d armor" % [ws.health[lvl], ws.armor[lvl]]
		C.Workshop.MAGNET: return "+%.1f magnet radius" % ws.magnet[lvl]
		C.Workshop.MOVEMENT: return "x%.2f speed" % ws.movement[lvl]
	return "+%ds shields" % int(ws.shield[lvl])

func try_upgrade(c: int) -> int:
	if c < 0 or c >= C.WORKSHOP_CATEGORIES: return Result.UNKNOWN
	if level(c) >= C.MAX_WORKSHOP_LEVEL: return Result.MAX_LEVEL
	if not economy.try_spend_coins(next_cost(c)): return Result.NOT_ENOUGH
	save.data.workshop[c] = level(c) + 1
	save.save_now()
	return Result.OK

func owns_trail(id: String) -> bool: return save.data.owned_trails.has(id)
func equipped_trail() -> String: return str(save.data.trail)

func try_buy_trail(id: String) -> int:
	var t := db.trail(id)
	if t.is_empty(): return Result.UNKNOWN
	if owns_trail(id): return Result.ALREADY_OWNED
	if not economy.try_spend_gems(int(t.gems)): return Result.NOT_ENOUGH
	save.data.owned_trails.append(id)
	save.data.trail = id
	save.save_now()
	return Result.OK

func try_equip_trail(id: String) -> int:
	if id != "" and not owns_trail(id): return Result.NOT_OWNED
	save.data.trail = id
	save.save_now()
	return Result.OK
