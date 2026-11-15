import numpy as np
import wave
import struct
import math
import os

SR = 44100
BPM = 135.0
BEAT_DUR = 60.0 / BPM
BAR_DUR = BEAT_DUR * 4.0
NUM_BARS = 36
TOTAL_DUR = BAR_DUR * NUM_BARS
TOTAL_SAMPLES = int(round(TOTAL_DUR * SR)) # Exactly 2,822,400 samples (64.000s)
EXTRA_TAIL = int(round(3.5 * SR))
BUFFER_SAMPLES = TOTAL_SAMPLES + EXTRA_TAIL

print(f"Generating Progressive Metal BGM for Triunvirato y Olazábal:")
print(f"BPM: {BPM}, Duration: {TOTAL_DUR:.3f}s, Sample Rate: {SR}, Total Samples: {TOTAL_SAMPLES}")

left_buf = np.zeros(BUFFER_SAMPLES, dtype=np.float32)
right_buf = np.zeros(BUFFER_SAMPLES, dtype=np.float32)

def midi_to_freq(m):
    return 440.0 * (2.0 ** ((m - 69.0) / 12.0))

# -------------------------------------------------------------
# Distortion & Guitar Cabinet Emulation
# -------------------------------------------------------------
def cab_filter(sig):
    # Emulate 4x12 guitar cabinet: boost 100Hz (thump), peak at 2.8kHz (presence), cut above 5.5kHz
    n = len(sig)
    t = np.arange(n) / SR
    # Simple multi-pole filter via difference and smoothing
    # Low-pass filter (cut fizz above 5.5 kHz)
    alpha = 0.45
    lpf = np.empty_like(sig)
    val = 0.0
    for i in range(n):
        val += alpha * (sig[i] - val)
        lpf[i] = val
    # High-pass filter (remove sub-rumble below 75 Hz)
    alpha_hp = 0.012
    hpf = np.empty_like(lpf)
    val_hp = 0.0
    for i in range(n):
        val_hp += alpha_hp * (lpf[i] - val_hp)
        hpf[i] = lpf[i] - val_hp
    return hpf

# -------------------------------------------------------------
# Distorted Rhythm Guitar (Drop-D / E-Minor Heavy Riffs)
# -------------------------------------------------------------
def add_rhythm_guitar(start_time, dur, root_midi, is_power_chord=True, palm_mute=False, vel=0.90):
    start_idx = int(round(start_time * SR))
    if start_idx >= BUFFER_SAMPLES: return
    
    decay_dur = 0.16 if palm_mute else min(1.6, dur + 0.35)
    n_samples = int(round(decay_dur * SR))
    if start_idx + n_samples > BUFFER_SAMPLES:
        n_samples = BUFFER_SAMPLES - start_idx
    if n_samples <= 0: return

    t = np.arange(n_samples, dtype=np.float32) / SR
    f0 = midi_to_freq(root_midi)
    
    # Generate notes: root, fifth (+7 semitones), octave (+12 semitones)
    freqs = [f0]
    if is_power_chord:
        freqs.append(f0 * (2.0 ** (7.0 / 12.0)))
        freqs.append(f0 * 2.0)

    # Render two distinct takes for stereo double-tracking (L & R wall of sound)
    for channel in ['left', 'right']:
        raw_sig = np.zeros(n_samples, dtype=np.float32)
        detune_offset = 0.4 if channel == 'left' else -0.4
        
        for f in freqs:
            f_actual = f + detune_offset
            # Saw + pulse wave harmonics
            for h in range(1, 10):
                fh = f_actual * h
                if fh > 14000: break
                raw_sig += (1.0 / (h ** 0.85)) * np.sin(2.0 * np.pi * fh * t + (h * 0.3))

        # Pick attack transient (fret click)
        pick = np.sin(2.0 * np.pi * 1800.0 * t[:min(350, n_samples)]) * np.exp(-t[:min(350, n_samples)] * 110.0)
        raw_sig[:len(pick)] += pick * 0.85

        # Envelope
        damping = 22.0 if palm_mute else (4.2 if dur > 0.4 else 9.0)
        env = np.exp(-t * damping)
        if not palm_mute and dur > 0.3:
            env = np.clip(env * 1.3, 0.0, 1.0)
            # Smooth release at note end
            end_s = int(round(dur * SR))
            if end_s < n_samples:
                rel_len = n_samples - end_s
                env[end_s:] *= np.cos(np.linspace(0, np.pi/2, rel_len)) ** 2

        # Heavy Guitar Distortion (High-Gain Amp Saturation)
        driven = raw_sig * env * 9.0 * vel
        distorted = np.tanh(driven) + 0.12 * np.tanh(driven * 2.5)
        
        # Speaker Cabinet Filtering
        processed = cab_filter(distorted) * 0.22 * vel

        if channel == 'left':
            left_buf[start_idx:start_idx + n_samples] += processed * 0.85
            right_buf[start_idx:start_idx + n_samples] += processed * 0.25
        else:
            left_buf[start_idx:start_idx + n_samples] += processed * 0.25
            right_buf[start_idx:start_idx + n_samples] += processed * 0.85

# -------------------------------------------------------------
# Melodic Lead Guitar (Heroic Arcade Solo & Theme)
# -------------------------------------------------------------
def add_lead_guitar(start_time, dur, midi_pitch, vel=0.92, staccato=False, pan=0.0):
    start_idx = int(round(start_time * SR))
    if start_idx >= BUFFER_SAMPLES: return
    
    note_dur = dur * 0.55 if staccato else dur * 0.95
    total_dur = note_dur + (0.08 if staccato else 0.28)
    n_samples = int(round(total_dur * SR))
    if start_idx + n_samples > BUFFER_SAMPLES:
        n_samples = BUFFER_SAMPLES - start_idx
    if n_samples <= 0: return

    t = np.arange(n_samples, dtype=np.float32) / SR
    base_f = midi_to_freq(midi_pitch)

    # Expressive guitar vibrato
    if not staccato and dur > 0.3:
        vib_del = 0.10
        vib_env = np.clip((t - vib_del) / 0.20, 0.0, 1.0)
        vib = 1.0 + 0.009 * vib_env * np.sin(2.0 * np.pi * 5.8 * t)
    else:
        vib = 1.0

    sig = np.zeros(n_samples, dtype=np.float32)
    for h in range(1, 12):
        fh = base_f * h
        if fh > 15000: break
        sig += (1.0 / (h ** 0.9)) * np.sin(2.0 * np.pi * fh * t * vib + (h * 0.4))

    # Pick click
    click = np.sin(2.0 * np.pi * 2600.0 * t[:min(300, n_samples)]) * np.exp(-t[:min(300, n_samples)] * 90.0)
    sig[:len(click)] += click * 0.7

    # Attack & Sustain
    att_time = 0.012 if staccato else 0.025
    att_s = int(round(att_time * SR))
    env = np.ones(n_samples, dtype=np.float32)
    if att_s > 0:
        env[:att_s] = np.sin(np.linspace(0, np.pi/2, att_s)) ** 2
    sust_end = int(round(note_dur * SR))
    if sust_end < n_samples:
        rel_len = n_samples - sust_end
        env[sust_end:] = np.cos(np.linspace(0, np.pi/2, rel_len)) ** 2

    # Tube distortion & cabinet filter
    driven = sig * env * 7.5 * vel
    distorted = np.tanh(driven) * 0.26
    filtered = cab_filter(distorted)

    pan_l = math.cos((pan + 1.0) * math.pi / 4.0)
    pan_r = math.sin((pan + 1.0) * math.pi / 4.0)

    left_buf[start_idx:start_idx + n_samples] += filtered * pan_l
    right_buf[start_idx:start_idx + n_samples] += filtered * pan_r

# -------------------------------------------------------------
# Heavy Distorted Bass
# -------------------------------------------------------------
def add_prog_bass(start_time, dur, midi_pitch, vel=0.92, staccato=False):
    start_idx = int(round(start_time * SR))
    if start_idx >= BUFFER_SAMPLES: return

    decay_dur = 0.18 if staccato else min(1.5, dur + 0.3)
    n_samples = int(round(decay_dur * SR))
    if start_idx + n_samples > BUFFER_SAMPLES:
        n_samples = BUFFER_SAMPLES - start_idx
    if n_samples <= 0: return

    t = np.arange(n_samples, dtype=np.float32) / SR
    f0 = midi_to_freq(midi_pitch)

    # Sub fundamental + saturated saw harmonics
    sub = np.sin(2.0 * np.pi * f0 * t) * 1.2
    grit = np.zeros(n_samples, dtype=np.float32)
    for h in range(2, 8):
        grit += (0.8 / h) * np.sin(2.0 * np.pi * (f0 * h) * t)

    # Finger attack
    click = np.sin(2.0 * np.pi * 950.0 * t[:min(300, n_samples)]) * np.exp(-t[:min(300, n_samples)] * 70.0)
    
    raw = sub + np.tanh(grit * 2.5) * 0.6
    raw[:len(click)] += click * 0.4

    damping = 16.0 if staccato else 3.8
    env = np.exp(-t * damping)
    bass_sig = raw * env * (vel * 0.36)

    left_buf[start_idx:start_idx + n_samples] += bass_sig * 0.707
    right_buf[start_idx:start_idx + n_samples] += bass_sig * 0.707

# -------------------------------------------------------------
# Arcade FM Synth (Brass / Stabs)
# -------------------------------------------------------------
def add_fm_synth_chord(start_time, dur, midi_notes, vel=0.75, staccato=False, pan=0.0):
    start_idx = int(round(start_time * SR))
    if start_idx >= BUFFER_SAMPLES: return
    
    decay_dur = 0.22 if staccato else min(1.6, dur + 0.4)
    n_samples = int(round(decay_dur * SR))
    if start_idx + n_samples > BUFFER_SAMPLES:
        n_samples = BUFFER_SAMPLES - start_idx
    if n_samples <= 0: return

    t = np.arange(n_samples, dtype=np.float32) / SR
    chord_sig = np.zeros(n_samples, dtype=np.float32)

    for p in midi_notes:
        fc = midi_to_freq(p)
        fm = fc * 2.0 # 2:1 FM ratio for rich synth brass
        mod_index = 2.4 * np.exp(-t * (12.0 if staccato else 4.0))
        modulator = mod_index * np.sin(2.0 * np.pi * fm * t)
        note_sig = np.sin(2.0 * np.pi * fc * t + modulator)
        # Add slight sub-octave saw
        note_sig += 0.3 * np.sin(2.0 * np.pi * (fc * 0.5) * t)
        chord_sig += note_sig

    damping = 14.0 if staccato else (3.5 if dur > 0.4 else 8.0)
    env = np.exp(-t * damping)
    chord_sig *= env * (vel * 0.20 / max(1.0, math.sqrt(len(midi_notes))))

    pan_l = math.cos((pan + 1.0) * math.pi / 4.0)
    pan_r = math.sin((pan + 1.0) * math.pi / 4.0)
    left_buf[start_idx:start_idx + n_samples] += chord_sig * pan_l
    right_buf[start_idx:start_idx + n_samples] += chord_sig * pan_r

# -------------------------------------------------------------
# Progressive Metal Drums (Double Kick, Snare, Ride Bell, Crashes)
# -------------------------------------------------------------
def add_kick(start_time, vel=0.88):
    start_idx = int(round(start_time * SR))
    dur = 0.16
    n_samples = int(round(dur * SR))
    if start_idx + n_samples > BUFFER_SAMPLES: return
    t = np.arange(n_samples, dtype=np.float32) / SR
    f_t = 48.0 + 115.0 * np.exp(-t * 42.0)
    phase = np.cumsum(2.0 * np.pi * f_t / SR)
    sig = np.sin(phase) * np.exp(-t * 24.0)
    # Beater click
    click = np.sin(2.0 * np.pi * 3200.0 * t[:min(180, n_samples)]) * np.exp(-t[:min(180, n_samples)] * 85.0)
    sig[:len(click)] += click * 0.6
    sig = np.tanh(sig * 1.6) * (vel * 0.38)
    left_buf[start_idx:start_idx + n_samples] += sig * 0.707
    right_buf[start_idx:start_idx + n_samples] += sig * 0.707

def add_snare(start_time, vel=0.82, rimshot=False):
    start_idx = int(round(start_time * SR))
    dur = 0.12 if rimshot else 0.18
    n_samples = int(round(dur * SR))
    if start_idx + n_samples > BUFFER_SAMPLES: return
    t = np.arange(n_samples, dtype=np.float32) / SR
    noise = np.random.uniform(-1.0, 1.0, n_samples).astype(np.float32)
    noise_filt = noise - 0.70 * np.roll(noise, 1)
    noise_env = np.exp(-t * (42.0 if rimshot else 26.0))
    tone_f = 260.0 if rimshot else 195.0
    tone = np.sin(2.0 * np.pi * tone_f * t) * np.exp(-t * 30.0)
    sig = (noise_filt * noise_env * 0.75 + tone * 0.35) * (vel * 0.32)
    left_buf[start_idx:start_idx + n_samples] += sig * 0.707
    right_buf[start_idx:start_idx + n_samples] += sig * 0.707

def add_hihat_ride(start_time, vel=0.55, is_ride=False):
    start_idx = int(round(start_time * SR))
    dur = 0.25 if is_ride else 0.05
    n_samples = int(round(dur * SR))
    if start_idx + n_samples > BUFFER_SAMPLES: return
    t = np.arange(n_samples, dtype=np.float32) / SR
    if is_ride:
        # Metallic bell harmonics
        bell = (np.sin(2.0 * np.pi * 3120.0 * t) + 0.6 * np.sin(2.0 * np.pi * 4850.0 * t) + 0.4 * np.sin(2.0 * np.pi * 6920.0 * t))
        noise = np.random.uniform(-0.3, 0.3, n_samples).astype(np.float32)
        sig = (bell + noise) * np.exp(-t * 18.0) * (vel * 0.18)
    else:
        noise = np.random.uniform(-1.0, 1.0, n_samples).astype(np.float32)
        hh = noise - 0.82 * np.roll(noise, 1)
        sig = hh * np.exp(-t * 70.0) * (vel * 0.14)
    left_buf[start_idx:start_idx + n_samples] += sig * 0.55
    right_buf[start_idx:start_idx + n_samples] += sig * 0.85

def add_crash(start_time, vel=0.75):
    start_idx = int(round(start_time * SR))
    dur = 0.75
    n_samples = int(round(dur * SR))
    if start_idx + n_samples > BUFFER_SAMPLES: return
    t = np.arange(n_samples, dtype=np.float32) / SR
    noise = np.random.uniform(-1.0, 1.0, n_samples).astype(np.float32)
    crash = noise - 0.72 * np.roll(noise, 1)
    sig = crash * np.exp(-t * 8.5) * (vel * 0.28)
    left_buf[start_idx:start_idx + n_samples] += sig * 0.707
    right_buf[start_idx:start_idx + n_samples] += sig * 0.707

# -------------------------------------------------------------
# SCORE COMPOSITION (36 BARS = 64.000 SECONDS AT 135 BPM)
# -------------------------------------------------------------
# E minor: E1=28, E2=40, G2=43, A2=45, B2=47, C3=48, D3=50, E3=52, F#3=54, G3=55, A3=57, B3=59, C4=60, D4=62, E4=64

# SECTION A: "Intersección Furia" (Intro Riff: Bars 0 - 3)
# SECTION B: "El Asfalto de Urquiza" (Main Heavy Prog Theme: Bars 4 - 11)
# SECTION C: "Síncopa & Desvío" (Odd-time stabs & Dual Guitars: Bars 12 - 19)
# SECTION D: "Esquina Nocturna" (Guitar Solo & Synth Duel: Bars 20 - 27)
# SECTION E: "Gran Clímax Progresivo" (All-out Battle Climax & Cadenza Loop: Bars 28 - 35)

print("Arranging 36 bars of Progressive Metal score...")

for b in range(NUM_BARS):
    b_time = b * BAR_DUR

    # Crash on key sectional downbeats
    if b in [0, 4, 12, 20, 28]:
        add_crash(b_time, vel=0.88)

    # 1. RHYTHMIC DRUMS & PERCUSSION
    if b == 35: # Final measure cadenza / drum turnaround into bar 0
        # Aggressive syncopated fill
        add_kick(b_time, vel=0.95); add_snare(b_time, vel=0.85, rimshot=True)
        add_kick(b_time + BEAT_DUR * 0.5, vel=0.90)
        add_snare(b_time + BEAT_DUR, vel=0.90, rimshot=True)
        add_kick(b_time + BEAT_DUR * 1.5, vel=0.92)
        add_snare(b_time + BEAT_DUR * 2, vel=0.95, rimshot=True)
        add_kick(b_time + BEAT_DUR * 2.5, vel=0.95)
        # Quadruple snare roll into crash
        add_snare(b_time + BEAT_DUR * 3.00, vel=0.88, rimshot=True)
        add_snare(b_time + BEAT_DUR * 3.25, vel=0.92, rimshot=True)
        add_snare(b_time + BEAT_DUR * 3.50, vel=0.96, rimshot=True)
        add_snare(b_time + BEAT_DUR * 3.75, vel=1.00, rimshot=True)
        
    elif 12 <= b <= 19: # 3+3+2 Polyrhythmic syncopated groove
        # Kick on 0, 0.75, 1.5, 2.0, 3.0
        add_kick(b_time, vel=0.92)
        add_hihat_ride(b_time, vel=0.65, is_ride=True)
        add_kick(b_time + BEAT_DUR * 0.75, vel=0.88)
        add_snare(b_time + BEAT_DUR * 1.0, vel=0.82)
        add_hihat_ride(b_time + BEAT_DUR * 1.0, vel=0.60, is_ride=True)
        add_kick(b_time + BEAT_DUR * 1.5, vel=0.90)
        add_kick(b_time + BEAT_DUR * 2.0, vel=0.85)
        add_snare(b_time + BEAT_DUR * 2.0, vel=0.85, rimshot=True)
        add_hihat_ride(b_time + BEAT_DUR * 2.0, vel=0.65, is_ride=True)
        add_hihat_ride(b_time + BEAT_DUR * 2.5, vel=0.55, is_ride=True)
        add_snare(b_time + BEAT_DUR * 3.0, vel=0.88, rimshot=True)
        add_kick(b_time + BEAT_DUR * 3.5, vel=0.85)
        
    elif 28 <= b <= 34: # Full double-kick speed assault
        for q in range(8):
            add_kick(b_time + BEAT_DUR * (q * 0.5), vel=0.90 if q % 2 == 0 else 0.75)
        add_snare(b_time + BEAT_DUR * 1.0, vel=0.88, rimshot=True)
        add_snare(b_time + BEAT_DUR * 3.0, vel=0.92, rimshot=True)
        for h in range(4):
            add_hihat_ride(b_time + BEAT_DUR * h, vel=0.65, is_ride=True)
            
    else: # Driving arcade metal beat (Bars 0-11, 20-27)
        add_kick(b_time, vel=0.92)
        add_hihat_ride(b_time, vel=0.55)
        add_kick(b_time + BEAT_DUR * 0.5, vel=0.82)
        add_hihat_ride(b_time + BEAT_DUR * 0.5, vel=0.45)
        
        add_snare(b_time + BEAT_DUR * 1.0, vel=0.85, rimshot=True)
        add_hihat_ride(b_time + BEAT_DUR * 1.0, vel=0.55)
        add_hihat_ride(b_time + BEAT_DUR * 1.5, vel=0.45)
        
        add_kick(b_time + BEAT_DUR * 2.0, vel=0.88)
        add_kick(b_time + BEAT_DUR * 2.5, vel=0.85)
        add_hihat_ride(b_time + BEAT_DUR * 2.0, vel=0.55)
        add_hihat_ride(b_time + BEAT_DUR * 2.5, vel=0.45)
        
        add_snare(b_time + BEAT_DUR * 3.0, vel=0.90, rimshot=True)
        add_hihat_ride(b_time + BEAT_DUR * 3.0, vel=0.55)
        add_kick(b_time + BEAT_DUR * 3.5, vel=0.82)
        add_hihat_ride(b_time + BEAT_DUR * 3.5, vel=0.50)

    # 2. GUITAR & BASS RIFFS
    # Harmonic roots per bar
    if b in [0, 1, 2, 4, 5, 8, 9, 20, 21, 24, 25, 28, 29]: root = 40 # E2
    elif b in [3, 7, 11]: root = 47 # B2
    elif b in [6, 10]: root = 45 # A2
    elif b in [12, 16]: root = 48 # C3
    elif b in [13, 17]: root = 50 # D3
    elif b in [14, 18]: root = 47 # B2
    elif b in [15, 19]: root = 40 # E2
    elif b in [22, 26]: root = 43 # G2
    elif b in [23, 27]: root = 45 # A2
    elif b in [30]: root = 43 # G2
    elif b in [31]: root = 45 # A2
    elif b in [32]: root = 48 # C3
    elif b in [33]: root = 50 # D3
    elif b in [34]: root = 47 # B2
    elif b in [35]: root = 40 # E2 turnaround

    # Rhythm Guitar Riff Patterns
    if b == 35: # Grand Cadence into Measure 0
        # Chromatic descending palm-muted run leading straight into E2!
        # B2 -> Bb2 -> A2 -> G#2 -> G2 -> F2 -> F#2 -> E2
        chrom = [47, 46, 45, 44, 43, 41, 42]
        for idx, pitch in enumerate(chrom):
            t_note = b_time + (idx * 0.5 * BEAT_DUR)
            add_rhythm_guitar(t_note, BEAT_DUR * 0.45, pitch, is_power_chord=True, palm_mute=True, vel=0.95)
            add_prog_bass(t_note, BEAT_DUR * 0.45, pitch, vel=0.95, staccato=True)
            
    elif 12 <= b <= 19: # Progressive 3+3+2 Syncopated Chug & Open Accents
        # Beat 0: Heavy Open Power Chord
        add_rhythm_guitar(b_time, BEAT_DUR * 0.7, root, is_power_chord=True, palm_mute=False, vel=0.92)
        add_prog_bass(b_time, BEAT_DUR * 0.7, root, vel=0.95)
        # Beat 0.75: Palm-mute chug
        add_rhythm_guitar(b_time + BEAT_DUR * 0.75, BEAT_DUR * 0.22, root, is_power_chord=True, palm_mute=True, vel=0.88)
        # Beat 1.5: Palm-mute chug
        add_rhythm_guitar(b_time + BEAT_DUR * 1.5, BEAT_DUR * 0.45, root, is_power_chord=True, palm_mute=True, vel=0.90)
        add_prog_bass(b_time + BEAT_DUR * 1.5, BEAT_DUR * 0.45, root, vel=0.90)
        # Beat 2.0: Open punch
        add_rhythm_guitar(b_time + BEAT_DUR * 2.0, BEAT_DUR * 0.8, root + 2, is_power_chord=True, palm_mute=False, vel=0.92)
        add_prog_bass(b_time + BEAT_DUR * 2.0, BEAT_DUR * 0.8, root + 2, vel=0.92)
        # Beat 3.0: Staccato hit
        add_rhythm_guitar(b_time + BEAT_DUR * 3.0, BEAT_DUR * 0.4, root, is_power_chord=True, palm_mute=True, vel=0.90)
        add_prog_bass(b_time + BEAT_DUR * 3.0, BEAT_DUR * 0.4, root, vel=0.90)

    else: # Heavy Gallop & Djent Chug Riff (E minor)
        # Beat 0: Open heavy chord
        add_rhythm_guitar(b_time, BEAT_DUR * 0.45, root, is_power_chord=True, palm_mute=False, vel=0.95)
        add_prog_bass(b_time, BEAT_DUR * 0.45, root, vel=0.95)
        # Beat 0.5 & 0.75: Tight palm-muted sixteenth gallops
        add_rhythm_guitar(b_time + BEAT_DUR * 0.5, BEAT_DUR * 0.22, root, is_power_chord=True, palm_mute=True, vel=0.85)
        add_rhythm_guitar(b_time + BEAT_DUR * 0.75, BEAT_DUR * 0.22, root, is_power_chord=True, palm_mute=True, vel=0.88)
        # Beat 1.0: Palm-mute
        add_rhythm_guitar(b_time + BEAT_DUR * 1.0, BEAT_DUR * 0.45, root, is_power_chord=True, palm_mute=True, vel=0.88)
        add_prog_bass(b_time + BEAT_DUR * 1.0, BEAT_DUR * 0.45, root, vel=0.88)
        # Beat 1.5: Quick upper chromatic stab (F2 or G2)
        stab = root + (3 if b % 2 == 1 else 1)
        add_rhythm_guitar(b_time + BEAT_DUR * 1.5, BEAT_DUR * 0.4, stab, is_power_chord=True, palm_mute=False, vel=0.92)
        add_prog_bass(b_time + BEAT_DUR * 1.5, BEAT_DUR * 0.4, stab, vel=0.92)
        # Beat 2.0: Open chord
        add_rhythm_guitar(b_time + BEAT_DUR * 2.0, BEAT_DUR * 0.45, root, is_power_chord=True, palm_mute=False, vel=0.92)
        add_prog_bass(b_time + BEAT_DUR * 2.0, BEAT_DUR * 0.45, root, vel=0.92)
        # Beat 2.5 & 2.75: Gallop chugs
        add_rhythm_guitar(b_time + BEAT_DUR * 2.5, BEAT_DUR * 0.22, root, is_power_chord=True, palm_mute=True, vel=0.85)
        add_rhythm_guitar(b_time + BEAT_DUR * 2.75, BEAT_DUR * 0.22, root, is_power_chord=True, palm_mute=True, vel=0.88)
        # Beat 3.0: Heavy syncopated stab
        add_rhythm_guitar(b_time + BEAT_DUR * 3.0, BEAT_DUR * 0.45, root, is_power_chord=True, palm_mute=True, vel=0.90)
        add_prog_bass(b_time + BEAT_DUR * 3.0, BEAT_DUR * 0.45, root, vel=0.90)
        # Beat 3.5: Upbeat kick stab
        add_rhythm_guitar(b_time + BEAT_DUR * 3.5, BEAT_DUR * 0.45, root + 5, is_power_chord=True, palm_mute=False, vel=0.90)
        add_prog_bass(b_time + BEAT_DUR * 3.5, BEAT_DUR * 0.45, root + 5, vel=0.90)

# -------------------------------------------------------------
# 3. LEAD GUITAR & FM SYNTH THEMES
# -------------------------------------------------------------
lead_notes = []

# --- THEME 1: "El Asfalto de Urquiza" (Bars 4 - 11) ---
# Heroic arcade metal melody in E minor / Dorian
lead_notes += [
    # Bar 4: Main Anthemic Hook
    (16.00, 0.45, 64, 0.95, False), # E4
    (16.50, 0.22, 67, 0.90, False), # G4
    (16.75, 0.22, 71, 0.92, False), # B4
    (17.00, 0.65, 76, 1.00, False), # E5 (Heroic peak!)
    (17.75, 0.22, 74, 0.90, False), # D5
    # Bar 5
    (18.00, 0.45, 72, 0.95, False), # C5
    (18.50, 0.45, 71, 0.92, False), # B4
    (19.00, 0.45, 69, 0.90, False), # A4
    (19.50, 0.45, 67, 0.92, False), # G4
    # Bar 6
    (20.00, 0.45, 69, 0.95, False), # A4
    (20.50, 0.45, 72, 0.95, False), # C5
    (21.00, 0.65, 76, 1.00, False), # E5
    (21.75, 0.22, 78, 0.95, False), # F#5
    # Bar 7
    (22.00, 0.45, 79, 1.00, False), # G5
    (22.50, 0.45, 78, 0.95, False), # F#5
    (23.00, 0.65, 71, 0.95, False), # B4
    # Bar 8: Reprise with energetic octave variations
    (24.00, 0.45, 76, 1.00, False), # E5
    (24.50, 0.22, 79, 0.95, False), # G5
    (24.75, 0.22, 83, 1.00, False), # B5!
    (25.00, 0.65, 88, 1.05, False), # E6 (Peak scream!)
    (25.75, 0.22, 86, 0.95, False), # D6
    # Bar 9
    (26.00, 0.45, 84, 0.98, False), # C6
    (26.50, 0.45, 83, 0.95, False), # B5
    (26.75, 0.45, 81, 0.92, False), # A5
    (27.50, 0.45, 79, 0.92, False), # G5
    # Bar 10
    (28.00, 0.45, 81, 0.95, False), # A5
    (28.50, 0.45, 83, 0.95, False), # B5
    (29.00, 0.45, 84, 0.98, False), # C6
    (29.50, 0.45, 86, 1.00, False), # D6
    # Bar 11: Staccato turnaround
    (30.00, 0.45, 83, 1.00, False), # B5
    (30.50, 0.22, 79, 0.90, True),  # G5 staccato
    (30.75, 0.22, 76, 0.90, True),  # E5 staccato
    (31.00, 0.45, 71, 0.92, True),  # B4
]

# --- THEME 2: "Síncopa & Desvío" (Progressive Harmony Duel - Bars 12 - 19) ---
# High syncopated twin-guitar harmonies
lead_notes += [
    # Bar 12 (over Cmaj7)
    (32.00, 0.35, 72, 0.95, False), # C5
    (32.45, 0.22, 76, 0.95, False), # E5
    (32.70, 0.22, 79, 0.98, False), # G5
    (33.00, 0.45, 83, 1.00, False), # B5
    (33.50, 0.45, 79, 0.92, False), # G5
    # Bar 13 (over D)
    (34.00, 0.35, 74, 0.95, False), # D5
    (34.45, 0.22, 78, 0.95, False), # F#5
    (34.70, 0.22, 81, 0.98, False), # A5
    (35.00, 0.45, 86, 1.00, False), # D6
    (35.50, 0.45, 81, 0.92, False), # A5
    # Bar 14 (over B7)
    (36.00, 0.35, 71, 0.95, False), # B4
    (36.45, 0.22, 75, 0.95, False), # D#5 (harmonic minor tension!)
    (36.70, 0.22, 78, 0.98, False), # F#5
    (37.00, 0.45, 83, 1.00, False), # B5
    (37.50, 0.45, 78, 0.92, False), # F#5
    # Bar 15 (over Em)
    (38.00, 0.65, 76, 1.00, False), # E5
    (38.75, 0.22, 74, 0.90, False), # D5
    (39.00, 0.45, 72, 0.90, False), # C5
    (39.50, 0.45, 71, 0.90, False), # B4
    # Bar 16 (over Cmaj7)
    (40.00, 0.45, 84, 1.00, False), # C6
    (40.50, 0.22, 83, 0.95, False), # B5
    (40.75, 0.22, 81, 0.92, False), # A5
    (41.00, 0.45, 79, 0.95, False), # G5
    (41.50, 0.45, 76, 0.92, False), # E5
    # Bar 17 (over D)
    (42.00, 0.45, 86, 1.02, False), # D6
    (42.50, 0.22, 84, 0.95, False), # C6
    (42.75, 0.22, 83, 0.92, False), # B5
    (43.00, 0.45, 81, 0.95, False), # A5
    (43.50, 0.45, 78, 0.92, False), # F#5
    # Bar 18 (over B7)
    (44.00, 0.45, 83, 1.00, False), # B5
    (44.50, 0.22, 81, 0.95, False), # A5
    (44.75, 0.22, 79, 0.92, False), # G5
    (45.00, 0.45, 78, 0.95, False), # F#5
    (45.50, 0.45, 75, 0.95, False), # D#5
    # Bar 19 (over Em)
    (46.00, 0.85, 76, 1.00, False), # E5 (Sustained power note)
]

# --- THEME 3: "Esquina Nocturna" (Guitar Solo & Synth Duel - Bars 20 - 27) ---
# Blazing arpeggio sweeps and call-and-response with arcade FM synth
lead_notes += [
    # Bar 20: Rapid E-minor arpeggio sweep
    (48.00, 0.18, 64, 0.90, True),  # E4
    (48.18, 0.18, 67, 0.90, True),  # G4
    (48.36, 0.18, 71, 0.92, True),  # B4
    (48.54, 0.18, 76, 0.95, True),  # E5
    (48.72, 0.18, 79, 0.98, True),  # G5
    (49.00, 0.45, 83, 1.02, False), # B5 (Hold!)
    (49.50, 0.45, 79, 0.92, False), # G5
    # Bar 21
    (50.00, 0.45, 81, 0.95, False), # A5
    (50.50, 0.22, 79, 0.90, False), # G5
    (50.75, 0.22, 78, 0.90, False), # F#5
    (51.00, 0.45, 76, 0.95, False), # E5
    (51.50, 0.45, 74, 0.90, False), # D5
    # Bar 22 (over G)
    (52.00, 0.45, 79, 0.98, False), # G5
    (52.50, 0.22, 83, 0.95, False), # B5
    (52.75, 0.22, 86, 0.98, False), # D6
    (53.00, 0.65, 91, 1.05, False), # G6 (High scream!)
    (53.75, 0.22, 88, 0.95, False), # E6
    # Bar 23 (over A)
    (54.00, 0.45, 86, 0.98, False), # D6
    (54.50, 0.45, 84, 0.95, False), # C6
    (55.00, 0.45, 83, 0.95, False), # B5
    (55.50, 0.45, 81, 0.95, False), # A5
    # Bar 24 (Reprise in lower register)
    (56.00, 0.45, 64, 0.95, False), # E4
    (56.50, 0.22, 67, 0.90, False), # G4
    (56.75, 0.22, 71, 0.92, False), # B4
    (57.00, 0.65, 76, 1.00, False), # E5
    # Bar 25
    (58.00, 0.45, 78, 0.95, False), # F#5
    (58.50, 0.45, 79, 0.98, False), # G5
    (59.00, 0.45, 81, 1.00, False), # A5
    (59.50, 0.45, 83, 1.02, False), # B5
    # Bar 26
    (60.00, 0.45, 84, 1.02, False), # C6
    (60.50, 0.45, 86, 1.05, False), # D6
    (61.00, 0.65, 88, 1.08, False), # E6!
    # Bar 27: Trill / whammy dive into Climax
    (62.00, 0.35, 86, 1.00, False), # D6
    (62.40, 0.22, 84, 0.95, False), # C6
    (62.65, 0.22, 83, 0.95, False), # B5
    (63.00, 0.45, 81, 0.95, False), # A5
    (63.50, 0.45, 78, 0.92, False), # F#5
]

# --- THEME 4: "Gran Clímax Progresivo & Coda de Enlace" (Bars 28 - 35) ---
# High speed double-kick charge, epic octave runs
lead_notes += [
    # Bar 28
    (64.00, 0.45, 76, 1.05, False), # E5
    (64.50, 0.22, 78, 0.95, False), # F#5
    (64.75, 0.22, 79, 0.98, False), # G5
    (65.00, 0.45, 81, 1.00, False), # A5
    (65.50, 0.45, 83, 1.02, False), # B5
    # Bar 29
    (66.00, 0.45, 84, 1.02, False), # C6
    (66.50, 0.22, 83, 0.98, False), # B5
    (66.75, 0.22, 81, 0.98, False), # A5
    (67.00, 0.45, 79, 1.00, False), # G5
    (67.50, 0.45, 76, 0.95, False), # E5
    # Bar 30 (over G)
    (68.00, 0.45, 79, 1.00, False), # G5
    (68.50, 0.22, 81, 0.98, False), # A5
    (68.75, 0.22, 83, 1.02, False), # B5
    (69.00, 0.45, 86, 1.05, False), # D6
    (69.50, 0.45, 88, 1.08, False), # E6
    # Bar 31 (over A)
    (70.00, 0.45, 89, 1.05, False), # F6
    (70.50, 0.22, 88, 1.02, False), # E6
    (70.75, 0.22, 86, 1.00, False), # D6
    (71.00, 0.45, 84, 0.98, False), # C6
    (71.50, 0.45, 81, 0.95, False), # A5
    # Bar 32 (over C)
    (72.00, 0.45, 84, 1.02, False), # C6
    (72.50, 0.45, 88, 1.05, False), # E6
    (73.00, 0.65, 91, 1.10, False), # G6
    # Bar 33 (over D)
    (74.00, 0.45, 86, 1.05, False), # D6
    (74.50, 0.45, 88, 1.08, False), # E6
    (75.00, 0.65, 93, 1.12, False), # A6!
    # Bar 34 (over B7)
    (76.00, 0.45, 91, 1.08, False), # G6
    (76.50, 0.22, 89, 1.02, False), # F#6
    (76.75, 0.22, 88, 1.00, False), # E6
    (77.00, 0.45, 86, 0.98, False), # D#6
    (77.50, 0.45, 83, 1.00, False), # B5
    # Bar 35: SEAMLESS LOOP RESOLUTION
    # Blazing descending run resolving directly to E4 in Bar 0!
    (78.00, 0.18, 83, 0.95, True),  # B5
    (78.20, 0.18, 81, 0.92, True),  # A5
    (78.40, 0.18, 79, 0.92, True),  # G5
    (78.60, 0.18, 78, 0.90, True),  # F#5
    (78.80, 0.18, 76, 0.92, True),  # E5
    (79.00, 0.18, 74, 0.90, True),  # D5
    (79.20, 0.18, 72, 0.90, True),  # C5
    (79.40, 0.18, 71, 0.92, True),  # B4
    (79.60, 0.18, 67, 0.90, True),  # G4
    (79.80, 0.18, 66, 0.92, True),  # F#4 (leading tone to E4!)
]

# Convert beat times to seconds and render lead notes
for beat_time, dur_beats, midi_p, vel, is_stacc in lead_notes:
    t_sec = beat_time * BEAT_DUR
    d_sec = dur_beats * BEAT_DUR
    add_lead_guitar(t_sec, d_sec, midi_p, vel=vel, staccato=is_stacc, pan=0.08)

# Add FM Synth Stabs across Section B and C (adds arcade flavor and harmonic depth)
synth_bars = [
    (12, [60, 64, 67, 71]), # Cmaj7
    (13, [62, 66, 69, 74]), # D
    (14, [59, 63, 66, 71]), # B7
    (15, [64, 67, 71, 76]), # Em
    (16, [60, 64, 67, 72]), # Cmaj7
    (17, [62, 66, 69, 74]), # D
    (18, [59, 63, 66, 71]), # B7
    (19, [64, 67, 71, 76]), # Em
    (30, [55, 59, 62, 67]), # G
    (31, [57, 60, 64, 69]), # Am
    (32, [60, 64, 67, 72]), # C
    (33, [62, 66, 69, 74]), # D
    (34, [59, 63, 66, 71]), # B7
]
for b_idx, notes in synth_bars:
    t_bar = b_idx * BAR_DUR
    add_fm_synth_chord(t_bar, BAR_DUR * 0.9, notes, vel=0.70, staccato=False, pan=-0.25)

print("Synthesized Prog Metal tracks.")

# -------------------------------------------------------------
# 4. SEAMLESS LOOP TAIL WRAP
# -------------------------------------------------------------
print("Wrapping loop tail seamlessly...")
tail_len = BUFFER_SAMPLES - TOTAL_SAMPLES
left_buf[:tail_len] += left_buf[TOTAL_SAMPLES:TOTAL_SAMPLES + tail_len]
right_buf[:tail_len] += right_buf[TOTAL_SAMPLES:TOTAL_SAMPLES + tail_len]

left_loop = left_buf[:TOTAL_SAMPLES]
right_loop = right_buf[:TOTAL_SAMPLES]

# -------------------------------------------------------------
# 5. MASTERING & SOFT CLIPPING (Arcade Headroom)
# -------------------------------------------------------------
peak = max(np.max(np.abs(left_loop)), np.max(np.abs(right_loop)))
print(f"Pre-master peak amplitude: {peak:.3f}")

target_peak = 0.88 # -1.1 dBFS headroom
gain = target_peak / max(0.001, peak)
left_master = np.tanh(left_loop * gain * 1.05) / 1.05
right_master = np.tanh(right_loop * gain * 1.05) / 1.05

# 64-sample loop boundary crossfade for 100% zero-crossing click-free loop
xfade_len = 64
fade_in = np.linspace(0.0, 1.0, xfade_len)
fade_out = np.linspace(1.0, 0.0, xfade_len)
left_master[:xfade_len] = left_master[:xfade_len] * fade_in + left_master[-xfade_len:] * fade_out
right_master[:xfade_len] = right_master[:xfade_len] * fade_in + right_master[-xfade_len:] * fade_out

# -------------------------------------------------------------
# 6. WRITE STEREO 16-BIT 44.1kHz WAV
# -------------------------------------------------------------
out_path = "Assets/Audio/Music/bgm_triunvirato_progmetal.wav"
os.makedirs(os.path.dirname(out_path), exist_ok=True)

left_int16 = np.clip(left_master * 32767.0, -32768, 32767).astype(np.int16)
right_int16 = np.clip(right_master * 32767.0, -32768, 32767).astype(np.int16)

stereo_data = np.empty((TOTAL_SAMPLES * 2,), dtype=np.int16)
stereo_data[0::2] = left_int16
stereo_data[1::2] = right_int16

with wave.open(out_path, "wb") as wf:
    wf.setnchannels(2)
    wf.setsampwidth(2)
    wf.setframerate(SR)
    wf.writeframes(stereo_data.tobytes())

file_size = os.path.getsize(out_path)
print(f"Successfully generated: {out_path} ({file_size / (1024*1024):.2f} MB, {TOTAL_DUR:.2f}s)")
