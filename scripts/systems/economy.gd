class_name Economy
extends RefCounted
## Coins and Sky Gems. Balances can never go negative and one-time rewards are granted once.

signal changed(coins: int, gems: int)

const MAX_BALANCE := 2000000000
var save: SaveManager

func _init(p_save: SaveManager) -> void:
	save = p_save

var coins: int:
	get: return int(save.data.coins)
var gems: int:
	get: return int(save.data.gems)

func can_afford_coins(amount: int) -> bool: return amount >= 0 and coins >= amount
func can_afford_gems(amount: int) -> bool: return amount >= 0 and gems >= amount

func add_coins(amount: int, persist := true) -> void:
	if amount <= 0: return
	save.data.coins = mini(MAX_BALANCE, coins + amount)
	_changed(persist)

func add_gems(amount: int, persist := true) -> void:
	if amount <= 0: return
	save.data.gems = mini(MAX_BALANCE, gems + amount)
	_changed(persist)

func try_spend_coins(amount: int) -> bool:
	if amount < 0 or coins < amount: return false
	save.data.coins = coins - amount
	_changed(true)
	return true

func try_spend_gems(amount: int) -> bool:
	if amount < 0 or gems < amount: return false
	save.data.gems = gems - amount
	_changed(true)
	return true

func is_claimed(reward_id: String) -> bool:
	return save.data.claimed.has(reward_id)

## Grants a reward exactly once per save. Returns false if it was already claimed.
func try_grant_once(reward_id: String, p_coins: int, p_gems: int) -> bool:
	if reward_id.is_empty() or is_claimed(reward_id): return false
	save.data.claimed.append(reward_id)
	if p_coins > 0: save.data.coins = mini(MAX_BALANCE, coins + p_coins)
	if p_gems > 0: save.data.gems = mini(MAX_BALANCE, gems + p_gems)
	_changed(true)
	return true

func _changed(persist: bool) -> void:
	if persist: save.save_now()
	else: save.mark_dirty()
	changed.emit(coins, gems)
