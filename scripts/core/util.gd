class_name U
## Small helpers: easing, formatting, deterministic random.

static func ease_out_cubic(t: float) -> float:
	t = 1.0 - clampf(t, 0.0, 1.0)
	return 1.0 - t * t * t

static func ease_in_out_sine(t: float) -> float:
	return -(cos(PI * clampf(t, 0.0, 1.0)) - 1.0) * 0.5

static func ease_out_back(t: float) -> float:
	var c1 := 1.70158
	var c3 := c1 + 1.0
	var x := clampf(t, 0.0, 1.0) - 1.0
	return 1.0 + c3 * x * x * x + c1 * x * x

static func fmt(n: int) -> String:
	var s := str(absi(n))
	var out := ""
	while s.length() > 3:
		out = "," + s.substr(s.length() - 3) + out
		s = s.substr(0, s.length() - 3)
	return ("-" if n < 0 else "") + s + out

static func lighten(c: Color, amount: float) -> Color:
	return c.lerp(Color.WHITE, amount)

static func rng(seed_value: int) -> RandomNumberGenerator:
	var r := RandomNumberGenerator.new()
	r.seed = seed_value
	return r
