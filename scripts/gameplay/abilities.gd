class_name Abilities
extends RefCounted
## Balloon abilities. Each ability is a small object that overrides only the hooks it needs
## (damage, death, coins, movement, environment, power-ups, rewards, visibility, active use);
## this aggregator asks every ability in turn, so new abilities plug in without touching gameplay.

class Ability:
	extends RefCounted
	var ctx: LevelCtx
	func begin(p_ctx: LevelCtx) -> void: ctx = p_ctx
	func tick(_dt: float) -> void: pass
	func prevent_damage(_dmg_type: int) -> bool: return false
	func prevent_death(_health: BalloonHealth) -> bool: return false
	func coin_value(v: int) -> int: return v
	func movement(_m: Dictionary) -> void: pass
	func environment(_m: Dictionary) -> void: pass
	func powerup_duration(_type: int, d: float) -> float: return d
	func reward_multiplier() -> float: return 1.0
	func invisible() -> bool: return false
	func is_active() -> bool: return false

class BonusCoins:
	extends Ability
	var bonus: float
	var _acc := 0.0
	func _init(p: float) -> void: bonus = p
	func coin_value(v: int) -> int:
		_acc += v * bonus
		var extra := int(floor(_acc))
		_acc -= extra
		return v + extra

class WindResist:
	extends Ability
	var reduction: float
	func _init(p: float) -> void: reduction = clampf(p, 0.0, 1.0)
	func environment(m: Dictionary) -> void: m.wind *= 1.0 - reduction

class EnvResist:
	extends Ability
	var factor: float
	func _init(p: float) -> void: factor = clampf(p, 0.0, 1.0)
	func environment(m: Dictionary) -> void:
		m.wind *= factor; m.slippery *= factor; m.gravity *= factor; m.darkness *= factor

class GravityImmunity:
	extends Ability
	func environment(m: Dictionary) -> void: m.gravity = 0.0

class PeriodicShield:
	extends Ability
	var duration: float
	var cooldown: float
	var _t := 0.0
	func _init(d: float, cd: float) -> void:
		duration = d
		cooldown = maxf(1.0, cd)
	func begin(p_ctx: LevelCtx) -> void:
		super.begin(p_ctx)
		ctx.health.add_shield_time(duration + float(ctx.stats.shield_bonus))
		_t = cooldown
	func tick(dt: float) -> void:
		_t -= dt
		if _t > 0.0: return
		_t = cooldown
		ctx.health.add_shield_time(duration + float(ctx.stats.shield_bonus))
		Sound.play("shield")

class RechargingShield:
	extends Ability
	var recharge: float
	var _t := 0.0
	func _init(r: float) -> void: recharge = maxf(1.0, r)
	func begin(p_ctx: LevelCtx) -> void:
		super.begin(p_ctx)
		ctx.health.add_shield_charge(1)
	func tick(dt: float) -> void:
		if ctx.health.has_shield_charge():
			_t = 0.0
			return
		_t += dt
		if _t >= maxf(1.0, recharge - float(ctx.stats.shield_bonus) * 0.5):
			_t = 0.0
			ctx.health.add_shield_charge(1)
			Sound.play("shield")

class Immunity:
	extends Ability
	var type: int
	func _init(t: int) -> void: type = t
	func prevent_damage(d: int) -> bool: return d == type

## Blocks every second hit of one damage type (50% resistance that is predictable).
class Alternating:
	extends Ability
	var type: int
	var _count := 0
	func _init(t: int) -> void: type = t
	func prevent_damage(d: int) -> bool:
		if d != type: return false
		_count += 1
		return _count % 2 == 0

class Maneuver:
	extends Ability
	var bonus: float
	func _init(b: float) -> void: bonus = b
	func movement(m: Dictionary) -> void:
		m.acceleration *= 1.0 + bonus
		m.max_speed *= 1.0 + bonus * 0.43

class RewardBoost:
	extends Ability
	var mult: float
	func _init(b: float) -> void: mult = 1.0 + b
	func reward_multiplier() -> float: return mult

class PowerupDuration:
	extends Ability
	var bonus: float
	func _init(b: float) -> void: bonus = b
	func powerup_duration(_type: int, d: float) -> float: return d * (1.0 + bonus)

class Rebirth:
	extends Ability
	var fraction: float
	var used := false
	func _init(f: float) -> void: fraction = clampf(f, 0.0, 1.0)
	func prevent_death(h: BalloonHealth) -> bool:
		if used: return false
		used = true
		h.set_health(maxi(1, ceili(h.maximum * fraction)))
		h.set_invulnerable(2.0)
		FX.burst(Vector3(ctx.balloon.x, ctx.balloon.y, 1), Color(1, 0.55, 0.15), 40, 6.0, 0.45, 0.9)
		Events.toast.emit("Rebirth!")
		Sound.play("powerup")
		return true

class Cooldown:
	extends Ability
	var cooldown: float
	var timer := 0.0
	var label := "GO"
	func _init(cd: float) -> void: cooldown = maxf(0.5, cd)
	func is_active() -> bool: return true
	func readiness() -> float: return 1.0 - clampf(timer / cooldown, 0.0, 1.0)
	func can_activate() -> bool: return timer <= 0.0 and ctx != null and not ctx.health.dead
	func activate() -> void:
		if not can_activate(): return
		timer = cooldown
		_on_activate()
	func _on_activate() -> void: pass
	func tick(dt: float) -> void:
		if timer > 0.0: timer -= dt

class Invisibility:
	extends Cooldown
	var duration: float
	var _active := 0.0
	func _init(d: float, cd: float) -> void:
		super(cd)
		duration = d
		label = "VEIL"
	func invisible() -> bool: return _active > 0.0
	func _on_activate() -> void:
		_active = duration
		ctx.balloon.model.set_invisible(true)
		Sound.play("teleport")
	func tick(dt: float) -> void:
		super.tick(dt)
		if _active <= 0.0: return
		_active -= dt
		if _active <= 0.0: ctx.balloon.model.set_invisible(false)

class Teleport:
	extends Cooldown
	var distance: float
	func _init(dist: float, cd: float) -> void:
		super(cd)
		distance = dist
		label = "BLINK"
	func _on_activate() -> void:
		var c := ctx.balloon
		var dir := signf(c.velocity) if absf(c.velocity) > 0.3 else (-1.0 if c.x > 0.0 else 1.0)
		if absf(c.x + dir * distance) > ctx.half_width - 1.2: dir = -dir
		c.teleport(dir * distance)
		ctx.health.set_invulnerable(0.45)
		Sound.play("teleport")

class Invulnerability:
	extends Cooldown
	var duration: float
	func _init(d: float, cd: float) -> void:
		super(cd)
		duration = d
		label = "ETERNAL"
	func _on_activate() -> void:
		ctx.health.set_invulnerable(duration + float(ctx.stats.shield_bonus) * 0.5)
		Sound.play("shield")
		Events.toast.emit("Invulnerable!")

# ---------------------------------------------------------------- aggregator

var list: Array = []
var active: Cooldown = null

static func create_for(b: Dictionary) -> Array:
	var v := float(b.value)
	var d := float(b.duration)
	var cd := float(b.cooldown)
	match str(b.ability):
		"bonus_coins": return [BonusCoins.new(v)]
		"wind_resist": return [WindResist.new(v)]
		"periodic_shield": return [PeriodicShield.new(d, cd)]
		"fire_resist": return [Immunity.new(C.Dmg.FIRE)]
		"env_resist": return [EnvResist.new(v), Alternating.new(C.Dmg.ICE)]
		"maneuver": return [Maneuver.new(v)]
		"physical_resist": return [Alternating.new(C.Dmg.PHYSICAL)]
		"reward_boost": return [RewardBoost.new(v)]
		"powerup_duration": return [PowerupDuration.new(v)]
		"invisibility": return [Invisibility.new(d, cd)]
		"space_resist": return [Immunity.new(C.Dmg.SPACE)]
		"gravity_resist": return [GravityImmunity.new(), Alternating.new(C.Dmg.SPACE)]
		"phoenix": return [Rebirth.new(v)]
		"teleport": return [Teleport.new(v, cd)]
		"heavenly_shield": return [RechargingShield.new(cd)]
		"strong_magnet": return [BonusCoins.new(0.25)]
		"invulnerability": return [Invulnerability.new(d, cd)]
		"ultimate": return [RechargingShield.new(cd), Rebirth.new(0.5), BonusCoins.new(v)]
	return [] # "none", "magnet" (applied to stats)

func _init(b: Dictionary) -> void:
	list = create_for(b)
	for a in list:
		if a.is_active() and active == null: active = a

func begin(ctx: LevelCtx) -> void:
	for a in list: a.begin(ctx)

func tick(dt: float) -> void:
	for a in list: a.tick(dt)
	if active: Events.ability_cooldown_changed.emit(active.readiness())

func try_prevent_damage(dmg_type: int) -> bool:
	for a in list:
		if a.prevent_damage(dmg_type): return true
	return false

func try_prevent_death(h: BalloonHealth) -> bool:
	for a in list:
		if a.prevent_death(h): return true
	return false

func modify_coin_value(v: int) -> int:
	for a in list: v = a.coin_value(v)
	return v

func modify_movement(m: Dictionary) -> void:
	for a in list: a.movement(m)

func environment_multipliers() -> Dictionary:
	var m := {"wind": 1.0, "slippery": 1.0, "gravity": 1.0, "darkness": 1.0}
	for a in list: a.environment(m)
	return m

func modify_powerup_duration(type: int, d: float) -> float:
	for a in list: d = a.powerup_duration(type, d)
	return d

func reward_multiplier() -> float:
	var m := 1.0
	for a in list: m *= a.reward_multiplier()
	return m

func is_invisible() -> bool:
	for a in list:
		if a.invisible(): return true
	return false
