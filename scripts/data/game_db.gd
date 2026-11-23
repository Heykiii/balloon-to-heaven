class_name GameDB
extends RefCounted
## Read-only campaign data with lookups. Built by Content.build().

var balloons: Array = []
var powerups: Array = []
var obstacles: Dictionary = {}
var bosses: Array = []
var worlds: Array = []
var levels: Array = []
var trails: Array = []
var achievements: Array = []
var workshop: Dictionary = {}

var _balloon_by_id := {}
var _boss_by_id := {}
var _trail_by_id := {}

func index() -> void:
	_balloon_by_id.clear()
	for b in balloons: _balloon_by_id[b.id] = b
	for b in bosses: _boss_by_id[b.id] = b
	for t in trails: _trail_by_id[t.id] = t

func balloon(id: String) -> Dictionary:
	return _balloon_by_id.get(id, {})

func default_balloon() -> Dictionary:
	return balloon(C.DEFAULT_BALLOON)

func level(n: int) -> Dictionary:
	return levels[n - 1] if n >= 1 and n <= levels.size() else {}

func world(n: int) -> Dictionary:
	return worlds[n - 1] if n >= 1 and n <= worlds.size() else {}

func boss(id: String) -> Dictionary:
	return _boss_by_id.get(id, {})

func obstacle(id: String) -> Dictionary:
	return obstacles.get(id, {})

func trail(id: String) -> Dictionary:
	return _trail_by_id.get(id, {})

func powerup(type: int) -> Dictionary:
	for p in powerups:
		if p.type == type: return p
	return {}

## Structural checks for the campaign. Returns "" when valid.
func validate() -> String:
	if balloons.size() != C.BALLOON_COUNT: return "Expected 20 balloons, found %d" % balloons.size()
	if worlds.size() != C.WORLDS: return "Expected 10 worlds, found %d" % worlds.size()
	if levels.size() != C.TOTAL_LEVELS: return "Expected 60 levels, found %d" % levels.size()
	if bosses.size() != C.WORLDS: return "Expected 10 bosses, found %d" % bosses.size()
	if powerups.size() != 5: return "Expected 5 power-ups"
	for i in levels.size():
		var l: Dictionary = levels[i]
		if l.number != i + 1: return "Level %d out of order" % (i + 1)
		if l.is_boss and boss(l.boss).is_empty(): return "Boss level %d has no boss" % l.number
		for e in l.obstacles:
			if obstacle(e.id).is_empty(): return "Level %d uses unknown obstacle %s" % [l.number, e.id]
	for b in bosses:
		for p in b.phases:
			for a in p.attacks:
				if obstacle(a.projectile).is_empty(): return "Boss %s uses unknown obstacle %s" % [b.id, a.projectile]
	return ""
