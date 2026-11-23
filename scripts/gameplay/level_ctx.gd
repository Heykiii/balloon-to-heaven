class_name LevelCtx
extends RefCounted
## Per-run state shared by the gameplay systems of one level. Systems receive this object
## instead of reaching for globals, which keeps them independent and testable.

var db: GameDB
var level: Dictionary
var world: Dictionary
var balloon_data: Dictionary
var stats: Dictionary
var rng: RandomNumberGenerator

var camera: Camera3D
var root: Node3D
var shake: CameraShake

var balloon: BalloonController
var health: BalloonHealth
var abilities: Abilities
var powerups: PowerUps
var obstacles: ObstacleSpawner
var coins: CoinSystem
var powerup_spawner: PowerUpSpawner
var effects: EnvEffects
var lanes: LanePlanner

var scroll_speed := 0.0
var distance := 0.0
var elapsed := 0.0
var normal_spawning := true
var boss_active := false

# run statistics
var coins_collected := 0
var special_collected := 0
var special_total := 0
var powerups_collected := 0

# playfield (world units; camera centred on 0,0)
var half_width := C.MAX_HALF_WIDTH
var visible_half_width := C.MAX_HALF_WIDTH
var top := C.CAMERA_HEIGHT * 0.5
var bottom := -C.CAMERA_HEIGHT * 0.5
var balloon_y := C.BALLOON_Y

var spawn_y: float:
	get: return top + C.SPAWN_MARGIN
var despawn_y: float:
	get: return bottom - C.DESPAWN_MARGIN
var progress: float:
	get: return clampf(distance / maxf(1.0, float(level.length)), 0.0, 1.0)
## Collision centre of the balloon envelope.
var balloon_center: Vector2:
	get: return Vector2(balloon.x if balloon else 0.0, balloon_y + 0.2)

func clamp_x(x: float, radius: float, margin: float) -> float:
	var limit := half_width - radius - margin
	return clampf(x, -limit, limit)

## Returns the orthographic camera height. Tall phones see more sky above and below
## instead of a narrower playfield, so every screen gets the same game.
func fit_to_viewport(size: Vector2) -> float:
	var aspect := maxf(0.3, size.x / maxf(1.0, size.y))
	var height := maxf(C.CAMERA_HEIGHT, 2.0 * C.MAX_HALF_WIDTH / aspect)
	top = height * 0.5
	bottom = -height * 0.5
	visible_half_width = height * 0.5 * aspect
	half_width = minf(C.MAX_HALF_WIDTH, visible_half_width)
	return height
