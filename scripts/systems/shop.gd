class_name Shop
extends RefCounted
## Balloon purchases, equipping, stage upgrades and final stat calculation.

enum Result { OK, ALREADY_OWNED, NOT_OWNED, NOT_ENOUGH_COINS, LOCKED, MAX_STAGE, UNKNOWN }

var save: SaveManager
var economy: Economy
var db: GameDB

func _init(p_save: SaveManager, p_economy: Economy, p_db: GameDB) -> void:
	save = p_save
	economy = p_economy
	db = p_db

static func describe(r: int) -> String:
	match r:
		Result.OK: return "Done!"
		Result.ALREADY_OWNED: return "You already own this balloon."
		Result.NOT_OWNED: return "Buy this balloon first."
		Result.NOT_ENOUGH_COINS: return "Not enough coins."
		Result.LOCKED: return "Reach a later world to unlock."
		Result.MAX_STAGE: return "Already at maximum stage."
	return "Unknown balloon."

func is_owned(id: String) -> bool: return save.data.owned.has(id)
func is_equipped(id: String) -> bool: return str(save.data.equipped) == id
func stage(id: String) -> int: return save.stage_of(id) if is_owned(id) else 1

func equipped() -> Dictionary:
	var b := db.balloon(str(save.data.equipped))
	return b if not b.is_empty() else db.default_balloon()

func is_unlocked_by_progress(b: Dictionary) -> bool:
	return not b.is_empty() and int(b.unlock_world) <= C.world_of(int(save.data.highest_unlocked))

## Final stats: base + reached stage bonus + workshop upgrades + magnet abilities.
static func compute_stats(b: Dictionary, p_stage: int, workshop_levels: Array, ws: Dictionary) -> Dictionary:
	var s := {"hp": maxi(1, int(b.hp)), "speed": maxf(0.5, float(b.speed)), "armor": maxi(0, int(b.armor)),
		"magnet": maxf(0.0, float(b.magnet)), "shield_bonus": 0.0}
	var st := clampi(p_stage, 1, C.MAX_STAGE)
	var bonus: Dictionary = b.stage_bonus[st - 1]
	s.hp += int(bonus.hp); s.speed += float(bonus.speed); s.armor += int(bonus.armor); s.magnet += float(bonus.magnet)
	if not ws.is_empty() and workshop_levels.size() >= C.WORKSHOP_CATEGORIES:
		s.hp += int(ws.health[int(workshop_levels[C.Workshop.DURABILITY])])
		s.armor += int(ws.armor[int(workshop_levels[C.Workshop.DURABILITY])])
		s.magnet += float(ws.magnet[int(workshop_levels[C.Workshop.MAGNET])])
		s.speed *= float(ws.movement[int(workshop_levels[C.Workshop.MOVEMENT])])
		s.shield_bonus = float(ws.shield[int(workshop_levels[C.Workshop.SHIELD])])
	if b.ability == "magnet" or b.ability == "strong_magnet":
		s.magnet += float(b.value)
	return s

func stats(b: Dictionary, stage_override := -1) -> Dictionary:
	var st := stage_override if stage_override > 0 else stage(b.id)
	return compute_stats(b, st, save.data.workshop, db.workshop)

func can_purchase(id: String) -> int:
	var b := db.balloon(id)
	if b.is_empty(): return Result.UNKNOWN
	if is_owned(id): return Result.ALREADY_OWNED
	if not is_unlocked_by_progress(b): return Result.LOCKED
	if not economy.can_afford_coins(int(b.price)): return Result.NOT_ENOUGH_COINS
	return Result.OK

func try_purchase(id: String) -> int:
	var check := can_purchase(id)
	if check != Result.OK: return check
	if not economy.try_spend_coins(int(db.balloon(id).price)): return Result.NOT_ENOUGH_COINS
	save.data.owned.append(id)
	save.data.stages[id] = 1
	save.save_now()
	return Result.OK

func try_equip(id: String) -> int:
	if db.balloon(id).is_empty(): return Result.UNKNOWN
	if not is_owned(id): return Result.NOT_OWNED
	save.data.equipped = id
	save.save_now()
	return Result.OK

func upgrade_cost(id: String) -> int:
	var b := db.balloon(id)
	var st := stage(id)
	if b.is_empty() or st >= C.MAX_STAGE: return 0
	return int(b.stage_costs[st - 1])

func can_upgrade(id: String) -> int:
	if db.balloon(id).is_empty(): return Result.UNKNOWN
	if not is_owned(id): return Result.NOT_OWNED
	if stage(id) >= C.MAX_STAGE: return Result.MAX_STAGE
	if not economy.can_afford_coins(upgrade_cost(id)): return Result.NOT_ENOUGH_COINS
	return Result.OK

func try_upgrade(id: String) -> int:
	var check := can_upgrade(id)
	if check != Result.OK: return check
	var cost := upgrade_cost(id)
	if not economy.try_spend_coins(cost): return Result.NOT_ENOUGH_COINS
	save.data.stages[id] = stage(id) + 1
	save.save_now()
	return Result.OK
