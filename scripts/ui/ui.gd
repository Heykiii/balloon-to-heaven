class_name UI
## UI kit: shared theme, colours and small builders so every screen looks consistent.

const BG := Color("#14233F")
const PANEL := Color("#1E3560")
const PANEL_LIGHT := Color("#2B4A80")
const PRIMARY := Color("#3D8BFF")
const SECONDARY := Color("#8E5CF7")
const SUCCESS := Color("#27C46B")
const DANGER := Color("#F25555")
const WARNING := Color("#FFB238")
const GOLD := Color("#FFD34D")
const GEM := Color("#6FE3FF")
const TEXT := Color("#FFFFFF")
const TEXT_DIM := Color("#B8C7E6")

static var _theme: Theme

static func theme() -> Theme:
	if _theme: return _theme
	var t := Theme.new()
	t.default_font_size = 24
	t.set_color("font_color", "Label", TEXT)
	t.set_color("font_outline_color", "Label", Color(0.05, 0.08, 0.2, 0.85))
	t.set_constant("outline_size", "Label", 6)
	t.set_color("font_color", "Button", TEXT)
	t.set_color("font_hover_color", "Button", TEXT)
	t.set_color("font_pressed_color", "Button", TEXT)
	t.set_color("font_focus_color", "Button", TEXT)
	t.set_color("font_disabled_color", "Button", Color(1, 1, 1, 0.55))
	t.set_color("font_outline_color", "Button", Color(0.05, 0.08, 0.2, 0.6))
	t.set_constant("outline_size", "Button", 5)
	t.set_font_size("font_size", "Button", 26)
	t.set_stylebox("panel", "PanelContainer", box(PANEL, 26, Color(1, 1, 1, 0.12), 3))
	t.set_stylebox("panel", "Panel", box(PANEL, 26, Color(1, 1, 1, 0.12), 3))
	var empty := StyleBoxEmpty.new()
	t.set_stylebox("focus", "Button", empty)
	# sliders
	var track := box(Color(0, 0, 0, 0.35), 8)
	track.content_margin_top = 6; track.content_margin_bottom = 6
	t.set_stylebox("slider", "HSlider", track)
	var fill := box(PRIMARY, 8)
	fill.content_margin_top = 6; fill.content_margin_bottom = 6
	t.set_stylebox("grabber_area", "HSlider", fill)
	t.set_stylebox("grabber_area_highlight", "HSlider", fill)
	var knob := _circle_tex(30, Color.WHITE)
	t.set_icon("grabber", "HSlider", knob)
	t.set_icon("grabber_highlight", "HSlider", knob)
	# scroll bars thin and subtle
	var sb := box(Color(1, 1, 1, 0.25), 4)
	t.set_stylebox("grabber", "VScrollBar", sb)
	t.set_stylebox("grabber_highlight", "VScrollBar", sb)
	t.set_stylebox("grabber_pressed", "VScrollBar", sb)
	t.set_stylebox("scroll", "VScrollBar", StyleBoxEmpty.new())
	_theme = t
	return t

static func _circle_tex(size: int, c: Color) -> Texture2D:
	var img := Image.create(size, size, false, Image.FORMAT_RGBA8)
	var r := size * 0.5
	for y in size:
		for x in size:
			var d := Vector2(x + 0.5 - r, y + 0.5 - r).length()
			img.set_pixel(x, y, Color(c, clampf(r - d, 0.0, 1.0)))
	return ImageTexture.create_from_image(img)

static func box(color: Color, radius := 18, border := Color(0, 0, 0, 0), border_w := 0, shadow := 0) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = color
	s.set_corner_radius_all(radius)
	if border_w > 0:
		s.border_color = border
		s.set_border_width_all(border_w)
	if shadow > 0:
		s.shadow_color = Color(0, 0, 0, 0.3)
		s.shadow_size = shadow
		s.shadow_offset = Vector2(0, shadow * 0.5)
	s.content_margin_left = 14; s.content_margin_right = 14
	s.content_margin_top = 10; s.content_margin_bottom = 10
	s.anti_aliasing = true
	return s

## Chunky game button with a darker "lip" underneath and a press animation.
static func button(text: String, color: Color, on_press: Callable, font_size := 26, min_size := Vector2(0, 64)) -> Button:
	var b := Button.new()
	b.text = text
	b.custom_minimum_size = min_size
	b.add_theme_font_size_override("font_size", font_size)
	var normal := box(color, 20, color.darkened(0.35), 0)
	normal.border_width_bottom = 6
	normal.border_color = color.darkened(0.35)
	var hover := normal.duplicate()
	hover.bg_color = color.lightened(0.1)
	var pressed := normal.duplicate()
	pressed.bg_color = color.darkened(0.1)
	pressed.border_width_bottom = 2
	pressed.content_margin_top = 14
	var disabled := normal.duplicate()
	disabled.bg_color = color.lerp(Color(0.4, 0.45, 0.55), 0.7)
	disabled.border_color = disabled.bg_color.darkened(0.3)
	b.add_theme_stylebox_override("normal", normal)
	b.add_theme_stylebox_override("hover", hover)
	b.add_theme_stylebox_override("pressed", pressed)
	b.add_theme_stylebox_override("disabled", disabled)
	b.add_theme_stylebox_override("focus", StyleBoxEmpty.new())
	b.pressed.connect(func():
		Sound.play("click", 0.7)
		on_press.call())
	b.button_down.connect(func(): _press_anim(b, 0.94))
	b.button_up.connect(func(): _press_anim(b, 1.0))
	b.resized.connect(func(): b.pivot_offset = b.size * 0.5)
	return b

static func _press_anim(c: Control, s: float) -> void:
	if not c.is_inside_tree(): return
	var tw := c.create_tween()
	tw.tween_property(c, "scale", Vector2(s, s), 0.07)

static func label(text: String, size := 24, color := TEXT, align := HORIZONTAL_ALIGNMENT_CENTER) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	l.horizontal_alignment = align
	l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	return l

static func wrap(l: Label) -> Label:
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	return l

static func panel(color := PANEL, radius := 26) -> PanelContainer:
	var p := PanelContainer.new()
	p.add_theme_stylebox_override("panel", box(color, radius, Color(1, 1, 1, 0.14), 3, 10))
	return p

static func vbox(sep := 12) -> VBoxContainer:
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", sep)
	return v

static func hbox(sep := 12) -> HBoxContainer:
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", sep)
	return h

static func margin(c: Control, l: int, t: int, r: int, b: int) -> MarginContainer:
	var m := MarginContainer.new()
	m.add_theme_constant_override("margin_left", l)
	m.add_theme_constant_override("margin_top", t)
	m.add_theme_constant_override("margin_right", r)
	m.add_theme_constant_override("margin_bottom", b)
	m.add_child(c)
	return m

static func spacer(h := 0, w := 0) -> Control:
	var c := Control.new()
	c.custom_minimum_size = Vector2(w, h)
	return c

static func expand(c: Control) -> Control:
	c.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	return c

static func full(c: Control) -> Control:
	c.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	return c

## Coin/gem counter pill.
static func currency(icon: String, value: int, color: Color) -> HBoxContainer:
	var h := hbox(6)
	h.add_child(Icon.make(icon, 30, color))
	var l := label(U.fmt(value), 24)
	l.name = "Value"
	h.add_child(l)
	return h

static func pop_in(c: Control) -> void:
	c.pivot_offset = c.size * 0.5
	c.scale = Vector2(0.85, 0.85)
	c.modulate.a = 0.0
	var tw := c.create_tween().set_parallel()
	tw.tween_property(c, "scale", Vector2.ONE, 0.25).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tw.tween_property(c, "modulate:a", 1.0, 0.18)

## Dimmed full-screen layer with a centred panel; returns [root, content vbox].
static func modal(parent: Node, width := 470.0) -> Array:
	var root := Control.new()
	full(root)
	root.mouse_filter = Control.MOUSE_FILTER_STOP
	var dim := ColorRect.new()
	dim.color = Color(0.02, 0.04, 0.12, 0.7)
	full(dim)
	root.add_child(dim)
	var center := CenterContainer.new()
	full(center)
	root.add_child(center)
	var p := panel()
	p.custom_minimum_size = Vector2(width, 0)
	center.add_child(p)
	var v := vbox(14)
	p.add_child(margin(v, 10, 10, 10, 10))
	parent.add_child(root)
	p.resized.connect(func(): p.pivot_offset = p.size * 0.5, CONNECT_ONE_SHOT)
	pop_in.call_deferred(p)
	return [root, v]

static func rarity_color(r: int) -> Color:
	return C.RARITY_COLORS[clampi(r, 0, C.RARITY_COLORS.size() - 1)]

## Top bar: back button, title and live coin/gem counters.
static func top_bar(title: String, on_back: Callable) -> PanelContainer:
	var bar := PanelContainer.new()
	bar.add_theme_stylebox_override("panel", box(Color(0.05, 0.09, 0.22, 0.72), 0))
	bar.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	var h := hbox(10)
	bar.add_child(h)
	if on_back.is_valid():
		var back := button("", PANEL_LIGHT, on_back, 24, Vector2(58, 54))
		attach_icon(back, Icon.make("back", 26))
		h.add_child(back)
	var t := label(title, 30, TEXT, HORIZONTAL_ALIGNMENT_LEFT)
	expand(t)
	h.add_child(t)
	var coins := currency("coin", Game.economy.coins, GOLD)
	var gems := currency("gem", Game.economy.gems, GEM)
	h.add_child(coins)
	h.add_child(spacer(0, 4))
	h.add_child(gems)
	var update := func(c: int, g: int):
		if is_instance_valid(coins): (coins.get_node("Value") as Label).text = U.fmt(c)
		if is_instance_valid(gems): (gems.get_node("Value") as Label).text = U.fmt(g)
	Events.currency_changed.connect(update)
	bar.tree_exiting.connect(func(): Events.currency_changed.disconnect(update))
	return bar

## Small stat bar used by the shop and workshop.
static func stat_row(name: String, value: float, max_value: float, text: String, color: Color) -> HBoxContainer:
	var h := hbox(8)
	var n := label(name, 20, TEXT_DIM, HORIZONTAL_ALIGNMENT_LEFT)
	n.custom_minimum_size = Vector2(110, 0)
	h.add_child(n)
	var bar := ProgressBar.new()
	bar.show_percentage = false
	bar.max_value = max_value
	bar.value = value
	bar.custom_minimum_size = Vector2(0, 14)
	bar.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	bar.add_theme_stylebox_override("background", box(Color(0, 0, 0, 0.35), 7))
	bar.add_theme_stylebox_override("fill", box(color, 7))
	expand(bar)
	h.add_child(bar)
	var v := label(text, 20, TEXT, HORIZONTAL_ALIGNMENT_RIGHT)
	v.custom_minimum_size = Vector2(64, 0)
	h.add_child(v)
	return h

## Pins an icon inside a button: vertically centred, at the left edge or in the middle.
static func attach_icon(b: Control, ic: Control, left := -1.0) -> void:
	var s := ic.custom_minimum_size
	ic.anchor_top = 0.5
	ic.anchor_bottom = 0.5
	ic.offset_top = -s.y * 0.5
	ic.offset_bottom = s.y * 0.5
	if left < 0.0:
		ic.anchor_left = 0.5
		ic.anchor_right = 0.5
		ic.offset_left = -s.x * 0.5
		ic.offset_right = s.x * 0.5
	else:
		ic.anchor_left = 0.0
		ic.anchor_right = 0.0
		ic.offset_left = left
		ic.offset_right = left + s.x
	b.add_child(ic)
