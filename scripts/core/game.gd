extends Node
## Game autoload: owns the services (content, save, economy, progression, shop, workshop,
## achievements), the session (selected level/balloon, last result) and screen navigation.

const SCREENS := {
	"boot": "res://scripts/screens/boot_screen.gd",
	"menu": "res://scripts/screens/menu_screen.gd",
	"map": "res://scripts/screens/map_screen.gd",
	"shop": "res://scripts/screens/shop_screen.gd",
	"workshop": "res://scripts/screens/workshop_screen.gd",
	"play": "res://scripts/screens/play_screen.gd",
	"results": "res://scripts/screens/results_screen.gd",
	"ending": "res://scripts/screens/ending_screen.gd",
}

var db: GameDB
var save: SaveManager
var economy: Economy
var progression: Progression
var shop: Shop
var workshop: WorkshopUpgrades
var achievements: Achievements
var ready_ok := false

# session
var selected_level := 1
var selected_balloon := ""
var last_result: Dictionary = {}
var show_ending := false
var map_world := 0
## Set by the smoke test: plays levels automatically.
var autopilot := false

var main: Node # main.gd, the screen host

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	get_tree().auto_accept_quit = false
	db = Content.build()
	var err := db.validate()
	if err != "": push_error("Content validation failed: " + err)
	save = SaveManager.new(SaveManager.FileStorage.new(), db)
	save.load_game()
	economy = Economy.new(save)
	progression = Progression.new(save, economy, db)
	shop = Shop.new(save, economy, db)
	workshop = WorkshopUpgrades.new(save, economy, db)
	achievements = Achievements.new(save, economy, db)
	economy.changed.connect(func(c, g): Events.currency_changed.emit(c, g))
	achievements.unlocked.connect(func(t):
		Events.achievement_unlocked.emit(t)
		Sound.play("unlock"))
	apply_settings()
	ready_ok = true

func apply_settings() -> void:
	var s: Dictionary = save.data.settings
	Sound.music_volume = float(s.music)
	Sound.sfx_volume = float(s.sfx)
	CameraShake.enabled = bool(s.shake)
	var q := int(s.quality)
	var vp := get_viewport()
	if vp: vp.msaa_3d = Viewport.MSAA_2X if q >= 2 else Viewport.MSAA_DISABLED
	if FX.current: FX.current.density = [0.5, 0.8, 1.0][clampi(q, 0, 2)]

func quality() -> int: return int(save.data.settings.quality)

func goto(screen: String) -> void:
	save.save_if_dirty()
	if main: main.show_screen(screen)

func start_level(n: int, balloon_id := "") -> void:
	selected_level = clampi(n, 1, C.TOTAL_LEVELS)
	selected_balloon = balloon_id
	goto("play")

func toast(text: String) -> void:
	Events.toast.emit(text)

func _notification(what: int) -> void:
	match what:
		NOTIFICATION_WM_CLOSE_REQUEST:
			if save: save.save_now()
			get_tree().quit()
		NOTIFICATION_APPLICATION_PAUSED, NOTIFICATION_APPLICATION_FOCUS_OUT, NOTIFICATION_WM_GO_BACK_REQUEST:
			if save and ready_ok: save.save_if_dirty()
			if what == NOTIFICATION_WM_GO_BACK_REQUEST and main: main.back_pressed()
