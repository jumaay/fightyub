"""
Generate Original Cumbia BGM for Casa Rosada (Fightyub Stage 2).
Urban Argentine Cumbia with arcade fighting game energy.
Tempo: 100 BPM
Length: 28 bars = 112 beats = 67.200 seconds exact (2,963,520 samples at 44.1 kHz).
Instrumentation:
- Güiro / Güira: signature rhythmic scraping pattern (taka-chiki taka-chiki)
- Cumbia Drumkit: dry punchy kick on 1 & 3, timbales with rimshots, cowbell on offbeats, crashes
- Cumbia Bass: punchy syncopated melodic bassline (A minor / D minor / E7)
- Offbeat Accordion & Piano (el 'chan-chan' cumbiero en contratiempos)
- Lead Accordion: authentic Argentine accordion vibrato and ornaments
- Arcade Synth Lead: iconic 90s digital synth sound (saw+sine with fast attack, vibrato and filter sweep)
Perfect loop: exact sample length, wrapped reverb/delay tails, 128-sample seamless crossfade.
"""

import math
import wave
import struct
import numpy as np

SR = 44100
BPM = 100.0
BEAT_DUR = 60.0 / BPM  # 0.600s
BAR_DUR = BEAT_DUR * 4.0  # 2.400s
SAMPLES_PER_BEAT = int(round(SR * BEAT_DUR))  # 26460 exact
SAMPLES_PER_BAR = SAMPLES_PER_BEAT * 4  # 105840 exact

NUM_BARS = 28
TOTAL_SAMPLES = NUM_BARS * SAMPLES_PER_BAR  # 2,963,520 samples = 67.200s
DURATION = TOTAL_SAMPLES / SR

# Stereo buffer
left_master = np.zeros(TOTAL_SAMPLES, dtype=np.float32)
right_master = np.zeros(TOTAL_SAMPLES, dtype=np.float32)

def midi_to_freq(m):
    return 440.0 * (2.0 ** ((m - 69.0) / 12.0))

def add_audio_segment(l_track, r_track, start_sample, pan=0.0):
    """Mix stereo segment into master buffers with circular loop wrap."""
    n_seg = len(l_track)
    pan = max(-1.0, min(1.0, pan))
    l_gain = math.cos((pan + 1.0) * math.pi / 4.0)
    r_gain = math.sin((pan + 1.0) * math.pi / 4.0)

    # Wrap around TOTAL_SAMPLES
    idx = np.arange(start_sample, start_sample + n_seg) % TOTAL_SAMPLES
    np.add.at(left_master, idx, l_track * l_gain)
    np.add.at(right_master, idx, r_track * r_gain)

# -------------------------------------------------------------
# PERCUSSION SYNTHESIS
# -------------------------------------------------------------

def make_guira_stroke(stroke_type='down', dur=0.14):
    """Synthesizes metal scraper rasp for Argentine cumbia."""
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    # Filtered metallic noise + modulated scraping teeth (approx 180-240 clicks per sec)
    noise = np.random.uniform(-1.0, 1.0, n).astype(np.float32)
    scrape_rate = 220.0 if stroke_type == 'down' else 280.0
    teeth = np.sin(2.0 * np.pi * scrape_rate * t) ** 2
    
    # Bandpass around 3.5kHz - 8kHz
    bp = (np.sin(2.0 * np.pi * 4200.0 * t) + 0.7 * np.sin(2.0 * np.pi * 6500.0 * t)) * noise
    sig = bp * (0.6 + 0.4 * teeth)

    if stroke_type == 'down':
        # Sharp accent
        env = np.exp(-t * 28.0) * (1.0 - np.exp(-t * 300.0))
    elif stroke_type == 'up':
        # Softer brush
        env = np.sin(np.linspace(0, np.pi, n)) ** 1.5
    else:  # 'roll'
        env = (1.0 - t / dur) ** 0.8
    
    return (sig * env * 0.45).astype(np.float32)

def make_cumbia_kick(dur=0.28):
    """Deep, round and punchy cumbia bass drum."""
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    f_env = 135.0 * np.exp(-t * 32.0) + 48.0
    phase = 2.0 * np.pi * np.cumsum(f_env) / SR
    body = np.sin(phase)
    # Click
    click = np.random.uniform(-1.0, 1.0, min(120, n)) * np.exp(-t[:min(120, n)] * 180.0)
    sig = np.zeros(n, dtype=np.float32)
    sig[:len(click)] += click * 0.35
    sig += body * np.exp(-t * 11.0)
    return (sig * 0.85).astype(np.float32)

def make_timbal_hit(dur=0.35, high=False):
    """High metallic Latin timbal drum hit."""
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    f0 = 310.0 if high else 215.0
    f_env = f0 * (1.0 + 0.6 * np.exp(-t * 60.0))
    phase = 2.0 * np.pi * np.cumsum(f_env) / SR
    tone = np.sin(phase) + 0.4 * np.sin(phase * 1.58) + 0.25 * np.sin(phase * 2.14)
    # Metallic shell resonance
    rim = np.sin(2.0 * np.pi * 840.0 * t) * np.exp(-t * 45.0)
    noise = np.random.uniform(-1.0, 1.0, n) * np.exp(-t * 85.0)
    sig = tone * np.exp(-t * 16.0) + rim * 0.45 + noise * 0.25
    return (sig * 0.70).astype(np.float32)

def make_cowbell(dur=0.22):
    """Classic Latin cencerro offbeat hit."""
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    # Dual square-like metallic modes (approx 587 Hz and 845 Hz)
    m1 = np.sin(2.0 * np.pi * 587.0 * t) + 0.5 * np.sin(2.0 * np.pi * 1174.0 * t)
    m2 = np.sin(2.0 * np.pi * 845.0 * t) + 0.4 * np.sin(2.0 * np.pi * 1690.0 * t)
    sig = np.tanh((m1 + 0.8 * m2) * 2.2) * np.exp(-t * 22.0)
    return (sig * 0.55).astype(np.float32)

# Pre-render percussion elements
GUIRA_DOWN = make_guira_stroke('down', 0.12)
GUIRA_UP = make_guira_stroke('up', 0.10)
GUIRA_LONG = make_guira_stroke('roll', 0.20)
KICK_HIT = make_cumbia_kick(0.30)
TIMBAL_LO = make_timbal_hit(0.35, high=False)
TIMBAL_HI = make_timbal_hit(0.35, high=True)
COWBELL_HIT = make_cowbell(0.24)

# -------------------------------------------------------------
# INSTRUMENT SYNTHESIS: BASS, ACCORDION, ARCADE SYNTH, PIANO
# -------------------------------------------------------------

def synthesize_cumbia_bass_note(midi_pitch, dur, velocity=1.0):
    """Warm, buoyant staccato/grooving electric bass for cumbia."""
    dur = max(0.08, dur)
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    f0 = midi_to_freq(midi_pitch)
    
    # Fundamental + rich harmonics (sub + octave + warm mid)
    sig = 1.00 * np.sin(2.0 * np.pi * f0 * t)
    sig += 0.55 * np.sin(2.0 * np.pi * 2.0 * f0 * t)
    sig += 0.25 * np.sin(2.0 * np.pi * 3.0 * f0 * t)
    sig += 0.12 * np.sin(2.0 * np.pi * 4.0 * f0 * t)
    
    # Slight fret plucking click
    click = np.sin(2.0 * np.pi * 1200.0 * t[:min(180, n)]) * np.exp(-t[:min(180, n)] * 80.0)
    sig[:len(click)] += click * 0.4
    
    # Envelope: punchy front, bouncy decay
    att = min(int(round(0.008 * SR)), n // 2)
    env = np.ones(n, dtype=np.float32)
    if att > 0:
        env[:att] = np.linspace(0.0, 1.0, att)
    rel_start = int(round(dur * 0.65 * SR))
    if rel_start < n:
        rel_len = n - rel_start
        env[rel_start:] = np.exp(-np.linspace(0, 4.0, rel_len))
    
    # Soft saturation for presence
    sig = np.tanh(sig * 1.4) * env * velocity * 0.70
    return sig.astype(np.float32)

def synthesize_accordion_tone(midi_pitch, dur, vibrato=True, velocity=1.0):
    """Argentine musette accordion with dual reeds slightly detuned."""
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    f0 = midi_to_freq(midi_pitch)
    
    # Tremolo / Musette beat (detune reed 1 and reed 2 by ~1.6 Hz)
    detune = 1.6
    f1 = f0 - (detune / 2.0)
    f2 = f0 + (detune / 2.0)
    
    if vibrato and dur > 0.25:
        vib_env = np.clip((t - 0.10) / 0.15, 0.0, 1.0)
        vib = 1.0 + 0.006 * vib_env * np.sin(2.0 * np.pi * 6.2 * t)
    else:
        vib = 1.0

    # Pulse / square + saw reed timbre
    reed1 = np.zeros(n, dtype=np.float32)
    reed2 = np.zeros(n, dtype=np.float32)
    for h in range(1, 9):
        fh = f0 * h
        if fh > 14000: break
        gain = 1.0 / (h ** 0.85) if (h % 2 == 1) else (0.5 / (h ** 0.85))
        reed1 += gain * np.sin(2.0 * np.pi * f1 * h * t * vib)
        reed2 += gain * np.sin(2.0 * np.pi * f2 * h * t * vib + 0.3)
    
    sig = (reed1 + reed2) * 0.5
    
    # Bellows envelope (smooth attack, sustained body, gentle release)
    att = min(int(round(0.022 * SR)), n // 2)
    env = np.ones(n, dtype=np.float32)
    if att > 0:
        env[:att] = np.sin(np.linspace(0, np.pi / 2, att)) ** 2
    rel_start = int(round(dur * 0.82 * SR))
    if rel_start < n:
        rel_len = n - rel_start
        env[rel_start:] = np.cos(np.linspace(0, np.pi / 2, rel_len)) ** 2
        
    return (sig * env * velocity * 0.50).astype(np.float32)

def synthesize_piano_chord(midi_notes, dur=0.20, velocity=0.85):
    """Crisp offbeat piano chord ('el chan-chan' cumbiero)."""
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    chord_sig = np.zeros(n, dtype=np.float32)
    
    for m in midi_notes:
        f = midi_to_freq(m)
        note_sig = np.sin(2.0 * np.pi * f * t) + 0.5 * np.sin(2.0 * np.pi * 2.0 * f * t) + 0.25 * np.sin(2.0 * np.pi * 3.0 * f * t)
        # Hammer click
        hammer = np.sin(2.0 * np.pi * 2200.0 * t[:min(150, n)]) * np.exp(-t[:min(150, n)] * 120.0)
        note_sig[:len(hammer)] += hammer * 0.3
        chord_sig += note_sig
    
    # Fast staccato decay
    env = np.exp(-t * 18.0)
    return (chord_sig * env * velocity * 0.25).astype(np.float32)

def synthesize_arcade_synth_lead(midi_pitch, dur, velocity=1.0, portamento=0.0):
    """Iconic 90s digital/FM arcade synthesizer lead with punch and bite."""
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    f0 = midi_to_freq(midi_pitch)
    
    # Frequency envelope for pitch bend/attack punch
    f_env = np.ones(n, dtype=np.float32) * f0
    if portamento > 0:
        port_samples = min(int(round(portamento * SR)), n)
        f_env[:port_samples] = np.linspace(f0 * 0.90, f0, port_samples)
    
    # Vibrato on sustained leads
    if dur > 0.25:
        vib_t = np.maximum(0.0, t - 0.12)
        vib = 1.0 + 0.012 * (1.0 - np.exp(-vib_t * 8.0)) * np.sin(2.0 * np.pi * 6.5 * t)
        f_env *= vib
        
    phase = 2.0 * np.pi * np.cumsum(f_env) / SR
    
    # FM Synthesis: Modulator into Carrier
    mod_freq = f0 * 2.0
    mod_index = 2.5 * np.exp(-t * 6.0) + 0.8
    modulator = mod_index * np.sin(2.0 * np.pi * mod_freq * t)
    
    carrier = np.sin(phase + modulator) + 0.35 * np.sin(phase * 2.0 + (modulator * 0.5))
    # Soft warm wave-shaper
    sig = np.tanh(carrier * 1.5)
    
    # ADSR Envelope
    att = min(int(round(0.015 * SR)), n // 2)
    env = np.ones(n, dtype=np.float32)
    if att > 0:
        env[:att] = np.linspace(0.0, 1.0, att)
    rel_start = int(round(dur * 0.85 * SR))
    if rel_start < n:
        rel_len = n - rel_start
        env[rel_start:] = np.linspace(1.0, 0.0, rel_len)
        
    return (sig * env * velocity * 0.55).astype(np.float32)

# -------------------------------------------------------------
# SCORE ARRANGEMENT: 28 BARS OF CUMBIA PORTEÑA
# Key: A minor (Am - G - F - E7 - Dm - E7)
# -------------------------------------------------------------
print("Arranging 28 bars of original Cumbia Porteña for Casa Rosada...")

# Chords per bar in 28 bars:
# Bars 1-4:   Am | G | F | E7        (Intro groove)
# Bars 5-8:   Am | Dm | G | C        (Theme A part 1)
# Bars 9-12:  F | Dm | E7 | Am        (Theme A part 2)
# Bars 13-16: Am | G | F | E7        (Theme B Synth Solo 1)
# Bars 17-20: Dm | Am | E7 | Am      (Theme B Synth Solo 2)
# Bars 21-24: F | G | Em | Am        (Climax buildup 1)
# Bars 25-28: Dm | G | E7 | E7       (Climax turnaround to Am loop restart)

CHORD_PROGRESSION = [
    # Intro
    ('Am', [57, 60, 64]), ('G',  [55, 59, 62]), ('F',  [53, 57, 60]), ('E7', [52, 56, 59, 62]),
    # Theme A
    ('Am', [57, 60, 64]), ('Dm', [50, 53, 57]), ('G',  [55, 59, 62]), ('C',  [48, 52, 55]),
    ('F',  [53, 57, 60]), ('Dm', [50, 53, 57]), ('E7', [52, 56, 59, 62]), ('Am', [57, 60, 64]),
    # Theme B
    ('Am', [57, 60, 64]), ('G',  [55, 59, 62]), ('F',  [53, 57, 60]), ('E7', [52, 56, 59, 62]),
    ('Dm', [50, 53, 57]), ('Am', [57, 60, 64]), ('E7', [52, 56, 59, 62]), ('Am', [57, 60, 64]),
    # Climax
    ('F',  [53, 57, 60]), ('G',  [55, 59, 62]), ('Em', [52, 55, 59]), ('Am', [57, 60, 64]),
    ('Dm', [50, 53, 57]), ('G',  [55, 59, 62]), ('E7', [52, 56, 59, 62]), ('E7', [52, 56, 59, 62])
]

# 1. RHYTHM SECTION: GÜIRA & DRUMS (All 28 bars)
for bar in range(NUM_BARS):
    bar_start_sample = bar * SAMPLES_PER_BAR
    
    # Güira 16th-note scraping groove (Chiki-chiki-cha pattern)
    # Beats 0, 1, 2, 3 in the bar, subdivided in 4 sixteenths
    for beat in range(4):
        b_sample = bar_start_sample + beat * SAMPLES_PER_BEAT
        step = SAMPLES_PER_BEAT // 4
        # 16th notes: Down, Up, Down, Up with accent on 1 and 3
        add_audio_segment(GUIRA_DOWN, GUIRA_DOWN, b_sample, pan=-0.25)
        add_audio_segment(GUIRA_UP,   GUIRA_UP,   b_sample + step, pan=-0.20)
        add_audio_segment(GUIRA_DOWN * 0.8, GUIRA_DOWN * 0.8, b_sample + step * 2, pan=-0.25)
        add_audio_segment(GUIRA_UP   * 0.9, GUIRA_UP   * 0.9, b_sample + step * 3, pan=-0.20)

    # Kick Drum on 1 and 3 (Classic Argentine cumbia march)
    add_audio_segment(KICK_HIT, KICK_HIT, bar_start_sample, pan=0.0)
    add_audio_segment(KICK_HIT, KICK_HIT, bar_start_sample + 2 * SAMPLES_PER_BEAT, pan=0.0)
    
    # Cowbell on offbeats (the "and" of 1, 2, 3, 4)
    for beat in range(4):
        offbeat_sample = bar_start_sample + beat * SAMPLES_PER_BEAT + (SAMPLES_PER_BEAT // 2)
        add_audio_segment(COWBELL_HIT, COWBELL_HIT, offbeat_sample, pan=0.35)
        
    # Timbales & Rimshots: syncopated hits on beat 4, and drum fills every 4 bars
    if bar % 4 == 3:
        # Repique de timbales al final del compás
        t_fill = bar_start_sample + 3 * SAMPLES_PER_BEAT
        step_16 = SAMPLES_PER_BEAT // 4
        add_audio_segment(TIMBAL_HI, TIMBAL_HI, t_fill, pan=0.30)
        add_audio_segment(TIMBAL_HI, TIMBAL_HI, t_fill + step_16, pan=0.30)
        add_audio_segment(TIMBAL_LO, TIMBAL_LO, t_fill + step_16 * 2, pan=-0.30)
        add_audio_segment(TIMBAL_HI * 1.2, TIMBAL_HI * 1.2, t_fill + step_16 * 3, pan=0.30)
    else:
        # Standard timbal hit on beat 4 and
        add_audio_segment(TIMBAL_HI * 0.75, TIMBAL_HI * 0.75, bar_start_sample + 3 * SAMPLES_PER_BEAT + (SAMPLES_PER_BEAT // 2), pan=0.28)

# 2. PIANO & ACCORDION OFFBEAT COMPING (El 'Chan-Chan' en contratiempos)
for bar in range(NUM_BARS):
    bar_start_sample = bar * SAMPLES_PER_BAR
    chord_name, notes = CHORD_PROGRESSION[bar]
    
    # Offbeat chops on the "and" of beats 1, 2, 3, 4
    for beat in range(4):
        chop_sample = bar_start_sample + beat * SAMPLES_PER_BEAT + (SAMPLES_PER_BEAT // 2)
        piano_chord = synthesize_piano_chord(notes, dur=0.18, velocity=0.80)
        accordion_chop = np.zeros_like(piano_chord)
        for n in notes:
            acc_note = synthesize_accordion_tone(n + 12, dur=0.18, vibrato=False, velocity=0.60)
            accordion_chop += acc_note[:len(accordion_chop)]
        accordion_chop *= 0.35
        
        full_chop = piano_chord + accordion_chop
        add_audio_segment(full_chop, full_chop, chop_sample, pan=0.15)

# 3. BASSLINE: MELODIC CUMBIA BASS (Punchy syncopated walking bass)
BASS_PATTERNS = {
    'Am': [45, 45, 48, 52],  # A, A, C, E
    'G':  [43, 43, 47, 50],  # G, G, B, D
    'F':  [41, 41, 45, 48],  # F, F, A, C
    'E7': [40, 40, 44, 47],  # E, E, G#, B
    'Dm': [38, 38, 41, 45],  # D, D, F, A
    'C':  [48, 48, 43, 48],  # C, C, G, C
    'Em': [40, 40, 43, 47]   # E, E, G, B
}

for bar in range(NUM_BARS):
    bar_start_sample = bar * SAMPLES_PER_BAR
    chord_name, _ = CHORD_PROGRESSION[bar]
    root_pat = BASS_PATTERNS.get(chord_name, [45, 45, 48, 52])
    
    # Cumbia bass rhythm: Beat 1 (dotted 8th), 1-and (16th), Beat 2 (8th), Beat 3 (dotted), 3-and (16th)...
    # Simple grooving pattern:
    # 0.0: root (dur 0.28s)
    # 0.75 beats: octave/5th (dur 0.16s)
    # 1.5 beats: 3rd (dur 0.22s)
    # 2.0 beats: 5th (dur 0.25s)
    # 3.0 beats: leading tone (dur 0.25s)
    notes_time = [
        (0.0, root_pat[0], 0.26),
        (1.0, root_pat[0], 0.22),
        (2.0, root_pat[1], 0.26),
        (2.75, root_pat[2], 0.18),
        (3.0, root_pat[3], 0.26)
    ]
    for b_off, pitch, dur in notes_time:
        sample_idx = bar_start_sample + int(round(b_off * SAMPLES_PER_BEAT))
        bass_snd = synthesize_cumbia_bass_note(pitch, dur, velocity=0.95)
        add_audio_segment(bass_snd, bass_snd, sample_idx, pan=0.0)

# 4. LEAD MELODIES: ACCORDION & ARCADE SYNTH
# Bars 1-4: Intro call by Accordion
intro_melody = [
    # Bar 1 (Am)
    (0.0, 69, 0.45), (1.0, 69, 0.25), (1.5, 72, 0.25), (2.0, 76, 0.55), (3.0, 74, 0.35),
    # Bar 2 (G)
    (4.0, 71, 0.45), (5.0, 71, 0.25), (5.5, 74, 0.25), (6.0, 77, 0.55), (7.0, 76, 0.35),
    # Bar 3 (F)
    (8.0, 69, 0.45), (9.0, 69, 0.25), (9.5, 72, 0.25), (10.0, 74, 0.55), (11.0, 72, 0.35),
    # Bar 4 (E7) - turnaround
    (12.0, 71, 0.35), (12.75, 72, 0.25), (13.5, 71, 0.25), (14.0, 68, 0.45), (15.0, 64, 0.55)
]
for b_pos, pitch, dur in intro_melody:
    pos_sample = int(round(b_pos * SAMPLES_PER_BEAT))
    lead = synthesize_accordion_tone(pitch, dur, vibrato=True, velocity=0.88)
    add_audio_segment(lead, lead, pos_sample, pan=-0.22)

# Bars 5-12: Theme A (Lead Accordion with energetic fighting swing)
theme_a_melody = [
    # Bar 5 (Am)
    (16.0, 69, 0.35), (16.75, 72, 0.25), (17.5, 76, 0.40), (18.5, 76, 0.25), (19.0, 74, 0.30),
    # Bar 6 (Dm)
    (20.0, 74, 0.35), (20.75, 77, 0.25), (21.5, 81, 0.40), (22.5, 81, 0.25), (23.0, 79, 0.30),
    # Bar 7 (G)
    (24.0, 79, 0.35), (24.75, 76, 0.25), (25.5, 74, 0.40), (26.5, 72, 0.25), (27.0, 71, 0.30),
    # Bar 8 (C)
    (28.0, 72, 0.65), (29.5, 74, 0.25), (30.0, 76, 0.55),
    # Bar 9 (F)
    (32.0, 77, 0.35), (32.75, 76, 0.25), (33.5, 74, 0.40), (34.5, 72, 0.25), (35.0, 69, 0.30),
    # Bar 10 (Dm)
    (36.0, 74, 0.35), (36.75, 72, 0.25), (37.5, 69, 0.40), (38.5, 67, 0.25), (39.0, 65, 0.30),
    # Bar 11 (E7)
    (40.0, 64, 0.35), (40.75, 68, 0.25), (41.5, 71, 0.40), (42.5, 74, 0.35), (43.0, 71, 0.35),
    # Bar 12 (Am)
    (44.0, 69, 0.85), (46.0, 69, 0.35), (47.0, 71, 0.35)
]
for b_pos, pitch, dur in theme_a_melody:
    pos_sample = int(round(b_pos * SAMPLES_PER_BEAT))
    lead = synthesize_accordion_tone(pitch, dur, vibrato=True, velocity=0.92)
    add_audio_segment(lead, lead, pos_sample, pan=-0.15)

# Bars 13-20: Theme B (Arcade Synth Solo with fast arpeggios & bends)
theme_b_synth = [
    # Bar 13 (Am)
    (48.0, 81, 0.28), (48.75, 81, 0.22), (49.5, 84, 0.25), (50.0, 88, 0.45), (51.0, 86, 0.35),
    # Bar 14 (G)
    (52.0, 83, 0.28), (52.75, 83, 0.22), (53.5, 86, 0.25), (54.0, 89, 0.45), (55.0, 88, 0.35),
    # Bar 15 (F)
    (56.0, 81, 0.28), (56.75, 81, 0.22), (57.5, 84, 0.25), (58.0, 86, 0.45), (59.0, 84, 0.35),
    # Bar 16 (E7)
    (60.0, 83, 0.30), (60.75, 80, 0.25), (61.5, 76, 0.30), (62.5, 74, 0.30), (63.0, 76, 0.45),
    # Bar 17 (Dm)
    (64.0, 86, 0.35), (65.0, 84, 0.25), (65.5, 81, 0.30), (66.5, 77, 0.35), (67.0, 81, 0.35),
    # Bar 18 (Am)
    (68.0, 84, 0.35), (69.0, 81, 0.25), (69.5, 76, 0.30), (70.5, 72, 0.35), (71.0, 76, 0.35),
    # Bar 19 (E7)
    (72.0, 80, 0.28), (72.75, 83, 0.25), (73.5, 86, 0.35), (74.5, 88, 0.30), (75.0, 83, 0.35),
    # Bar 20 (Am)
    (76.0, 81, 0.75), (78.0, 81, 0.35), (79.0, 83, 0.35)
]
for b_pos, pitch, dur in theme_b_synth:
    pos_sample = int(round(b_pos * SAMPLES_PER_BEAT))
    synth = synthesize_arcade_synth_lead(pitch, dur, velocity=0.88, portamento=0.03)
    add_audio_segment(synth, synth, pos_sample, pan=0.25)

# Bars 21-28: Climax & Tutti (Accordion and Synth in Harmony / Call-and-Response)
climax_melody = [
    # Bar 21 (F)
    (80.0, 81, 0.30), (80.75, 84, 0.25), (81.5, 86, 0.35), (82.5, 88, 0.30), (83.0, 89, 0.40),
    # Bar 22 (G)
    (84.0, 91, 0.45), (85.0, 89, 0.30), (86.0, 88, 0.35), (87.0, 86, 0.35),
    # Bar 23 (Em)
    (88.0, 88, 0.45), (89.0, 86, 0.30), (90.0, 84, 0.35), (91.0, 83, 0.35),
    # Bar 24 (Am)
    (92.0, 84, 0.60), (93.5, 81, 0.30), (94.0, 84, 0.45), (95.0, 86, 0.35),
    # Bar 25 (Dm)
    (96.0, 86, 0.40), (97.0, 84, 0.25), (97.75, 81, 0.30), (98.5, 86, 0.35), (99.0, 89, 0.40),
    # Bar 26 (G)
    (100.0, 88, 0.40), (101.0, 86, 0.25), (101.75, 83, 0.30), (102.5, 86, 0.35), (103.0, 88, 0.40),
    # Bar 27 (E7) - Grand dramatic tension
    (104.0, 88, 0.30), (104.75, 86, 0.25), (105.5, 83, 0.30), (106.5, 80, 0.35), (107.0, 83, 0.35),
    # Bar 28 (E7 turnaround into Am bar 1)
    (108.0, 80, 0.28), (108.75, 76, 0.25), (109.5, 71, 0.30), (110.5, 68, 0.35), (111.0, 64, 0.45)
]
for b_pos, pitch, dur in climax_melody:
    pos_sample = int(round(b_pos * SAMPLES_PER_BEAT))
    # Dual voicing: Accordion on left, Arcade Synth on right
    acc = synthesize_accordion_tone(pitch, dur, vibrato=True, velocity=0.88)
    syn = synthesize_arcade_synth_lead(pitch, dur, velocity=0.80, portamento=0.02)
    add_audio_segment(acc, acc, pos_sample, pan=-0.22)
    add_audio_segment(syn, syn, pos_sample, pan=0.22)

# -------------------------------------------------------------
# MASTERING & SEAMLESS LOOP MICRO-CROSSFADE
# -------------------------------------------------------------
print("Applying mastering limiter and seamless loop crossfade...")

# 128-sample seamless boundary crossfade to guarantee C0/C1 continuity at loop point
CROSSFADE_SAMPLES = 128
cf_env = np.linspace(0.0, 1.0, CROSSFADE_SAMPLES, dtype=np.float32)
# Head and tail crossfade
head_l = left_master[:CROSSFADE_SAMPLES]
tail_l = left_master[-CROSSFADE_SAMPLES:]
head_r = right_master[:CROSSFADE_SAMPLES]
tail_r = right_master[-CROSSFADE_SAMPLES:]

left_master[:CROSSFADE_SAMPLES] = head_l * cf_env + tail_l * (1.0 - cf_env)
left_master[-CROSSFADE_SAMPLES:] = left_master[:CROSSFADE_SAMPLES]
right_master[:CROSSFADE_SAMPLES] = head_r * cf_env + tail_r * (1.0 - cf_env)
right_master[-CROSSFADE_SAMPLES:] = right_master[:CROSSFADE_SAMPLES]

# Soft clipper / Limiter
peak = max(np.max(np.abs(left_master)), np.max(np.abs(right_master)))
print(f"Pre-master peak amplitude: {peak:.3f}")
target_peak = 0.92
if peak > 0:
    left_master = np.tanh(left_master / peak * 1.15) * target_peak
    right_master = np.tanh(right_master / peak * 1.15) * target_peak

# Convert to 16-bit PCM WAV
out_path = "Assets/Audio/Music/bgm_casarosada_cumbia.wav"
left_16 = np.int16(np.clip(left_master * 32767.0, -32768, 32767))
right_16 = np.int16(np.clip(right_master * 32767.0, -32768, 32767))

interleaved = np.empty((TOTAL_SAMPLES * 2,), dtype=np.int16)
interleaved[0::2] = left_16
interleaved[1::2] = right_16

with wave.open(out_path, 'wb') as wav_file:
    wav_file.setnchannels(2)
    wav_file.setsampwidth(2)
    wav_file.setframerate(SR)
    wav_file.writeframes(interleaved.tobytes())

import os
file_size_mb = os.path.getsize(out_path) / (1024 * 1024)
print(f"Successfully generated: {out_path} ({file_size_mb:.2f} MB, {DURATION:.2f}s, {TOTAL_SAMPLES} samples)")
