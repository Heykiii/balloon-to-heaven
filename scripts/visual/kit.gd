class_name Kit
## Procedural 3D building blocks: cached meshes, toon materials, glow sprites and textures.

static var _mats := {}
static var _meshes := {}
static var _tex := {}

# ---------------------------------------------------------------- meshes

static func mesh(kind: String) -> Mesh:
	if _meshes.has(kind): return _meshes[kind]
	var m: Mesh
	match kind:
		"sphere":
			var s := SphereMesh.new(); s.radius = 0.5; s.height = 1.0; s.radial_segments = 24; s.rings = 12; m = s
		"low_sphere":
			var s := SphereMesh.new(); s.radius = 0.5; s.height = 1.0; s.radial_segments = 7; s.rings = 4; m = s
		"octa":
			var s := SphereMesh.new(); s.radius = 0.5; s.height = 1.0; s.radial_segments = 4; s.rings = 1; m = s
		"hemi":
			var s := SphereMesh.new(); s.radius = 0.5; s.height = 0.5; s.is_hemisphere = true; s.radial_segments = 20; s.rings = 6; m = s
		"box":
			m = BoxMesh.new()
		"cylinder":
			var c := CylinderMesh.new(); c.top_radius = 0.5; c.bottom_radius = 0.5; c.height = 1.0; c.radial_segments = 14; m = c
		"cone":
			var c := CylinderMesh.new(); c.top_radius = 0.0; c.bottom_radius = 0.5; c.height = 1.0; c.radial_segments = 14; m = c
		"funnel":
			var c := CylinderMesh.new(); c.top_radius = 0.5; c.bottom_radius = 0.14; c.height = 1.0; c.radial_segments = 24; c.cap_top = false; m = c
		"torus":
			var t := TorusMesh.new(); t.inner_radius = 0.38; t.outer_radius = 0.5; t.rings = 24; t.ring_segments = 8; m = t
		"thin_torus":
			var t := TorusMesh.new(); t.inner_radius = 0.47; t.outer_radius = 0.5; t.rings = 28; t.ring_segments = 6; m = t
		"prism":
			m = PrismMesh.new()
		"quad":
			m = QuadMesh.new()
		"arc":
			m = _arc_mesh(0.8, 1.0, 32)
		"capsule":
			var c := CapsuleMesh.new(); c.radius = 0.25; c.height = 1.0; m = c
	_meshes[kind] = m
	return m

## Upper half ring in the XY plane facing +z (rainbows, arches).
static func _arc_mesh(inner: float, outer: float, segs: int) -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in segs:
		var a0 := PI * i / segs
		var a1 := PI * (i + 1) / segs
		var p0 := Vector3(cos(a0), sin(a0), 0)
		var p1 := Vector3(cos(a1), sin(a1), 0)
		st.set_normal(Vector3.BACK)
		for v in [p0 * inner, p0 * outer, p1 * outer, p0 * inner, p1 * outer, p1 * inner]:
			st.set_uv(Vector2(0.5, 0.5))
			st.add_vertex(v)
	return st.commit()

# ---------------------------------------------------------------- materials

## Cel-shaded material (toon diffuse + toon specular + rim light).
static func toon(color: Color, emission := 0.0, gloss := 0.3, texture: Texture2D = null) -> StandardMaterial3D:
	var key := "t%s_%.2f_%.2f_%d" % [color.to_html(), emission, gloss, texture.get_instance_id() if texture else 0]
	if _mats.has(key): return _mats[key]
	var m := toon_instance(color, emission, gloss, texture)
	_mats[key] = m
	return m

static func toon_instance(color: Color, emission := 0.0, gloss := 0.3, texture: Texture2D = null) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.diffuse_mode = BaseMaterial3D.DIFFUSE_TOON
	m.specular_mode = BaseMaterial3D.SPECULAR_TOON
	m.roughness = 1.0 - gloss * 0.8
	m.metallic_specular = 0.25 + gloss * 0.4
	m.rim_enabled = true
	m.rim = 0.35
	m.rim_tint = 0.6
	if texture:
		m.albedo_texture = texture
	if emission > 0.0:
		m.emission_enabled = true
		m.emission = color
		m.emission_energy_multiplier = emission
	if color.a < 0.99:
		m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	return m

static func unlit(color: Color, additive := false, texture: Texture2D = null) -> StandardMaterial3D:
	var key := "u%s_%s_%d" % [color.to_html(), additive, texture.get_instance_id() if texture else 0]
	if _mats.has(key): return _mats[key]
	var m := unlit_instance(color, additive, texture)
	_mats[key] = m
	return m

static func unlit_instance(color: Color, additive := false, texture: Texture2D = null) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.albedo_color = color
	m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	m.cull_mode = BaseMaterial3D.CULL_DISABLED
	m.no_depth_test = false
	if additive: m.blend_mode = BaseMaterial3D.BLEND_MODE_ADD
	if texture: m.albedo_texture = texture
	return m

# ---------------------------------------------------------------- nodes

static func node(name: String, parent: Node, pos := Vector3.ZERO) -> Node3D:
	var n := Node3D.new()
	n.name = name
	parent.add_child(n)
	n.position = pos
	return n

static func part(parent: Node, kind: String, mat: Material, pos := Vector3.ZERO, scl := Vector3.ONE, rot_deg := Vector3.ZERO) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.mesh = mesh(kind)
	mi.material_override = mat
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(mi)
	mi.position = pos
	mi.scale = scl
	mi.rotation_degrees = rot_deg
	return mi

## Camera-facing soft glow sprite.
static func glow(parent: Node, pos: Vector3, size: float, color: Color, additive := true) -> MeshInstance3D:
	var key := "glow%s_%s" % [color.to_html(), additive]
	var m: StandardMaterial3D = _mats.get(key)
	if m == null:
		m = unlit_instance(color, additive, soft_circle())
		m.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
		m.billboard_keep_scale = true
		_mats[key] = m
	return part(parent, "quad", m, pos, Vector3(size, size, 1.0))

static func rope(parent: Node, a: Vector3, b: Vector3, thickness: float, mat: Material) -> MeshInstance3D:
	var mid := (a + b) * 0.5
	var dir := b - a
	var mi := part(parent, "cylinder", mat, mid, Vector3(thickness, dir.length(), thickness))
	if dir.length() > 0.0001:
		var up := dir.normalized()
		var axis := Vector3.UP.cross(up)
		if axis.length() > 0.0001:
			mi.basis = Basis(axis.normalized(), Vector3.UP.angle_to(up)) * Basis.from_scale(Vector3(thickness, dir.length(), thickness))
	return mi

# ---------------------------------------------------------------- textures

static func soft_circle() -> Texture2D:
	if _tex.has("soft"): return _tex.soft
	var g := Gradient.new()
	g.set_color(0, Color(1, 1, 1, 1))
	g.set_color(1, Color(1, 1, 1, 0))
	g.add_point(0.35, Color(1, 1, 1, 0.55))
	var t := GradientTexture2D.new()
	t.gradient = g
	t.fill = GradientTexture2D.FILL_RADIAL
	t.fill_from = Vector2(0.5, 0.5)
	t.fill_to = Vector2(1.0, 0.5)
	t.width = 64; t.height = 64
	_tex.soft = t
	return t

## Horizontal light streak (bright centre line, fading to the edges and ends).
static func streak() -> Texture2D:
	if _tex.has("streak"): return _tex.streak
	var w := 64
	var h := 32
	var img := Image.create(w, h, false, Image.FORMAT_RGBA8)
	for y in h:
		for x in w:
			var v := absf(float(y) / (h - 1) * 2.0 - 1.0)
			var u := absf(float(x) / (w - 1) * 2.0 - 1.0)
			var a := pow(1.0 - v, 1.6) * (1.0 - pow(u, 6.0))
			img.set_pixel(x, y, Color(1, 1, 1, clampf(a, 0.0, 1.0)))
	var t := ImageTexture.create_from_image(img)
	_tex.streak = t
	return t

static func vertical_gradient(top: Color, bottom: Color) -> Texture2D:
	var key := "g" + top.to_html() + bottom.to_html()
	if _tex.has(key): return _tex[key]
	var g := Gradient.new()
	g.set_color(0, top)
	g.set_color(1, bottom)
	var t := GradientTexture2D.new()
	t.gradient = g
	t.fill_from = Vector2(0, 0)
	t.fill_to = Vector2(0, 1)
	t.width = 4; t.height = 128
	_tex[key] = t
	return t

## Balloon envelope pattern (equirectangular: x = around, y = top to bottom).
static func balloon_pattern(b: Dictionary) -> Texture2D:
	var key := "p_" + str(b.id)
	if _tex.has(key): return _tex[key]
	var w := 128
	var h := 64
	var img := Image.create(w, h, false, Image.FORMAT_RGBA8)
	var c1: Color = b.c1
	var c2: Color = b.c2
	var c3: Color = b.c3
	var rainbow := [Color("#FF6B6B"), Color("#FECA57"), Color("#1DD1A1"), Color("#48DBFB"), Color("#5F27CD"), Color("#FF9FF3")]
	for y in h:
		for x in w:
			var u := float(x) / w
			var v := float(y) / h
			var c := c1
			match str(b.pattern):
				"stripes": c = c1 if int(u * 12.0) % 2 == 0 else c2
				"bands": c = c2 if int(v * 8.0) % 2 == 1 else c1
				"zigzag":
					var zz := absf(fmod(u * 16.0, 2.0) - 1.0)
					c = c2 if absf(v * 6.0 - floor(v * 6.0) - zz * 0.6 - 0.2) < 0.18 else c1
				"dots":
					var cell := Vector2(fmod(u * 16.0, 1.0), fmod(v * 8.0, 1.0)) - Vector2(0.5, 0.5)
					c = c2 if cell.length() < 0.3 else c1
				"stars":
					var cell2 := Vector2(fmod(u * 12.0 + (int(v * 6.0) % 2) * 0.5, 1.0), fmod(v * 6.0, 1.0)) - Vector2(0.5, 0.5)
					var ang := atan2(cell2.y, cell2.x)
					var r := 0.18 + 0.12 * cos(ang * 5.0)
					c = c3 if cell2.length() < r else c1
				"checker": c = c1 if (int(u * 12.0) + int(v * 6.0)) % 2 == 0 else c2
				"gradient": c = c1.lerp(c2, v)
				"swirl": c = c1 if int((u + v * 0.8) * 10.0) % 2 == 0 else c2
				"flames":
					var f := sin(u * TAU * 8.0) * 0.08 + 0.55
					c = c2.lerp(c3, clampf((v - f) * 4.0, 0.0, 1.0)) if v > f else c1
				"crystal":
					var cu := fmod(u * 10.0, 1.0)
					var cv := fmod(v * 5.0, 1.0)
					c = c1.lerp(c2, absf(cu - cv))
				"galaxy":
					c = c1.lerp(c2, 0.5 + 0.5 * sin(u * TAU * 2.0 + v * 9.0))
					if (x * 7919 + y * 104729) % 97 == 0: c = Color.WHITE
				"rainbow": c = rainbow[int(v * 6.0) % 6]
				"sunrays": c = c1 if int(u * 16.0) % 2 == 0 else c1.lerp(c2, 0.6 + 0.4 * v)
				"leaves":
					var l := Vector2(fmod(u * 10.0, 1.0) - 0.5, (fmod(v * 5.0, 1.0) - 0.5) * 1.8)
					c = c2 if l.length() < 0.35 else c1
				_: c = c1
			# Gold trim near the mouth
			if v > 0.9: c = c3
			img.set_pixel(x, y, c)
	var t := ImageTexture.create_from_image(img)
	_tex[key] = t
	return t

static func clear_cache() -> void:
	_mats.clear()
