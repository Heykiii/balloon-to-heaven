extends Node
## Global event hub (autoload "Events"). Systems talk through these signals instead of
## holding references to each other.

signal currency_changed(coins: int, gems: int)
signal toast(text: String)
signal achievement_unlocked(title: String)

# Gameplay
signal health_changed(current: int, maximum: int)
signal balloon_damaged(amount: int, damage_type: int)
signal hit_blocked
signal level_coins_changed(coins: int)
signal level_progress_changed(progress: float)
signal powerup_updated(type: int, remaining: float, duration: float)
## seconds left on a timed shield; -1 = a charge shield is ready; 0 = none
signal shield_changed(value: float)
## 0..1, negative hides the boss bar
signal boss_health_changed(value: float)
signal boss_phase_started(name: String)
## 0..1 readiness of the balloon's active ability
signal ability_cooldown_changed(readiness: float)
