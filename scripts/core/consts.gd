class_name C
## Game-wide constants, enums and tuning values (the "GameConfig").

const WORLDS := 10
const LEVELS_PER_WORLD := 6
const TOTAL_LEVELS := 60
const BALLOON_COUNT := 20
const MAX_STAGE := 5
const WORKSHOP_CATEGORIES := 4
const MAX_WORKSHOP_LEVEL := 5
const DEFAULT_BALLOON := "classic"
const TOTAL_STARS := 180

enum Dmg { PHYSICAL, FIRE, ICE, ELECTRIC, SPACE, MAGIC }
enum Move { STATIC, CROSS, PATROL, FALLING, DIAGONAL, CIRCULAR, HOMING, SWARM, BOB }
enum PU { MAGNET, SHIELD, ROCKET, LIFE, DOUBLE }
enum Challenge { NO_DAMAGE, ALL_SPECIAL, COIN_TARGET, POWERUPS }
enum Wind { NONE, GUSTS, UNPREDICTABLE }
enum Role { INTRO, NEW_OBSTACLE, HARDER, COMBINATION, SURVIVAL, BOSS }
enum Attack { FAN, AIMED, COLUMN, RAIN, SUMMON, WALL, SPIRAL }
enum BossMove { SWEEP, FIGURE8, TRACK, HOVER, TELEPORT }
enum Workshop { DURABILITY, MAGNET, MOVEMENT, SHIELD }

const RARITY_NAMES := ["Common", "Rare", "Epic", "Legendary", "Mythical"]
const RARITY_COLORS := [Color("#B2BEC3"), Color("#48DBFB"), Color("#A29BFE"), Color("#FDCB6E"), Color("#FF7675")]
const PU_NAMES := ["Coin Magnet", "Energy Shield", "Sky Rocket", "Extra Life", "Double Coins"]

# Camera / playfield (world units). The camera shows CAMERA_HEIGHT units vertically.
const CAMERA_HEIGHT := 20.0
const MAX_HALF_WIDTH := 5.625
const EDGE_MARGIN := 0.35
const BALLOON_Y := -5.2
const BALLOON_RADIUS := 0.7

# Movement
const H_SPEED := 9.0
const H_ACCEL := 60.0
const SMOOTHING := 0.06
const MAX_VELOCITY := 13.0
const RELEASE_DAMPING := 6.0
const MAX_TILT := 14.0

# Damage
const HIT_INVULNERABILITY := 1.4
const BLOCK_INVULNERABILITY := 0.6
const ARMOR_PER_POINT := 0.05
const MAX_ARMOR_REDUCTION := 0.4

# Spawning
const SPAWN_MARGIN := 2.0
const DESPAWN_MARGIN := 3.0
const LANES := 7
const MIN_REACTION_TIME := 1.3
const BASE_MAGNET := 1.1
const MAGNET_PULL := 14.0

# Rewards
const R_COIN := 1
const R_SPECIAL_COIN := 10
const R_LEVEL_COMPLETE := 100
const R_NO_DAMAGE := 50
const R_ALL_SPECIAL := 100
const R_THREE_STAR := 100
const R_BOSS := 500
const R_BOSS_GEMS := 5
const R_FIRST_THREE_STAR_GEMS := 1

static func world_of(level: int) -> int:
	return int((clampi(level, 1, TOTAL_LEVELS) - 1) / LEVELS_PER_WORLD) + 1

static func index_in_world(level: int) -> int:
	return (clampi(level, 1, TOTAL_LEVELS) - 1) % LEVELS_PER_WORLD + 1

static func first_level_of(world: int) -> int:
	return (clampi(world, 1, WORLDS) - 1) * LEVELS_PER_WORLD + 1
