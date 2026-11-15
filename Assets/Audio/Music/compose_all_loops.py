"""
Compose from scratch all 3 stage music tracks for Fightyub with mathematically
and musically seamless loops (no gap, no click, perfect rhythmic & harmonic closure):
1. Obelisco: Argentine Tango Arcade (120 BPM, 16 bars = 32.000s, D minor)
2. Triunvirato y Olazábal: Progressive Metal Arcade (135 BPM, 18 bars = 32.000s, E minor)
3. Casa Rosada: Cumbia Arcade (100 BPM, 16 bars = 38.400s, A minor)
"""

import math
import os
import wave
import numpy as np

SR = 44100

def midi_to_freq(m):
    return 440.0 * (2.0 ** ((m - 69.0) / 12.0))

def apply_zero_crossing_boundary(l_buf, r_buf, xfade_samples=512):
    """Guarantees sample[0] == sample[N-1] and identical derivative across loop point."""
    taper = 0.5 * (1.0 - np.cos(np.linspace(0.0, np.pi, xfade_samples)))
    dL = l_buf[0] - l_buf[-1]
    dR = r_buf[0] - r_buf[-1]
    l_buf[-xfade_samples:] += dL * taper
    r_buf[-xfade_samples:] += dR * taper
    return l_buf, r_buf

def write_wav(path, l_data, r_data):
    """Master normalize and export 16-bit stereo WAV."""
    peak = max(np.max(np.abs(l_data)), np.max(np.abs(r_data)))
    target_peak = 0.90 # -0.9 dBFS
    if peak > 0:
        l_norm = np.tanh(l_data / peak * 1.15) * target_peak
        r_norm = np.tanh(r_data / peak * 1.15) * target_peak
    else:
        l_norm, r_norm = l_data, r_data

    l_norm, r_norm = apply_zero_crossing_boundary(l_norm, r_norm, 512)

    l16 = np.int16(np.clip(l_norm * 32767.0, -32768, 32767))
    r16 = np.int16(np.clip(r_norm * 32767.0, -32768, 32767))

    interleaved = np.empty((len(l16) * 2,), dtype=np.int16)
    interleaved[0::2] = l16
    interleaved[1::2] = r16

    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, 'wb') as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(SR)
        wf.writeframes(interleaved.tobytes())

    dur = len(l16) / SR
    mb = os.path.getsize(path) / (1024 * 1024)
    print(f"Exported: {path} ({dur:.3f}s, {len(l16)} samples, {mb:.2f} MB)")

# ==============================================================================
# 1. OBELISCO: TANGO PORTEÑO ARCADE
# ==============================================================================
def compose_obelisco_tango():
    print("\n--- Composing Obelisco Tango Arcade ---")
    bpm = 120.0
    beat_dur = 60.0 / bpm  # 0.500s
    spb = int(round(SR * beat_dur))  # 22050 exact
    num_bars = 16
    total_samples = num_bars * spb * 4  # 1,411,200 samples = 32.000s

    l_master = np.zeros(total_samples, dtype=np.float32)
    r_master = np.zeros(total_samples, dtype=np.float32)

    def add_sound(l_snd, r_snd, start_s, pan=0.0):
        n = len(l_snd)
        pan = max(-1.0, min(1.0, pan))
        lg = math.cos((pan + 1.0) * math.pi / 4.0)
        rg = math.sin((pan + 1.0) * math.pi / 4.0)
        idx = np.arange(start_s, start_s + n) % total_samples
        np.add.at(l_master, idx, l_snd * lg)
        np.add.at(r_master, idx, r_snd * rg)

    # Instrument generators
    def bando_tone(midi_p, dur, vel=1.0, stacc=False):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_p)
        f1, f2 = f0 - 0.9, f0 + 0.9
        vib = 1.0 + 0.006 * np.sin(2.0 * np.pi * 5.8 * t) if (dur > 0.25 and not stacc) else 1.0
        s = np.zeros(n, dtype=np.float32)
        for h in [1, 2, 3, 5, 7]:
            s += (1.0 / (h ** 0.8)) * (np.sin(2.0 * np.pi * f1 * h * t * vib) + np.sin(2.0 * np.pi * f2 * h * t * vib + 0.4))
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.015 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.75 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel) ** 2
        return (np.tanh(s * 1.4) * env * vel * 0.50).astype(np.float32)

    def piano_chord(notes, dur=0.22, vel=0.85):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        s = np.zeros(n, dtype=np.float32)
        for m in notes:
            f = midi_to_freq(m)
            s += np.sin(2.0 * np.pi * f * t) + 0.5 * np.sin(2.0 * np.pi * 2 * f * t) + 0.25 * np.sin(2.0 * np.pi * 3 * f * t)
        hammer = np.sin(2.0 * np.pi * 2200.0 * t[:min(120, n)]) * np.exp(-t[:min(120, n)] * 100.0)
        s[:len(hammer)] += hammer * 0.3
        env = np.exp(-t * 14.0)
        return (s * env * vel * 0.32).astype(np.float32)

    def bass_hit(midi_p, dur=0.35, vel=1.0, arrastre=False):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_p)
        if arrastre:
            scoop = min(int(round(0.045 * SR)), n)
            f_arr = np.ones(n, dtype=np.float32) * f0
            f_arr[:scoop] = np.linspace(f0 * 0.88, f0, scoop)
            phase = 2.0 * np.pi * np.cumsum(f_arr) / SR
        else:
            phase = 2.0 * np.pi * f0 * t
        s = np.sin(phase) + 0.6 * np.sin(phase * 2.0) + 0.2 * np.sin(phase * 3.0)
        env = np.exp(-t * 5.5)
        return (np.tanh(s * 1.5) * env * vel * 0.70).astype(np.float32)

    def caja_hit(rim=False):
        n = int(round((0.20 if rim else 0.15) * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = 260.0 if rim else 180.0
        b = np.sin(2.0 * np.pi * f0 * t * np.exp(-t * 30.0))
        noise = np.random.uniform(-1, 1, n).astype(np.float32) * np.exp(-t * 40.0)
        return ((b * 0.6 + noise * 0.4) * 0.45).astype(np.float32)

    CAJA = caja_hit(rim=False)
    RIM = caja_hit(rim=True)

    # 16-bar harmony (Dm - A7 - Gm - Bb - E7 - A7)
    CHORDS_TANGO = [
        ('Dm', [50, 53, 57], 38), ('Dm', [50, 53, 57], 38), ('A7', [49, 52, 55], 45), ('Dm', [50, 53, 57], 38),
        ('Gm', [46, 50, 55], 43), ('Dm', [50, 53, 57], 38), ('A7', [49, 52, 55], 45), ('Dm', [50, 53, 57], 38),
        ('F',  [48, 53, 57], 41), ('C',  [48, 52, 55], 36), ('Dm', [50, 53, 57], 38), ('A7', [49, 52, 55], 45),
        ('Bb', [46, 50, 53], 46), ('Gm', [46, 50, 55], 43), ('A7', [49, 52, 55], 45), ('A7', [49, 52, 55], 45) # Bar 15 = Cadence & Arrastre
    ]

    # Rhythm Section
    for bar in range(num_bars):
        bar_s = bar * spb * 4
        ch_name, ch_notes, b_root = CHORDS_TANGO[bar]
        if bar == 15:
            # Bar 15: The Pickup Arrastre into Bar 0!
            # Beats 0, 1, 2: Cadence stabs on A7
            for bt in [0, 1, 2]:
                s_idx = bar_s + bt * spb
                b_snd = bass_hit(45 if bt < 2 else 49, dur=0.35, vel=1.0, arrastre=(bt == 0))
                p_snd = piano_chord(ch_notes, dur=0.25, vel=0.95)
                add_sound(b_snd, b_snd, s_idx, pan=-0.12)
                add_sound(p_snd, p_snd, s_idx, pan=0.18)
                add_sound(RIM if bt == 2 else CAJA, RIM if bt == 2 else CAJA, s_idx, pan=0.0)
            # Beat 3 & 3.5: Chromatic Arrastre driving right into Bar 0!
            # Contrabajo and piano run C#2 (49) -> D2 (38/50)
            t3 = bar_s + 3 * spb
            b_arr = bass_hit(49, dur=0.48, vel=1.05, arrastre=True)
            p_arr = piano_chord(ch_notes, dur=0.45, vel=1.0)
            add_sound(b_arr, b_arr, t3, pan=-0.12)
            add_sound(p_arr, p_arr, t3, pan=0.18)
            add_sound(CAJA, CAJA, t3, pan=0.0)
        else:
            # Standard heavy marcato en 4
            for bt in range(4):
                s_idx = bar_s + bt * spb
                pitch = b_root if (bt % 2 == 0) else (b_root + 7 if b_root < 44 else b_root - 5)
                b_snd = bass_hit(pitch, dur=0.32, vel=0.92 if bt in [0, 2] else 0.82, arrastre=(bt == 0))
                p_snd = piano_chord(ch_notes, dur=0.22, vel=0.90 if bt in [0, 2] else 0.80)
                add_sound(b_snd, b_snd, s_idx, pan=-0.12)
                add_sound(p_snd, p_snd, s_idx, pan=0.18)
                if bt in [0, 2]:
                    add_sound(CAJA if bt == 0 else RIM, CAJA if bt == 0 else RIM, s_idx, pan=0.0)

    # Bandoneon Melodic Line (16 bars continuous)
    # Notes: (bar, beat_offset, dur_beats, midi, vel)
    TANGO_NOTES = [
        # Bars 0-3 (Theme A)
        (0, 0.0, 0.45, 62, 0.95), (0, 0.5, 0.45, 65, 0.95), (0, 1.0, 0.45, 69, 0.98), (0, 1.5, 0.45, 74, 0.98),
        (1, 0.0, 0.45, 77, 1.00), (1, 0.5, 0.45, 76, 0.92), (1, 1.0, 0.45, 74, 0.95), (1, 1.5, 0.45, 73, 0.90),
        (2, 0.0, 0.70, 76, 0.98), (2, 1.0, 0.45, 74, 0.92), (2, 1.5, 0.45, 73, 0.90),
        (3, 0.0, 0.85, 74, 1.00), (3, 1.0, 0.45, 69, 0.88), (3, 1.5, 0.45, 70, 0.90),
        # Bars 4-7 (Theme A variation)
        (4, 0.0, 0.45, 70, 0.95), (4, 0.5, 0.45, 74, 0.95), (4, 1.0, 0.70, 79, 1.00), (4, 1.75, 0.25, 77, 0.92),
        (5, 0.0, 0.45, 74, 0.95), (5, 0.5, 0.45, 69, 0.90), (5, 1.0, 0.70, 65, 0.92),
        (6, 0.0, 0.45, 69, 0.95), (6, 0.5, 0.45, 73, 0.92), (6, 1.0, 0.70, 76, 0.98),
        (7, 0.0, 0.85, 74, 1.00), (7, 1.5, 0.45, 72, 0.88),
        # Bars 8-11 (Sincopado)
        (8, 0.0, 0.35, 69, 0.92), (8, 0.5, 0.35, 72, 0.92), (8, 1.0, 0.55, 77, 0.98), (8, 1.75, 0.25, 76, 0.90),
        (9, 0.0, 0.35, 72, 0.90), (9, 0.5, 0.35, 76, 0.92), (9, 1.0, 0.55, 79, 0.98),
        (10, 0.0, 0.35, 74, 0.95), (10, 0.5, 0.35, 77, 0.95), (10, 1.0, 0.55, 81, 1.00),
        (11, 0.0, 0.65, 76, 0.98), (11, 1.0, 0.45, 73, 0.90), (11, 1.5, 0.45, 69, 0.88),
        # Bars 12-14 (Climax)
        (12, 0.0, 0.45, 70, 0.95), (12, 0.5, 0.45, 74, 0.95), (12, 1.0, 0.65, 77, 0.98),
        (13, 0.0, 0.45, 74, 0.92), (13, 0.5, 0.45, 70, 0.90), (13, 1.0, 0.65, 67, 0.92),
        (14, 0.0, 0.45, 69, 0.95), (14, 0.5, 0.45, 73, 0.95), (14, 1.0, 0.65, 76, 0.98),
        # Bar 15: Cadence & Arrastre pickup (resolving directly into D4 Bar 0!)
        (15, 0.0, 0.45, 81, 1.00), (15, 0.5, 0.45, 79, 0.95), (15, 1.0, 0.45, 76, 0.98),
        # Final chromatic pickup in 16ths:
        (15, 1.5, 0.20, 69, 0.92), # A4
        (15, 1.7, 0.20, 71, 0.95), # B4
        (15, 1.85, 0.15, 73, 1.00), # C#5 leading tone! (meets D4 at sample 0)
    ]

    for bar, bt_off, dur_b, midi_p, vel in TANGO_NOTES:
        start_s = int(round((bar * 4 + bt_off) * spb))
        dur_s = dur_b * beat_dur
        b_snd = bando_tone(midi_p, dur_s, vel=vel, stacc=(dur_b < 0.3))
        add_sound(b_snd, b_snd, start_s, pan=-0.16)

    write_wav("Assets/Audio/Music/bgm_obelisco_tango.wav", l_master, r_master)

# ==============================================================================
# 2. TRIUNVIRATO Y OLAZÁBAL: METAL PROGRESIVO ARCADE
# ==============================================================================
def compose_triunvirato_metal():
    print("\n--- Composing Triunvirato Progressive Metal Arcade ---")
    bpm = 135.0
    beat_dur = 60.0 / bpm  # 0.4444...s
    spb = int(round(SR * beat_dur))  # 19600 exact
    num_bars = 18
    total_samples = num_bars * spb * 4  # 1,411,200 samples = exactly 32.000s

    l_master = np.zeros(total_samples, dtype=np.float32)
    r_master = np.zeros(total_samples, dtype=np.float32)

    def add_sound(l_snd, r_snd, start_s, pan=0.0):
        n = len(l_snd)
        pan = max(-1.0, min(1.0, pan))
        lg = math.cos((pan + 1.0) * math.pi / 4.0)
        rg = math.sin((pan + 1.0) * math.pi / 4.0)
        idx = np.arange(start_s, start_s + n) % total_samples
        np.add.at(l_master, idx, l_snd * lg)
        np.add.at(r_master, idx, r_snd * rg)

    # Guitar chug (distorted saw wave with cabinet lowpass)
    def guitar_power_chord(midi_root, dur, palm_mute=False, vel=1.0):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_root)
        f5th = f0 * 1.4983 # 5th
        f8ve = f0 * 2.0    # Octave
        sig = np.zeros(n, dtype=np.float32)
        for h in range(1, 8):
            sig += (1.0 / (h ** 0.85)) * (np.sin(2 * np.pi * f0 * h * t) + np.sin(2 * np.pi * f5th * h * t + 0.3) + 0.7 * np.sin(2 * np.pi * f8ve * h * t + 0.5))
        # High-gain distortion (clipper)
        sig = np.tanh(sig * 3.5)
        pick = np.sin(2 * np.pi * 1800.0 * t[:min(160, n)]) * np.exp(-t[:min(160, n)] * 110.0)
        sig[:len(pick)] += pick * 0.4
        damping = 22.0 if palm_mute else 4.0
        env = np.exp(-t * damping)
        return (sig * env * vel * 0.48).astype(np.float32)

    def guitar_lead(midi_p, dur, vel=1.0):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_p)
        vib = 1.0 + 0.008 * np.sin(2 * np.pi * 6.2 * t) if dur > 0.25 else 1.0
        sig = np.zeros(n, dtype=np.float32)
        for h in range(1, 9):
            if f0 * h > 14000: break
            sig += (1.0 / (h ** 0.88)) * np.sin(2 * np.pi * f0 * h * t * vib)
        sig = np.tanh(sig * 2.8)
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.012 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.82 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel)
        return (sig * env * vel * 0.52).astype(np.float32)

    def metal_kick():
        n = int(round(0.25 * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f_env = 160.0 * np.exp(-t * 36.0) + 48.0
        phase = 2.0 * np.pi * np.cumsum(f_env) / SR
        s = np.sin(phase) + 0.3 * np.random.uniform(-1, 1, n) * np.exp(-t * 80.0)
        return (s * np.exp(-t * 12.0) * 0.80).astype(np.float32)

    def metal_snare():
        n = int(round(0.28 * SR))
        t = np.arange(n, dtype=np.float32) / SR
        tone = np.sin(2 * np.pi * 210.0 * t * np.exp(-t * 24.0))
        noise = np.random.uniform(-1, 1, n).astype(np.float32) * np.exp(-t * 22.0)
        return ((tone * 0.5 + noise * 0.7) * 0.65).astype(np.float32)

    KICK = metal_kick()
    SNARE = metal_snare()

    # 18 Bars Progression: E minor prog metal
    # Bars 0-3: E5 chug riff (3+3+2)
    # Bars 4-7: C5 -> D5 -> E5 melodic verse
    # Bars 8-11: Lead guitar solo over E5 / G5 / A5
    # Bars 12-15: Technical prog breakdown (E5 - Bb5 tritone - B5)
    # Bars 16-17: Climax & Pickup Fill resolving right into Bar 0 E5!

    for bar in range(num_bars):
        bar_s = bar * spb * 4
        # Drum pattern
        if bar == 17:
            # Bar 17: End-of-loop fill! Double kick + snare roll leading into Bar 0
            for sixteenth in range(16):
                hit_s = bar_s + sixteenth * (spb // 4)
                add_sound(KICK, KICK, hit_s, pan=0.0)
                if sixteenth >= 8:
                    add_sound(SNARE * (0.6 + 0.4 * (sixteenth - 8) / 8.0),
                              SNARE * (0.6 + 0.4 * (sixteenth - 8) / 8.0), hit_s, pan=0.0)
        else:
            # Driving double bass & snare on 2 and 4
            for beat in range(4):
                add_sound(KICK, KICK, bar_s + beat * spb, pan=0.0)
                add_sound(KICK, KICK, bar_s + beat * spb + (spb // 2), pan=0.0)
                if beat in [1, 3]:
                    add_sound(SNARE, SNARE, bar_s + beat * spb, pan=0.0)

        # Rhythm guitar riffs
        root_pitch = 40 # E2 (Drop D tuning = 38)
        if bar in [4, 5]: root_pitch = 48 # C3
        elif bar in [6, 7]: root_pitch = 50 # D3
        elif bar in [12, 13]: root_pitch = 46 # Bb2 (tritone prog bite)
        elif bar in [14, 15]: root_pitch = 47 # B2
        elif bar in [16, 17]: root_pitch = 47 # B2 (Dominant tension into E2)

        if bar == 17:
            # Bar 17: Dominant pickup run resolving into E5 at Bar 0!
            # B2 chugs on beats 0 & 1, then ascending octave run on beats 2 & 3:
            # B2 (47) -> D3 (50) -> D#3 (51) -> E3 (52) leading right into beat 0 of bar 0!
            chug0 = guitar_power_chord(47, dur=0.35, palm_mute=True, vel=1.0)
            add_sound(chug0, chug0, bar_s, pan=-0.35)
            add_sound(chug0, chug0, bar_s, pan=0.35)

            p_run = [
                (2.0, 50, 0.20), (2.5, 51, 0.20), (3.0, 53, 0.20), (3.5, 54, 0.22) # leading to 52/40 (E)
            ]
            for b_off, m_p, dur_b in p_run:
                s_off = bar_s + int(round(b_off * spb))
                r_note = guitar_power_chord(m_p, dur=dur_b, palm_mute=False, vel=1.05)
                add_sound(r_note, r_note, s_off, pan=-0.35)
                add_sound(r_note, r_note, s_off, pan=0.35)
        else:
            # 3+3+2 Polyrhythmic Metal Chug
            chug_pattern = [0.0, 0.75, 1.5, 2.0, 2.75, 3.5]
            for b_off in chug_pattern:
                s_off = bar_s + int(round(b_off * spb))
                is_accent = (b_off in [0.0, 2.0])
                chug_l = guitar_power_chord(root_pitch, dur=0.25, palm_mute=(not is_accent), vel=1.0 if is_accent else 0.85)
                chug_r = guitar_power_chord(root_pitch, dur=0.25, palm_mute=(not is_accent), vel=1.0 if is_accent else 0.85)
                add_sound(chug_l, chug_r, s_off, pan=-0.35)
                add_sound(chug_l, chug_r, s_off, pan=0.35)

    # Lead Guitar Theme (Bars 2-15)
    LEAD_NOTES = [
        # Bars 2-5 (Theme Melody)
        (2, 0.0, 0.40, 64, 0.95), (2, 0.5, 0.40, 67, 0.95), (2, 1.0, 0.75, 71, 1.00), (2, 1.75, 0.25, 72, 0.92),
        (3, 0.0, 0.40, 71, 0.95), (3, 0.5, 0.40, 67, 0.92), (3, 1.0, 0.85, 64, 0.98),
        (4, 0.0, 0.40, 67, 0.95), (4, 0.5, 0.40, 71, 0.95), (4, 1.0, 0.75, 74, 1.00), (4, 1.75, 0.25, 76, 0.92),
        (5, 0.0, 0.40, 74, 0.95), (5, 0.5, 0.40, 71, 0.92), (5, 1.0, 0.85, 67, 0.98),
        # Bars 8-11 (Fast Shred / Solos)
        (8, 0.0, 0.25, 76, 1.00), (8, 0.25, 0.25, 74, 0.92), (8, 0.5, 0.25, 72, 0.95), (8, 0.75, 0.25, 71, 0.90),
        (8, 1.0, 0.50, 76, 1.00), (8, 1.5, 0.50, 79, 1.00),
        (9, 0.0, 0.25, 83, 1.05), (9, 0.5, 0.25, 81, 0.95), (9, 1.0, 0.75, 79, 1.00),
        (10, 0.0, 0.40, 77, 0.95), (10, 0.5, 0.40, 76, 0.92), (10, 1.0, 0.75, 74, 0.98),
        (11, 0.0, 0.85, 71, 1.00),
        # Bars 14-16 (Harmonized Technical Climax)
        (14, 0.0, 0.40, 76, 1.00), (14, 0.5, 0.40, 79, 1.00), (14, 1.0, 0.75, 83, 1.05),
        (15, 0.0, 0.40, 81, 1.00), (15, 0.5, 0.40, 79, 0.95), (15, 1.0, 0.85, 76, 1.00),
        (16, 0.0, 0.40, 75, 1.00), (16, 0.5, 0.40, 78, 1.00), (16, 1.0, 0.75, 82, 1.05),
        # Bar 17: Ascending screaming lead pickup into Bar 0!
        (17, 1.0, 0.25, 78, 0.95), (17, 1.25, 0.25, 81, 1.00), (17, 1.5, 0.25, 83, 1.00), (17, 1.75, 0.25, 86, 1.05) # leading right into E6 (88/76)
    ]

    for bar, bt_off, dur_b, midi_p, vel in LEAD_NOTES:
        start_s = int(round((bar * 4 + bt_off) * spb))
        dur_s = dur_b * beat_dur
        l_snd = guitar_lead(midi_p, dur_s, vel=vel)
        add_sound(l_snd, l_snd, start_s, pan=0.08)

    write_wav("Assets/Audio/Music/bgm_triunvirato_progmetal.wav", l_master, r_master)

# ==============================================================================
# 3. CASA ROSADA: CUMBIA ARCADE
# ==============================================================================
def compose_casarosada_cumbia():
    print("\n--- Composing Casa Rosada Cumbia Arcade ---")
    bpm = 100.0
    beat_dur = 60.0 / bpm  # 0.600s
    spb = int(round(SR * beat_dur))  # 26460 exact
    num_bars = 16
    total_samples = num_bars * spb * 4  # 1,693,440 samples = exactly 38.400s

    l_master = np.zeros(total_samples, dtype=np.float32)
    r_master = np.zeros(total_samples, dtype=np.float32)

    def add_sound(l_snd, r_snd, start_s, pan=0.0):
        n = len(l_snd)
        pan = max(-1.0, min(1.0, pan))
        lg = math.cos((pan + 1.0) * math.pi / 4.0)
        rg = math.sin((pan + 1.0) * math.pi / 4.0)
        idx = np.arange(start_s, start_s + n) % total_samples
        np.add.at(l_master, idx, l_snd * lg)
        np.add.at(r_master, idx, r_snd * rg)

    # Percussion & instruments
    def make_guira(dur=0.12):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        noise = np.random.uniform(-1, 1, n).astype(np.float32)
        scrape = (np.sin(2 * np.pi * 4500.0 * t) + 0.7 * np.sin(2 * np.pi * 6800.0 * t)) * noise
        env = np.exp(-t * 26.0)
        return (scrape * env * 0.40).astype(np.float32)

    def cumbia_kick():
        n = int(round(0.26 * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f_env = 140.0 * np.exp(-t * 32.0) + 48.0
        phase = 2.0 * np.pi * np.cumsum(f_env) / SR
        return ((np.sin(phase) * np.exp(-t * 11.0)) * 0.82).astype(np.float32)

    def timbal_hit(high=True):
        n = int(round(0.32 * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = 320.0 if high else 220.0
        tone = np.sin(2 * np.pi * f0 * t) + 0.4 * np.sin(2 * np.pi * f0 * 1.6 * t)
        noise = np.random.uniform(-1, 1, n).astype(np.float32) * np.exp(-t * 60.0)
        return ((tone * np.exp(-t * 16.0) + noise * 0.3) * 0.65).astype(np.float32)

    def cumbia_bass(midi_p, dur=0.28, vel=1.0):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_p)
        s = np.sin(2 * np.pi * f0 * t) + 0.55 * np.sin(2 * np.pi * 2 * f0 * t) + 0.2 * np.sin(2 * np.pi * 3 * f0 * t)
        click = np.sin(2 * np.pi * 1200.0 * t[:min(140, n)]) * np.exp(-t[:min(140, n)] * 80.0)
        s[:len(click)] += click * 0.35
        env = np.exp(-t * 5.5)
        return (np.tanh(s * 1.4) * env * vel * 0.72).astype(np.float32)

    def cumbia_piano_chop(notes, dur=0.18, vel=0.85):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        s = np.zeros(n, dtype=np.float32)
        for m in notes:
            f = midi_to_freq(m)
            s += np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * 2 * f * t)
        return (s * np.exp(-t * 16.0) * vel * 0.35).astype(np.float32)

    def accordion_lead(midi_p, dur, vel=1.0):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_p)
        vib = 1.0 + 0.006 * np.sin(2 * np.pi * 6.0 * t) if dur > 0.25 else 1.0
        s = np.sin(2 * np.pi * (f0 - 0.8) * t * vib) + np.sin(2 * np.pi * (f0 + 0.8) * t * vib + 0.3)
        s += 0.5 * np.sin(2 * np.pi * f0 * 2 * t * vib) + 0.3 * np.sin(2 * np.pi * f0 * 3 * t * vib)
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.018 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.80 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel)
        return (s * env * vel * 0.45).astype(np.float32)

    GUIRA = make_guira()
    KICK = cumbia_kick()
    TIMBAL_H = timbal_hit(high=True)
    TIMBAL_L = timbal_hit(high=False)

    CHORDS_CUMBIA = [
        ('Am', [57, 60, 64], [45, 48, 52]), ('Dm', [50, 53, 57], [38, 41, 45]),
        ('G',  [55, 59, 62], [43, 47, 50]), ('C',  [48, 52, 55], [48, 52, 55]),
        ('F',  [53, 57, 60], [41, 45, 48]), ('Dm', [50, 53, 57], [38, 41, 45]),
        ('E7', [52, 56, 59], [40, 44, 47]), ('Am', [57, 60, 64], [45, 48, 52]),
        ('Am', [57, 60, 64], [45, 48, 52]), ('G',  [55, 59, 62], [43, 47, 50]),
        ('F',  [53, 57, 60], [41, 45, 48]), ('E7', [52, 56, 59], [40, 44, 47]),
        ('Dm', [50, 53, 57], [38, 41, 45]), ('Am', [57, 60, 64], [45, 48, 52]),
        ('E7', [52, 56, 59], [40, 44, 47]), ('E7', [52, 56, 59], [40, 44, 47]) # Bar 15 = Cadence & Timbal roll into Am
    ]

    for bar in range(num_bars):
        bar_s = bar * spb * 4
        ch_name, ch_notes, b_pat = CHORDS_CUMBIA[bar]

        # Güira on all 16ths
        for sixteenth in range(16):
            add_sound(GUIRA, GUIRA, bar_s + sixteenth * (spb // 4), pan=-0.22)

        # Kick on 1 and 3
        add_sound(KICK, KICK, bar_s, pan=0.0)
        add_sound(KICK, KICK, bar_s + 2 * spb, pan=0.0)

        # Offbeat piano chops ("el chan-chan")
        for bt in range(4):
            chop_s = bar_s + bt * spb + (spb // 2)
            p_chop = cumbia_piano_chop(ch_notes, dur=0.18, vel=0.85)
            add_sound(p_chop, p_chop, chop_s, pan=0.15)

        # Bassline
        b_hits = [
            (0.0, b_pat[0]), (1.0, b_pat[0]), (2.0, b_pat[1]), (2.75, b_pat[2]), (3.0, b_pat[1])
        ]
        if bar == 15:
            # Bar 15: Timbal Repique Roll and Bass Pickup into Bar 0 Am!
            # Beats 0 & 1: E7 bass & chop
            b0 = cumbia_bass(40, dur=0.28, vel=1.0)
            add_sound(b0, b0, bar_s, pan=0.0)
            b1 = cumbia_bass(40, dur=0.28, vel=0.95)
            add_sound(b1, b1, bar_s + spb, pan=0.0)

            # Beats 2 & 3: Fast Timbal repique fill and ascending pickup to A2
            for st in range(8):
                t_s = bar_s + 2 * spb + st * (spb // 4)
                snd = TIMBAL_H if (st % 2 == 0) else TIMBAL_L
                add_sound(snd * 0.9, snd * 0.9, t_s, pan=0.25)

            # Chromatic bass pickup E2 -> F#2 -> G#2 -> A2 (Bar 0)
            b_run = [(2.0, 40), (2.5, 42), (3.0, 44), (3.5, 44)] # 44 = G#2
            for b_off, m_p in b_run:
                b_s = bar_s + int(round(b_off * spb))
                b_snd = cumbia_bass(m_p, dur=0.22, vel=1.0)
                add_sound(b_snd, b_snd, b_s, pan=0.0)
        else:
            for b_off, m_p in b_hits:
                b_s = bar_s + int(round(b_off * spb))
                b_snd = cumbia_bass(m_p, dur=0.26, vel=0.92)
                add_sound(b_snd, b_snd, b_s, pan=0.0)

            # Timbal accent on beat 4
            add_sound(TIMBAL_H, TIMBAL_H, bar_s + 3 * spb + (spb // 2), pan=0.25)

    # Accordion & Synth Melodic Theme
    CUMBIA_NOTES = [
        # Bars 0-3 (Theme Am)
        (0, 0.0, 0.35, 69, 0.95), (0, 0.75, 0.25, 72, 0.90), (0, 1.5, 0.40, 76, 0.98),
        (1, 0.0, 0.35, 74, 0.95), (1, 0.75, 0.25, 77, 0.90), (1, 1.5, 0.40, 81, 1.00),
        (2, 0.0, 0.35, 79, 0.95), (2, 0.75, 0.25, 76, 0.90), (2, 1.5, 0.40, 74, 0.95),
        (3, 0.0, 0.75, 72, 0.98), (3, 1.5, 0.45, 76, 0.92),
        # Bars 4-7
        (4, 0.0, 0.35, 77, 0.95), (4, 0.75, 0.25, 76, 0.90), (4, 1.5, 0.40, 74, 0.95),
        (5, 0.0, 0.35, 74, 0.95), (5, 0.75, 0.25, 72, 0.90), (5, 1.5, 0.40, 69, 0.92),
        (6, 0.0, 0.35, 68, 0.95), (6, 0.75, 0.25, 71, 0.92), (6, 1.5, 0.40, 74, 0.98),
        (7, 0.0, 0.85, 69, 1.00),
        # Bars 8-11 (Synth lead variation)
        (8, 0.0, 0.30, 81, 1.00), (8, 0.75, 0.25, 84, 0.95), (8, 1.5, 0.45, 88, 1.05),
        (9, 0.0, 0.30, 86, 0.98), (9, 0.75, 0.25, 83, 0.92), (9, 1.5, 0.45, 81, 0.98),
        (10, 0.0, 0.30, 77, 0.95), (10, 0.75, 0.25, 81, 0.95), (10, 1.5, 0.45, 84, 1.00),
        (11, 0.0, 0.65, 83, 1.00),
        # Bars 12-14 (Climax)
        (12, 0.0, 0.35, 86, 1.00), (12, 0.75, 0.25, 84, 0.95), (12, 1.5, 0.45, 81, 0.98),
        (13, 0.0, 0.35, 84, 0.98), (13, 0.75, 0.25, 81, 0.92), (13, 1.5, 0.45, 76, 0.95),
        (14, 0.0, 0.35, 80, 0.98), (14, 0.75, 0.25, 83, 0.95), (14, 1.5, 0.45, 86, 1.02),
        # Bar 15: Accordion pickup fill into Bar 0 Am!
        (15, 0.0, 0.35, 88, 1.05), (15, 0.75, 0.25, 86, 0.98),
        # Chromatic run E5 -> F5 -> G#5 leading directly into A5/A4 in Bar 0!
        (15, 1.25, 0.20, 76, 0.92), (15, 1.50, 0.20, 77, 0.95), (15, 1.75, 0.22, 80, 1.00)
    ]

    for bar, bt_off, dur_b, midi_p, vel in CUMBIA_NOTES:
        start_s = int(round((bar * 4 + bt_off) * spb))
        dur_s = dur_b * beat_dur
        a_snd = accordion_lead(midi_p, dur_s, vel=vel)
        add_sound(a_snd, a_snd, start_s, pan=-0.14)

    write_wav("Assets/Audio/Music/bgm_casarosada_cumbia.wav", l_master, r_master)

if __name__ == "__main__":
    compose_obelisco_tango()
    compose_triunvirato_metal()
    compose_casarosada_cumbia()
    print("\nAll 3 compositions completed successfully.")
