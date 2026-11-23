class_name SaveManager
extends RefCounted
## Versioned JSON save with an FNV-1a checksum, a backup slot and full validation/repair.
## Storage is pluggable so tests can run on memory.

const VERSION := 1
const MAIN_SLOT := "save"
const BACKUP_SLOT := "save_backup"

class Storage:
	func read(_slot: String) -> String: return ""
	func write(_slot: String, _text: String) -> void: pass
	func remove(_slot: String) -> void: pass

class MemoryStorage extends Storage:
	var slots := {}
	func read(slot: String) -> String: return slots.get(slot, "")
	func write(slot: String, text: String) -> void: slots[slot] = text
	func remove(slot: String) -> void: slots.erase(slot)

## user:// is a real folder on desktop/Android and IndexedDB in the browser.
class FileStorage extends Storage:
	var dir := "user://"
	func _path(slot: String) -> String: return dir + slot + ".json"
	func read(slot: String) -> String:
		if not FileAccess.file_exists(_path(slot)): return ""
		var f := FileAccess.open(_path(slot), FileAccess.READ)
		return f.get_as_text() if f else ""
	func write(slot: String, text: String) -> void:
		var tmp := _path(slot) + ".tmp"
		var f := FileAccess.open(tmp, FileAccess.WRITE)
		if f == null: return
		f.store_string(text)
		f.close()
		DirAccess.rename_absolute(tmp, _path(slot))
	func remove(slot: String) -> void:
		if FileAccess.file_exists(_path(slot)): DirAccess.remove_absolute(_path(slot))

var storage: Storage
var db: GameDB
var data: Dictionary = {}
var dirty := false
var loaded_from_backup := false
var recovered_from_corruption := false

func _init(p_storage: Storage, p_db: GameDB) -> void:
	storage = p_storage
	db = p_db
	data = new_save()

static func new_save() -> Dictionary:
	var stars := []
	var best := []
	var done := []
	for i in C.TOTAL_LEVELS:
		stars.append(0); best.append(0); done.append(false)
	return {
		"version": VERSION, "coins": 0, "gems": 0, "highest_unlocked": 1,
		"stars": stars, "best_coins": best, "completed": done,
		"owned": [C.DEFAULT_BALLOON], "equipped": C.DEFAULT_BALLOON, "stages": {C.DEFAULT_BALLOON: 1},
		"workshop": [0, 0, 0, 0], "claimed": [], "achievements": [],
		"owned_trails": [], "trail": "",
		"stats": {"total_coins": 0, "levels_completed": 0, "no_damage_clears": 0, "powerups": 0, "levels_failed": 0, "bosses": []},
		"settings": {"music": 0.7, "sfx": 0.8, "quality": 2, "shake": true},
		"campaign_completed": false, "saved_at": 0,
	}

static func checksum(text: String) -> String:
	var h: int = 2166136261
	var bytes := text.to_utf8_buffer()
	for b in bytes:
		h = h ^ b
		h = (h * 16777619) & 0xFFFFFFFF
	return "%08x" % h

static func serialize(d: Dictionary) -> String:
	var payload := JSON.stringify(d)
	return JSON.stringify({"version": VERSION, "checksum": checksum(payload), "payload": payload})

static func try_parse(raw: String) -> Variant:
	if raw.is_empty(): return null
	var json := JSON.new()
	if json.parse(raw) != OK: return null
	var env = json.data
	if typeof(env) != TYPE_DICTIONARY or not env.has("payload") or not env.has("checksum"): return null
	if typeof(env.payload) != TYPE_STRING or checksum(env.payload) != str(env.checksum): return null
	if json.parse(env.payload) != OK: return null
	var d = json.data
	if typeof(d) != TYPE_DICTIONARY: return null
	return migrate(d, int(env.get("version", 1)))

static func migrate(d: Dictionary, _from_version: int) -> Dictionary:
	# Version 1 is the first released format; future migrations go here.
	d["version"] = VERSION
	return d

func load_game() -> void:
	loaded_from_backup = false
	recovered_from_corruption = false
	var raw := storage.read(MAIN_SLOT)
	var d = try_parse(raw)
	if d == null:
		if not raw.is_empty(): recovered_from_corruption = true
		d = try_parse(storage.read(BACKUP_SLOT))
		if d != null: loaded_from_backup = true
	data = d if d != null else new_save()
	validate(data, db)
	if recovered_from_corruption:
		push_warning("Main save was corrupted; restored " + ("backup." if loaded_from_backup else "a fresh save."))

## Repairs out-of-range or inconsistent values so a broken state can never load.
static func validate(d: Dictionary, p_db: GameDB) -> void:
	var fresh := new_save()
	for key in fresh.keys():
		if not d.has(key) or not _same_kind(d[key], fresh[key]):
			d[key] = fresh[key]
	d.coins = maxi(0, int(d.coins))
	d.gems = maxi(0, int(d.gems))
	d.stars = _fit(d.stars, C.TOTAL_LEVELS, 0)
	d.best_coins = _fit(d.best_coins, C.TOTAL_LEVELS, 0)
	d.completed = _fit(d.completed, C.TOTAL_LEVELS, false)
	for i in C.TOTAL_LEVELS:
		d.stars[i] = clampi(int(d.stars[i]), 0, 3)
		d.best_coins[i] = maxi(0, int(d.best_coins[i]))
		d.completed[i] = bool(d.completed[i])
	# Highest unlocked must follow completed levels and stay in range.
	var highest := 1
	for i in C.TOTAL_LEVELS:
		if d.completed[i]: highest = maxi(highest, mini(i + 2, C.TOTAL_LEVELS))
	d.highest_unlocked = clampi(maxi(int(d.highest_unlocked), highest), 1, C.TOTAL_LEVELS)
	# Balloons
	var owned := []
	for id in d.owned:
		if p_db != null and p_db.balloon(str(id)).is_empty(): continue
		if not owned.has(str(id)): owned.append(str(id))
	if not owned.has(C.DEFAULT_BALLOON): owned.insert(0, C.DEFAULT_BALLOON)
	d.owned = owned
	if not owned.has(str(d.equipped)): d.equipped = C.DEFAULT_BALLOON
	var stages := {}
	for id in owned:
		stages[id] = clampi(int(d.stages.get(id, 1)), 1, C.MAX_STAGE)
	d.stages = stages
	d.workshop = _fit(d.workshop, C.WORKSHOP_CATEGORIES, 0)
	for i in C.WORKSHOP_CATEGORIES:
		d.workshop[i] = clampi(int(d.workshop[i]), 0, C.MAX_WORKSHOP_LEVEL)
	var trails := []
	for t in d.owned_trails:
		if p_db == null or not p_db.trail(str(t)).is_empty():
			if not trails.has(str(t)): trails.append(str(t))
	d.owned_trails = trails
	if str(d.trail) != "" and not trails.has(str(d.trail)): d.trail = ""
	var stats: Dictionary = d.stats
	for key in fresh.stats.keys():
		if not stats.has(key) or not _same_kind(stats[key], fresh.stats[key]): stats[key] = fresh.stats[key]
	for key in ["total_coins", "levels_completed", "no_damage_clears", "powerups", "levels_failed"]:
		stats[key] = maxi(0, int(stats[key]))
	var s: Dictionary = d.settings
	for key in fresh.settings.keys():
		if not s.has(key): s[key] = fresh.settings[key]
	s.music = clampf(float(s.music), 0.0, 1.0)
	s.sfx = clampf(float(s.sfx), 0.0, 1.0)
	s.quality = clampi(int(s.quality), 0, 2)
	s.shake = bool(s.shake)

## JSON turns ints into floats, so numbers of either kind are accepted.
static func _same_kind(a, b) -> bool:
	var na := typeof(a) == TYPE_INT or typeof(a) == TYPE_FLOAT
	var nb := typeof(b) == TYPE_INT or typeof(b) == TYPE_FLOAT
	if na and nb: return true
	return typeof(a) == typeof(b)

static func _fit(arr: Array, size: int, filler) -> Array:
	var out := arr.duplicate()
	while out.size() < size: out.append(filler)
	if out.size() > size: out.resize(size)
	return out

func mark_dirty() -> void:
	dirty = true

## Writes immediately (after purchases and level results). Keeps the previous good save as backup.
func save_now() -> void:
	data.saved_at = int(Time.get_unix_time_from_system())
	var previous := storage.read(MAIN_SLOT)
	if try_parse(previous) != null: storage.write(BACKUP_SLOT, previous)
	storage.write(MAIN_SLOT, serialize(data))
	dirty = false

func save_if_dirty() -> void:
	if dirty: save_now()

func reset_progress() -> void:
	var settings: Dictionary = data.settings
	data = new_save()
	data.settings = settings
	save_now()

func total_stars() -> int:
	var n := 0
	for s in data.stars: n += int(s)
	return n

func stage_of(id: String) -> int:
	return int(data.stages.get(id, 1))
