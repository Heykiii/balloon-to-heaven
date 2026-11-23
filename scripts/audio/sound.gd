extends Node
## Audio autoload "Sound". Everything is synthesised at startup, so the game ships no audio files.
## SFX are short rendered samples. Music is a small sequencer: one rendered sample per instrument,
## re-pitched per note (pitch_scale), which is cheap enough for the web build.

enum Cue { MENU, WORLD, BOSS, VICTORY, FAIL, ENDING }

const SR := 22050
const VOICES := 10
const MUSIC_VOICES := 20
const BASE_FREQ := 220.0 # A3: every instrument sample is rendered at this pitch

var music_volume := 0.7
var sfx_volume := 0.8
var ducked := false

var _sfx := {}
var _last := {}
var _voices: Array[AudioStreamPlayer] = []
var _next_voice := 0
var _inst := {}
var _mvoices: Array[AudioStreamPlayer] = []
var _next_mvoice := 0
var _noise := RandomNumberGenerator.new()

# sequencer
var _events: Array = []     # [beat, instrument, midi, length_beats, volume]
var _loop_beats := 32.0
var _beat_len := 0.6
var _looping := true
var _song_t := 0.0
var _cursor := 0
var _playing := false
var _key := ""
var _fade := 1.0
var _ready_done := false

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_noise.seed = 22222
	for i in VOICES:
		var p := AudioStreamPlayer.new()
		add_child(p)
		_voices.append(p)
	for i in MUSIC_VOICES:
		var p := AudioStreamPlayer.new()
		add_child(p)
		_mvoices.append(p)

## Renders every sample. Called by the boot screen (spread over frames there).
func prepare_step(i: int) -> bool:
	var names := sfx_names()
	if i < names.size():
		_sfx[names[i]] = _make_sfx(names[i])
		return false
	var inst := ["pad", "bass", "triangle", "square", "bell", "sine", "kick", "hat"]
	var j := i - names.size()
	if j < inst.size():
		_inst[inst[j]] = _make_inst(inst[j])
		return false
	_ready_done = true
	return true

func prepare_all() -> void:
	var i := 0
	while not prepare_step(i): i += 1

func steps_total() -> int: return sfx_names().size() + 8

static func sfx_names() -> Array:
	return ["click", "coin", "special_coin", "hit", "shield", "powerup", "upgrade", "purchase", "boss_attack",
		"level_complete", "fail", "whoosh", "thunder", "deny", "star", "boss_defeat", "unlock", "crystal", "teleport"]

# ---------------------------------------------------------------- SFX

func play(sfx_name: String, volume := 1.0) -> void:
	if sfx_volume <= 0.001: return
	var now := Time.get_ticks_msec()
	if now - int(_last.get(sfx_name, -1000)) < 35: return # no stacking in one frame (coin lines)
	_last[sfx_name] = now
	if not _sfx.has(sfx_name): _sfx[sfx_name] = _make_sfx(sfx_name)
	var p := _voices[_next_voice]
	_next_voice = (_next_voice + 1) % _voices.size()
	p.stream = _sfx[sfx_name]
	p.pitch_scale = randf_range(0.96, 1.08) if sfx_name == "coin" else randf_range(0.98, 1.02)
	p.volume_db = linear_to_db(maxf(0.0001, clampf(volume, 0.0, 1.0) * sfx_volume))
	p.play()

static func _osc(wave: String, ph: float) -> float:
	var p := ph - floorf(ph)
	match wave:
		"sine": return sin(p * TAU)
		"triangle": return 1.0 - 4.0 * absf(p - 0.5)
		"square": return clampf(sin(p * TAU) * 3.0, -1.0, 1.0) * 0.7
		"bell": return sin(p * TAU) * 0.7 + sin(p * TAU * 2.76) * 0.2 + sin(p * TAU * 5.4) * 0.1
	return 0.0

## tone: [start, length, f0, f1, volume, wave, noise, attack, vibrato]
func _render(length: float, tones: Array) -> AudioStreamWAV:
	var n := int(ceil(length * SR))
	var data := PackedFloat32Array()
	data.resize(n)
	for t in tones:
		var s0 := int(t[0] * SR)
		var count := int(ceil(t[1] * SR))
		var wave: String = t[5] if t.size() > 5 else "sine"
		var nz: float = t[6] if t.size() > 6 else 0.0
		var attack: float = t[7] if t.size() > 7 else 0.005
		var vib: float = t[8] if t.size() > 8 else 0.0
		var phase := 0.0
		for i in count:
			if s0 + i >= n: break
			var u := float(i) / count
			var f := lerpf(t[2], t[3], u) * (1.0 + sin(float(i) / SR * 34.0) * vib)
			phase += f / SR
			var env := minf(1.0, i / (maxf(0.001, attack) * SR)) * (1.0 - u) * (1.0 - u)
			var v := _osc(wave, phase) * (1.0 - nz) + (_noise.randf() * 2.0 - 1.0) * nz
			data[s0 + i] += v * env * t[4]
	return _to_wav(data)

static func _to_wav(data: PackedFloat32Array, loop := false) -> AudioStreamWAV:
	var bytes := PackedByteArray()
	bytes.resize(data.size() * 2)
	for i in data.size():
		var x := data[i]
		x = x / (1.0 + absf(x)) # soft clip
		bytes.encode_s16(i * 2, int(clampf(x, -1.0, 1.0) * 32000.0))
	var w := AudioStreamWAV.new()
	w.format = AudioStreamWAV.FORMAT_16_BITS
	w.mix_rate = SR
	w.stereo = false
	w.data = bytes
	if loop:
		w.loop_mode = AudioStreamWAV.LOOP_FORWARD
		w.loop_end = data.size()
	return w

func _make_sfx(id: String) -> AudioStreamWAV:
	match id:
		"click": return _render(0.08, [[0, 0.07, 900, 600, 0.6, "triangle"]])
		"coin": return _render(0.22, [[0, 0.08, 1318, 1318, 0.5, "bell"], [0.06, 0.15, 1760, 1760, 0.55, "bell"]])
		"special_coin": return _render(0.5, [[0, 0.15, 1046, 1046, 0.5, "bell"], [0.1, 0.15, 1318, 1318, 0.5, "bell"],
			[0.2, 0.3, 1568, 1568, 0.55, "bell"], [0.2, 0.3, 2093, 2093, 0.25, "sine"]])
		"hit": return _render(0.35, [[0, 0.3, 220, 70, 0.9, "square"], [0, 0.15, 0, 0, 0.6, "sine", 1.0]])
		"shield": return _render(0.45, [[0, 0.4, 400, 900, 0.5, "sine", 0.0, 0.02, 0.02], [0.02, 0.4, 600, 1300, 0.3, "triangle"]])
		"powerup": return _render(0.5, [[0, 0.12, 523, 523, 0.45, "triangle"], [0.1, 0.12, 659, 659, 0.45, "triangle"],
			[0.2, 0.12, 784, 784, 0.45, "triangle"], [0.3, 0.2, 1046, 1046, 0.5, "triangle"]])
		"upgrade": return _render(0.7, [[0, 0.6, 300, 1200, 0.45, "triangle", 0.0, 0.01], [0.35, 0.35, 1046, 1046, 0.4, "bell"], [0.45, 0.25, 1568, 1568, 0.3, "bell"]])
		"purchase": return _render(0.6, [[0, 0.12, 1568, 1568, 0.4, "bell"], [0.08, 0.12, 2093, 2093, 0.4, "bell"],
			[0.16, 0.4, 2637, 2637, 0.4, "bell"], [0, 0.08, 0, 0, 0.25, "sine", 1.0]])
		"boss_attack": return _render(0.6, [[0, 0.55, 110, 55, 0.8, "square", 0.25, 0.02], [0, 0.4, 82, 41, 0.6, "sine"]])
		"level_complete": return _render(1.2, [[0, 0.2, 523, 523, 0.45, "triangle"], [0.15, 0.2, 659, 659, 0.45, "triangle"],
			[0.3, 0.2, 784, 784, 0.45, "triangle"], [0.45, 0.7, 1046, 1046, 0.5, "triangle", 0.0, 0.01, 0.01], [0.45, 0.7, 659, 659, 0.3, "sine"]])
		"fail": return _render(1.0, [[0, 0.3, 392, 370, 0.45, "triangle"], [0.28, 0.3, 330, 311, 0.45, "triangle"], [0.56, 0.45, 262, 220, 0.5, "triangle", 0.0, 0.01, 0.02]])
		"whoosh": return _render(0.5, [[0, 0.45, 0, 0, 0.5, "sine", 1.0, 0.2]])
		"thunder": return _render(1.2, [[0, 1.1, 60, 30, 0.6, "sine", 0.85, 0.01], [0, 0.2, 0, 0, 0.6, "sine", 1.0]])
		"deny": return _render(0.25, [[0, 0.1, 180, 170, 0.5, "square"], [0.12, 0.12, 150, 140, 0.5, "square"]])
		"star": return _render(0.6, [[0, 0.5, 1568, 2093, 0.45, "bell", 0.0, 0.005, 0.01], [0.05, 0.5, 2637, 3136, 0.2, "sine"]])
		"boss_defeat": return _render(2.0, [[0, 1.4, 120, 40, 0.7, "sine", 0.6, 0.01], [0.6, 0.3, 523, 523, 0.4, "triangle"],
			[0.85, 0.3, 659, 659, 0.4, "triangle"], [1.1, 0.9, 784, 784, 0.45, "triangle", 0.0, 0.01, 0.01]])
		"unlock": return _render(0.8, [[0, 0.3, 784, 784, 0.4, "bell"], [0.15, 0.3, 988, 988, 0.4, "bell"], [0.3, 0.5, 1175, 1175, 0.45, "bell"]])
		"crystal": return _render(0.7, [[0, 0.6, 1760, 1760, 0.45, "bell", 0.0, 0.005, 0.015], [0.03, 0.6, 2637, 2637, 0.3, "bell"], [0.06, 0.5, 3520, 3520, 0.15, "sine"]])
		"teleport": return _render(0.4, [[0, 0.35, 2000, 300, 0.4, "triangle"], [0, 0.35, 300, 2000, 0.25, "sine"]])
	return _render(0.1, [[0, 0.1, 440, 440, 0.3]])

# ---------------------------------------------------------------- instruments

## One note at BASE_FREQ with its own envelope; the sequencer re-pitches it.
func _make_inst(kind: String) -> AudioStreamWAV:
	var length := 0.6
	var wave := kind
	var attack := 0.01
	var sustain := 0.35
	match kind:
		"pad": length = 2.6; wave = "sine"; attack = 0.25; sustain = 0.5
		"bass": length = 1.2; wave = "triangle"; attack = 0.01; sustain = 0.2
		"bell": length = 0.9
		"kick":
			var n := SR / 6
			var d := PackedFloat32Array()
			d.resize(n)
			var ph := 0.0
			for i in n:
				var u := float(i) / n
				ph += lerpf(120.0, 45.0, sqrt(u)) / SR
				d[i] = sin(ph * TAU) * (1.0 - u) * (1.0 - u)
			return _to_wav(d)
		"hat":
			var n2 := int(SR * 0.08)
			var d2 := PackedFloat32Array()
			d2.resize(n2)
			var prev := 0.0
			for i in n2:
				var u2 := float(i) / n2
				var nz := _noise.randf() * 2.0 - 1.0
				d2[i] = (nz - prev) * (1.0 - u2) * (1.0 - u2) * 0.8
				prev = nz
			return _to_wav(d2)
	var count := int(length * SR)
	var data := PackedFloat32Array()
	data.resize(count)
	var inc := BASE_FREQ / SR
	var phase := 0.0
	var an := maxi(1, int(attack * SR))
	for i in count:
		var u := float(i) / count
		var env := minf(1.0, float(i) / an) * lerpf(1.0, sustain, u) * minf(1.0, (count - i) / (SR * 0.02))
		data[i] = _osc(wave, phase) * env
		phase += inc
	return _to_wav(data)

# ---------------------------------------------------------------- music

const SCALES := {
	"major": [0, 2, 4, 5, 7, 9, 11], "minor": [0, 2, 3, 5, 7, 8, 10], "pentatonic": [0, 2, 4, 7, 9, 12, 14],
	"dorian": [0, 2, 3, 5, 7, 9, 10], "lydian": [0, 2, 4, 6, 7, 9, 11], "harmonic_minor": [0, 2, 3, 5, 7, 8, 11],
}
const PROGRESSIONS := [[0, 4, 5, 3], [0, 5, 3, 4], [0, 3, 4, 3], [5, 3, 0, 4]]

static func style_for(cue: int, world: Dictionary) -> Dictionary:
	var base := world if not world.is_empty() else {"tempo": 96, "root": 60, "scale": "major", "lead": "bell", "percussion": 0.25, "brightness": 0.6, "seed": 7}
	match cue:
		Cue.MENU: return {"tempo": 88, "root": 62, "scale": "lydian", "lead": "bell", "percussion": 0.2, "brightness": 0.6, "seed": 3}
		Cue.BOSS:
			return {"tempo": roundi(float(base.tempo) * 1.2), "root": int(base.root) - 2,
				"scale": "harmonic_minor" if base.scale == "major" or base.scale == "lydian" else base.scale,
				"lead": "square", "percussion": maxf(0.7, float(base.percussion)), "brightness": 0.7, "seed": int(base.seed) + 100}
		Cue.ENDING: return {"tempo": 76, "root": 60, "scale": "lydian", "lead": "bell", "percussion": 0.15, "brightness": 0.9, "seed": 777}
	return base

func music(cue: int, world: Dictionary = {}) -> void:
	var st := style_for(cue, world)
	var key := "%d_%s_%s_%s_%s" % [cue, st.tempo, st.root, st.scale, st.seed]
	if key == _key and _playing: return
	_key = key
	_beat_len = 60.0 / clampf(float(st.tempo), 50.0, 200.0)
	_song_t = 0.0
	_cursor = 0
	_fade = 0.0
	_playing = true
	if cue == Cue.VICTORY:
		_events = _stinger([0, 2, 4, 7, 9, 7, 11, 12], true, int(st.root))
		_looping = false
	elif cue == Cue.FAIL:
		_events = _stinger([7, 5, 3, 2, 0], false, int(st.root))
		_looping = false
	else:
		_events = _compose(st, cue)
		_looping = true

func stop_music() -> void:
	_playing = false
	_key = ""

func _compose(st: Dictionary, cue: int) -> Array:
	var rng := RandomNumberGenerator.new()
	rng.seed = int(st.seed) * 31 + cue
	var scale: Array = SCALES.get(st.scale, SCALES.major)
	var prog: Array = PROGRESSIONS[rng.randi() % PROGRESSIONS.size()]
	var root := int(st.root)
	var bars := 8
	var bpb := 4
	_loop_beats = bars * bpb
	var ev := []
	var degree := func(dg: int, octave: int) -> int:
		var ln := scale.size()
		var o := floori(float(dg) / ln)
		var d := ((dg % ln) + ln) % ln
		return root + scale[d] + 12 * (o + octave)
	for bar in bars:
		var chord: int = prog[bar % prog.size()]
		var start := float(bar * bpb)
		for k in 3:
			ev.append([start, "pad", degree.call(chord + k * 2, -1), 4.0, 0.16])
		for b in range(0, bpb, 2):
			ev.append([start + b, "bass", degree.call(chord, -2), 1.8, 0.5])
	var motif := []
	var p := 0.0
	var deg := 2 + rng.randi() % 3
	while p < 2 * bpb:
		var ln2 := 0.5 if rng.randf() < 0.6 else (1.0 if rng.randf() < 0.5 else 1.5)
		if rng.randf() < 0.18:
			p += ln2
			continue
		deg = clampi(deg + rng.randi_range(-2, 2), 0, 9)
		motif.append([p, ln2, deg])
		p += ln2
	var lead_vol := 0.3 + float(st.brightness) * 0.12
	for rep in bars / 2:
		var chord_root: int = prog[(rep * 2) % prog.size()]
		var answer := rep % 2 == 1
		for m in motif:
			var d: int = m[2] + ((chord_root % 3) - 1 if answer else 0)
			if rep == bars / 2 - 1 and m[0] >= 6.0: d = 7
			ev.append([rep * 2 * bpb + m[0], str(st.lead), degree.call(d, 0), m[1] * 0.95, lead_vol])
	var perc := float(st.percussion)
	if perc > 0.05:
		for b in int(_loop_beats):
			if b % 2 == 0: ev.append([float(b), "kick", 57, 1.0, 0.8 * perc])
			if b % 4 == 2 and perc > 0.45: ev.append([float(b), "hat", 69, 1.0, 0.5 * perc])
			ev.append([b + 0.5, "hat", 81, 1.0, 0.25 * perc])
	ev.sort_custom(func(a, b): return a[0] < b[0])
	return ev

func _stinger(semis: Array, major: bool, root: int) -> Array:
	_beat_len = 0.13
	var ev := []
	for i in semis.size():
		ev.append([float(i), "bell" if major else "triangle", root + semis[i], 10.0 if i == semis.size() - 1 else 1.6, 0.7])
	var chord := [0, 4, 7] if major else [0, 3, 7]
	for c in chord: ev.append([semis.size() - 1.0, "pad", root - 12 + c, 10.0, 0.35])
	_loop_beats = semis.size() + 12.0
	return ev

func _process(dt: float) -> void:
	if not _playing or _events.is_empty(): return
	_fade = minf(1.0, _fade + dt / 1.2)
	_song_t += dt
	var beat := _song_t / _beat_len
	while _cursor < _events.size() and _events[_cursor][0] <= beat:
		_note(_events[_cursor])
		_cursor += 1
	if _cursor >= _events.size() and beat >= _loop_beats:
		if _looping:
			_song_t -= _loop_beats * _beat_len
			_cursor = 0
		else:
			_playing = false

func _note(e: Array) -> void:
	var vol := float(e[4]) * music_volume * (0.35 if ducked else 1.0) * 0.55 * _fade
	if vol <= 0.002 or not _inst.has(e[1]): return
	var p := _mvoices[_next_mvoice]
	_next_mvoice = (_next_mvoice + 1) % _mvoices.size()
	p.stream = _inst[e[1]]
	var freq := 440.0 * pow(2.0, (float(e[2]) - 69.0) / 12.0)
	p.pitch_scale = 1.0 if e[1] == "kick" or e[1] == "hat" else clampf(freq / BASE_FREQ, 0.05, 8.0)
	p.volume_db = linear_to_db(vol)
	p.play()
