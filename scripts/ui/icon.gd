class_name Icon
extends Control
## Vector icons drawn in code (no image assets): coin, gem, star, heart, lock, pause, back,
## shield, magnet, rocket, double, gear, trophy, play, shop, wrench, check, close, balloon, map.

var kind := "star"
var color := Color.WHITE
var filled := true

static func make(p_kind: String, size := 32.0, p_color := Color.WHITE, p_filled := true) -> Icon:
	var i := Icon.new()
	i.kind = p_kind
	i.color = p_color
	i.filled = p_filled
	i.custom_minimum_size = Vector2(size, size)
	i.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return i

func set_kind(k: String, c := color, f := true) -> void:
	kind = k
	color = c
	filled = f
	queue_redraw()

static func star_points(center: Vector2, r_out: float, r_in: float, points := 5) -> PackedVector2Array:
	var pts := PackedVector2Array()
	for i in points * 2:
		var a := -PI / 2 + i * PI / points
		var r := r_out if i % 2 == 0 else r_in
		pts.append(center + Vector2(cos(a), sin(a)) * r)
	return pts

func _outline(pts: PackedVector2Array, c: Color, w: float) -> void:
	var closed := pts.duplicate()
	closed.append(pts[0])
	draw_polyline(closed, c, w, true)

func _draw() -> void:
	var s := minf(size.x, size.y)
	var c := size * 0.5
	var r := s * 0.5
	var dark := color.darkened(0.45)
	var ow := maxf(1.5, s * 0.07)
	match kind:
		"coin":
			draw_circle(c, r * 0.92, dark)
			draw_circle(c, r * 0.78, color)
			draw_circle(c + Vector2(-r * 0.2, -r * 0.2), r * 0.22, color.lightened(0.45))
			draw_rect(Rect2(c + Vector2(-r * 0.12, -r * 0.42), Vector2(r * 0.24, r * 0.84)), dark.lerp(color, 0.35))
		"gem":
			var pts := PackedVector2Array([c + Vector2(-r * 0.85, -r * 0.3), c + Vector2(-r * 0.45, -r * 0.8), c + Vector2(r * 0.45, -r * 0.8),
				c + Vector2(r * 0.85, -r * 0.3), c + Vector2(0, r * 0.9)])
			draw_colored_polygon(pts, color)
			draw_colored_polygon(PackedVector2Array([c + Vector2(-r * 0.85, -r * 0.3), c + Vector2(r * 0.85, -r * 0.3), c + Vector2(0, r * 0.9)]), color.darkened(0.15))
			draw_colored_polygon(PackedVector2Array([c + Vector2(-r * 0.45, -r * 0.8), c + Vector2(-r * 0.1, -r * 0.3), c + Vector2(-r * 0.85, -r * 0.3)]), color.lightened(0.4))
			_outline(pts, dark, ow)
		"star":
			var pts2 := star_points(c + Vector2(0, r * 0.06), r * 0.98, r * 0.45)
			if filled:
				draw_colored_polygon(pts2, color)
				draw_colored_polygon(star_points(c + Vector2(-r * 0.05, -r * 0.02), r * 0.55, r * 0.25), color.lightened(0.3))
				_outline(pts2, dark, ow)
			else:
				draw_colored_polygon(pts2, Color(0, 0, 0, 0.3))
				_outline(pts2, Color(1, 1, 1, 0.35), ow)
		"heart":
			var hc := color if filled else Color(0, 0, 0, 0.35)
			draw_circle(c + Vector2(-r * 0.42, -r * 0.2), r * 0.46, hc)
			draw_circle(c + Vector2(r * 0.42, -r * 0.2), r * 0.46, hc)
			draw_colored_polygon(PackedVector2Array([c + Vector2(-r * 0.86, -r * 0.05), c + Vector2(r * 0.86, -r * 0.05), c + Vector2(0, r * 0.85)]), hc)
			if filled: draw_circle(c + Vector2(-r * 0.5, -r * 0.35), r * 0.14, Color(1, 1, 1, 0.6))
		"lock":
			draw_arc(c + Vector2(0, -r * 0.15), r * 0.42, PI, TAU, 16, color, r * 0.16, true)
			draw_rect(Rect2(c + Vector2(-r * 0.6, -r * 0.15), Vector2(r * 1.2, r * 0.95)), color)
			draw_circle(c + Vector2(0, r * 0.25), r * 0.14, dark)
		"pause":
			draw_rect(Rect2(c + Vector2(-r * 0.55, -r * 0.6), Vector2(r * 0.38, r * 1.2)), color)
			draw_rect(Rect2(c + Vector2(r * 0.17, -r * 0.6), Vector2(r * 0.38, r * 1.2)), color)
		"back":
			draw_polyline(PackedVector2Array([c + Vector2(r * 0.25, -r * 0.6), c + Vector2(-r * 0.4, 0), c + Vector2(r * 0.25, r * 0.6)]), color, r * 0.28, true)
		"play":
			draw_colored_polygon(PackedVector2Array([c + Vector2(-r * 0.45, -r * 0.65), c + Vector2(r * 0.7, 0), c + Vector2(-r * 0.45, r * 0.65)]), color)
		"shield":
			var sp := PackedVector2Array([c + Vector2(0, -r * 0.9), c + Vector2(r * 0.8, -r * 0.55), c + Vector2(r * 0.65, r * 0.3),
				c + Vector2(0, r * 0.92), c + Vector2(-r * 0.65, r * 0.3), c + Vector2(-r * 0.8, -r * 0.55)])
			draw_colored_polygon(sp, color)
			draw_colored_polygon(PackedVector2Array([c + Vector2(0, -r * 0.9), c + Vector2(0, r * 0.92), c + Vector2(-r * 0.65, r * 0.3), c + Vector2(-r * 0.8, -r * 0.55)]), color.lightened(0.25))
			_outline(sp, dark, ow)
		"magnet":
			draw_arc(c + Vector2(0, -r * 0.05), r * 0.55, 0, PI, 20, color, r * 0.36, true)
			draw_rect(Rect2(c + Vector2(-r * 0.73, -r * 0.7), Vector2(r * 0.36, r * 0.65)), color)
			draw_rect(Rect2(c + Vector2(r * 0.37, -r * 0.7), Vector2(r * 0.36, r * 0.65)), color)
			draw_rect(Rect2(c + Vector2(-r * 0.73, -r * 0.9), Vector2(r * 0.36, r * 0.25)), Color.WHITE)
			draw_rect(Rect2(c + Vector2(r * 0.37, -r * 0.9), Vector2(r * 0.36, r * 0.25)), Color.WHITE)
		"rocket":
			draw_colored_polygon(PackedVector2Array([c + Vector2(0, -r * 0.95), c + Vector2(r * 0.35, -r * 0.35), c + Vector2(r * 0.35, r * 0.45),
				c + Vector2(-r * 0.35, r * 0.45), c + Vector2(-r * 0.35, -r * 0.35)]), Color.WHITE)
			draw_colored_polygon(PackedVector2Array([c + Vector2(0, -r * 0.95), c + Vector2(r * 0.3, -r * 0.45), c + Vector2(-r * 0.3, -r * 0.45)]), color)
			draw_colored_polygon(PackedVector2Array([c + Vector2(-r * 0.35, r * 0.05), c + Vector2(-r * 0.7, r * 0.6), c + Vector2(-r * 0.35, r * 0.45)]), color)
			draw_colored_polygon(PackedVector2Array([c + Vector2(r * 0.35, r * 0.05), c + Vector2(r * 0.7, r * 0.6), c + Vector2(r * 0.35, r * 0.45)]), color)
			draw_colored_polygon(PackedVector2Array([c + Vector2(-r * 0.2, r * 0.5), c + Vector2(r * 0.2, r * 0.5), c + Vector2(0, r * 0.95)]), Color(1, 0.6, 0.2))
			draw_circle(c + Vector2(0, -r * 0.2), r * 0.15, color)
		"double":
			draw_circle(c + Vector2(-r * 0.25, r * 0.15), r * 0.6, color.darkened(0.3))
			draw_circle(c + Vector2(-r * 0.25, r * 0.15), r * 0.48, color.darkened(0.1))
			draw_circle(c + Vector2(r * 0.25, -r * 0.15), r * 0.6, color.darkened(0.3))
			draw_circle(c + Vector2(r * 0.25, -r * 0.15), r * 0.48, color)
		"gear":
			for i in 8:
				var a := i * TAU / 8.0
				draw_line(c, c + Vector2(cos(a), sin(a)) * r * 0.92, color, r * 0.32)
			draw_circle(c, r * 0.65, color)
			draw_circle(c, r * 0.28, dark)
		"trophy":
			draw_colored_polygon(PackedVector2Array([c + Vector2(-r * 0.6, -r * 0.8), c + Vector2(r * 0.6, -r * 0.8), c + Vector2(r * 0.45, -r * 0.1),
				c + Vector2(0, r * 0.2), c + Vector2(-r * 0.45, -r * 0.1)]), color)
			draw_arc(c + Vector2(-r * 0.6, -r * 0.45), r * 0.28, PI * 0.5, PI * 1.5, 10, color, r * 0.12)
			draw_arc(c + Vector2(r * 0.6, -r * 0.45), r * 0.28, -PI * 0.5, PI * 0.5, 10, color, r * 0.12)
			draw_rect(Rect2(c + Vector2(-r * 0.1, r * 0.15), Vector2(r * 0.2, r * 0.4)), color)
			draw_rect(Rect2(c + Vector2(-r * 0.45, r * 0.55), Vector2(r * 0.9, r * 0.3)), dark.lerp(color, 0.4))
		"shop":
			draw_rect(Rect2(c + Vector2(-r * 0.75, -r * 0.3), Vector2(r * 1.5, r * 1.05)), color)
			draw_arc(c + Vector2(0, -r * 0.3), r * 0.4, PI, TAU, 14, color, r * 0.16, true)
			draw_circle(c + Vector2(0, r * 0.2), r * 0.16, dark)
		"wrench":
			draw_line(c + Vector2(-r * 0.55, r * 0.55), c + Vector2(r * 0.25, -r * 0.25), color, r * 0.3, true)
			draw_arc(c + Vector2(r * 0.4, -r * 0.4), r * 0.38, -PI * 0.25, PI * 1.25, 14, color, r * 0.26, true)
		"check":
			draw_polyline(PackedVector2Array([c + Vector2(-r * 0.6, 0), c + Vector2(-r * 0.15, r * 0.5), c + Vector2(r * 0.65, -r * 0.5)]), color, r * 0.28, true)
		"close":
			draw_line(c + Vector2(-r * 0.5, -r * 0.5), c + Vector2(r * 0.5, r * 0.5), color, r * 0.26, true)
			draw_line(c + Vector2(r * 0.5, -r * 0.5), c + Vector2(-r * 0.5, r * 0.5), color, r * 0.26, true)
		"balloon":
			draw_circle(c + Vector2(0, -r * 0.2), r * 0.62, color)
			draw_colored_polygon(PackedVector2Array([c + Vector2(-r * 0.5, r * 0.1), c + Vector2(r * 0.5, r * 0.1), c + Vector2(r * 0.15, r * 0.55), c + Vector2(-r * 0.15, r * 0.55)]), color)
			draw_rect(Rect2(c + Vector2(-r * 0.18, r * 0.65), Vector2(r * 0.36, r * 0.28)), Color(0.6, 0.4, 0.25))
			draw_circle(c + Vector2(-r * 0.22, -r * 0.4), r * 0.15, Color(1, 1, 1, 0.5))
		"map":
			draw_colored_polygon(PackedVector2Array([c + Vector2(-r * 0.85, -r * 0.6), c + Vector2(-r * 0.3, -r * 0.8), c + Vector2(-r * 0.3, r * 0.65), c + Vector2(-r * 0.85, r * 0.85)]), color)
			draw_colored_polygon(PackedVector2Array([c + Vector2(-r * 0.3, -r * 0.8), c + Vector2(r * 0.3, -r * 0.6), c + Vector2(r * 0.3, r * 0.85), c + Vector2(-r * 0.3, r * 0.65)]), color.darkened(0.2))
			draw_colored_polygon(PackedVector2Array([c + Vector2(r * 0.3, -r * 0.6), c + Vector2(r * 0.85, -r * 0.8), c + Vector2(r * 0.85, r * 0.65), c + Vector2(r * 0.3, r * 0.85)]), color)
		"music":
			draw_circle(c + Vector2(-r * 0.4, r * 0.5), r * 0.25, color)
			draw_circle(c + Vector2(r * 0.45, r * 0.35), r * 0.25, color)
			draw_line(c + Vector2(-r * 0.17, r * 0.5), c + Vector2(-r * 0.17, -r * 0.7), color, r * 0.12)
			draw_line(c + Vector2(r * 0.68, r * 0.35), c + Vector2(r * 0.68, -r * 0.85), color, r * 0.12)
			draw_line(c + Vector2(-r * 0.17, -r * 0.7), c + Vector2(r * 0.68, -r * 0.85), color, r * 0.22)
		_:
			draw_circle(c, r * 0.8, color)
