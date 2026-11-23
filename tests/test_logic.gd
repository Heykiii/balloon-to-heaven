extends RefCounted
## Logic tests: content, saves, economy, progression, shop, workshop, fairness.

var runner
static var _db: GameDB

func ok(cond: bool, msg := "check failed") -> void: runner.check(cond, msg)
func eq(a, b, msg := "") -> void: runner.check(a == b, "%s expected %s, got %s" % [msg, str(b), str(a)])

func db() -> GameDB:
	if _db == null: _db = Content.build()
	return _db

class World:
	var db: GameDB
	var storage := SaveManager.MemoryStorage.new()
	var save: SaveManager
	var economy: Economy
	var progression: Progression
	var shop: Shop
	var workshop: WorkshopUpgrades
	var achievements: Achievements
	func _init(p_db: GameDB) -> void:
		db = p_db
		save = SaveManager.new(storage, db)
		save.load_game()
		economy = Economy.new(save)
		progression = Progression.new(save, economy, db)
		shop = Shop.new(save, economy, db)
		workshop = WorkshopUpgrades.new(save, economy, db)
		achievements = Achievements.new(save, economy, db)
	func win(n: int, coins := 0, damage := true) -> Dictionary:
		var l := db.level(n)
		var r := Progression.new_result(n)
		r.victory = true; r.coins = coins; r.took_damage = damage
		r.boss_defeated = l.is_boss; r.special_total = l.special_coins
		progression.record_result(r)
		return r

func w() -> World: return World.new(db())

# ---------------------------------------------------------------- content

func test_database_valid() -> void:
	eq(db().validate(), "", "validate")
	eq(db().balloons.size(), 20); eq(db().worlds.size(), 10); eq(db().levels.size(), 60); eq(db().bosses.size(), 10)

func test_balloons_unique_and_costs_increase() -> void:
	var ids := {}
	for b in db().balloons:
		ok(not ids.has(b.id), "duplicate " + b.id); ids[b.id] = true
		ok(int(b.unlock_world) >= 1 and int(b.unlock_world) <= 10, b.id)
		for s in range(1, 4): ok(b.stage_costs[s] > b.stage_costs[s - 1], b.id + " stage costs")
	eq(int(db().default_balloon().price), 0, "classic price")

func test_world_structure() -> void:
	for wn in range(1, 11):
		for i in range(1, 7):
			var l := db().level((wn - 1) * 6 + i)
			eq(int(l.world), wn); eq(int(l.index), i)
			eq(l.is_boss, i == 6, "boss flag %d" % l.number)

func test_coin_targets_reachable() -> void:
	for l in db().levels:
		var max_coins: float = l.duration / maxf(0.5, l.coin_interval) * l.coins_per_pattern
		if l.is_boss: max_coins += 60
		ok(l.coins_for_second_star <= max_coins, "level %d coin target" % l.number)

func test_bosses_have_phases() -> void:
	for b in db().bosses:
		ok(b.phases.size() >= 3, b.id)
		for p in b.phases: ok(p.attacks.size() > 0, b.id + "/" + p.name)

# ---------------------------------------------------------------- save

func test_save_round_trip() -> void:
	var a := w()
	a.save.data.coins = 1234
	a.save.data.stars[3] = 2
	a.save.save_now()
	var b := SaveManager.new(a.storage, db())
	b.load_game()
	eq(int(b.data.coins), 1234, "coins"); eq(int(b.data.stars[3]), 2, "stars")
	ok(not b.recovered_from_corruption)

func test_corrupted_save_uses_backup() -> void:
	var a := w()
	a.save.data.coins = 100; a.save.save_now()
	a.save.data.coins = 200; a.save.save_now()
	a.storage.write(SaveManager.MAIN_SLOT, a.storage.read(SaveManager.MAIN_SLOT).replace("200", "999"))
	var b := SaveManager.new(a.storage, db())
	b.load_game()
	ok(b.recovered_from_corruption, "corruption detected"); ok(b.loaded_from_backup, "backup used")
	eq(int(b.data.coins), 100, "backup coins")

func test_garbage_save_starts_fresh() -> void:
	var st := SaveManager.MemoryStorage.new()
	st.write(SaveManager.MAIN_SLOT, "{not json")
	var s := SaveManager.new(st, db())
	s.load_game()
	eq(int(s.data.highest_unlocked), 1); ok(s.data.owned.has(C.DEFAULT_BALLOON))

func test_validate_repairs() -> void:
	var d := SaveManager.new_save()
	d.coins = -50; d.gems = -1; d.stars = [9, -2, 1]; d.highest_unlocked = 500
	d.equipped = "nope"; d.owned = []; d.workshop = [9]
	SaveManager.validate(d, db())
	eq(d.coins, 0); eq(d.gems, 0); eq(d.stars.size(), 60)
	for s in d.stars: ok(s >= 0 and s <= 3)
	ok(d.highest_unlocked <= 60); ok(d.owned.has(C.DEFAULT_BALLOON)); ok(d.owned.has(d.equipped))
	eq(d.workshop, [5, 0, 0, 0])

# ---------------------------------------------------------------- economy

func test_spending_never_negative() -> void:
	var a := w()
	a.save.data.coins = 10
	ok(not a.economy.try_spend_coins(11)); eq(a.economy.coins, 10)
	ok(not a.economy.try_spend_coins(-5)); ok(a.economy.try_spend_coins(10)); eq(a.economy.coins, 0)
	ok(not a.economy.try_spend_gems(1))

func test_one_time_reward() -> void:
	var a := w()
	ok(a.economy.try_grant_once("x", 0, 5)); ok(not a.economy.try_grant_once("x", 0, 5)); eq(a.economy.gems, 5)

# ---------------------------------------------------------------- progression

func test_unlocks() -> void:
	var a := w()
	ok(a.progression.is_level_unlocked(1)); ok(not a.progression.is_level_unlocked(2))
	var fail := Progression.new_result(1)
	fail.coins = 5
	a.progression.record_result(fail)
	ok(not a.progression.is_level_unlocked(2), "fail must not unlock")
	eq(a.economy.coins, 5, "coins kept on failure")
	a.win(1, 10)
	ok(a.progression.is_level_unlocked(2)); ok(a.progression.is_level_completed(1))

func test_stars() -> void:
	var l := db().level(1)
	var r := Progression.new_result(1)
	r.victory = true; r.took_damage = true
	eq(Progression.calculate_stars(l, r).count(true), 1)
	r.coins = l.coins_for_second_star
	ok(Progression.calculate_stars(l, r).count(true) >= 2)
	r.victory = false
	eq(Progression.calculate_stars(l, r).count(true), 0)

func test_no_duplicate_one_time_rewards() -> void:
	var a := w()
	for n in range(1, 7): a.win(n, 500, false)
	var gems := a.economy.gems
	a.win(6, 500, false)
	eq(a.economy.gems, gems, "replay gems")

func test_boss_unlocks_next_world() -> void:
	var a := w()
	for n in range(1, 7): a.win(n)
	ok(a.progression.is_world_completed(1)); ok(a.progression.is_world_unlocked(2)); ok(not a.progression.is_world_unlocked(3))

func test_campaign_completion() -> void:
	var a := w()
	var last := {}
	for n in range(1, 61): last = a.win(n)
	ok(a.save.data.campaign_completed); ok(last.campaign_completed_now)
	ok(not a.win(60).campaign_completed_now, "only once")
	a.achievements.evaluate()
	ok(a.achievements.is_unlocked("heaven_reached"))

# ---------------------------------------------------------------- shop & workshop

func test_purchase_rules() -> void:
	var a := w()
	var cheap := db().balloon("sunshine")
	a.save.data.coins = int(cheap.price) - 1
	eq(a.shop.try_purchase("sunshine"), Shop.Result.NOT_ENOUGH_COINS); ok(not a.shop.is_owned("sunshine"))
	a.save.data.coins = int(cheap.price)
	eq(a.shop.try_purchase("sunshine"), Shop.Result.OK); eq(a.economy.coins, 0)
	eq(a.shop.try_purchase("sunshine"), Shop.Result.ALREADY_OWNED)
	a.save.data.coins = 10000000
	eq(a.shop.try_purchase("galaxy"), Shop.Result.LOCKED)

func test_equip_and_upgrade() -> void:
	var a := w()
	eq(a.shop.try_equip("ocean"), Shop.Result.NOT_OWNED)
	eq(a.shop.try_upgrade("ocean"), Shop.Result.NOT_OWNED)
	a.save.data.coins = 10000000
	for i in 4: eq(a.shop.try_upgrade("classic"), Shop.Result.OK)
	eq(a.shop.stage("classic"), 5); eq(a.shop.try_upgrade("classic"), Shop.Result.MAX_STAGE)

func test_stages_never_reduce_stats() -> void:
	var a := w()
	for b in db().balloons:
		var s1 := a.shop.stats(b, 1)
		var s5 := a.shop.stats(b, 5)
		ok(s5.hp >= s1.hp and s5.armor >= s1.armor and s5.speed >= s1.speed, b.id)

func test_workshop() -> void:
	var a := w()
	eq(a.workshop.try_upgrade(C.Workshop.DURABILITY), WorkshopUpgrades.Result.NOT_ENOUGH)
	a.save.data.coins = 10000000
	for i in 5: eq(a.workshop.try_upgrade(C.Workshop.DURABILITY), WorkshopUpgrades.Result.OK)
	eq(a.workshop.try_upgrade(C.Workshop.DURABILITY), WorkshopUpgrades.Result.MAX_LEVEL)
	var t: Dictionary = db().trails[0]
	eq(a.workshop.try_buy_trail(t.id), WorkshopUpgrades.Result.NOT_ENOUGH)
	eq(a.workshop.try_equip_trail(t.id), WorkshopUpgrades.Result.NOT_OWNED)
	a.save.data.gems = int(t.gems)
	eq(a.workshop.try_buy_trail(t.id), WorkshopUpgrades.Result.OK); eq(a.economy.gems, 0); eq(a.workshop.equipped_trail(), t.id)

# ---------------------------------------------------------------- fairness

func test_lane_planner_keeps_corridor() -> void:
	var lp := LanePlanner.new(7, 5.4, U.rng(12345))
	var r := U.rng(99)
	for step in 5000:
		var t := step * 0.25
		if step % 2 == 0: lp.advance_corridor(t)
		ok(lp.is_free(lp.corridor, t) and lp.is_free(lp.corridor + 1, t), "corridor blocked at %d" % step)
		var count := 1 + r.randi_range(0, 2)
		var s := lp.find_placement(count, t, 3)
		if s >= 0:
			for i in range(s, s + count): ok(not lp.is_corridor_lane(i), "placement hit corridor")
			lp.block(s, count, t + 1.0 + r.randf() * 2.0)
		ok(lp.free_count(t) >= 3, "free lanes at %d" % step)

func test_full_row_keeps_gap() -> void:
	var lp := LanePlanner.new(7, 5.4, U.rng(7))
	ok(lp.try_reserve_full_row(0.0, 2.0))
	ok(lp.is_free(lp.corridor, 1.0) and lp.is_free(lp.corridor + 1, 1.0))
	ok(not lp.try_reserve_full_row(1.0, 3.0), "rows must not overlap")
