class_name LanePlanner
extends RefCounted
## Fairness: the spawn row is split into lanes; a two-lane safe corridor always stays free and
## only random-walks onto lanes that are free, so every pattern has a way through.

var lane_count: int
var half_width: float
var lane_width: float
var corridor := 0
var row_blocked_until := 0.0
var _blocked: PackedFloat32Array
var _rng: RandomNumberGenerator

func _init(count: int, p_half_width: float, rng: RandomNumberGenerator) -> void:
	lane_count = count
	half_width = p_half_width
	lane_width = half_width * 2.0 / count
	_rng = rng
	_blocked = PackedFloat32Array()
	_blocked.resize(count)
	corridor = int((count - 2) / 2)

func lane_center(lane: int) -> float: return -half_width + lane_width * (lane + 0.5)
func lane_of(x: float) -> int: return clampi(int(floor((x + half_width) / lane_width)), 0, lane_count - 1)
func corridor_center() -> float: return -half_width + lane_width * (corridor + 1.0)
func is_corridor_lane(lane: int) -> bool: return lane == corridor or lane == corridor + 1
func is_free(lane: int, time: float) -> bool: return _blocked[lane] <= time

func free_count(time: float) -> int:
	var n := 0
	for i in lane_count:
		if _blocked[i] <= time: n += 1
	return n

func all_free(time: float) -> bool: return free_count(time) == lane_count

## Moves the corridor by at most one lane, and only onto lanes that are free now.
func advance_corridor(time: float) -> void:
	var next := clampi(corridor + _rng.randi_range(-1, 1), 0, lane_count - 2)
	if next == corridor: return
	if not is_free(next, time) or not is_free(next + 1, time): return
	corridor = next

func lanes_for(width: float) -> int:
	return clampi(int(ceil(width / lane_width - 0.05)), 1, lane_count - 2)

func can_block(start: int, count: int, time: float, min_free: int) -> bool:
	if start < 0 or start + count > lane_count: return false
	var newly := 0
	for i in range(start, start + count):
		if is_corridor_lane(i): return false
		if _blocked[i] <= time: newly += 1
	return free_count(time) - newly >= min_free

func block(start: int, count: int, until: float) -> void:
	for i in range(maxi(0, start), mini(lane_count, start + count)):
		_blocked[i] = maxf(_blocked[i], until)

## Finds a random valid start lane for 'count' lanes, or -1.
func find_placement(count: int, time: float, min_free: int, avoid_lane := -1) -> int:
	var options := lane_count - count + 1
	if options <= 0: return -1
	var offset := _rng.randi_range(0, options - 1)
	for k in options:
		var s := (offset + k) % options
		if avoid_lane >= s and avoid_lane < s + count: continue
		if can_block(s, count, time, min_free): return s
	return -1

## Reserves the whole row except the corridor (walls with a gap). Needs an empty row.
func try_reserve_full_row(time: float, until: float) -> bool:
	if not all_free(time) or row_blocked_until > time: return false
	for i in lane_count:
		if not is_corridor_lane(i): _blocked[i] = until
	row_blocked_until = until
	return true

func mark_row_event(until: float) -> void:
	row_blocked_until = maxf(row_blocked_until, until)
