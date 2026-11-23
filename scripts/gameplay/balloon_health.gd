class_name BalloonHealth
extends RefCounted
## Health, shields and the damage pipeline:
## invulnerable? -> shield -> ability immunity -> armor deflect -> damage -> lethal handlers.

signal died

var model: BalloonModel
var abilities: Abilities
var rng: RandomNumberGenerator
var armor := 0
var current := 1
var maximum := 1
var dead := false
var took_damage := false
var _invulnerable := 0.0     # post-hit blink
var _ability_invuln := 0.0   # ability / rocket invulnerability (no blink)
var _shield := 0.0
var _charges := 0
var _shield_event := 0.0

func _init(max_hp: int, p_armor: int, p_model: BalloonModel, p_abilities: Abilities, p_rng: RandomNumberGenerator) -> void:
	model = p_model
	abilities = p_abilities
	rng = p_rng
	armor = maxi(0, p_armor)
	maximum = maxi(1, max_hp)
	current = maximum
	Events.health_changed.emit(current, maximum)

func has_shield() -> bool: return _shield > 0.0 or _charges > 0
func has_shield_charge() -> bool: return _charges > 0
func is_invulnerable() -> bool: return _invulnerable > 0.0 or _ability_invuln > 0.0
func shield_time() -> float: return _shield

func _shield_value() -> float:
	return _shield if _shield > 0.0 else (-1.0 if _charges > 0 else 0.0)

func add_shield_time(seconds: float) -> void:
	if seconds <= 0.0: return
	_shield = maxf(_shield, seconds)
	Events.shield_changed.emit(_shield_value())

func add_shield_charge(n := 1) -> void:
	_charges = clampi(_charges + n, 0, 1)
	Events.shield_changed.emit(_shield_value())

func set_invulnerable(seconds: float) -> void:
	_ability_invuln = maxf(_ability_invuln, seconds)

func heal(amount: int) -> void:
	if dead or amount <= 0: return
	current = mini(maximum, current + amount)
	Events.health_changed.emit(current, maximum)

func set_health(v: int) -> void:
	current = clampi(v, 1, maximum)
	dead = false
	Events.health_changed.emit(current, maximum)

## Returns true when the balloon actually lost health.
func try_hit(amount: int, dmg_type: int) -> bool:
	if dead or amount <= 0 or is_invulnerable(): return false
	if has_shield():
		if _shield <= 0.0: _charges -= 1
		_blocked()
		return false
	if abilities and abilities.try_prevent_damage(dmg_type):
		_blocked()
		return false
	var deflect := minf(C.MAX_ARMOR_REDUCTION, armor * C.ARMOR_PER_POINT)
	if deflect > 0.0 and rng.randf() < deflect:
		_blocked()
		return false
	current -= amount
	took_damage = true
	_invulnerable = C.HIT_INVULNERABILITY
	if model: model.flash()
	Events.balloon_damaged.emit(amount, dmg_type)
	if current <= 0:
		if abilities and abilities.try_prevent_death(self):
			_invulnerable = maxf(_invulnerable, 2.0)
		else:
			current = 0
			dead = true
			Events.health_changed.emit(current, maximum)
			died.emit()
			return true
	Events.health_changed.emit(current, maximum)
	return true

func _blocked() -> void:
	_invulnerable = maxf(_invulnerable, C.BLOCK_INVULNERABILITY)
	Events.hit_blocked.emit()
	Sound.play("shield", 0.7)
	Events.shield_changed.emit(_shield_value())

func tick(dt: float) -> void:
	if dt <= 0.0: return
	if _invulnerable > 0.0: _invulnerable -= dt
	if _ability_invuln > 0.0: _ability_invuln -= dt
	if _shield > 0.0:
		_shield -= dt
		_shield_event -= dt
		if _shield <= 0.0 or _shield_event <= 0.0:
			_shield_event = 0.1
			_shield = maxf(0.0, _shield)
			Events.shield_changed.emit(_shield_value())
	if model:
		model.set_shield(has_shield() or _ability_invuln > 0.0)
		model.set_blinking(_invulnerable > 0.0 and not dead)
