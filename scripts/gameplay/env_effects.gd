class_name EnvEffects
extends RefCounted
## Level environment: wind gusts, wind zones, gravity wells, icy controls, darkness and
## light/shadow zones. Balloon abilities scale each effect through environment_multipliers().

const DARK_SHADER := """
shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, blend_mix;
uniform float darkness = 0.0;
uniform float clear_radius = 0.2;
void fragment() {
	float d = distance(UV, vec2(0.5));
	ALBEDO = vec3(0.02, 0.02, 0.06);
	ALPHA = darkness * smoothstep(clear_radius, clear_radius + 0.09, d);
}
"""

var ctx: LevelCtx
var zone_wind := 0.0
var _gust_target := 0.0
var _gust := 0.0
var _gust_timer := 0.0
var _gravity_timer := 0.0
var _gravity_target := 0.0
var _gravity_center := 0.0
var _dark_quad: MeshInstance3D
var _dark_mat: ShaderMaterial
var _well: Node3D
var _shadow_phase := 0.0
var _t := 0.0

func _init(p_ctx: LevelCtx, parent: Node3D) -> void:
	ctx = p_ctx
	var lv := ctx.level
	if float(lv.darkness) > 0.0 or lv.light_shadow:
		var sh := Shader.new()
		sh.code = DARK_SHADER
		_dark_mat = ShaderMaterial.new()
		_dark_mat.shader = sh
		_dark_quad = Kit.part(parent, "quad", _dark_mat, Vector3(0, ctx.balloon_y, 4.0), Vector3(70, 70, 1))
	if float(lv.gravity) > 0.0:
		_well = Kit.node("GravityWell", parent)
		Kit.part(_well, "thin_torus", Kit.unlit(Color(0.7, 0.5, 1, 0.8), true), Vector3.ZERO, Vector3.ONE * 2.6, Vector3(90, 0, 0))
		Kit.part(_well, "thin_torus", Kit.unlit(Color(0.6, 0.4, 1, 0.5), true), Vector3.ZERO, Vector3.ONE * 1.7, Vector3(90, 0, 0))
		Kit.glow(_well, Vector3.ZERO, 4.0, Color(0.6, 0.4, 1, 0.35))
		_well.visible = false

func tick(dt: float) -> void:
	if dt <= 0.0: return
	_t += dt
	var lv := ctx.level
	var mods := ctx.abilities.environment_multipliers() if ctx.abilities else {"wind": 1.0, "slippery": 1.0, "gravity": 1.0, "darkness": 1.0}
	_gust_timer -= dt
	if _gust_timer <= 0.0:
		match int(lv.wind):
			C.Wind.GUSTS:
				_gust_target = ((-1.0 if ctx.rng.randf() < 0.5 else 1.0) * float(lv.wind_strength)) if _gust_target == 0.0 else 0.0
				_gust_timer = 2.5 if _gust_target == 0.0 else 2.0
			C.Wind.UNPREDICTABLE:
				_gust_target = (ctx.rng.randf() * 2.0 - 1.0) * float(lv.wind_strength)
				_gust_timer = 1.2 + ctx.rng.randf() * 1.5
			_:
				_gust_target = 0.0
				_gust_timer = 10.0
	_gust = move_toward(_gust, _gust_target, dt * 2.5)
	var pull := 0.0
	if float(lv.gravity) > 0.0:
		_gravity_timer -= dt
		if _gravity_timer <= 0.0:
			var on := _gravity_target == 0.0
			_gravity_target = float(lv.gravity) if on else 0.0
			_gravity_center = (ctx.rng.randf() * 2.0 - 1.0) * (ctx.half_width - 1.5)
			_gravity_timer = 3.5 if on else 3.0
			_well.visible = on and float(mods.gravity) > 0.0
			_well.position = Vector3(_gravity_center, ctx.balloon_y + 1.0, -2.0)
		if _gravity_target > 0.0:
			pull = clampf(_gravity_center - ctx.balloon.x, -1.0, 1.0) * _gravity_target * 3.2 * float(mods.gravity)
			_well.scale = Vector3.ONE * (1.0 + sin(_t * 4.0) * 0.1)
			_well.rotation_degrees.z += dt * 90.0
	ctx.balloon.external_velocity = (zone_wind + _gust) * float(mods.wind) + pull
	ctx.balloon.slipperiness = float(lv.slippery) * float(mods.slippery)
	if _dark_quad:
		var darkness := float(lv.darkness)
		if lv.light_shadow:
			_shadow_phase += dt
			var zone := sin(_shadow_phase * 0.7) * 0.5 + 0.5
			darkness = maxf(darkness, smoothstep(0.0, 0.75, zone))
		darkness *= float(mods.darkness)
		_dark_quad.position = Vector3(ctx.balloon.x, ctx.balloon_y + 1.5, 4.0)
		# Clear radius in UV units of the 70-unit quad: 20 units wide at no darkness, 7.5 at full.
		_dark_mat.set_shader_parameter("clear_radius", lerpf(20.0, 7.5, darkness) / 70.0 * 0.5 * 2.0 * 0.5)
		_dark_mat.set_shader_parameter("darkness", clampf(darkness * 1.15, 0.0, 0.92))

## Current horizontal wind for the HUD and weather (-1..1-ish).
func wind_value() -> float: return _gust + zone_wind
