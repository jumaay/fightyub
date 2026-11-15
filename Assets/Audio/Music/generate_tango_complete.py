"""
Generate Authentic Argentine Tango BGM for Obelisco Stage (Fightyub).
True Seamless Loop: Continuous energy, no trailing silence, exact 120.0 BPM,
perfect cadence pickup connecting Bar 29 into Bar 0.

Tempo: 120.0 BPM (0.500s per beat = 22,050 samples exact at 44.1 kHz).
Bars: 30 bars = 120 beats = exactly 60.000s (2,646,000 samples).
Instruments:
- Bandoneón: Argentine musette / double-reed bellows with vibrato and expression
- Piano: Marcato in 4, syncopated chords, chromatic runs
- Contrabajo: Deep acoustic bass with arrastre and marcato pizzicato
- Percussion: Caja tanguera, rimshots, woodblocks, subtle ride / hi-hat
"""

import math
import os
import wave
import numpy as np

SR = 44100
BPM = 120.0
BEAT_DUR = 60.0 / BPM  # 0.5000s exact
BAR_DUR = BEAT_DUR * 4.0  # 2.0000s exact
SAMPLES_PER_BEAT = int(round(SR * BEAT_DUR))  # 22050 exact
SAMPLES_PER_BAR = SAMPLES_PER_BEAT * 4  # 88200 exact

NUM_BARS = 30
TOTAL_SAMPLES = NUM_BARS * SAMPLES_PER_BAR  # 2,646,000 samples = exactly 60.000s
TOTAL_DUR = TOTAL_SAMPLES / SR

# Master stereo buffers
left_master = np.zeros(TOTAL_SAMPLES, dtype=np.float32)
right_master = np.zeros(TOTAL_SAMPLES, dtype=np.float32)

def midi_to_freq(m):
    return 440.0 * (2.0 ** ((m - 69.0) / 12.0))

def add_segment(l_track, r_track, start_sample, pan=0.0):
    """Mix stereo segment into master buffers with circular loop wrap."""
    n_seg = len(l_track)
    pan = max(-1.0, min(1.0, pan))
    l_gain = math.cos((pan + 1.0) * math.pi / 4.0)
    r_gain = math.sin((pan + 1.0) * math.pi / 4.0)

    idx = np.arange(start_sample, start_sample + n_seg) % TOTAL_SAMPLES
    np.add.at(left_master, idx, l_track * l_gain)
    np.add.at(right_master, idx, r_track * r_gain)

# -------------------------------------------------------------
# INSTRUMENT SYNTHESIS
# -------------------------------------------------------------

def make_bandoneon_note(midi_pitch, dur, velocity=1.0, vibrato=True, staccato=False):
    """Rich bandoneón reed tone with natural bellows dynamics."""
    dur = max(0.06, dur)
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    f0 = midi_to_freq(midi_pitch)

    # Musette reed pair with slight chorusing detune (~1.8 Hz)
    f1 = f0 - 0.9
    f2 = f0 + 0.9

    if vibrato and not staccato and dur > 0.22:
        vib_env = np.clip((t - 0.08) / 0.15, 0.0, 1.0)
        vib = 1.0 + 0.007 * vib_env * np.sin(2.0 * np.pi * 5.6 * t)
    else:
        vib = 1.0

    # Odd and even reed harmonics
    reed1 = np.zeros(n, dtype=np.float32)
    reed2 = np.zeros(n, dtype=np.float32)
    for h in range(1, 10):
        fh = f0 * h
        if fh > 14000: break
        # Bandoneon prominent 1st, 2nd, 3rd, 5th harmonics
        harm_gain = (1.0 / (h ** 0.82)) if (h % 2 == 1) else (0.55 / (h ** 0.88))
        reed1 += harm_gain * np.sin(2.0 * np.pi * f1 * h * t * vib)
        reed2 += harm_gain * np.sin(2.0 * np.pi * f2 * h * t * vib + 0.4)

    sig = (reed1 + reed2) * 0.5
    # Air hiss
    air = np.random.uniform(-1.0, 1.0, n).astype(np.float32) * 0.035
    sig += air

    # Bellows envelope
    att_time = 0.015 if staccato else 0.030
    att = min(int(round(att_time * SR)), n // 2)
    env = np.ones(n, dtype=np.float32)
    if att > 0:
        env[:att] = np.sin(np.linspace(0, np.pi / 2, att)) ** 2

    decay_start = int(round(dur * 0.75 * SR))
    if decay_start < n:
        rel_len = n - decay_start
        env[decay_start:] = np.cos(np.linspace(0, np.pi / 2, rel_len)) ** 2

    # Warm compression
    sig = np.tanh(sig * 1.3) * env * velocity * 0.52
    return sig.astype(np.float32)

def make_piano_chord(midi_notes, dur=0.25, velocity=0.85, staccato=False):
    """Crisp, punchy marcato piano chord for tango rhythms."""
    dur = max(0.08, dur)
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    chord_sig = np.zeros(n, dtype=np.float32)

    for m in midi_notes:
        f = midi_to_freq(m)
        note_sig = np.sin(2.0 * np.pi * f * t) + 0.55 * np.sin(2.0 * np.pi * 2.0 * f * t) + 0.28 * np.sin(2.0 * np.pi * 3.0 * f * t)
        # Hammer strike transient
        hammer = np.sin(2.0 * np.pi * 2400.0 * t[:min(140, n)]) * np.exp(-t[:min(140, n)] * 110.0)
        note_sig[:len(hammer)] += hammer * 0.35
        chord_sig += note_sig

    damping = 16.0 if staccato else 6.5
    env = np.exp(-t * damping)
    return (chord_sig * env * velocity * 0.32).astype(np.float32)

def make_bass_note(midi_pitch, dur, velocity=1.0, arrastre=False):
    """Acoustic upright contrabajo with deep low end and arrastre scoop."""
    dur = max(0.10, dur)
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    f0 = midi_to_freq(midi_pitch)

    if arrastre:
        # Arrastre: 40ms upward pitch scoop from minor 2nd below
        scoop_s = min(int(round(0.050 * SR)), n)
        f_env = np.ones(n, dtype=np.float32) * f0
        f_env[:scoop_s] = np.linspace(f0 * 0.88, f0, scoop_s)
        phase = 2.0 * np.pi * np.cumsum(f_env) / SR
    else:
        phase = 2.0 * np.pi * f0 * t

    # Deep fundamental + woody body resonance
    sig = 1.0 * np.sin(phase) + 0.6 * np.sin(phase * 2.0) + 0.25 * np.sin(phase * 3.0)
    # Finger snap / string click
    click = np.sin(2.0 * np.pi * 850.0 * t[:min(180, n)]) * np.exp(-t[:min(180, n)] * 75.0)
    sig[:len(click)] += click * 0.45

    env = np.exp(-t * 4.8)
    # Sustain end
    end_s = int(round(dur * 0.80 * SR))
    if end_s < n:
        rel_len = n - end_s
        env[end_s:] *= np.cos(np.linspace(0, np.pi / 2, rel_len)) ** 2

    return (np.tanh(sig * 1.5) * env * velocity * 0.72).astype(np.float32)

def make_caja_golpe(rimshot=False, vel=0.8):
    """Tango snare / caja golpe."""
    dur = 0.22 if rimshot else 0.16
    n = int(round(dur * SR))
    t = np.arange(n, dtype=np.float32) / SR
    f0 = 240.0 if rimshot else 180.0
    body = np.sin(2.0 * np.pi * f0 * t * np.exp(-t * 28.0))
    noise = np.random.uniform(-1.0, 1.0, n).astype(np.float32)
    sig = body * 0.6 + noise * np.exp(-t * 42.0) * 0.4
    if rimshot:
        rim = np.sin(2.0 * np.pi * 1250.0 * t) * np.exp(-t * 60.0)
        sig += rim * 0.5
    return (sig * vel * 0.55).astype(np.float32)

# Pre-render percussion hits
CAJA_MARCATO = make_caja_golpe(rimshot=False, vel=0.7)
CAJA_RIMSHOT = make_caja_golpe(rimshot=True, vel=0.85)

# -------------------------------------------------------------
# SCORE ARRANGEMENT: 30 BARS OF TANGO PORTEÑO
# 120 BPM, Key: D Minor (Dm - A7 - Gm - Bb - E7 - A7)
# -------------------------------------------------------------
print("Composing 30 bars of authentic Argentine Tango for Obelisco...")

CHORDS = {
    'Dm': [50, 53, 57],      # D3, F3, A3
    'Dm_high': [62, 65, 69], # D4, F4, A4
    'A7': [49, 52, 55, 57],  # C#3, E3, G3, A3
    'Gm': [46, 50, 55],      # Bb2, D3, G3
    'Bb': [46, 50, 53],      # Bb2, D3, F3
    'E7': [52, 56, 59, 62],  # E3, G#3, B3, D4
    'F':  [48, 53, 57],      # C3, F3, A3
    'C':  [48, 52, 55]       # C3, E3, G3
}

# 30-bar chord chart:
# Bars 0-3:   Dm | Dm | A7 | Dm
# Bars 4-7:   Gm | Dm | A7 | Dm
# Bars 8-11:  F  | C  | Dm | A7   (Sincopado)
# Bars 12-15: Bb | Gm | A7 | Dm   (Sincopado)
# Bars 16-19: Dm | Gm | E7 | A7   (Bandoneon Solo)
# Bars 20-23: Dm | Bb | Gm | A7   (Bandoneon Solo)
# Bars 24-27: Dm | Gm | Bb | E7   (Climax Tutti)
# Bars 28-29: A7 | A7 (Pickup cadence connecting to Bar 0 Dm!)

BAR_HARMONY = [
    ('Dm', 38), ('Dm', 38), ('A7', 45), ('Dm', 38),
    ('Gm', 43), ('Dm', 38), ('A7', 45), ('Dm', 38),
    ('F',  41), ('C',  36), ('Dm', 38), ('A7', 45),
    ('Bb', 46), ('Gm', 43), ('A7', 45), ('Dm', 38),
    ('Dm', 38), ('Gm', 43), ('E7', 40), ('A7', 45),
    ('Dm', 38), ('Bb', 46), ('Gm', 43), ('A7', 45),
    ('Dm', 38), ('Gm', 43), ('Bb', 46), ('E7', 40),
    ('A7', 45), ('A7', 45) # Bar 29: Final cadence into Bar 0
]

# 1. RHYTHMIC SECTION: PIANO MARCATO & CONTRABAJO
for b in range(NUM_BARS):
    bar_start_sample = b * SAMPLES_PER_BAR
    ch_name, bass_root = BAR_HARMONY[b]
    ch_notes = CHORDS[ch_name]

    if b == 29:
        # BAR 29: THE CADENCE & PICKUP ARRASTRE DIRECTLY INTO BAR 0!
        # Beat 0: Marcato hit on A7
        bass0 = make_bass_note(bass_root, BEAT_DUR * 0.8, velocity=1.0, arrastre=True)
        p0 = make_piano_chord(ch_notes, dur=BEAT_DUR * 0.45, velocity=0.95, staccato=True)
        add_segment(bass0, bass0, bar_start_sample, pan=-0.10)
        add_segment(p0, p0, bar_start_sample, pan=0.20)
        add_segment(CAJA_RIMSHOT, CAJA_RIMSHOT, bar_start_sample, pan=0.0)

        # Beat 1: Marcato hit
        bass1 = make_bass_note(bass_root, BEAT_DUR * 0.7, velocity=0.90)
        p1 = make_piano_chord(ch_notes, dur=BEAT_DUR * 0.40, velocity=0.90, staccato=True)
        add_segment(bass1, bass1, bar_start_sample + SAMPLES_PER_BEAT, pan=-0.10)
        add_segment(p1, p1, bar_start_sample + SAMPLES_PER_BEAT, pan=0.20)

        # Beat 2: Heavy leading tone C#2
        bass2 = make_bass_note(49, BEAT_DUR * 0.8, velocity=1.0, arrastre=True) # C#2
        p2 = make_piano_chord(ch_notes, dur=BEAT_DUR * 0.45, velocity=0.95, staccato=True)
        add_segment(bass2, bass2, bar_start_sample + 2 * SAMPLES_PER_BEAT, pan=-0.10)
        add_segment(p2, p2, bar_start_sample + 2 * SAMPLES_PER_BEAT, pan=0.20)
        add_segment(CAJA_RIMSHOT, CAJA_RIMSHOT, bar_start_sample + 2 * SAMPLES_PER_BEAT, pan=0.0)

        # Beat 3 & 3.5: THE LEGENDARY TANGO ARRASTRE INTO BAR 0!
        # Contrabajo and Bandoneon chromatic pickup driving into the downbeat of Bar 0:
        # C#2 (3.0) -> D2 (3.5) with continuous energy that connects seamlessly to sample 0!
        t_arrastre = bar_start_sample + 3 * SAMPLES_PER_BEAT
        b_arr = make_bass_note(49, BEAT_DUR * 1.2, velocity=1.05, arrastre=True) # C#2 leading to D2
        p_arr = make_piano_chord(ch_notes, dur=BEAT_DUR * 0.9, velocity=1.0, staccato=False)
        add_segment(b_arr, b_arr, t_arrastre, pan=-0.10)
        add_segment(p_arr, p_arr, t_arrastre, pan=0.20)
        add_segment(CAJA_MARCATO, CAJA_MARCATO, t_arrastre, pan=0.0)

    elif 8 <= b <= 15:
        # TANGO SINCOPADO PATTERN (Pugliese style offbeat hits)
        # Beat 0: Bass downbeat
        bass0 = make_bass_note(bass_root, BEAT_DUR * 0.8, velocity=0.95, arrastre=(b % 2 == 0))
        add_segment(bass0, bass0, bar_start_sample, pan=-0.10)

        # Beat 0.5 (offbeat): Piano chord bite
        p_off1 = make_piano_chord(ch_notes, dur=0.18, velocity=0.90, staccato=True)
        add_segment(p_off1, p_off1, bar_start_sample + (SAMPLES_PER_BEAT // 2), pan=0.20)

        # Beat 1.5 (offbeat): Piano stab + caja
        p_off2 = make_piano_chord(ch_notes, dur=0.22, velocity=0.95, staccato=True)
        add_segment(p_off2, p_off2, bar_start_sample + SAMPLES_PER_BEAT + (SAMPLES_PER_BEAT // 2), pan=0.20)
        add_segment(CAJA_RIMSHOT, CAJA_RIMSHOT, bar_start_sample + SAMPLES_PER_BEAT + (SAMPLES_PER_BEAT // 2), pan=0.0)

        # Beat 2: Heavy marcato beat
        alt_bass = bass_root + (7 if bass_root < 44 else -5)
        bass2 = make_bass_note(alt_bass, BEAT_DUR * 0.8, velocity=0.90, arrastre=True)
        p2 = make_piano_chord(ch_notes, dur=0.25, velocity=0.85, staccato=True)
        add_segment(bass2, bass2, bar_start_sample + 2 * SAMPLES_PER_BEAT, pan=-0.10)
        add_segment(p2, p2, bar_start_sample + 2 * SAMPLES_PER_BEAT, pan=0.20)

        # Beat 3: Syncopated chord
        p3 = make_piano_chord(ch_notes, dur=0.20, velocity=0.85, staccato=True)
        add_segment(p3, p3, bar_start_sample + 3 * SAMPLES_PER_BEAT, pan=0.20)

    else:
        # HEAVY TANGO MARCATO EN 4 (D'Arienzo arcade fighting drive)
        for beat in range(4):
            b_sample = bar_start_sample + beat * SAMPLES_PER_BEAT
            # Alternate bass root and fifth
            b_pitch = bass_root if (beat % 2 == 0) else (bass_root + 7 if bass_root < 44 else bass_root - 5)
            bass_note = make_bass_note(b_pitch, BEAT_DUR * 0.7, velocity=0.95 if (beat == 0 or beat == 2) else 0.85, arrastre=(beat == 0))
            piano_hit = make_piano_chord(ch_notes, dur=BEAT_DUR * 0.42, velocity=0.92 if (beat == 0 or beat == 2) else 0.82, staccato=True)

            add_segment(bass_note, bass_note, b_sample, pan=-0.10)
            add_segment(piano_hit, piano_hit, b_sample, pan=0.20)

            # Caja golpes on beats 0 and 2
            if beat in [0, 2]:
                add_segment(CAJA_MARCATO if beat == 0 else CAJA_RIMSHOT,
                            CAJA_MARCATO if beat == 0 else CAJA_RIMSHOT,
                            b_sample, pan=0.0)

# -------------------------------------------------------------
# 2. BANDONEÓN MELODY (30 BARS COMPLETE)
# -------------------------------------------------------------
# Format: (beat_position, duration_in_beats, midi_note, velocity, is_staccato)
BANDONEON_MELODY = []

# --- THEME A: "El Duelo del Obelisco" (Bars 0 - 7, beats 0 - 31) ---
BANDONEON_MELODY += [
    # Bar 0 (Dm)
    (0.00, 0.45, 62, 0.95, False), # D4 marcato
    (0.50, 0.45, 65, 0.95, False), # F4
    (1.00, 0.45, 69, 0.98, False), # A4
    (1.50, 0.25, 73, 0.90, False), # C#5
    (1.75, 0.25, 74, 0.95, False), # D5
    # Bar 1 (Dm)
    (2.00, 0.45, 77, 0.98, False), # F5
    (2.50, 0.45, 76, 0.92, False), # E5
    (3.00, 0.45, 74, 0.95, False), # D5
    (3.50, 0.45, 73, 0.90, False), # C#5
    # Bar 2 (A7)
    (4.00, 0.70, 76, 0.98, False), # E5 (dramatic held note)
    (5.00, 0.45, 74, 0.92, False), # D5
    (5.50, 0.25, 73, 0.90, False), # C#5
    (5.75, 0.25, 69, 0.92, False), # A4
    # Bar 3 (Dm)
    (6.00, 0.85, 74, 0.98, False), # D5 sustained
    (7.00, 0.45, 69, 0.88, False), # A4
    (7.50, 0.45, 70, 0.90, False), # Bb4
    # Bar 4 (Gm)
    (8.00, 0.45, 70, 0.95, False), # Bb4
    (8.50, 0.45, 74, 0.95, False), # D5
    (9.00, 0.70, 79, 1.00, False), # G5! Peak
    (9.75, 0.25, 77, 0.92, False), # F5
    # Bar 5 (Dm)
    (10.00, 0.45, 74, 0.95, False), # D5
    (10.50, 0.45, 69, 0.90, False), # A4
    (11.00, 0.70, 65, 0.92, False), # F4
    (11.75, 0.25, 67, 0.88, False), # G4
    # Bar 6 (A7)
    (12.00, 0.45, 69, 0.95, False), # A4
    (12.50, 0.45, 73, 0.92, False), # C#5
    (13.00, 0.70, 76, 0.98, False), # E5
    (13.75, 0.25, 74, 0.90, False), # D5
    # Bar 7 (Dm) - Cadence to Theme B
    (14.00, 0.95, 74, 1.00, False), # D5
    (15.50, 0.45, 72, 0.88, False), # C5
]

# --- THEME B: "Variación Sincopada" (Bars 8 - 15, beats 32 - 63) ---
BANDONEON_MELODY += [
    # Bar 8 (F)
    (16.00, 0.35, 69, 0.92, True),  # A4
    (16.50, 0.35, 72, 0.92, True),  # C5
    (17.00, 0.55, 77, 0.98, False), # F5
    (17.75, 0.25, 76, 0.90, False), # E5
    # Bar 9 (C)
    (18.00, 0.35, 72, 0.90, True),  # C5
    (18.50, 0.35, 76, 0.92, True),  # E5
    (19.00, 0.55, 79, 0.98, False), # G5
    (19.75, 0.25, 77, 0.92, False), # F5
    # Bar 10 (Dm)
    (20.00, 0.35, 74, 0.95, True),  # D5
    (20.50, 0.35, 77, 0.95, True),  # F5
    (21.00, 0.55, 81, 1.00, False), # A5! High point
    (21.75, 0.25, 79, 0.92, False), # G5
    # Bar 11 (A7)
    (22.00, 0.65, 76, 0.98, False), # E5
    (23.00, 0.45, 73, 0.90, False), # C#5
    (23.50, 0.45, 69, 0.88, False), # A4
    # Bar 12 (Bb)
    (24.00, 0.45, 70, 0.95, False), # Bb4
    (24.50, 0.45, 74, 0.95, False), # D5
    (25.00, 0.65, 77, 0.98, False), # F5
    (25.75, 0.25, 76, 0.90, False), # E5
    # Bar 13 (Gm)
    (26.00, 0.45, 74, 0.92, False), # D5
    (26.50, 0.45, 70, 0.90, False), # Bb4
    (27.00, 0.65, 67, 0.92, False), # G4
    (27.75, 0.25, 69, 0.88, False), # A4
    # Bar 14 (A7)
    (28.00, 0.45, 69, 0.95, False), # A4
    (28.50, 0.45, 73, 0.92, False), # C#5
    (29.00, 0.65, 76, 0.98, False), # E5
    (29.75, 0.25, 73, 0.90, False), # C#5
    # Bar 15 (Dm)
    (30.00, 0.85, 74, 1.00, False), # D5
    (31.00, 0.45, 74, 0.92, False), # D5
    (31.50, 0.45, 76, 0.90, False), # E5
]

# --- THEME C: "Solo Virtuoso de Bandoneón" (Bars 16 - 23, beats 64 - 95) ---
BANDONEON_MELODY += [
    # Bar 16 (Dm)
    (32.00, 0.25, 77, 0.95, True),  # F5 staccato run
    (32.25, 0.25, 76, 0.92, True),  # E5
    (32.50, 0.25, 74, 0.95, True),  # D5
    (32.75, 0.25, 73, 0.92, True),  # C#5
    (33.00, 0.45, 74, 0.98, False), # D5
    (33.50, 0.45, 77, 0.95, False), # F5
    # Bar 17 (Gm)
    (34.00, 0.25, 79, 0.98, True),  # G5
    (34.25, 0.25, 77, 0.92, True),  # F5
    (34.50, 0.25, 76, 0.90, True),  # E5
    (34.75, 0.25, 74, 0.90, True),  # D5
    (35.00, 0.45, 70, 0.95, False), # Bb4
    (35.50, 0.45, 74, 0.95, False), # D5
    # Bar 18 (E7)
    (36.00, 0.45, 76, 0.95, False), # E5
    (36.50, 0.45, 74, 0.92, False), # D5
    (37.00, 0.45, 72, 0.90, False), # C5
    (37.50, 0.45, 71, 0.92, False), # B4
    # Bar 19 (A7)
    (38.00, 0.65, 69, 0.98, False), # A4
    (39.00, 0.45, 73, 0.92, False), # C#5
    (39.50, 0.45, 76, 0.95, False), # E5
    # Bar 20 (Dm)
    (40.00, 0.45, 74, 0.98, False), # D5
    (40.50, 0.45, 77, 0.95, False), # F5
    (41.00, 0.45, 81, 1.00, False), # A5
    (41.50, 0.45, 79, 0.92, False), # G5
    # Bar 21 (Bb)
    (42.00, 0.45, 77, 0.95, False), # F5
    (42.50, 0.45, 74, 0.92, False), # D5
    (43.00, 0.65, 70, 0.95, False), # Bb4
    (43.75, 0.25, 72, 0.88, False), # C5
    # Bar 22 (Gm)
    (44.00, 0.45, 74, 0.95, False), # D5
    (44.50, 0.45, 70, 0.92, False), # Bb4
    (45.00, 0.65, 67, 0.95, False), # G4
    (45.75, 0.25, 69, 0.90, False), # A4
    # Bar 23 (A7)
    (46.00, 0.75, 69, 1.00, False), # A4
    (47.00, 0.45, 73, 0.95, False), # C#5
    (47.50, 0.45, 76, 0.98, False), # E5
]

# --- THEME D: "Gran Clímax & Conector Perfecto a Bar 0" (Bars 24 - 29, beats 96 - 119) ---
BANDONEON_MELODY += [
    # Bar 24 (Dm)
    (48.00, 0.45, 81, 1.00, False), # A5! Tutti
    (48.50, 0.45, 79, 0.92, False), # G5
    (49.00, 0.45, 77, 0.95, False), # F5
    (49.50, 0.45, 76, 0.90, False), # E5
    # Bar 25 (Gm)
    (50.00, 0.45, 74, 0.95, False), # D5
    (50.50, 0.45, 76, 0.92, False), # E5
    (51.00, 0.45, 77, 0.95, False), # F5
    (51.50, 0.45, 79, 0.98, False), # G5
    # Bar 26 (Bb)
    (52.00, 0.45, 82, 1.00, False), # Bb5!
    (52.50, 0.45, 81, 0.95, False), # A5
    (53.00, 0.45, 77, 0.95, False), # F5
    (53.50, 0.45, 74, 0.92, False), # D5
    # Bar 27 (E7)
    (54.00, 0.45, 76, 0.98, False), # E5
    (54.50, 0.45, 74, 0.92, False), # D5
    (55.0,  0.45, 73, 0.92, False), # C#5
    (55.50, 0.45, 71, 0.90, False), # B4
    # Bar 28 (A7) - Suspense & building cadence
    (56.00, 0.45, 69, 0.95, False), # A4
    (56.50, 0.45, 73, 0.95, False), # C#5
    (57.00, 0.45, 76, 0.98, False), # E5
    (57.50, 0.45, 79, 1.00, False), # G5
    # Bar 29 (A7) - THE CADENCE INTO BAR 0!
    (58.00, 0.45, 81, 1.00, True),  # A5 marcato bite
    (58.50, 0.45, 79, 0.95, True),  # G5 marcato
    (59.00, 0.45, 76, 0.98, True),  # E5 marcato
    # Beat 3.0 to 3.8: CHROMATIC PICKUP ARRASTRE LEADING DIRECTLY INTO D4 OF BAR 0!
    # A4 (59.40) -> Bb4 (59.55) -> B4 (59.70) -> C#5 (59.85) -> resolves to D4 at 0.0s!
    (59.40, 0.15, 69, 0.92, False), # A4
    (59.55, 0.15, 70, 0.92, False), # Bb4
    (59.70, 0.15, 71, 0.95, False), # B4
    (59.85, 0.14, 73, 1.00, False), # C#5 leading tone! (Ends exactly at 59.99s, resolving to D at 0.00s)
]

# Render all bandoneon notes
for b_time, dur_b, midi_p, vel, is_stacc in BANDONEON_MELODY:
    sample_pos = int(round(b_time * SAMPLES_PER_BEAT))
    dur_sec = dur_b * BEAT_DUR
    b_sound = make_bandoneon_note(midi_p, dur_sec, velocity=vel, vibrato=True, staccato=is_stacc)
    add_segment(b_sound, b_sound, sample_pos, pan=-0.18)

# -------------------------------------------------------------
# 3. SEAMLESS LOOP BOUNDARY MATCHING
# -------------------------------------------------------------
print("Applying mastering limiter and zero-crossing seamless boundary taper...")

# Peak limiter / Master normalizer before boundary match
peak = max(np.max(np.abs(left_master)), np.max(np.abs(right_master)))
print(f"Pre-master peak amplitude: {peak:.3f}")
target_peak = 0.92
if peak > 0:
    left_master = np.tanh(left_master / peak * 1.12) * target_peak
    right_master = np.tanh(right_master / peak * 1.12) * target_peak

# Smooth cosine S-curve taper over 1024 samples (~23.2 ms)
# This mathematically guarantees left_master[-1] == left_master[0] and
# right_master[-1] == right_master[0] with 0 click, 0 jump, and continuous waveform.
L_TAPER = 1024
taper = (1.0 - np.cos(np.linspace(0.0, np.pi, L_TAPER))) * 0.5

delta_l = left_master[0] - left_master[-1]
delta_r = right_master[0] - right_master[-1]
left_master[-L_TAPER:] += delta_l * taper
right_master[-L_TAPER:] += delta_r * taper

# -------------------------------------------------------------
# 4. EXPORT TO 16-BIT 44.1kHz STEREO WAV
# -------------------------------------------------------------
out_path = "Assets/Audio/Music/bgm_obelisco_tango.wav"
left_16 = np.int16(np.clip(left_master * 32767.0, -32768, 32767))
right_16 = np.int16(np.clip(right_master * 32767.0, -32768, 32767))

stereo_interleaved = np.empty((TOTAL_SAMPLES * 2,), dtype=np.int16)
stereo_interleaved[0::2] = left_16
stereo_interleaved[1::2] = right_16

with wave.open(out_path, 'wb') as wf:
    wf.setnchannels(2)
    wf.setsampwidth(2)
    wf.setframerate(SR)
    wf.writeframes(stereo_interleaved.tobytes())

file_mb = os.path.getsize(out_path) / (1024 * 1024)
print(f"Successfully generated seamless Obelisco Tango: {out_path} ({file_mb:.2f} MB, {TOTAL_DUR:.3f}s, {TOTAL_SAMPLES} samples)")
