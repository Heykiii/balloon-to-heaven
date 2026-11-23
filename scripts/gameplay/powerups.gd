class_name PowerUps
extends RefCounted
## Active power-up timers: Coin Magnet, Energy Shield, Sky Rocket, Extra Life, Double Coins.

var ctx: LevelCtx
var remaining := [0.0, 0.0, 0.0, 0.0, 0.0]
var durations := [0.0, 0.0, 0.0, 0.0, 0.0]

func _init(p_ctx: LevelCtx) -> void:
	ctx = p_ctx

func is_active(t: int) -> bool: return remaining[t] > 0.0
func _mag(t: int, fallback: float) -> float:
	var d := ctx.db.powerup(t)
	return float(d.magnitude) if not d.is_empty() else fallback
func magnet_radius() -> float: return _mag(C.PU.MAGNET, 4.5) if is_active(C.PU.MAGNET) else 0.0
func scroll_multiplier() -> float: return _mag(C.PU.ROCKET, 1.8) if is_active(C.PU.ROCKET) else 1.0
func coin_multiplier() -> int: return maxi(1, roundi(_mag(C.PU.DOUBLE, 2.0))) if is_active(C.PU.DOUBLE) else 1
func spawn_paused() -> bool: return is_active(C.PU.ROCKET)

func activate(t: int) -> void:
	var d := ctx.db.powerup(t)
	ctx.powerups_collected += 1
	Sound.play("powerup")
	Events.toast.emit((d.name + "!") if not d.is_empty() else "Power-up!")
	if t == C.PU.LIFE:
		ctx.health.heal(1)
		return
	var dur := float(d.get("duration", 0.0))
	if ctx.abilities: dur = ctx.abilities.modify_powerup_duration(t, dur)
	if t == C.PU.SHIELD: dur += float(ctx.stats.shield_bonus)
	remaining[t] = dur
	durations[t] = dur
	if t == C.PU.SHIELD:
		ctx.health.add_shield_time(dur)
	if t == C.PU.ROCKET:
		ctx.health.set_invulnerable(dur + 0.5)
		if ctx.shake: ctx.shake.add(0.25)
	Events.powerup_updated.emit(t, dur, dur)

func tick(dt: float) -> void:
	for i in remaining.size():
		if remaining[i] <= 0.0: continue
		remaining[i] = maxf(0.0, remaining[i] - dt)
		Events.powerup_updated.emit(i, remaining[i], durations[i])
