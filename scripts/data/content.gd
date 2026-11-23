class_name Content
## Builds the whole campaign as plain data: 20 balloons, 5 power-ups, obstacles, 10 bosses,
## 10 worlds, 60 levels, 5 trails and 17 achievements. Everything a designer tunes lives here.

static func build() -> GameDB:
	var db := GameDB.new()
	db.balloons = _balloons()
	db.powerups = _powerups()
	db.obstacles = _obstacles()
	db.bosses = _bosses(db.obstacles)
	db.worlds = _worlds(db.bosses)
	db.levels = _levels(db.worlds, db.obstacles)
	db.trails = _trails()
	db.achievements = _achievements()
	db.workshop = _workshop()
	db.index()
	return db

# --------------------------------------------------------------------------- balloons

static func _balloon(order: int, id: String, name: String, rarity: int, price: int, unlock: int,
		hp: int, speed: float, armor: int, magnet: float, ability: String, ability_name: String,
		ability_desc: String, shape: String, pattern: String, c1: String, c2: String, c3: String,
		ornaments: Array) -> Dictionary:
	var base_cost := maxf(price, 1200.0)
	var costs := []
	for f in [0.25, 0.55, 1.0, 1.6]:
		costs.append(int(round(base_cost * f / 50.0)) * 50)
	return {
		"order": order, "id": id, "name": name, "rarity": rarity, "price": price, "unlock_world": unlock,
		"hp": hp, "speed": speed, "armor": armor, "magnet": magnet,
		"ability": ability, "ability_name": ability_name, "ability_desc": ability_desc,
		"value": 0.0, "duration": 0.0, "cooldown": 0.0,
		"shape": shape, "pattern": pattern,
		"c1": Color(c1), "c2": Color(c2), "c3": Color(c3), "glow": Color(c1).lerp(Color.WHITE, 0.45),
		"basket": Color("#8E6B3A") if rarity >= 3 else Color("#9C6B3C"),
		"ornaments": ornaments,
		"description": "%s, a %s balloon." % [name, C.RARITY_NAMES[rarity].to_lower()],
		"stage_costs": costs,
		# Cumulative bonus of each stage (only the reached stage applies).
		"stage_bonus": [
			{"hp": 0, "speed": 0.0, "armor": 0, "magnet": 0.0},
			{"hp": 0, "speed": 0.03, "armor": 0, "magnet": 0.15},
			{"hp": 1, "speed": 0.03, "armor": 0, "magnet": 0.15},
			{"hp": 1, "speed": 0.04, "armor": 1, "magnet": 0.35},
			{"hp": 2, "speed": 0.08, "armor": 1, "magnet": 0.5},
		],
	}

static func _balloons() -> Array:
	var list := [
		_balloon(1, "classic", "Classic Balloon", 0, 0, 1, 1, 1.0, 0, 1.1, "none", "None", "A trusty basic balloon with no special ability.", "classic", "stripes", "#E8413C", "#FFF4E0", "#FFC93C", []),
		_balloon(2, "sunshine", "Sunshine Balloon", 0, 500, 1, 1, 1.0, 0, 1.1, "bonus_coins", "Sunny Fortune", "+25% coins from every pickup.", "round", "sunrays", "#FFC93C", "#FF8C42", "#FFFFFF", ["sunrays"]),
		_balloon(3, "ocean", "Ocean Balloon", 0, 1000, 1, 2, 1.0, 0, 1.1, "magnet", "Tidal Pull", "Coin magnet radius +1.2.", "round", "zigzag", "#2E86DE", "#48DBFB", "#FFFFFF", []),
		_balloon(4, "forest", "Forest Balloon", 0, 1500, 1, 2, 1.0, 0, 1.1, "wind_resist", "Deep Roots", "Wind pushes you 60% less.", "classic", "leaves", "#27AE60", "#A3E048", "#6D4C2F", ["leaves"]),
		_balloon(5, "magic", "Magic Balloon", 1, 2500, 2, 2, 1.05, 0, 1.1, "periodic_shield", "Arcane Ward", "A 2.5s shield at the start and every 18 seconds.", "tall", "stars", "#8E44AD", "#F368E0", "#FFD86B", ["stars"]),
		_balloon(6, "fireball", "Fireball Balloon", 1, 4000, 2, 2, 1.05, 0, 1.1, "fire_resist", "Flameproof", "Fire hazards cannot damage you.", "teardrop", "flames", "#E74C3C", "#F39C12", "#FFE066", ["flames"]),
		_balloon(7, "ice_crystal", "Ice Crystal Balloon", 1, 6000, 3, 3, 1.0, 0, 1.1, "env_resist", "Frost Skin", "Wind, ice, gravity and darkness effects are halved.", "round", "crystal", "#74B9FF", "#DFF9FB", "#FFFFFF", ["crystals"]),
		_balloon(8, "cloud_rider", "Cloud Rider Balloon", 1, 8000, 3, 3, 1.15, 0, 1.1, "maneuver", "Sky Glide", "+35% acceleration and +15% top speed.", "wide", "swirl", "#F5F6FA", "#81ECEC", "#74B9FF", []),
		_balloon(9, "iron_shield", "Iron Shield Balloon", 2, 12000, 4, 3, 0.95, 2, 1.1, "physical_resist", "Iron Hull", "Every second physical hit is deflected.", "round", "checker", "#636E72", "#B2BEC3", "#F1C40F", []),
		_balloon(10, "golden_king", "Golden King Balloon", 2, 16000, 4, 3, 1.0, 0, 1.2, "reward_boost", "Royal Treasury", "Level completion coin rewards +50%.", "classic", "bands", "#F1C40F", "#B7950B", "#C0392B", ["crown"]),
		_balloon(11, "rainbow", "Rainbow Balloon", 2, 22000, 5, 3, 1.05, 0, 1.2, "powerup_duration", "Prism Power", "Power-ups last 50% longer.", "round", "rainbow", "#FF6B6B", "#48DBFB", "#FFFFFF", []),
		_balloon(12, "shadow", "Shadow Balloon", 2, 28000, 5, 4, 1.1, 0, 1.2, "invisibility", "Shadow Veil", "Tap the ability button to turn invisible for 3s (16s cooldown).", "tall", "swirl", "#2D3436", "#6C5CE7", "#A29BFE", ["shadow"]),
		_balloon(13, "galaxy", "Galaxy Balloon", 3, 35000, 6, 4, 1.05, 1, 1.3, "space_resist", "Stardust Hull", "Space hazards (asteroids, meteors, drones) cannot damage you.", "round", "galaxy", "#1E1E5A", "#6C5CE7", "#FFFFFF", ["stars"]),
		_balloon(14, "saturn", "Saturn Balloon", 3, 45000, 6, 4, 1.05, 1, 1.3, "gravity_resist", "Orbital Balance", "Immune to gravity distortion and every second space hit is deflected.", "wide", "bands", "#E0A96D", "#F5E6CA", "#C0392B", ["ring"]),
		_balloon(15, "phoenix", "Phoenix Balloon", 3, 55000, 7, 4, 1.1, 1, 1.3, "phoenix", "Rebirth", "Once per level, survive a fatal hit and recover half your HP.", "teardrop", "flames", "#FF6B35", "#FFD23F", "#C0392B", ["wings", "flames"]),
		_balloon(16, "quantum", "Quantum Balloon", 3, 70000, 7, 5, 1.1, 1, 1.3, "teleport", "Quantum Leap", "Tap the ability button to blink 3 units sideways (5s cooldown).", "round", "crystal", "#00CEC9", "#0984E3", "#FFFFFF", ["orbit"]),
		_balloon(17, "angel", "Angel Balloon", 4, 90000, 8, 5, 1.1, 1, 1.4, "heavenly_shield", "Heavenly Shield", "A shield blocks one hit and recharges every 14 seconds.", "round", "solid", "#FFFFFF", "#FFEAA7", "#F1C40F", ["wings", "halo"]),
		_balloon(18, "divine_sun", "Divine Sun Balloon", 4, 120000, 8, 5, 1.1, 1, 1.4, "strong_magnet", "Solar Pull", "Huge coin magnet (+3 radius) and +25% coins from pickups.", "round", "sunrays", "#FDCB6E", "#E17055", "#FFFFFF", ["sunrays", "crown"]),
		_balloon(19, "eternal", "Eternal Balloon", 4, 160000, 9, 6, 1.15, 1, 1.4, "invulnerability", "Eternity", "Tap the ability button for 4s of invulnerability (18s cooldown).", "tall", "gradient", "#A29BFE", "#FD79A8", "#FFFFFF", ["stars", "halo"]),
		_balloon(20, "heavens_king", "Heaven's King Balloon", 4, 220000, 9, 6, 1.2, 2, 1.5, "ultimate", "Divine Aegis", "Recharging one-hit shield every 10s, one rebirth per level and +25% coins.", "classic", "bands", "#F9E79F", "#FFFFFF", "#5DADE2", ["crown", "wings", "halo"]),
	]
	for b in list:
		match b.ability:
			"bonus_coins": b.value = 0.25
			"magnet": b.value = 1.2
			"wind_resist": b.value = 0.6
			"periodic_shield": b.duration = 2.5; b.cooldown = 18.0
			"env_resist": b.value = 0.5
			"maneuver": b.value = 0.35
			"reward_boost": b.value = 0.5
			"powerup_duration": b.value = 0.5
			"invisibility": b.duration = 3.0; b.cooldown = 16.0
			"phoenix": b.value = 0.5
			"teleport": b.value = 3.0; b.cooldown = 5.0
			"heavenly_shield": b.cooldown = 14.0
			"strong_magnet": b.value = 3.0
			"invulnerability": b.duration = 4.0; b.cooldown = 18.0
			"ultimate": b.cooldown = 10.0; b.value = 0.25
	return list

# --------------------------------------------------------------------------- power-ups

static func _powerups() -> Array:
	return [
		{"type": C.PU.MAGNET, "name": "Coin Magnet", "desc": "Attracts nearby coins for 10 seconds.", "duration": 10.0, "magnitude": 4.5, "color": Color("#E84393")},
		{"type": C.PU.SHIELD, "name": "Energy Shield", "desc": "Blocks all collisions for 8 seconds.", "duration": 8.0, "magnitude": 1.0, "color": Color("#00CEC9")},
		{"type": C.PU.ROCKET, "name": "Sky Rocket", "desc": "Boosts your ascent for 5 seconds.", "duration": 5.0, "magnitude": 1.9, "color": Color("#FF7675")},
		{"type": C.PU.LIFE, "name": "Extra Life", "desc": "Restores one durability point.", "duration": 0.0, "magnitude": 1.0, "color": Color("#55EFC4")},
		{"type": C.PU.DOUBLE, "name": "Double Coins", "desc": "Doubles collected coins for 15 seconds.", "duration": 15.0, "magnitude": 2.0, "color": Color("#FDCB6E")},
	]

# --------------------------------------------------------------------------- obstacles

static func _o(m: Dictionary, id: String, name: String, visual: String, dmg: int, move: int, speed: float,
		radius: float, c1: String, c2: String, worlds: Array) -> Dictionary:
	var o := {
		"id": id, "name": name, "visual": visual, "dmg": dmg, "move": move, "speed": speed,
		"radius": radius, "box": Vector2.ZERO, "c1": Color(c1), "c2": Color(c2), "worlds": worlds,
		"damage": 1, "harmless": false, "push": 0.0, "destroy_on_hit": false,
		"amplitude": 1.0, "frequency": 1.0, "homing": 0.0, "group_min": 1, "group_max": 1, "spin": 0.0,
		"timed": false, "warn": 0.0, "active": 0.0, "cooldown": 0.0,
		"full_row": false, "gap": 0.0, "scale": 1.0,
	}
	m[id] = o
	return o

static func _box(o: Dictionary, w: float, h: float) -> Dictionary:
	o.box = Vector2(w, h)
	return o

static func _obstacles() -> Dictionary:
	var m := {}
	var P := C.Dmg.PHYSICAL
	var F := C.Dmg.FIRE
	var I := C.Dmg.ICE
	var E := C.Dmg.ELECTRIC
	var S := C.Dmg.SPACE
	var M := C.Dmg.MAGIC
	_o(m, "small_bird", "Small Bird", "small_bird", P, C.Move.CROSS, 2.6, 0.45, "#8D6E63", "#FFF3E0", [1, 2, 3])
	var eagle := _o(m, "eagle", "Eagle", "eagle", P, C.Move.HOMING, 3.0, 0.7, "#5D4037", "#FFFFFF", [1, 2])
	eagle.homing = 1.4; eagle.scale = 1.25
	var swarm := _o(m, "butterfly_swarm", "Butterfly Swarm", "butterfly", P, C.Move.SWARM, 1.4, 0.33, "#FD79A8", "#FDCB6E", [3])
	swarm.group_min = 4; swarm.group_max = 6; swarm.amplitude = 1.1
	var rock := _o(m, "falling_rock", "Falling Rock", "rock", P, C.Move.FALLING, 2.4, 0.6, "#7F8C8D", "#95A5A6", [2])
	rock.spin = 90.0; rock.destroy_on_hit = true
	var cliff := _box(_o(m, "cliff_passage", "Narrow Passage", "cliff", P, C.Move.STATIC, 0.0, 0.8, "#8D8D8D", "#5E5E5E", [2]), 4.0, 1.6)
	cliff.full_row = true; cliff.gap = 3.4
	var wind := _box(_o(m, "wind_current", "Wind Current", "wind", P, C.Move.STATIC, 0.0, 1.0, "#DFF9FB", "#C7ECEE", [1, 2, 3, 5, 10]), 3.2, 3.6)
	wind.harmless = true; wind.push = 7.0
	var platform := _box(_o(m, "moving_platform", "Moving Platform", "platform", P, C.Move.PATROL, 1.6, 0.5, "#55EFC4", "#81ECEC", [3, 8]), 2.4, 0.6)
	platform.amplitude = 2.0
	var dark := _o(m, "dark_cloud", "Storm Cloud", "dark_cloud", E, C.Move.PATROL, 0.8, 0.85, "#2D3436", "#636E72", [4])
	dark.amplitude = 1.0
	var lightning := _box(_o(m, "lightning_cloud", "Lightning Cloud", "lightning", E, C.Move.STATIC, 0.0, 0.5, "#FFF200", "#3D3D5C", [4, 10]), 0.8, 6.5)
	lightning.timed = true; lightning.warn = 1.1; lightning.active = 0.6; lightning.cooldown = 2.2
	_o(m, "electric_bird", "Electric Bird", "electric_bird", E, C.Move.CROSS, 3.3, 0.5, "#0984E3", "#FFF200", [4])
	var frozen := _o(m, "frozen_bird", "Frozen Bird", "frozen_bird", I, C.Move.SWARM, 1.8, 0.42, "#74B9FF", "#FFFFFF", [5])
	frozen.group_min = 3; frozen.group_max = 5; frozen.amplitude = 1.0
	var snowball := _o(m, "snowball", "Snowball", "snowball", I, C.Move.FALLING, 3.0, 0.5, "#FFFFFF", "#DFE6E9", [5])
	snowball.spin = 120.0; snowball.destroy_on_hit = true
	var ice := _o(m, "ice_crystal", "Ice Crystal", "ice_crystal", I, C.Move.BOB, 0.6, 0.62, "#A5F3FF", "#74B9FF", [5])
	ice.spin = 45.0; ice.amplitude = 0.5
	_o(m, "burning_bird", "Burning Bird", "burning_bird", F, C.Move.CROSS, 3.6, 0.5, "#E17055", "#FDCB6E", [6])
	var fireball := _o(m, "fireball", "Fireball", "fireball", F, C.Move.DIAGONAL, 3.8, 0.48, "#FF7675", "#FDCB6E", [6])
	fireball.destroy_on_hit = true
	var lava := _box(_o(m, "lava_column", "Lava Column", "lava", F, C.Move.STATIC, 0.0, 0.5, "#FF5E13", "#FFC048", [6]), 1.0, 5.0)
	lava.timed = true; lava.warn = 1.2; lava.active = 1.2; lava.cooldown = 1.9
	var asteroid := _o(m, "asteroid", "Small Asteroid", "asteroid", S, C.Move.DIAGONAL, 1.4, 0.55, "#6D6875", "#B5838D", [7])
	asteroid.spin = 60.0
	var meteor := _o(m, "meteorite", "Meteorite", "meteor", S, C.Move.DIAGONAL, 6.0, 0.45, "#E84118", "#FBC531", [7, 10])
	meteor.destroy_on_hit = true
	var drone := _o(m, "alien_drone", "Alien Drone", "drone", S, C.Move.HOMING, 2.4, 0.55, "#A3CB38", "#C4E538", [7])
	drone.homing = 2.0
	var temple := _box(_o(m, "temple_block", "Temple Block", "temple", P, C.Move.PATROL, 1.2, 0.6, "#F6E58D", "#BA8B02", [8]), 1.8, 1.2)
	temple.amplitude = 1.8
	var barrier := _box(_o(m, "magic_barrier", "Magical Barrier", "barrier", M, C.Move.STATIC, 0.0, 0.3, "#E056FD", "#F9CA24", [8, 10]), 4.2, 0.45)
	barrier.timed = true; barrier.warn = 0.9; barrier.active = 1.6; barrier.cooldown = 1.6
	var guardian := _o(m, "guardian_orb", "Cloud Guardian", "guardian", M, C.Move.HOMING, 1.8, 0.6, "#F9CA24", "#FFFFFF", [8, 10])
	guardian.homing = 1.1
	var debris := _o(m, "temple_debris", "Temple Debris", "rock", P, C.Move.FALLING, 3.0, 0.55, "#E1B12C", "#FBC531", [8])
	debris.spin = 80.0; debris.destroy_on_hit = true
	_o(m, "celestial_bird", "Celestial Bird", "celestial_bird", M, C.Move.CROSS, 3.7, 0.5, "#DFE6E9", "#81ECEC", [9, 10])
	var orb := _o(m, "energy_orb", "Energy Orb", "orb", M, C.Move.CIRCULAR, 1.8, 0.45, "#A29BFE", "#FFFFFF", [9, 10])
	orb.amplitude = 1.4
	var cb := _box(_o(m, "crystal_barrier", "Crystal Barrier", "crystal_barrier", M, C.Move.PATROL, 1.2, 0.4, "#81ECEC", "#A29BFE", [9]), 2.6, 0.5)
	cb.amplitude = 1.5
	var shard := _o(m, "crystal_shard", "Crystal Shard", "shard", M, C.Move.FALLING, 3.6, 0.4, "#81ECEC", "#DFE6E9", [9])
	shard.spin = 150.0; shard.destroy_on_hit = true
	var gate := _box(_o(m, "golden_gate", "Golden Gate", "gate", M, C.Move.STATIC, 0.0, 0.6, "#F9CA24", "#FFF7D6", [10]), 4.0, 1.2)
	gate.full_row = true; gate.gap = 3.2

	# Boss projectiles and boss-only hazards
	var proj := [
		["feather", "Guardian Feather", "feather", P, 0.35, "#F6E58D", "#6AB04C"],
		["boss_rock", "Boulder", "rock", P, 0.75, "#636E72", "#B2BEC3"],
		["pollen_orb", "Pollen Orb", "orb", M, 0.38, "#FD79A8", "#FFEAA7"],
		["spark", "Storm Spark", "orb", E, 0.38, "#FFF200", "#74B9FF"],
		["ice_shard", "Ice Shard", "shard", I, 0.38, "#DFF9FB", "#74B9FF"],
		["boss_fireball", "Phoenix Flame", "fireball", F, 0.45, "#FF5E13", "#FFC048"],
		["boss_meteor", "Cosmic Meteor", "meteor", S, 0.5, "#9C88FF", "#E84118"],
		["cosmic_orb", "Void Orb", "orb", S, 0.4, "#8C7AE6", "#00A8FF"],
		["golden_orb", "Golden Orb", "guardian", M, 0.42, "#F9CA24", "#FFFFFF"],
		["light_orb", "Light Orb", "orb", M, 0.4, "#FFFFFF", "#F9E79F"],
	]
	for p in proj:
		var o := _o(m, p[0], p[1], p[2], p[3], C.Move.FALLING, 5.0, p[4], p[5], p[6], [])
		o.destroy_on_hit = true; o.spin = 120.0
	for c in [["storm_column", "Thunder Strike", E, "#FFF200", "#74B9FF"], ["lava_burst", "Lava Burst", F, "#FF5E13", "#FFC048"],
			["light_column", "Pillar of Light", M, "#FFFFFF", "#F9E79F"], ["rock_slide", "Rock Slide", P, "#B2BEC3", "#636E72"]]:
		var col := _box(_o(m, c[0], c[1], "beam", c[2], C.Move.STATIC, 0.0, 0.5, c[3], c[4], []), 1.1, 30.0)
		col.timed = true; col.warn = 1.0; col.active = 0.9; col.cooldown = 999.0
	var ice_wall := _box(_o(m, "ice_wall", "Ice Wall", "cliff", I, C.Move.STATIC, 0.0, 0.8, "#C7ECEE", "#74B9FF", []), 4.0, 1.2)
	ice_wall.full_row = true; ice_wall.gap = 3.3
	var flame_wall := _box(_o(m, "flame_wall", "Wall of Fire", "cliff", F, C.Move.STATIC, 0.0, 0.8, "#E17055", "#FDCB6E", []), 4.0, 1.0)
	flame_wall.full_row = true; flame_wall.gap = 3.3
	return m

# --------------------------------------------------------------------------- bosses

static func _atk(type: int, projectile: String, count: int, cooldown: float, speed: float, telegraph := 0.9) -> Dictionary:
	return {"type": type, "projectile": projectile, "count": count, "cooldown": cooldown, "speed": speed, "telegraph": telegraph}

static func _phase(name: String, threshold: float, move: int, speed: float, attacks: Array) -> Dictionary:
	return {"name": name, "threshold": threshold, "move": move, "move_speed": speed, "attacks": attacks}

static func _bosses(o: Dictionary) -> Array:
	var Fan := C.Attack.FAN
	var Aim := C.Attack.AIMED
	var Col := C.Attack.COLUMN
	var Rain := C.Attack.RAIN
	var Sum := C.Attack.SUMMON
	var Wall := C.Attack.WALL
	var Spi := C.Attack.SPIRAL
	var SW := C.BossMove.SWEEP
	var F8 := C.BossMove.FIGURE8
	var TR := C.BossMove.TRACK
	var HO := C.BossMove.HOVER
	var TP := C.BossMove.TELEPORT
	var defs := [
		["valley_guardian", "Valley Guardian", "Great Eagle of the Valley", "#6AB04C", "#F6E58D", "#BADC58", 40.0, 1.0, [
			_phase("Gliding Watch", 1.0, SW, 2.0, [_atk(Fan, "feather", 5, 3.4, 4.5)]),
			_phase("Call of the Flock", 0.66, SW, 2.8, [_atk(Fan, "feather", 5, 3.2, 5.0), _atk(Sum, "small_bird", 2, 5.0, 0.0)]),
			_phase("Feather Storm", 0.33, F8, 2.4, [_atk(Rain, "feather", 6, 3.4, 5.0), _atk(Fan, "feather", 6, 4.0, 5.5)])]],
		["mountain_king", "Mountain King", "Stone Crown of the Peaks", "#7F8C8D", "#F1C40F", "#E67E22", 44.0, 1.1, [
			_phase("Rumbling Throne", 1.0, HO, 1.2, [_atk(Rain, "boss_rock", 5, 3.2, 4.5)]),
			_phase("Landslide", 0.66, TR, 1.6, [_atk(Col, "rock_slide", 2, 3.6, 0.0, 1.1), _atk(Rain, "boss_rock", 4, 4.0, 5.0)]),
			_phase("Mountain's Wrath", 0.33, SW, 2.2, [_atk(Wall, "cliff_passage", 1, 4.2, 0.0), _atk(Fan, "boss_rock", 4, 3.6, 5.0)])]],
		["butterfly_queen", "Sky Butterfly Queen", "Monarch of Rainbow Skies", "#FD79A8", "#74B9FF", "#FFEAA7", 46.0, 1.0, [
			_phase("Royal Dance", 1.0, F8, 2.2, [_atk(Sum, "butterfly_swarm", 1, 4.5, 0.0), _atk(Fan, "pollen_orb", 5, 3.6, 4.5)]),
			_phase("Pollen Spiral", 0.66, F8, 2.6, [_atk(Spi, "pollen_orb", 8, 4.2, 3.2)]),
			_phase("Rainbow Tempest", 0.33, SW, 3.0, [_atk(Spi, "pollen_orb", 9, 4.6, 3.5), _atk(Sum, "butterfly_swarm", 1, 5.5, 0.0), _atk(Rain, "pollen_orb", 5, 4.0, 4.5)])]],
		["thunder_dragon", "Thunder Dragon", "Lord of the Storm", "#273C75", "#FBC531", "#00A8FF", 50.0, 1.15, [
			_phase("Storm Flight", 1.0, SW, 2.4, [_atk(Col, "storm_column", 2, 3.2, 0.0, 1.0)]),
			_phase("Electric Brood", 0.66, SW, 3.4, [_atk(Col, "storm_column", 2, 3.4, 0.0, 0.95), _atk(Sum, "electric_bird", 2, 4.5, 0.0)]),
			_phase("Great Tempest", 0.33, F8, 3.0, [_atk(Col, "storm_column", 3, 3.6, 0.0, 1.0), _atk(Rain, "spark", 6, 3.4, 5.0)])]],
		["ice_guardian", "Ice Guardian", "Warden of the Frozen Heaven", "#74B9FF", "#DFF9FB", "#A5F3FF", 52.0, 1.1, [
			_phase("Frost Gaze", 1.0, HO, 1.4, [_atk(Fan, "ice_shard", 5, 3.0, 5.0)]),
			_phase("Blizzard", 0.66, SW, 2.0, [_atk(Rain, "snowball", 6, 3.2, 4.0), _atk(Fan, "ice_shard", 5, 4.0, 5.5)]),
			_phase("Glacial Wall", 0.33, TR, 2.0, [_atk(Wall, "ice_wall", 1, 4.0, 0.0), _atk(Aim, "ice_shard", 3, 3.4, 6.0)])]],
		["fire_phoenix", "Fire Phoenix", "Eternal Flame of the Volcano", "#E84118", "#FBC531", "#FF9F1A", 54.0, 1.15, [
			_phase("Blazing Wings", 1.0, SW, 2.6, [_atk(Aim, "boss_fireball", 3, 2.8, 6.0)]),
			_phase("Eruption", 0.66, F8, 2.6, [_atk(Col, "lava_burst", 2, 3.4, 0.0, 1.05), _atk(Aim, "boss_fireball", 3, 3.6, 6.0)]),
			_phase("Rebirth Inferno", 0.33, SW, 3.4, [_atk(Rain, "boss_fireball", 7, 3.2, 5.0), _atk(Wall, "flame_wall", 1, 5.0, 0.0)])]],
		["cosmic_guardian", "Cosmic Guardian", "Eye Between the Stars", "#192A56", "#9C88FF", "#00D2D3", 56.0, 1.1, [
			_phase("Starfall", 1.0, TP, 2.0, [_atk(Rain, "boss_meteor", 6, 3.0, 5.5)]),
			_phase("Drone Fleet", 0.66, TP, 2.4, [_atk(Sum, "alien_drone", 2, 5.0, 0.0), _atk(Fan, "cosmic_orb", 6, 3.6, 4.5)]),
			_phase("Event Horizon", 0.33, F8, 2.8, [_atk(Spi, "cosmic_orb", 10, 4.2, 3.4), _atk(Rain, "boss_meteor", 5, 3.8, 6.0)])]],
		["golden_titan", "Golden Titan", "Colossus of the Golden Temples", "#F9CA24", "#B7950B", "#FFF7D6", 58.0, 1.25, [
			_phase("Temple Guard", 1.0, HO, 1.4, [_atk(Wall, "golden_gate", 1, 4.0, 0.0), _atk(Fan, "golden_orb", 5, 3.6, 4.5)]),
			_phase("Golden Barrage", 0.66, TR, 1.8, [_atk(Fan, "golden_orb", 7, 3.0, 5.0), _atk(Rain, "temple_debris", 5, 3.6, 4.5)]),
			_phase("Collapse", 0.33, SW, 2.2, [_atk(Wall, "golden_gate", 1, 4.2, 0.0), _atk(Rain, "temple_debris", 6, 3.2, 5.0), _atk(Aim, "golden_orb", 3, 3.8, 6.0)])]],
		["celestial_guardian", "Celestial Guardian", "Keeper of the Crystal Light", "#A29BFE", "#81ECEC", "#FFFFFF", 60.0, 1.15, [
			_phase("Orbit of Light", 1.0, F8, 2.0, [_atk(Spi, "light_orb", 8, 3.8, 3.3)]),
			_phase("Crystal Storm", 0.66, SW, 2.6, [_atk(Rain, "crystal_shard", 7, 3.0, 5.0), _atk(Col, "light_column", 2, 4.0, 0.0, 1.0)]),
			_phase("Light and Shadow", 0.33, TP, 2.8, [_atk(Col, "light_column", 3, 3.6, 0.0, 1.0), _atk(Spi, "light_orb", 10, 4.4, 3.6)])]],
		["heavens_guardian", "Heaven's Guardian", "Seraph of the Final Gate", "#FFFFFF", "#F9CA24", "#FFF7D6", 70.0, 1.3, [
			_phase("Judgement", 1.0, SW, 2.2, [_atk(Fan, "light_orb", 6, 3.2, 5.0), _atk(Col, "light_column", 2, 4.2, 0.0, 1.0)]),
			_phase("Heavenly Host", 0.75, F8, 2.6, [_atk(Sum, "celestial_bird", 2, 4.5, 0.0), _atk(Spi, "light_orb", 9, 4.2, 3.4)]),
			_phase("Golden Gates", 0.5, TR, 2.2, [_atk(Wall, "golden_gate", 1, 3.8, 0.0), _atk(Aim, "light_orb", 3, 3.4, 6.0)]),
			_phase("Final Ascent", 0.25, SW, 3.2, [_atk(Col, "light_column", 3, 3.6, 0.0, 1.0), _atk(Spi, "light_orb", 10, 4.6, 3.6), _atk(Rain, "boss_meteor", 5, 4.2, 5.5)])]],
	]
	var list := []
	for i in defs.size():
		var d: Array = defs[i]
		list.append({
			"world": i + 1, "id": d[0], "name": d[1], "title": d[2],
			"c1": Color(d[3]), "c2": Color(d[4]), "glow": Color(d[5]),
			"duration": d[6], "scale": d[7], "phases": d[8],
			"reward_coins": 2000 if i == 9 else 500, "reward_gems": 25 if i == 9 else 5,
			"crystal_damage": 0.06, "crystal_interval": 6.0 if i == 9 else 5.5,
		})
	return list

# --------------------------------------------------------------------------- worlds

static func _worlds(bosses: Array) -> Array:
	var rows := [
		[1, "Green Valley", "#4FA3F7", "#CDEBFF", "#FFFFFF", "#6AB04C", "#F6E58D", "#8FA7BF", "#FFF6E0", "pollen", 96, 60, "major", "triangle", 0.35, 0.6, "Peaceful green landscapes, floating grass islands and fluffy clouds."],
		[2, "Mountain Kingdom", "#5B7DB1", "#F8C9A9", "#FDF2F8", "#95A5A6", "#E67E22", "#8C8FA8", "#FFE8D0", "none", 104, 57, "dorian", "square", 0.45, 0.5, "Tall peaks, steep cliffs and drifting rocks."],
		[3, "Rainbow Skies", "#7ED6DF", "#FFD3E8", "#FFFFFF", "#7BED9F", "#FF6B81", "#A6A0C8", "#FFF7FB", "petals", 112, 62, "lydian", "bell", 0.5, 0.8, "Colorful floating islands, rainbows and waterfalls."],
		[4, "Thunderstorm", "#1E2235", "#4B5574", "#5D6584", "#2F3640", "#FBC531", "#3E4560", "#C7D3FF", "rain", 124, 55, "minor", "square", 0.75, 0.35, "Dark clouds, lightning, rain and powerful storms."],
		[5, "Frozen Heaven", "#6AB7FF", "#EAF6FF", "#F5FBFF", "#DFF9FB", "#74B9FF", "#9DB8D6", "#F0F8FF", "snow", 92, 64, "pentatonic", "bell", 0.3, 0.7, "Snow, frozen islands and shimmering ice crystals."],
		[6, "Fire Kingdom", "#5A1A1A", "#FF8C42", "#FFB27A", "#3D1F1F", "#FF5E13", "#8A4A3A", "#FFD2A6", "embers", 132, 52, "harmonic_minor", "square", 0.85, 0.45, "Volcanoes, lava rivers and burning orange clouds."],
		[7, "Cosmic Journey", "#05051A", "#2C1A5A", "#4B3B8C", "#1E1E3F", "#00D2D3", "#3A3A6A", "#D6E4FF", "stars", 100, 57, "dorian", "sine", 0.4, 0.55, "Outer space, planets, galaxies and asteroid fields."],
		[8, "Golden Clouds", "#F5B041", "#FFF3D6", "#FFF8E7", "#F6E58D", "#B7950B", "#C7A97A", "#FFF1CC", "gold", 108, 65, "major", "bell", 0.45, 0.75, "Golden floating temples and ancient structures."],
		[9, "Celestial Realm", "#3B2C85", "#C3B1E1", "#E9E1FF", "#A29BFE", "#81ECEC", "#8B80C8", "#F3EEFF", "motes", 98, 62, "lydian", "sine", 0.35, 0.7, "Crystal islands, heavenly light and magical energy."],
		[10, "Heaven's Gate", "#FFE9A8", "#FFFFFF", "#FFFFFF", "#FFF7D6", "#F9CA24", "#D9CBA8", "#FFFBEA", "gold", 88, 60, "major", "bell", 0.3, 0.9, "Golden gates, magnificent castles and divine light."],
	]
	var themes := ["valley", "mountain", "rainbow", "storm", "frozen", "fire", "cosmic", "golden", "celestial", "heaven"]
	var list := []
	for r in rows:
		var n: int = r[0]
		list.append({
			"number": n, "name": r[1], "theme": themes[n - 1], "description": r[16],
			"sky_top": Color(r[2]), "sky_bottom": Color(r[3]), "cloud": Color(r[4]), "ground": Color(r[5]),
			"accent": Color(r[6]), "ambient": Color(r[7]), "light": Color(r[8]), "weather": r[9],
			"music": {"tempo": r[10], "root": r[11], "scale": r[12], "lead": r[13], "percussion": r[14], "brightness": r[15], "seed": n * 7919},
			"boss": bosses[n - 1].id, "completion_coins": 1000, "completion_gems": 50 if n == 10 else 10,
		})
	return list

# --------------------------------------------------------------------------- levels

static func _spec(name: String, hint: String, obstacles: Array, extra := {}) -> Dictionary:
	var s := {"name": name, "hint": hint, "obstacles": obstacles, "wind": C.Wind.NONE, "wind_strength": 0.0,
		"darkness": 0.0, "slippery": 0.0, "gravity": 0.0, "light_shadow": false, "extra_coins": 0, "extra_special": 0}
	s.merge(extra, true)
	return s

static func _table() -> Array:
	var G := C.Wind.GUSTS
	var UN := C.Wind.UNPREDICTABLE
	return [
		[ # World 1 - Green Valley
			_spec("First Flight", "Drag anywhere to steer your balloon. Collect the coins!", [], {"extra_coins": 1}),
			_spec("Bird Crossing", "Birds fly across the sky. Watch their path and slip past.", ["small_bird:1"]),
			_spec("Coin Trail", "Follow the coin trails and grab every star coin.", ["small_bird:1"], {"extra_coins": 3, "extra_special": 2}),
			_spec("Forest Wind", "Wind currents push you sideways. Steer against them!", ["small_bird:1", "wind_current:0.8"], {"wind": G, "wind_strength": 1.2}),
			_spec("Eagle Attack", "Eagles dive towards you. Move late to dodge them.", ["small_bird:1", "eagle:0.9@0.1"]),
			_spec("Valley Guardian", "Survive the Guardian's attacks. Collect Sky Crystals to weaken it!", ["small_bird:1", "eagle:0.4"]),
		],
		[ # World 2 - Mountain Kingdom
			_spec("Mountain Entrance", "Welcome to the peaks. The air gets busier up here.", ["small_bird:1", "eagle:0.5"]),
			_spec("Falling Rocks", "Rocks tumble down from the cliffs. Keep moving!", ["falling_rock:1.2", "small_bird:0.5"]),
			_spec("Narrow Passage", "Fly through the gaps between the cliffs.", ["cliff_passage:0.8", "falling_rock:0.6"]),
			_spec("Strong Winds", "Mountain gales are strong. Fight the current!", ["wind_current:1", "falling_rock:0.8", "small_bird:0.5"], {"wind": G, "wind_strength": 2.2}),
			_spec("Eagle Territory", "This is eagle land. Stay alert.", ["eagle:1.2", "falling_rock:0.7", "cliff_passage:0.4"]),
			_spec("Mountain King", "The Mountain King awakens!", ["falling_rock:1", "eagle:0.5"]),
		],
		[ # World 3 - Rainbow Skies
			_spec("Rainbow Gate", "Colorful skies, colorful dangers.", ["small_bird:1", "butterfly_swarm:0.4"]),
			_spec("Butterfly Swarm", "Butterflies travel in swarms. Find the gaps.", ["butterfly_swarm:1.2", "small_bird:0.4"]),
			_spec("Moving Platforms", "Floating platforms slide back and forth.", ["moving_platform:1.1", "butterfly_swarm:0.5"]),
			_spec("Wind Tunnels", "The wind changes direction without warning!", ["wind_current:1", "moving_platform:0.8", "butterfly_swarm:0.5"], {"wind": UN, "wind_strength": 2.0}),
			_spec("Rainbow Storm", "Survive the storm of colors.", ["butterfly_swarm:1", "moving_platform:0.8", "small_bird:0.7", "wind_current:0.4"], {"wind": UN, "wind_strength": 1.6}),
			_spec("Sky Butterfly Queen", "The Queen of the skies descends!", ["butterfly_swarm:1", "moving_platform:0.4"]),
		],
		[ # World 4 - Thunderstorm
			_spec("Dark Clouds", "Storm clouds crackle with electricity.", ["dark_cloud:1", "small_bird:0.4"], {"darkness": 0.2}),
			_spec("Lightning Strikes", "Lightning flashes before it strikes. Move away from the glow!", ["lightning_cloud:1.1", "dark_cloud:0.5"], {"darkness": 0.25}),
			_spec("Electric Birds", "Charged birds zip across the storm.", ["electric_bird:1.1", "lightning_cloud:0.6"], {"darkness": 0.3}),
			_spec("Storm Corridors", "Weave through corridors of thunderclouds.", ["dark_cloud:1", "lightning_cloud:0.9", "electric_bird:0.5"], {"wind": G, "wind_strength": 1.4, "darkness": 0.35}),
			_spec("Thunder Survival", "Hold on through the heart of the storm!", ["lightning_cloud:1", "electric_bird:1", "dark_cloud:0.7"], {"wind": UN, "wind_strength": 1.5, "darkness": 0.4}),
			_spec("Thunder Dragon", "The Thunder Dragon rules this storm!", ["electric_bird:0.8", "dark_cloud:0.6"], {"darkness": 0.3}),
		],
		[ # World 5 - Frozen Heaven
			_spec("Frozen Entrance", "Icy air makes your balloon slide a little.", ["frozen_bird:0.6", "ice_crystal:0.8"], {"slippery": 0.25}),
			_spec("Snowball Rain", "Snowballs fall from the frozen clouds.", ["snowball:1.2", "ice_crystal:0.5"], {"slippery": 0.3}),
			_spec("Ice Crystals", "Sharp crystals float in the air.", ["ice_crystal:1.2", "snowball:0.6"], {"slippery": 0.35}),
			_spec("Freezing Winds", "Freezing winds and slippery air!", ["wind_current:0.9", "snowball:1", "ice_crystal:0.5"], {"wind": G, "wind_strength": 1.8, "slippery": 0.45}),
			_spec("Frozen Bird Swarm", "Flocks of frozen birds fill the sky.", ["frozen_bird:1.3", "snowball:0.8", "ice_crystal:0.5"], {"slippery": 0.4}),
			_spec("Ice Guardian", "The Ice Guardian blocks the way!", ["snowball:0.8", "ice_crystal:0.5"], {"slippery": 0.3}),
		],
		[ # World 6 - Fire Kingdom
			_spec("Volcanic Entrance", "The air is burning hot. Watch for embers.", ["burning_bird:0.7", "fireball:0.6"]),
			_spec("Fireballs", "Fireballs streak across the sky.", ["fireball:1.3", "burning_bird:0.4"]),
			_spec("Lava Columns", "Lava erupts where the ground glows.", ["lava_column:1.1", "fireball:0.6"]),
			_spec("Burning Birds", "Birds of flame dart through the smoke.", ["burning_bird:1.2", "lava_column:0.6", "fireball:0.5"]),
			_spec("Volcanic Eruption", "The volcano erupts! Survive the inferno.", ["fireball:1.3", "lava_column:0.9", "burning_bird:0.8"], {"darkness": 0.15}),
			_spec("Fire Phoenix", "The immortal Phoenix rises!", ["fireball:0.8", "burning_bird:0.5"]),
		],
		[ # World 7 - Cosmic Journey
			_spec("Space Entrance", "You have left the sky behind. Welcome to space!", ["asteroid:1"]),
			_spec("Small Asteroids", "Drifting asteroids everywhere.", ["asteroid:1.4", "meteorite:0.3"]),
			_spec("Meteorite Shower", "Meteorites streak down at high speed!", ["meteorite:1.3", "asteroid:0.6"]),
			_spec("Alien Drones", "Drones track your position. Dodge at the last moment.", ["alien_drone:1.1", "asteroid:0.7"]),
			_spec("Gravity Distortion", "Gravity wells pull you sideways!", ["asteroid:1", "meteorite:0.8", "alien_drone:0.6"], {"gravity": 0.6}),
			_spec("Cosmic Guardian", "The Cosmic Guardian watches from the void.", ["asteroid:0.8", "meteorite:0.4"], {"gravity": 0.3}),
		],
		[ # World 8 - Golden Clouds
			_spec("Golden Entrance", "Ancient temples float among golden clouds.", ["temple_block:1", "moving_platform:0.4"]),
			_spec("Floating Temple", "Temple structures slide through the sky.", ["temple_block:1.3", "temple_debris:0.4"]),
			_spec("Magical Barriers", "Barriers flash before they activate.", ["magic_barrier:1.1", "temple_block:0.6"]),
			_spec("Cloud Guardians", "Golden guardians pursue intruders.", ["guardian_orb:1.1", "magic_barrier:0.6"]),
			_spec("Temple Collapse", "The temple is falling apart!", ["temple_debris:1.3", "temple_block:0.8", "magic_barrier:0.6"]),
			_spec("Golden Titan", "The Golden Titan awakens!", ["temple_debris:0.8", "guardian_orb:0.5"]),
		],
		[ # World 9 - Celestial Realm
			_spec("Celestial Entrance", "Heavenly light fills the crystal realm.", ["celestial_bird:1", "energy_orb:0.4"]),
			_spec("Energy Orbs", "Orbs of energy circle their anchors.", ["energy_orb:1.3", "celestial_bird:0.4"]),
			_spec("Crystal Storm", "Crystal shards rain down!", ["crystal_shard:1.2", "crystal_barrier:0.8"]),
			_spec("Celestial Birds", "Swift celestial birds dart across the light.", ["celestial_bird:1.3", "energy_orb:0.6", "crystal_barrier:0.4"]),
			_spec("Light and Shadow", "Shadows hide the danger. Trust the light!", ["energy_orb:1", "crystal_shard:0.8", "celestial_bird:0.7"], {"light_shadow": true}),
			_spec("Celestial Guardian", "The Celestial Guardian tests your soul.", ["energy_orb:0.8", "crystal_shard:0.5"]),
		],
		[ # World 10 - Heaven's Gate
			_spec("Heavenly Entrance", "Heaven's Gate is near. Stay strong.", ["celestial_bird:1", "energy_orb:0.6"]),
			_spec("Golden Gates", "Pass through the golden gates.", ["golden_gate:0.9", "guardian_orb:0.6", "celestial_bird:0.5"]),
			_spec("Guardian Trials", "The guardians test every traveller.", ["guardian_orb:1.1", "magic_barrier:0.8", "energy_orb:0.6"]),
			_spec("Endless Storm", "A final storm guards the heavens.", ["lightning_cloud:1", "meteorite:0.8", "wind_current:0.5", "celestial_bird:0.6"], {"wind": UN, "wind_strength": 1.6, "darkness": 0.3}),
			_spec("Final Ascent", "Everything you have learned leads to this.", ["golden_gate:0.6", "guardian_orb:0.8", "magic_barrier:0.7", "energy_orb:0.8", "meteorite:0.6"], {"wind": G, "wind_strength": 1.2}),
			_spec("Heaven's Guardian", "Face Heaven's Guardian and reach the Gate!", ["celestial_bird:0.8", "energy_orb:0.6"]),
		],
	]

static func _levels(worlds: Array, obstacles: Dictionary) -> Array:
	var table := _table()
	var roles := [C.Role.INTRO, C.Role.NEW_OBSTACLE, C.Role.HARDER, C.Role.COMBINATION, C.Role.SURVIVAL, C.Role.BOSS]
	var levels := []
	for w in C.WORLDS:
		for i in C.LEVELS_PER_WORLD:
			var number := w * C.LEVELS_PER_WORLD + i + 1
			var spec: Dictionary = table[w][i]
			var role: int = roles[i]
			var d := float(number - 1) / float(C.TOTAL_LEVELS - 1)
			var duration := lerpf(42.0, 66.0, d)
			if role == C.Role.INTRO: duration *= 0.9
			if role == C.Role.SURVIVAL: duration *= 1.18
			if role == C.Role.BOSS: duration = 18.0 # approach before the boss appears
			var interval := lerpf(2.5, 1.05, d)
			if role == C.Role.INTRO: interval *= 1.25
			if role == C.Role.SURVIVAL: interval *= 0.82
			if role == C.Role.BOSS: interval *= 1.3
			var entries := []
			for e in spec.obstacles:
				var parts: PackedStringArray = String(e).split(":")
				var id := parts[0]
				var weight := 1.0
				var start := 0.0
				if parts.size() > 1:
					var ws: PackedStringArray = parts[1].split("@")
					weight = ws[0].to_float()
					if ws.size() > 1: start = ws[1].to_float()
				if obstacles.has(id):
					entries.append({"id": id, "weight": weight, "start": start})
				else:
					push_warning("Unknown obstacle " + id)
			var powerups := [{"type": C.PU.MAGNET, "weight": 1.0}]
			if number >= 3: powerups.append({"type": C.PU.DOUBLE, "weight": 0.9})
			if number >= 4: powerups.append({"type": C.PU.SHIELD, "weight": 1.0})
			if number >= 5: powerups.append({"type": C.PU.LIFE, "weight": 0.6})
			if number >= 7: powerups.append({"type": C.PU.ROCKET, "weight": 0.7})
			var level := {
				"number": number, "world": w + 1, "index": i + 1, "name": spec.name, "hint": spec.hint,
				"role": role, "difficulty": d,
				"ascent_speed": lerpf(3.0, 5.2, d), "duration": round(duration),
				"obstacles": entries, "interval_start": interval,
				"interval_end": interval * (0.65 if role == C.Role.SURVIVAL else 0.78),
				"max_obstacles": 3 + int(round(d * 7.0)) + (2 if role == C.Role.SURVIVAL else 0),
				"obstacle_speed": 1.0 + d * 0.55, "min_free_lanes": 3 if d < 0.35 else 2,
				"coin_interval": lerpf(2.2, 2.9, d), "coins_per_pattern": 5 + int(round(d * 2.0)) + int(spec.extra_coins),
				"special_coins": 3 + int(spec.extra_special),
				"powerup_interval": lerpf(11.0, 15.0, d), "powerups": powerups,
				"wind": spec.wind, "wind_strength": spec.wind_strength, "darkness": spec.darkness,
				"slippery": spec.slippery, "gravity": spec.gravity, "light_shadow": spec.light_shadow,
				"is_boss": role == C.Role.BOSS, "boss": worlds[w].boss if role == C.Role.BOSS else "",
			}
			var estimated := int(round(level.duration / level.coin_interval * level.coins_per_pattern * 0.85))
			if role == C.Role.BOSS: estimated += 30
			level["coins_for_second_star"] = maxi(5, int(round(estimated * 0.5)))
			level["challenge_value"] = 0
			match role:
				C.Role.INTRO:
					level["challenge"] = C.Challenge.COIN_TARGET
					level["challenge_value"] = int(round(estimated * 0.75))
				C.Role.NEW_OBSTACLE, C.Role.SURVIVAL:
					level["challenge"] = C.Challenge.NO_DAMAGE
				C.Role.COMBINATION:
					level["challenge"] = C.Challenge.POWERUPS
					level["challenge_value"] = 2
				_:
					level["challenge"] = C.Challenge.ALL_SPECIAL
			level["length"] = maxf(1.0, level.ascent_speed * level.duration)
			levels.append(level)
	return levels

# --------------------------------------------------------------------------- cosmetics, achievements, workshop

static func _trails() -> Array:
	return [
		{"id": "sparkle", "name": "Sparkle Trail", "gems": 15, "a": Color("#FFFFFF"), "b": Color("#FFF3B0")},
		{"id": "ember", "name": "Ember Trail", "gems": 25, "a": Color("#FF7F11"), "b": Color("#FF1E56")},
		{"id": "rainbow", "name": "Rainbow Trail", "gems": 40, "a": Color("#FF6B6B"), "b": Color("#48DBFB")},
		{"id": "stardust", "name": "Stardust Trail", "gems": 55, "a": Color("#A29BFE"), "b": Color("#00D2D3")},
		{"id": "halo", "name": "Halo Trail", "gems": 80, "a": Color("#FFE9A8"), "b": Color("#FFFFFF")},
	]

static func _achievements() -> Array:
	var rows := [
		["first_flight", "First Flight", "Complete your first level.", "levels_completed", 1, 2],
		["coin_collector", "Coin Collector", "Collect 1,000 coins in total.", "total_coins", 1000, 3],
		["treasure_hunter", "Treasure Hunter", "Collect 10,000 coins in total.", "total_coins", 10000, 5],
		["sky_tycoon", "Sky Tycoon", "Collect 100,000 coins in total.", "total_coins", 100000, 15],
		["new_ride", "New Ride", "Own 2 balloons.", "balloons_owned", 2, 2],
		["collector", "Balloon Collector", "Own 10 balloons.", "balloons_owned", 10, 10],
		["full_fleet", "Full Fleet", "Own all 20 balloons.", "balloons_owned", 20, 40],
		["boss_slayer", "Guardian Breaker", "Defeat your first boss.", "bosses_defeated", 1, 3],
		["boss_master", "Guardian Master", "Defeat 5 different bosses.", "bosses_defeated", 5, 10],
		["untouchable", "Untouchable", "Finish 10 levels without taking damage.", "no_damage_clears", 10, 5],
		["star_hunter", "Star Hunter", "Earn 60 stars.", "total_stars", 60, 8],
		["star_legend", "Star Legend", "Earn all 180 stars.", "total_stars", 180, 50],
		["master_crafter", "Master Crafter", "Upgrade a balloon to stage 5.", "max_stage", 5, 8],
		["workshop_pro", "Workshop Pro", "Buy 10 workshop upgrades.", "workshop_levels", 10, 8],
		["power_player", "Power Player", "Collect 50 power-ups.", "powerups", 50, 5],
		["world_traveller", "World Traveller", "Complete 5 worlds.", "worlds_completed", 5, 10],
		["heaven_reached", "Heaven Reached", "Complete the campaign and reach Heaven's Gate.", "campaign", 1, 50],
	]
	var list := []
	for r in rows:
		list.append({"id": r[0], "title": r[1], "desc": r[2], "stat": r[3], "target": r[4], "gems": r[5]})
	return list

static func _workshop() -> Dictionary:
	return {
		"names": ["Durability", "Coin Magnet", "Movement", "Shield Duration"],
		"costs": [[800, 2000, 4500, 9000, 16000], [600, 1500, 3500, 7000, 12000], [700, 1800, 4000, 8000, 14000], [700, 1800, 4000, 8000, 14000]],
		"health": [0, 1, 1, 2, 2, 3],
		"armor": [0, 0, 1, 1, 2, 2],
		"magnet": [0.0, 0.3, 0.6, 0.9, 1.3, 1.8],
		"movement": [1.0, 1.04, 1.08, 1.12, 1.16, 1.2],
		"shield": [0.0, 1.0, 2.0, 3.0, 4.0, 5.0],
	}
