"""
Synthesis script for Fightyub Menu BGM:
"Arcade Selection / Battle Preparation" - Original 16-bit Arcade Fighting Menu Track
Tempo: 126.0 BPM (4/4 time), 16 bars = 64 beats = 1,344,000 samples = 30.476s exact @ 44.1 kHz.
Tonality: D minor / D Dorian.

Guarantees 100% mathematically and musically seamless loop:
- Exact integer samples per beat (21,000 samples exact)
- Circular buffer wrapping so drum and synth decays resolve naturally across loop seam
- Bar 15 cadence (A7 with ascending chromatic lead pickup) resolving into Bar 0 Dm downbeat
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


def compose_menu_arcade():
    print("\n--- Composing Fightyub Menu Arcade Theme ---")
    bpm = 126.0
    beat_dur = 60.0 / bpm  # 0.47619s
    spb = int(round(SR * beat_dur))  # 21,000 samples exact
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

    # 16-bit Arcade Instrument Generators
    def arcade_kick():
        dur = 0.30
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f_env = 160.0 * np.exp(-t * 30.0) + 48.0
        phase = 2.0 * np.pi * np.cumsum(f_env) / SR
        body = np.sin(phase)
        click = np.random.uniform(-1, 1, n).astype(np.float32) * np.exp(-t * 180.0) * 0.4
        env = np.exp(-t * 11.0)
        s = (body + click) * env
        return (np.tanh(s * 1.6) * 0.90).astype(np.float32)

    def arcade_snare(vel=1.0):
        dur = 0.30
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        tone = (np.sin(2.0 * np.pi * 210.0 * t) + 0.5 * np.sin(2.0 * np.pi * 330.0 * t)) * np.exp(-t * 24.0)
        noise = np.random.uniform(-1, 1, n).astype(np.float32) * np.exp(-t * 18.0)
        s = (tone * 0.50 + noise * 0.80) * vel
        return (np.tanh(s * 1.5) * 0.72).astype(np.float32)

    def arcade_tom(pitch=140.0, vel=0.85):
        dur = 0.32
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f_env = pitch * 1.6 * np.exp(-t * 25.0) + pitch
        phase = 2.0 * np.pi * np.cumsum(f_env) / SR
        tone = np.sin(phase) * np.exp(-t * 12.0)
        return (np.tanh(tone * 1.4) * vel * 0.65).astype(np.float32)

    def arcade_hihat(open_hh=False):
        dur = 0.20 if open_hh else 0.055
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        noise = np.random.uniform(-1, 1, n).astype(np.float32)
        decay = 16.0 if open_hh else 60.0
        env = np.exp(-t * decay)
        hpf = noise - np.roll(noise, 1) * 0.85
        return (hpf * env * (0.32 if open_hh else 0.18)).astype(np.float32)

    def arcade_crash():
        dur = 1.2
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        noise = np.random.uniform(-1, 1, n).astype(np.float32)
        hpf = noise - np.roll(noise, 1) * 0.75
        env = np.exp(-t * 4.0)
        return (hpf * env * 0.38).astype(np.float32)

    def fm_arcade_bass(midi_p, dur, vel=0.85):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        fc = midi_to_freq(midi_p)
        fm = fc * 2.0 # 2:1 ratio for snappy FM slap bass
        mod_env = np.exp(-t * 22.0)
        mod_idx = 3.5 * mod_env
        mod = np.sin(2.0 * np.pi * fm * t) * mod_idx
        car = np.sin(2.0 * np.pi * fc * t + mod)
        # Add subtle sub sine
        sub = np.sin(2.0 * np.pi * fc * t) * 0.4
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.004 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.75 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel) ** 1.8
        s = (car + sub) * env * vel * 0.70
        return np.tanh(s * 1.5).astype(np.float32)

    def arcade_brass_chord(midi_notes, dur=0.25, vel=0.7):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        s = np.zeros(n, dtype=np.float32)
        for p in midi_notes:
            f0 = midi_to_freq(p)
            for detune in [-0.8, 0.8]:
                saw = 2.0 * (np.fmod((f0 + detune) * t, 1.0)) - 1.0
                s += saw * 0.15
        # Low-pass filter envelope opening
        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.012 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.7 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel) ** 1.6
        return (np.tanh(s * 2.0) * env * vel * 0.45).astype(np.float32)

    def arcade_bell_arp(midi_p, dur=0.12, vel=0.45):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        fc = midi_to_freq(midi_p)
        fm = fc * 3.5 # FM bell ratio
        mod = np.sin(2.0 * np.pi * fm * t) * 2.0 * np.exp(-t * 28.0)
        car = np.sin(2.0 * np.pi * fc * t + mod)
        env = np.exp(-t * 18.0)
        return (car * env * vel * 0.32).astype(np.float32)

    def arcade_lead_synth(midi_p, dur, vel=0.85, portamento_from=None):
        n = int(round(dur * SR))
        t = np.arange(n, dtype=np.float32) / SR
        f_target = midi_to_freq(midi_p)
        if portamento_from is not None:
            f_start = midi_to_freq(portamento_from)
            f_cur = f_start + (f_target - f_start) * (1.0 - np.exp(-t * 26.0))
        else:
            f_cur = np.full(n, f_target, dtype=np.float32)
        phase = 2.0 * np.pi * np.cumsum(f_cur) / SR

        # 16-bit Arcade Square + Pulse with subtle vibrato
        vib = 1.0 + (0.010 * np.sin(2.0 * np.pi * 5.8 * t) * (1.0 - np.exp(-t * 3.5)) if dur > 0.28 else 0.0)
        sqr = np.sign(np.sin(phase * vib))
        saw = 2.0 * (np.fmod(phase * vib / (2.0 * np.pi), 1.0)) - 1.0
        s = sqr * 0.55 + saw * 0.45
        dist = np.tanh(s * 1.8)

        env = np.ones(n, dtype=np.float32)
        att = min(int(round(0.008 * SR)), n // 2)
        if att > 0: env[:att] = np.linspace(0, 1, att)
        rel = int(round(dur * 0.8 * SR))
        if rel < n: env[rel:] *= np.linspace(1, 0, n - rel) ** 1.8
        return (dist * env * vel * 0.46).astype(np.float32)

    # Harmonic Progression: D minor / D Dorian
    # Bars 0-3: Dm (38) - Bb (34) - C (36) - Dm (38) [Main Theme Riff]
    # Bars 4-7: Dm (38) - F (41) - G (43) - A7 (45) [Theme Continuation]
    # Bars 8-11: Bb (34) - C (36) - Dm (38) - Am (45) [Heroic Hook / Peak Selection]
    # Bars 12-14: Gm (43) - Bb (34) - C (36) [Tension Build]
    # Bar 15: A7 (45) Cadence & Ascending Arpeggio into Bar 0 Dm

    roots = [
        38, 34, 36, 38, # 0-3: D2, Bb1, C2, D2
        38, 41, 43, 45, # 4-7: D2, F2, G2, A2
        34, 36, 38, 45, # 8-11: Bb1, C2, D2, A2
        43, 34, 36, 45  # 12-15: G2, Bb1, C2, A2
    ]

    # 1. DRUMS TRACK (Punchy 16-bit arcade rhythm)
    for bar in range(num_bars):
        bar_start_beat = bar * 4
        # Crash on bar 0 and bar 8 downbeat
        if bar in [0, 8]:
            cr = arcade_crash()
            add_sound(cr, cr, int(round(bar_start_beat * spb)), pan=0.0)

        if bar == 15:
            # Bar 15 = Cadence & Drum Roll into Bar 0
            # Kick on 0, 1
            k = arcade_kick()
            add_sound(k, k, int(round((bar_start_beat + 0) * spb)))
            add_sound(k, k, int(round((bar_start_beat + 1) * spb)))

            # Snare hit on 1
            sn = arcade_snare(0.85)
            add_sound(sn, sn, int(round((bar_start_beat + 1) * spb)), pan=-0.1)

            # Rapid 16th-note tom & snare roll on beats 2 and 3
            fill_hits = [
                (2.00, arcade_snare(0.75), -0.2),
                (2.25, arcade_snare(0.80), -0.1),
                (2.50, arcade_tom(170.0, 0.85), 0.1),
                (2.75, arcade_tom(150.0, 0.85), 0.2),
                (3.00, arcade_tom(130.0, 0.90), 0.25),
                (3.25, arcade_tom(110.0, 0.95), 0.3),
                (3.50, arcade_snare(1.0), 0.0),
                (3.75, arcade_snare(1.1), 0.0)
            ]
            for beat_off, snd, pan in fill_hits:
                add_sound(snd, snd, int(round((bar_start_beat + beat_off) * spb)), pan=pan)
        else:
            # Driving arcade beat:
            # Kick on 0, 1.5, 2, 3.5 (or syncopated arcade pattern)
            k = arcade_kick()
            add_sound(k, k, int(round((bar_start_beat + 0.0) * spb)))
            add_sound(k, k, int(round((bar_start_beat + 1.5) * spb)))
            add_sound(k, k, int(round((bar_start_beat + 2.0) * spb)))
            if bar in [1, 3, 5, 7, 9, 11, 13]:
                add_sound(k, k, int(round((bar_start_beat + 3.0) * spb)))

            # Snare on 1 and 3 (beats 2 and 4 in music notation)
            sn = arcade_snare(0.95)
            add_sound(sn, sn, int(round((bar_start_beat + 1.0) * spb)), pan=-0.05)
            add_sound(sn, sn, int(round((bar_start_beat + 3.0) * spb)), pan=-0.05)

            # Hi-hat groove: 8th notes with open hat on upbeat 2.5 and 3.5 in odd bars
            for b8 in range(8):
                is_open = (b8 in [5, 7] and bar in [1, 3, 7, 11, 13])
                hh = arcade_hihat(open_hh=is_open)
                add_sound(hh, hh, int(round((bar_start_beat + b8 * 0.5) * spb)), pan=0.25)

    # 2. FM SLAP BASS (Punchy syncopated arcade bassline)
    for bar in range(num_bars):
        root = roots[bar]
        bar_start_beat = bar * 4
        if bar == 15:
            # Bar 15: Ascending bassline resolving to D2 (root)
            # A1 -> B1 -> C#2 -> D2 (bar 0)
            fill_bass = [
                (0.0, 33, 0.40), # A1
                (1.0, 33, 0.40), # A1
                (2.0, 35, 0.22), # B1
                (2.5, 37, 0.22), # C#2
                (3.0, 36, 0.22), # C2
                (3.5, 37, 0.22), # C#2 (leading tone directly into D2 in bar 0)
            ]
            for beat_off, p, dur in fill_bass:
                bs = fm_arcade_bass(p, dur, vel=0.95)
                add_sound(bs, bs, int(round((bar_start_beat + beat_off) * spb)), pan=-0.1)
        else:
            # Snappy arcade syncopation: root on 0, octave on 0.75, root on 1.5, fifth on 2.0, octave on 2.75, root on 3.5
            pattern = [
                (0.00, root, 0.22),
                (0.75, root + 12, 0.18),
                (1.50, root, 0.22),
                (2.00, root + 7, 0.22),
                (2.75, root + 12, 0.18),
                (3.50, root, 0.22)
            ]
            for beat_off, p, dur in pattern:
                bs = fm_arcade_bass(p, dur, vel=0.88)
                add_sound(bs, bs, int(round((bar_start_beat + beat_off) * spb)), pan=-0.1)

    # 3. SYNTH BRASS / CHORD STABS (Offbeat arcade punches)
    for bar in range(num_bars):
        if bar == 15: continue
        root = roots[bar]
        bar_start_beat = bar * 4
        # Minor vs Major stabs
        is_maj = bar in [1, 2, 5, 8, 9, 13]
        third = 16 if is_maj else 15
        chord = [root + 12, root + 12 + third, root + 12 + 19]

        # Stabs on beats 0.5, 2.5
        for b_off in [0.5, 2.5]:
            br = arcade_brass_chord(chord, beat_dur * 0.40, vel=0.60)
            add_sound(br, br, int(round((bar_start_beat + b_off) * spb)), pan=0.35)
            add_sound(br, br, int(round((bar_start_beat + b_off) * spb)), pan=-0.35)

    # 4. BELL ARPEGGIATOR (16th-note competitive pulse)
    for bar in range(num_bars):
        root = roots[bar]
        bar_start_beat = bar * 4
        is_maj = bar in [1, 2, 5, 8, 9, 13]
        arp_notes = [root + 24, root + 24 + (16 if is_maj else 15), root + 24 + 19, root + 36]
        for b16 in range(16):
            if bar == 15 and b16 >= 10: continue # leave space for cadence pickup
            note = arp_notes[b16 % 4]
            a = arcade_bell_arp(note, dur=0.10, vel=0.38)
            pan = 0.3 if (b16 % 2 == 0) else -0.3
            add_sound(a, a, int(round((bar_start_beat + b16 * 0.25) * spb)), pan=pan)

    # 5. ARCADE LEAD SYNTH (Memorable, catchy, competitive fighting melody)
    # Bars 0-3: Main Theme Hook (Battle Ready)
    intro_melody = [
        # Bar 0 (Dm)
        (0.0, 62, 0.45), (0.5, 65, 0.45), (1.0, 69, 0.90), (2.0, 67, 0.45), (2.5, 65, 0.45), (3.0, 62, 0.90),
        # Bar 1 (Bb)
        (4.0, 65, 0.45), (4.5, 69, 0.45), (5.0, 70, 0.90), (6.0, 69, 0.45), (6.5, 67, 0.45), (7.0, 65, 0.90),
        # Bar 2 (C)
        (8.0, 67, 0.45), (8.5, 70, 0.45), (9.0, 72, 0.90), (10.0, 74, 0.45), (10.5, 72, 0.45), (11.0, 69, 0.90),
        # Bar 3 (Dm)
        (12.0, 69, 0.45), (12.5, 67, 0.45), (13.0, 65, 0.90), (14.0, 64, 0.45), (14.5, 62, 0.45), (15.0, 62, 0.90),
    ]
    for beat_pos, pitch, dur in intro_melody:
        lead = arcade_lead_synth(pitch, dur, vel=0.82)
        add_sound(lead, lead, int(round(beat_pos * spb)), pan=0.12)

    # Bars 4-7: Second Phrase (Rising anticipation)
    phrase2_melody = [
        # Bar 4 (Dm)
        (16.0, 62, 0.45), (16.5, 65, 0.45), (17.0, 69, 0.90), (18.0, 72, 0.45), (18.5, 69, 0.45), (19.0, 65, 0.90),
        # Bar 5 (F)
        (20.0, 69, 0.45), (20.5, 72, 0.45), (21.0, 74, 0.90), (22.0, 72, 0.45), (22.5, 69, 0.45), (23.0, 67, 0.90),
        # Bar 6 (G)
        (24.0, 71, 0.45), (24.5, 74, 0.45), (25.0, 76, 0.90), (26.0, 74, 0.45), (26.5, 71, 0.45), (27.0, 69, 0.90),
        # Bar 7 (A7)
        (28.0, 69, 0.90), (29.5, 67, 0.45), (30.0, 69, 0.85), (31.0, 70, 0.85),
    ]
    for beat_pos, pitch, dur in phrase2_melody:
        lead = arcade_lead_synth(pitch, dur, vel=0.85)
        add_sound(lead, lead, int(round(beat_pos * spb)), pan=-0.1)

    # Bars 8-11: Heroic Climax (Character / Stage Selection Excitement)
    heroic_melody = [
        # Bar 8 (Bb)
        (32.0, 70, 0.90), (33.0, 72, 0.45), (33.5, 74, 0.45), (34.0, 77, 0.90), (35.0, 74, 0.90),
        # Bar 9 (C)
        (36.0, 72, 0.90), (37.0, 74, 0.45), (37.5, 76, 0.45), (38.0, 79, 0.90), (39.0, 76, 0.90),
        # Bar 10 (Dm)
        (40.0, 74, 0.90), (41.0, 76, 0.45), (41.5, 77, 0.45), (42.0, 81, 0.90), (43.0, 77, 0.90),
        # Bar 11 (Am)
        (44.0, 76, 1.20), (45.5, 74, 0.45), (46.0, 72, 0.45), (47.0, 69, 0.90),
    ]
    for beat_pos, pitch, dur in heroic_melody:
        lead = arcade_lead_synth(pitch, dur, vel=0.92)
        add_sound(lead, lead, int(round(beat_pos * spb)), pan=0.18)

    # Bars 12-14: Intense Pre-Fight Build
    build_melody = [
        # Bar 12 (Gm)
        (48.0, 67, 0.45), (48.5, 70, 0.45), (49.0, 74, 0.45), (49.5, 72, 0.45), (50.0, 70, 0.90), (51.0, 72, 0.90),
        # Bar 13 (Bb)
        (52.0, 70, 0.45), (52.5, 74, 0.45), (53.0, 77, 0.90), (54.0, 76, 0.45), (54.5, 74, 0.45), (55.0, 72, 0.90),
        # Bar 14 (C)
        (56.0, 72, 0.45), (56.5, 76, 0.45), (57.0, 79, 0.90), (58.0, 77, 0.45), (58.5, 76, 0.45), (59.0, 74, 0.90),
    ]
    for beat_pos, pitch, dur in build_melody:
        lead = arcade_lead_synth(pitch, dur, vel=0.90)
        add_sound(lead, lead, int(round(beat_pos * spb)), pan=0.1)

    # Bar 15: Ascending Cadence Pickup (A4 -> C5 -> D5 -> E5 -> F5 -> G5 -> G#5 -> A5)
    # Directly and triumphantly resolving into D4 / D5 of Bar 0
    pickup_phrase = [
        (60.0, 69, 0.40), # A4
        (60.5, 72, 0.40), # C5
        (61.0, 74, 0.40), # D5
        (61.5, 76, 0.40), # E5
        (62.0, 77, 0.22), # F5
        (62.5, 79, 0.22), # G5
        (63.0, 80, 0.22), # G#5
        (63.5, 81, 0.45), # A5 (high lead-in resolving straight into D5 / Bar 0)
    ]
    for beat_pos, pitch, dur in pickup_phrase:
        lead = arcade_lead_synth(pitch, dur, vel=1.0)
        add_sound(lead, lead, int(round(beat_pos * spb)), pan=0.15)

    out_path = "Assets/Audio/Music/bgm_menu_arcade.wav"
    write_wav(out_path, l_master, r_master)


if __name__ == "__main__":
    compose_menu_arcade()
