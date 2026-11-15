"""
Synthesis script for two new Fightyub stages:
1. Floralis Genérica: Rock Nacional Argentino Arcade (125 BPM, 16 bars = 30.720s, A minor / A major modal)
2. Puente de la Mujer: Música Electrónica Dinámica y Urbana Arcade (126 BPM, 16 bars = 30.476s, F minor)

Guarantees 100% mathematically and musically seamless loops:
- Exact integer samples per beat and total length
- Musical cadence and pickup in bar 15 resolving to bar 0 downbeat
- Circular buffer wrapping so tails resolve smoothly across loop seam
- Cosine S-curve zero-crossing boundary matching (sample[0] == sample[-1] and dL=0, dR=0)
"""

import math
import os
import wave
import numpy as np

SR = 44100

def midi_to_freq(m):
    return 440.0 * (2.0 ** ((m - 69.0) / 12.0))

def apply_zero_crossing_boundary(l_buf, r_buf, xfade_samples=512):
    taper = 0.5 * (1.0 - np.cos(np.linspace(0.0, np.pi, xfade_samples)))
    dL = l_buf[0] - l_buf[-1]
    dR = r_buf[0] - r_buf[-1]
    l_buf[-xfade_samples:] += dL * taper
    r_buf[-xfade_samples:] += dR * taper
    return l_buf, r_buf

def write_wav(path, l_data, r_data):
    peak = max(np.max(np.abs(l_data)), np.max(np.abs(r_data)))
    target_peak = 0.90 # -0.9 dBFS headroom
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
# 1. FLORALIS GENÉRICA: ROCK NACIONAL ARGENTINO ARCADE (125 BPM)
# ==============================================================================
def compose_floralis_rock():
    print("\n--- Composing Floralis Genérica: Rock Nacional Argentino Arcade ---")
    bpm = 125.0
    beat_dur = 60.0 / bpm  # 0.480s exact
    spb = int(round(SR * beat_dur))  # 21168 samples exact
    num_bars = 16
    total_samples = num_bars * spb * 4  # 1,354,752 samples = 30.720s exact

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

    # Generators:
    def rock_kick():
        dur = 0.28
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f_env = 140.0 * np.exp(-t * 26.0) + 48.0
        phase = 2.0 * np.pi * np.cumsum(f_env) / SR
        body = np.sin(phase)
        click = np.random.uniform(-1, 1, n).astype(np.float32) * np.exp(-t * 120.0) * 0.4
        env = np.exp(-t * 12.0)
        s = (body + click) * env
        return (np.tanh(s * 1.5) * 0.90).astype(np.float32)

    def rock_snare(vel=1.0):
        dur = 0.32
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        tone = (np.sin(2.0 * np.pi * 185.0 * t) + 0.6 * np.sin(2.0 * np.pi * 320.0 * t)) * np.exp(-t * 22.0)
        noise = np.random.uniform(-1, 1, n).astype(np.float32) * np.exp(-t * 16.0)
        s = (tone * 0.55 + noise * 0.75) * vel
        return (np.tanh(s * 1.4) * 0.75).astype(np.float32)

    def rock_tom(pitch=130.0, vel=0.85):
        dur = 0.35
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f_env = pitch * 1.5 * np.exp(-t * 24.0) + pitch
        phase = 2.0 * np.pi * np.cumsum(f_env) / SR
        tone = np.sin(phase) * np.exp(-t * 10.0)
        return (np.tanh(tone * 1.3) * vel * 0.7).astype(np.float32)

    def rock_hihat(open_hh=False):
        dur = 0.26 if open_hh else 0.06
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        noise = np.random.uniform(-1, 1, n).astype(np.float32)
        decay = 12.0 if open_hh else 55.0
        env = np.exp(-t * decay)
        hpf = noise - np.roll(noise, 1) * 0.8
        return (hpf * env * (0.35 if open_hh else 0.22)).astype(np.float32)

    def rock_crash():
        dur = 1.4
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        noise = np.random.uniform(-1, 1, n).astype(np.float32)
        hpf = noise - np.roll(noise, 1) * 0.75
        env = np.exp(-t * 3.5)
        return (hpf * env * 0.40).astype(np.float32)

    def rock_bass(midi_p, dur, vel=0.85):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_p)
        tone = (np.sin(2.0 * np.pi * f0 * t) +
                0.7 * np.sin(2.0 * np.pi * 2.0 * f0 * t) +
                0.35 * np.sin(2.0 * np.pi * 3.0 * f0 * t))
        att = min(int(round(0.005 * SR)), n // 2)
        env = np.ones(n, dtype=np.float32)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.7 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel) ** 1.8
        return (np.tanh(tone * 1.6) * env * vel * 0.65).astype(np.float32)

    def electric_guitar_power(root_midi, dur, vel=0.9, chug=False):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        # Root + Fifth + Octave
        notes = [root_midi, root_midi + 7, root_midi + 12]
        s = np.zeros(n, dtype=np.float32)
        for p in notes:
            f0 = midi_to_freq(p)
            for detune in [-0.8, 0.8]:
                saw = 2.0 * (np.fmod((f0 + detune) * t, 1.0)) - 1.0
                s += saw * 0.35
        # Overdrive / Tube saturation
        drive = 3.5 if not chug else 2.2
        dist = np.tanh(s * drive)
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.004 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        decay = 14.0 if chug else (3.2 / dur)
        env *= np.exp(-t * decay)
        return (dist * env * vel * 0.45).astype(np.float32)

    def lead_guitar_solo(midi_p, dur, vel=0.9, vib=True):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_p)
        vibrato = 1.0 + (0.012 * np.sin(2.0 * np.pi * 5.6 * t) * (1.0 - np.exp(-t * 4.0)) if vib and dur > 0.2 else 0.0)
        # Saturated lead tone (harmonics 1..6)
        s = np.zeros(n, dtype=np.float32)
        for h, w in [(1, 1.0), (2, 0.8), (3, 0.5), (4, 0.3), (5, 0.2)]:
            s += w * np.sin(2.0 * np.pi * f0 * h * t * vibrato)
        dist = np.tanh(s * 4.0) + 0.3 * np.sin(2.0 * np.pi * f0 * 2.0 * t * vibrato)
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.012 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.75 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel) ** 1.5
        return (np.tanh(dist * 1.5) * env * vel * 0.48).astype(np.float32)

    def rock_organ(midi_p, dur, vel=0.6):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_p)
        s = (np.sin(2.0 * np.pi * f0 * t) +
             0.8 * np.sin(2.0 * np.pi * 2.0 * f0 * t) +
             0.5 * np.sin(2.0 * np.pi * 3.0 * f0 * t) +
             0.4 * np.sin(2.0 * np.pi * 4.0 * f0 * t))
        leslie = 1.0 + 0.15 * np.sin(2.0 * np.pi * 6.0 * t)
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.02 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.8 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel)
        return (s * leslie * env * vel * 0.28).astype(np.float32)

    # Structure: 16 bars (4 beats per bar = 64 beats)
    # Harmony:
    # Bars 0-3 (Intro Riff): Am (bar 0) - F (bar 1) - C (bar 2) - G (bar 3)
    # Bars 4-7 (Verse 1): Am (bar 4) - Em (bar 5) - F (bar 6) - G (bar 7)
    # Bars 8-11 (Chorus): C (bar 8) - G (bar 9) - Am (bar 10) - F (bar 11)
    # Bars 12-14 (Bridge): Dm (bar 12) - F (bar 13) - G (bar 14)
    # Bar 15 (Cadence & Drum Fill): E7 -> chromatic pickup into Am bar 0

    roots = [
        45, 41, 48, 43, # 0-3: A2, F2, C3, G2
        45, 40, 41, 43, # 4-7: A2, E2, F2, G2
        48, 43, 45, 41, # 8-11: C3, G2, A2, F2
        45, 41, 43, 40  # 12-15: D2(43)->F2(41)->G2(43)->E2(40)
    ]
    roots[12] = 38 # D2

    # 1. DRUMS TRACK
    for bar in range(num_bars):
        bar_start_beat = bar * 4
        # Crash on bar 0 and bar 8
        if bar in [0, 8]:
            cr = rock_crash()
            add_sound(cr, cr, int(round(bar_start_beat * spb)), pan=0.0)

        if bar == 15:
            # Bar 15 = Cadence & Energetic Drum Fill resolving into Bar 0
            # Kick on beats 0 and 1
            k = rock_kick()
            add_sound(k, k, int(round((bar_start_beat + 0) * spb)))
            add_sound(k, k, int(round((bar_start_beat + 1) * spb)))
            add_sound(rock_snare(0.8), rock_snare(0.8), int(round((bar_start_beat + 1) * spb)), pan=-0.1)

            # 16th note rolls on toms and snare on beats 2 and 3
            fill_subdivs = [
                (2.00, rock_snare(0.75), -0.2),
                (2.25, rock_snare(0.85), -0.1),
                (2.50, rock_tom(160.0, 0.85), 0.1),
                (2.75, rock_tom(140.0, 0.90), 0.2),
                (3.00, rock_tom(120.0, 0.95), 0.3),
                (3.25, rock_tom(100.0, 0.95), 0.35),
                (3.50, rock_snare(1.0), 0.0),
                (3.75, rock_snare(1.1), 0.0)
            ]
            for beat_off, snd, pan in fill_subdivs:
                add_sound(snd, snd, int(round((bar_start_beat + beat_off) * spb)), pan=pan)
        else:
            # Standard driving rock pattern
            # Kick on 1 and 2.5 (or 1, 2.5, 3)
            k = rock_kick()
            add_sound(k, k, int(round((bar_start_beat + 0) * spb)))
            add_sound(k, k, int(round((bar_start_beat + 2.5) * spb)))
            if bar in [2, 6, 10, 14]:
                add_sound(k, k, int(round((bar_start_beat + 1.5) * spb)))

            # Snare on 2 and 4
            sn = rock_snare(1.0)
            add_sound(sn, sn, int(round((bar_start_beat + 1) * spb)), pan=-0.05)
            add_sound(sn, sn, int(round((bar_start_beat + 3) * spb)), pan=-0.05)

            # Hi-hats 8th notes
            for b8 in range(8):
                is_open = (b8 == 7 and bar in [3, 7, 11])
                hh = rock_hihat(open_hh=is_open)
                add_sound(hh, hh, int(round((bar_start_beat + b8 * 0.5) * spb)), pan=0.25)

    # 2. BASS GUITAR
    for bar in range(num_bars):
        root = roots[bar]
        bar_start_beat = bar * 4
        if bar == 15:
            # Resolving bass line in bar 15: E2 -> F#2 -> G#2 -> chromatic pickup to A2
            bass_seq = [(0.0, 40, 0.45), (1.0, 40, 0.45), (2.0, 42, 0.45), (2.5, 44, 0.45),
                        (3.0, 43, 0.22), (3.5, 44, 0.22)]
            for beat_off, pitch, dur in bass_seq:
                bs = rock_bass(pitch, dur)
                add_sound(bs, bs, int(round((bar_start_beat + beat_off) * spb)), pan=-0.1)
        else:
            # Driving rock bass pattern (root 8th notes with occasional octave jump)
            for b8 in range(8):
                pitch = root if b8 not in [3, 7] else root + 12
                dur = 0.22
                bs = rock_bass(pitch, dur, vel=0.85 if b8 % 2 == 0 else 0.70)
                add_sound(bs, bs, int(round((bar_start_beat + b8 * 0.5) * spb)), pan=-0.1)

    # 3. RHYTHM ELECTRIC GUITARS (Stereo Double-Tracked)
    for bar in range(num_bars):
        root = roots[bar]
        bar_start_beat = bar * 4
        if bar == 15:
            # Bar 15: Power chords on E5 (beat 0 and 1) then crescendo stabs
            g_l = electric_guitar_power(40, 0.42, vel=0.95)
            g_r = electric_guitar_power(40, 0.42, vel=0.95)
            add_sound(g_l, g_r, int(round((bar_start_beat + 0) * spb)), pan=-0.65)
            add_sound(g_r, g_l, int(round((bar_start_beat + 0) * spb)), pan=0.65)

            add_sound(g_l, g_r, int(round((bar_start_beat + 1) * spb)), pan=-0.65)
            add_sound(g_r, g_l, int(round((bar_start_beat + 1) * spb)), pan=0.65)

            # Stabs on beats 2, 2.5, 3, 3.5 rising: E -> F -> F# -> G#
            stabs = [(2.0, 40), (2.5, 41), (3.0, 42), (3.5, 44)]
            for beat_off, p in stabs:
                gt = electric_guitar_power(p, 0.20, vel=1.0, chug=True)
                add_sound(gt, gt, int(round((bar_start_beat + beat_off) * spb)), pan=-0.6)
                add_sound(gt, gt, int(round((bar_start_beat + beat_off) * spb)), pan=0.6)
        else:
            # Dynamic rhythm guitar: Sustained power chord on 1, chugs on 2, 2.5, stab on 3, chug 3.5
            g_sus = electric_guitar_power(root, 0.85, vel=0.85)
            add_sound(g_sus, g_sus, int(round((bar_start_beat + 0) * spb)), pan=-0.7)
            add_sound(g_sus, g_sus, int(round((bar_start_beat + 0) * spb)), pan=0.7)

            for b_off in [1.5, 2.0, 2.5, 3.5]:
                g_chug = electric_guitar_power(root, 0.18, vel=0.75, chug=True)
                add_sound(g_chug, g_chug, int(round((bar_start_beat + b_off) * spb)), pan=-0.65)
                add_sound(g_chug, g_chug, int(round((bar_start_beat + b_off) * spb)), pan=0.65)

            g_stab = electric_guitar_power(root, 0.40, vel=0.9)
            add_sound(g_stab, g_stab, int(round((bar_start_beat + 3.0) * spb)), pan=-0.7)
            add_sound(g_stab, g_stab, int(round((bar_start_beat + 3.0) * spb)), pan=0.7)

    # 4. ROCK ORGAN (Hammond Chords)
    for bar in range(num_bars):
        root = roots[bar]
        bar_start_beat = bar * 4
        if bar != 15:
            # Organ pad holding chord
            chord_notes = [root + 12, root + 16 if bar in [3, 8, 9, 14] else root + 15, root + 19]
            for cn in chord_notes:
                org = rock_organ(cn, beat_dur * 3.8, vel=0.45)
                add_sound(org, org, int(round(bar_start_beat * spb)), pan=0.35)

    # 5. LEAD GUITAR: MEMORABLE ROCK NACIONAL MELODY
    # Bars 0-3: Intro guitar hook
    intro_melody = [
        # Bar 0 (over Am)
        (0.0, 69, 0.45), (0.5, 72, 0.45), (1.0, 76, 0.90), (2.0, 74, 0.45), (2.5, 72, 0.45), (3.0, 69, 0.90),
        # Bar 1 (over F)
        (4.0, 65, 0.45), (4.5, 69, 0.45), (5.0, 72, 0.90), (6.0, 71, 0.45), (6.5, 69, 0.45), (7.0, 65, 0.90),
        # Bar 2 (over C)
        (8.0, 67, 0.45), (8.5, 71, 0.45), (9.0, 72, 0.90), (10.0, 74, 0.45), (10.5, 76, 0.45), (11.0, 79, 0.90),
        # Bar 3 (over G)
        (12.0, 79, 0.45), (12.5, 76, 0.45), (13.0, 74, 0.90), (14.0, 72, 0.45), (14.5, 71, 0.45), (15.0, 69, 0.90),
    ]
    for beat_pos, pitch, dur in intro_melody:
        lead = lead_guitar_solo(pitch, dur, vel=0.88)
        add_sound(lead, lead, int(round(beat_pos * spb)), pan=0.15)

    # Bars 4-7: Verse vocal-style guitar phrasing
    verse_melody = [
        # Bar 4
        (16.0, 69, 0.40), (16.5, 69, 0.40), (17.0, 71, 0.40), (17.5, 72, 0.85), (19.0, 71, 0.40), (19.5, 69, 0.40),
        # Bar 5
        (20.0, 67, 0.90), (21.5, 64, 0.40), (22.0, 67, 0.85), (23.0, 69, 0.85),
        # Bar 6
        (24.0, 69, 0.40), (24.5, 69, 0.40), (25.0, 72, 0.40), (25.5, 74, 0.85), (27.0, 72, 0.40), (27.5, 71, 0.40),
        # Bar 7
        (28.0, 71, 0.90), (29.5, 67, 0.40), (30.0, 71, 0.85), (31.0, 72, 0.85),
    ]
    for beat_pos, pitch, dur in verse_melody:
        lead = lead_guitar_solo(pitch, dur, vel=0.82)
        add_sound(lead, lead, int(round(beat_pos * spb)), pan=-0.1)

    # Bars 8-11: Soaring Chorus Hook (Arcade Anthem)
    chorus_melody = [
        # Bar 8 (C)
        (32.0, 76, 0.90), (33.0, 76, 0.45), (33.5, 79, 0.45), (34.0, 81, 0.90), (35.0, 79, 0.90),
        # Bar 9 (G)
        (36.0, 74, 0.90), (37.0, 74, 0.45), (37.5, 76, 0.45), (38.0, 77, 0.90), (39.0, 76, 0.90),
        # Bar 10 (Am)
        (40.0, 72, 0.90), (41.0, 72, 0.45), (41.5, 74, 0.45), (42.0, 76, 0.90), (43.0, 74, 0.90),
        # Bar 11 (F)
        (44.0, 69, 1.30), (46.0, 72, 0.45), (46.5, 74, 0.45), (47.0, 76, 0.90),
    ]
    for beat_pos, pitch, dur in chorus_melody:
        lead = lead_guitar_solo(pitch, dur, vel=0.95)
        add_sound(lead, lead, int(round(beat_pos * spb)), pan=0.2)

    # Bars 12-14: Solo Climax
    solo_climax = [
        # Bar 12 (Dm)
        (48.0, 77, 0.45), (48.5, 76, 0.45), (49.0, 74, 0.45), (49.5, 72, 0.45), (50.0, 74, 0.90), (51.0, 76, 0.90),
        # Bar 13 (F)
        (52.0, 77, 0.45), (52.5, 79, 0.45), (53.0, 81, 0.90), (54.0, 83, 0.45), (54.5, 84, 0.45), (55.0, 86, 0.90),
        # Bar 14 (G)
        (56.0, 86, 0.45), (56.5, 84, 0.45), (57.0, 83, 0.45), (57.5, 81, 0.45), (58.0, 79, 0.90), (59.0, 76, 0.90),
    ]
    for beat_pos, pitch, dur in solo_climax:
        lead = lead_guitar_solo(pitch, dur, vel=0.95)
        add_sound(lead, lead, int(round(beat_pos * spb)), pan=0.1)

    # Bar 15: Cadence pickup phrase (B4 -> C5 -> D5 -> E5 -> G#5) resolving right into A5 (downbeat of Bar 0)
    pickup_phrase = [
        (60.0, 71, 0.40), # B4
        (60.5, 72, 0.40), # C5
        (61.0, 74, 0.40), # D5
        (61.5, 76, 0.40), # E5
        (62.0, 77, 0.22), # F5
        (62.5, 79, 0.22), # G5
        (63.0, 80, 0.22), # G#5
        (63.5, 81, 0.45), # A5 (high screaming lead-in directly resolving onto the bar 0 A4/A5 intro riff)
    ]
    for beat_pos, pitch, dur in pickup_phrase:
        lead = lead_guitar_solo(pitch, dur, vel=1.0)
        add_sound(lead, lead, int(round(beat_pos * spb)), pan=0.15)

    out_path = "Assets/Audio/Music/bgm_floralis_rock.wav"
    write_wav(out_path, l_master, r_master)


# ==============================================================================
# 2. PUENTE DE LA MUJER: MÚSICA ELECTRÓNICA DINÁMICA Y URBANA ARCADE (126 BPM)
# ==============================================================================
def compose_puentemujer_electronic():
    print("\n--- Composing Puente de la Mujer: Música Electrónica Arcade ---")
    bpm = 126.0
    beat_dur = 60.0 / bpm  # 0.47619s
    spb = int(round(SR * beat_dur))  # 21000 samples exact
    num_bars = 16
    total_samples = num_bars * spb * 4  # 1,344,000 samples = 30.476s exact

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

    # Generators:
    def edm_kick():
        dur = 0.32
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f_env = 180.0 * np.exp(-t * 32.0) + 42.0
        phase = 2.0 * np.pi * np.cumsum(f_env) / SR
        body = np.sin(phase)
        click = np.random.uniform(-1, 1, n).astype(np.float32) * np.exp(-t * 200.0) * 0.5
        env = np.exp(-t * 10.0)
        s = (body + click) * env
        return (np.tanh(s * 1.8) * 0.95).astype(np.float32)

    def edm_clap():
        dur = 0.28
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        # 3 micro pre-claps then main burst
        noise = np.random.uniform(-1, 1, n).astype(np.float32)
        env = np.zeros(n, dtype=np.float32)
        p1 = int(round(0.010 * SR))
        p2 = int(round(0.022 * SR))
        p3 = int(round(0.035 * SR))
        if p1 < n: env[:p1] += np.exp(-t[:p1] * 80.0) * 0.5
        if p2 < n: env[p1:p2] += np.exp(-t[:p2-p1] * 80.0) * 0.7
        if p3 < n: env[p2:p3] += np.exp(-t[:p3-p2] * 80.0) * 0.9
        if p3 < n: env[p3:] += np.exp(-t[:n-p3] * 18.0) * 1.1
        hpf = noise - np.roll(noise, 1) * 0.8
        return (np.tanh(hpf * env * 1.6) * 0.70).astype(np.float32)

    def edm_hihat(open_hh=False):
        dur = 0.22 if open_hh else 0.05
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        noise = np.random.uniform(-1, 1, n).astype(np.float32)
        decay = 14.0 if open_hh else 65.0
        env = np.exp(-t * decay)
        hpf = noise - np.roll(noise, 1) * 0.85
        return (hpf * env * (0.35 if open_hh else 0.20)).astype(np.float32)

    def electro_bass(midi_p, dur, vel=0.9):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_p)
        # Saturated square + saw with filter bite
        saw = 2.0 * np.fmod(f0 * t, 1.0) - 1.0
        sqr = np.sign(np.sin(2.0 * np.pi * f0 * t))
        s = saw * 0.6 + sqr * 0.4
        # Resonant filter sweep
        flt_env = np.exp(-t * 20.0)
        s = s * (0.4 + 0.6 * flt_env)
        dist = np.tanh(s * 2.8)
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.005 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.8 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel) ** 2
        return (dist * env * vel * 0.65).astype(np.float32)

    def supersaw_chord(midi_notes, dur, vel=0.6):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        s = np.zeros(n, dtype=np.float32)
        for p in midi_notes:
            f0 = midi_to_freq(p)
            for detune in [-1.5, -0.6, 0.0, 0.6, 1.5]:
                saw = 2.0 * np.fmod((f0 + detune) * t, 1.0) - 1.0
                s += saw * 0.08
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.015 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.7 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel) ** 1.5
        return (np.tanh(s * 1.5) * env * vel * 0.40).astype(np.float32)

    def arp_synth(midi_p, dur=0.11, vel=0.5):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f0 = midi_to_freq(midi_p)
        pulse = np.sign(np.sin(2.0 * np.pi * f0 * t) - 0.2)
        env = np.exp(-t * 28.0)
        return (pulse * env * vel * 0.35).astype(np.float32)

    def edm_lead(midi_p, dur, vel=0.85, portamento_from=None):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f_target = midi_to_freq(midi_p)
        if portamento_from is not None:
            f_start = midi_to_freq(portamento_from)
            f_cur = f_start + (f_target - f_start) * (1.0 - np.exp(-t * 25.0))
        else:
            f_cur = np.full(n, f_target, dtype=np.float32)
        phase = 2.0 * np.pi * np.cumsum(f_cur) / SR
        # Rich stereo lead: PWM + Sync buzz
        s = np.sin(phase) + 0.6 * np.sin(2.0 * phase) + 0.35 * np.sin(3.0 * phase) + 0.25 * np.sin(4.0 * phase)
        vib = 1.0 + (0.010 * np.sin(2.0 * np.pi * 6.0 * t) if dur > 0.3 else 0.0)
        s = s * vib
        dist = np.tanh(s * 2.2)
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.010 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.8 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel) ** 1.8
        return (dist * env * vel * 0.50).astype(np.float32)

    def edm_riser(dur):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f_env = 200.0 * np.exp(t * 1.5)
        phase = 2.0 * np.pi * np.cumsum(f_env) / SR
        noise = np.random.uniform(-1, 1, n).astype(np.float32)
        sweep = (np.sin(phase) * 0.4 + noise * 0.6) * (t / dur) ** 2
        return (sweep * 0.35).astype(np.float32)

    # Structure: 16 bars (4 beats per bar = 64 beats)
    # Harmony: Key of F Minor / Ab Major
    # Bars 0-3: Fm (bar 0) - Db (bar 1) - Eb (bar 2) - C7 (bar 3)
    # Bars 4-7: Fm (bar 4) - Ab (bar 5) - Bbm (bar 6) - C7 (bar 7)
    # Bars 8-11: Drop / Climax: Fm (bar 8) - Db (bar 9) - Eb (bar 10) - Cm (bar 11)
    # Bars 12-14: Build: Db (bar 12) - Eb (bar 13) - C7 (bar 14)
    # Bar 15: Cadence & Glitch Riser / Drum Roll resolving straight into Fm beat 0

    roots = [
        41, 37, 39, 36, # Fm(41), Db(37), Eb(39), C(36)
        41, 44, 34, 36, # Fm(41), Ab(44), Bbm(34), C(36)
        41, 37, 39, 36, # Fm(41), Db(37), Eb(39), Cm(36)
        37, 39, 36, 36  # Db(37), Eb(39), C(36), C(36)
    ]

    # 1. DRUMS TRACK
    for bar in range(num_bars):
        bar_start_beat = bar * 4
        if bar == 15:
            # Bar 15 = Cadence & Glitch Electro Fill resolving into Bar 0
            # Kick on 0, 1
            k = edm_kick()
            add_sound(k, k, int(round((bar_start_beat + 0) * spb)))
            add_sound(k, k, int(round((bar_start_beat + 1) * spb)))

            # Rapid 16th/32nd note snare & clap build on beats 2 and 3
            cl = edm_clap()
            for b16 in range(8, 16):
                add_sound(cl * 0.85, cl * 0.85, int(round((bar_start_beat + b16 * 0.25) * spb)), pan=0.0)

            # Riser sweep in second half of bar 15
            riser = edm_riser(beat_dur * 2.0)
            add_sound(riser, riser, int(round((bar_start_beat + 2.0) * spb)), pan=0.0)
        else:
            # 4-on-the-floor kick
            k = edm_kick()
            for b in range(4):
                add_sound(k, k, int(round((bar_start_beat + b) * spb)))

            # Clap on 2 and 4 (beats 1 and 3)
            cl = edm_clap()
            add_sound(cl, cl, int(round((bar_start_beat + 1) * spb)), pan=0.0)
            add_sound(cl, cl, int(round((bar_start_beat + 3) * spb)), pan=0.0)

            # Open Hi-Hat on upbeats (0.5, 1.5, 2.5, 3.5)
            ohh = edm_hihat(open_hh=True)
            for b in range(4):
                add_sound(ohh, ohh, int(round((bar_start_beat + b + 0.5) * spb)), pan=0.25)

            # 16th closed hi-hats
            chh = edm_hihat(open_hh=False)
            for b16 in range(16):
                if b16 % 4 != 2: # Don't clash with open hh
                    add_sound(chh, chh, int(round((bar_start_beat + b16 * 0.25) * spb)), pan=-0.2)

    # 2. SYNTH BASS (Syncopated 16th note electro bassline with sidechain feel)
    for bar in range(num_bars):
        root = roots[bar]
        bar_start_beat = bar * 4
        if bar == 15:
            # Bar 15: Bass walking up from C -> Db -> D -> Eb -> E -> F (root)
            fill_bass = [
                (0.0, 36, 0.40), # C2
                (1.0, 36, 0.40), # C2
                (2.0, 37, 0.20), # Db2
                (2.5, 38, 0.20), # D2
                (3.0, 39, 0.20), # Eb2
                (3.5, 40, 0.20), # E2 (leading tone resolving into Fm 41 in bar 0)
            ]
            for beat_off, p, dur in fill_bass:
                bs = electro_bass(p, dur, vel=0.95)
                add_sound(bs, bs, int(round((bar_start_beat + beat_off) * spb)), pan=0.0)
        else:
            # Driving syncopated 16th electro groove:
            # Offbeat hits ducking the kick (on 0.5, 0.75, 1.5, 1.75, 2.5, 2.75, 3.5, 3.75)
            pattern = [0.5, 0.75, 1.5, 2.25, 2.5, 2.75, 3.5]
            for off in pattern:
                p = root if off < 2.5 else (root + 12 if off == 2.75 else root + 7)
                bs = electro_bass(p, 0.18, vel=0.85)
                add_sound(bs, bs, int(round((bar_start_beat + off) * spb)), pan=0.0)

    # 3. 16th-NOTE ARPEGGIATOR (Runs throughout to give high-energy pulse)
    for bar in range(num_bars):
        root = roots[bar]
        bar_start_beat = bar * 4
        # 16th arp notes over minor chord: root + 12, root + 15/16, root + 19, root + 24
        is_maj = bar in [1, 2, 5]
        chord_arp = [root + 12, root + (16 if is_maj else 15), root + 19, root + 24]
        for b16 in range(16):
            if bar == 15 and b16 >= 12: continue # leave space for riser and pickup
            note = chord_arp[b16 % 4]
            a = arp_synth(note, dur=0.10, vel=0.45)
            pan = 0.4 if (b16 % 2 == 0) else -0.4
            add_sound(a, a, int(round((bar_start_beat + b16 * 0.25) * spb)), pan=pan)

    # 4. SUPERSAW STABS (Bars 4-14)
    for bar in range(4, num_bars):
        if bar == 15: continue
        root = roots[bar]
        bar_start_beat = bar * 4
        is_maj = bar in [1, 2, 5, 9, 10, 12, 13]
        third = 16 if is_maj else 15
        chord = [root + 12, root + 12 + third, root + 12 + 19, root + 24]

        # Offbeat stabs on beat 1.5 and 3.5
        for b_off in [1.5, 3.5]:
            st = supersaw_chord(chord, beat_dur * 0.45, vel=0.55)
            add_sound(st, st, int(round((bar_start_beat + b_off) * spb)), pan=0.3)
            add_sound(st, st, int(round((bar_start_beat + b_off) * spb)), pan=-0.3)

    # 5. LEAD SYNTH: MEMORABLE URBAN ARCADE MELODY
    # Bars 0-3: Hook teaser / Intro lead
    intro_lead = [
        # Bar 0 (Fm)
        (0.0, 65, 0.45), (0.5, 68, 0.45), (1.0, 72, 0.90), (2.0, 70, 0.45), (2.5, 68, 0.45), (3.0, 65, 0.90),
        # Bar 1 (Db)
        (4.0, 68, 0.45), (4.5, 72, 0.45), (5.0, 75, 0.90), (6.0, 73, 0.45), (6.5, 72, 0.45), (7.0, 68, 0.90),
        # Bar 2 (Eb)
        (8.0, 70, 0.45), (8.5, 73, 0.45), (9.0, 75, 0.90), (10.0, 77, 0.45), (10.5, 75, 0.45), (11.0, 73, 0.90),
        # Bar 3 (C7)
        (12.0, 72, 0.90), (13.5, 71, 0.45), (14.0, 72, 0.85), (15.0, 68, 0.85),
    ]
    for beat_pos, pitch, dur in intro_lead:
        ld = edm_lead(pitch, dur, vel=0.82)
        add_sound(ld, ld, int(round(beat_pos * spb)), pan=0.15)

    # Bars 4-7: Driving Main Theme
    main_theme = [
        # Bar 4 (Fm)
        (16.0, 77, 0.45), (16.5, 77, 0.45), (17.0, 75, 0.45), (17.5, 72, 0.85), (19.0, 70, 0.45), (19.5, 72, 0.45),
        # Bar 5 (Ab)
        (20.0, 75, 0.90), (21.5, 72, 0.45), (22.0, 75, 0.85), (23.0, 77, 0.85),
        # Bar 6 (Bbm)
        (24.0, 78, 0.45), (24.5, 78, 0.45), (25.0, 77, 0.45), (25.5, 75, 0.85), (27.0, 73, 0.45), (27.5, 75, 0.45),
        # Bar 7 (C7)
        (28.0, 76, 0.90), (29.5, 72, 0.45), (30.0, 76, 0.85), (31.0, 77, 0.85),
    ]
    for beat_pos, pitch, dur in main_theme:
        ld = edm_lead(pitch, dur, vel=0.88)
        add_sound(ld, ld, int(round(beat_pos * spb)), pan=-0.1)

    # Bars 8-11: High-Octane Climax Melody
    drop_melody = [
        # Bar 8 (Fm)
        (32.0, 84, 0.90), (33.0, 82, 0.45), (33.5, 80, 0.45), (34.0, 77, 0.90), (35.0, 75, 0.90),
        # Bar 9 (Db)
        (36.0, 80, 0.90), (37.0, 82, 0.45), (37.5, 84, 0.45), (38.0, 85, 0.90), (39.0, 84, 0.90),
        # Bar 10 (Eb)
        (40.0, 87, 0.90), (41.0, 85, 0.45), (41.5, 84, 0.45), (42.0, 82, 0.90), (43.0, 80, 0.90),
        # Bar 11 (Cm)
        (44.0, 79, 1.20), (45.5, 77, 0.45), (46.0, 75, 0.45), (47.0, 72, 0.90),
    ]
    for beat_pos, pitch, dur in drop_melody:
        ld = edm_lead(pitch, dur, vel=0.95)
        add_sound(ld, ld, int(round(beat_pos * spb)), pan=0.2)

    # Bars 12-14: Rising tension
    build_melody = [
        # Bar 12 (Db)
        (48.0, 73, 0.45), (48.5, 75, 0.45), (49.0, 77, 0.45), (49.5, 80, 0.45), (50.0, 82, 0.90), (51.0, 84, 0.90),
        # Bar 13 (Eb)
        (52.0, 85, 0.45), (52.5, 84, 0.45), (53.0, 82, 0.45), (53.5, 80, 0.45), (54.0, 82, 0.90), (55.0, 85, 0.90),
        # Bar 14 (C7)
        (56.0, 84, 0.45), (56.5, 82, 0.45), (57.0, 80, 0.45), (57.5, 79, 0.45), (58.0, 76, 0.90), (59.0, 72, 0.90),
    ]
    for beat_pos, pitch, dur in build_melody:
        ld = edm_lead(pitch, dur, vel=0.92)
        add_sound(ld, ld, int(round(beat_pos * spb)), pan=0.1)

    # Bar 15: Rapid ascending cyber-synth pickup into Fm downbeat of Bar 0
    pickup_lead = [
        (60.0, 72, 0.40), # C5
        (60.5, 73, 0.40), # Db5
        (61.0, 75, 0.40), # Eb5
        (61.5, 76, 0.40), # E5
        (62.0, 77, 0.22), # F5
        (62.5, 80, 0.22), # Ab5
        (63.0, 82, 0.22), # Bb5
        (63.5, 84, 0.45), # C6 (soaring lead-in resolving straight into F5/Fm of Bar 0)
    ]
    for beat_pos, pitch, dur in pickup_lead:
        ld = edm_lead(pitch, dur, vel=1.0)
        add_sound(ld, ld, int(round(beat_pos * spb)), pan=0.15)

    out_path = "Assets/Audio/Music/bgm_puentemujer_electronic.wav"
    write_wav(out_path, l_master, r_master)


if __name__ == "__main__":
    compose_floralis_rock()
    compose_puentemujer_electronic()
    print("\n--- All new stage music generated successfully ---")
