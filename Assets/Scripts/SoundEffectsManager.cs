using System;
using UnityEngine;

namespace Fightyub
{
    public enum CharacterVocalType
    {
        MaleHuman,
        FemaleHuman,
        CanineAnimal
    }

    public class SoundEffectsManager : MonoBehaviour
    {
        public static SoundEffectsManager Instance { get; private set; }

        [Header("Generic Combat & Movement Audio Clips")]
        public AudioClip step01Clip;
        public AudioClip step02Clip;
        public AudioClip punchHitClip;
        public AudioClip kickHitClip;
        public AudioClip attackWhiffClip;
        public AudioClip perfectDefenseClip;
        public AudioClip parryStunClip;
        public AudioClip jumpMaleClip;
        public AudioClip jumpFemaleClip;
        public AudioClip jumpCanineClip;

        [Header("Subtle UI Audio Clips")]
        public AudioClip uiMoveClip;
        public AudioClip uiConfirmClip;

        [Header("Character-Specific Jump Audio Clips (Trimmed)")]
        public AudioClip luliJumpClip;
        public AudioClip pipiJumpClip;
        public AudioClip jumaayJumpClip;
        public AudioClip tatoonJumpClip;
        public AudioClip jere7emJumpClip;

        [Header("Character-Specific Special Attack Audio Clips (Trimmed)")]
        public AudioClip luliSpecialClip;
        public AudioClip pipiSpecialClip;
        public AudioClip jumaaySpecialClip;
        public AudioClip tatoonSpecialClip;
        public AudioClip jere7emSpecialClip;

        [Header("Audio Sources")]
        public AudioSource sfxSource;
        public AudioSource uiSource;

        private bool _stepToggle = false;
        private float _lastUiMoveTime = -10f;
        private float _lastUiConfirmTime = -10f;
        private float _lastWhiffTime = -10f;

        private const float MinUiMoveInterval = 0.045f;
        private const float MinUiConfirmInterval = 0.060f;
        private const float MinWhiffInterval = 0.075f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            EnsureAudioSource();
            LoadClipsIfMissing();
        }

        private void Start()
        {
            EnsureAudioSource();
            LoadClipsIfMissing();
        }

        public void EnsureAudioSource()
        {
            var sources = GetComponents<AudioSource>();
            if (sfxSource == null)
            {
                if (sources.Length > 0) sfxSource = sources[0];
                else sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f; // 2D clean stereo playback
            sfxSource.volume = 1f;

            if (uiSource == null)
            {
                if (sources.Length > 1) uiSource = sources[1];
                else uiSource = gameObject.AddComponent<AudioSource>();
            }
            uiSource.playOnAwake = false;
            uiSource.loop = false;
            uiSource.spatialBlend = 0f;
            uiSource.volume = 1f;
        }

        public void PauseCombatAudio()
        {
            if (sfxSource != null && sfxSource.isPlaying)
            {
                sfxSource.Pause();
            }
        }

        public void ResumeCombatAudio()
        {
            if (sfxSource != null)
            {
                sfxSource.UnPause();
            }
        }

        public void StopCombatAudio()
        {
            if (sfxSource != null)
            {
                sfxSource.Stop();
            }
        }

        public void LoadClipsIfMissing()
        {
#if UNITY_EDITOR
            if (step01Clip == null) step01Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_step_01.wav");
            if (step02Clip == null) step02Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_step_02.wav");
            if (punchHitClip == null) punchHitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_punch_hit.wav");
            if (kickHitClip == null) kickHitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_kick_hit.wav");
            if (attackWhiffClip == null) attackWhiffClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_attack_whiff.wav");
            if (perfectDefenseClip == null) perfectDefenseClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_perfect_defense.wav");
            if (parryStunClip == null) parryStunClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_parry_stun.wav");
            if (jumpMaleClip == null) jumpMaleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_jump_male.wav");
            if (jumpFemaleClip == null) jumpFemaleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_jump_female.wav");
            if (jumpCanineClip == null) jumpCanineClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_jump_canine.wav");

            // Subtle UI clips
            if (uiMoveClip == null) uiMoveClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_ui_move.wav");
            if (uiConfirmClip == null) uiConfirmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_ui_confirm.wav");

            // Load trimmed character-specific Jump clips (only for Luli, Pipi, Jumaay, Tatoon, Jere7em)
            if (luliJumpClip == null) luliJumpClip = LoadCharacterClip("luli_salto");
            if (pipiJumpClip == null) pipiJumpClip = LoadCharacterClip("pipi_salto");
            if (jumaayJumpClip == null) jumaayJumpClip = LoadCharacterClip("jumaay_salto");
            if (tatoonJumpClip == null) tatoonJumpClip = LoadCharacterClip("tatoon_salto");
            if (jere7emJumpClip == null) jere7emJumpClip = LoadCharacterClip("jere7em_salto");

            // Load trimmed character-specific Special Attack clips (only for Luli, Pipi, Jumaay, Tatoon, Jere7em)
            if (luliSpecialClip == null) luliSpecialClip = LoadCharacterClip("luli_ataque_especial");
            if (pipiSpecialClip == null) pipiSpecialClip = LoadCharacterClip("pipi_ataque_especial");
            if (jumaaySpecialClip == null) jumaaySpecialClip = LoadCharacterClip("jumaay_ataque_especial");
            if (tatoonSpecialClip == null) tatoonSpecialClip = LoadCharacterClip("tatoon_ataque_especial");
            if (jere7emSpecialClip == null) jere7emSpecialClip = LoadCharacterClip("jere7em_ataque_especial");
#endif
        }

#if UNITY_EDITOR
        private static AudioClip LoadCharacterClip(string baseName)
        {
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{baseName}_trimmed.ogg");
            if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{baseName}.wav");
            if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{baseName}.ogg");
            return clip;
        }
#endif

        public AudioClip GetCharacterJumpClip(string fighterName)
        {
            if (string.IsNullOrEmpty(fighterName)) return null;
            string n = fighterName.Trim();
            if (n.IndexOf("luli", StringComparison.OrdinalIgnoreCase) >= 0) return luliJumpClip;
            if (n.IndexOf("pipi", StringComparison.OrdinalIgnoreCase) >= 0) return pipiJumpClip;
            if (n.IndexOf("jumaay", StringComparison.OrdinalIgnoreCase) >= 0) return jumaayJumpClip;
            if (n.IndexOf("tatoon", StringComparison.OrdinalIgnoreCase) >= 0) return tatoonJumpClip;
            if (n.IndexOf("jere7em", StringComparison.OrdinalIgnoreCase) >= 0) return jere7emJumpClip;
            return null;
        }

        public AudioClip GetCharacterSpecialClip(string fighterName)
        {
            if (string.IsNullOrEmpty(fighterName)) return null;
            string n = fighterName.Trim();
            if (n.IndexOf("luli", StringComparison.OrdinalIgnoreCase) >= 0) return luliSpecialClip;
            if (n.IndexOf("pipi", StringComparison.OrdinalIgnoreCase) >= 0) return pipiSpecialClip;
            if (n.IndexOf("jumaay", StringComparison.OrdinalIgnoreCase) >= 0) return jumaaySpecialClip;
            if (n.IndexOf("tatoon", StringComparison.OrdinalIgnoreCase) >= 0) return tatoonSpecialClip;
            if (n.IndexOf("jere7em", StringComparison.OrdinalIgnoreCase) >= 0) return jere7emSpecialClip;
            return null;
        }

        public float MasterSfxVolume
        {
            get
            {
                if (OptionsManager.Instance != null &&
                    OptionsManager.Instance.optionsPanel != null &&
                    OptionsManager.Instance.optionsPanel.activeInHierarchy)
                {
                    return Mathf.Clamp01(OptionsManager.Instance.pendingSfxVolume);
                }
                return OptionsManager.SavedSfxVolume;
            }
        }

        public void StopAllSfx()
        {
            if (sfxSource != null)
            {
                sfxSource.Stop();
            }
        }

        /// <summary>
        /// Sonido breve y discreto al cambiar la opción enfocada en menús, selección de personaje o escenario.
        /// Incluye control anti-solapamiento y respeta el volumen de efectos de sonido.
        /// </summary>
        public void PlayUiMove(float relativeVolume = 0.24f)
        {
            float now = Time.unscaledTime;
            if (now - _lastUiMoveTime < MinUiMoveInterval) return;

            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            var src = (uiSource != null) ? uiSource : sfxSource;
            if (uiMoveClip != null && src != null)
            {
                _lastUiMoveTime = now;
                src.volume = 1f;
                src.pitch = 1.0f;
                src.PlayOneShot(uiMoveClip, vol);
            }
        }

        /// <summary>
        /// Clic suave al confirmar una opción de menú o selección.
        /// Incluye control anti-solapamiento y respeta el volumen de efectos de sonido.
        /// </summary>
        public void PlayUiConfirm(float relativeVolume = 0.30f)
        {
            float now = Time.unscaledTime;
            if (now - _lastUiConfirmTime < MinUiConfirmInterval) return;

            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            var src = (uiSource != null) ? uiSource : sfxSource;
            if (uiConfirmClip != null && src != null)
            {
                _lastUiConfirmTime = now;
                src.volume = 1f;
                src.pitch = 1.0f;
                src.PlayOneShot(uiConfirmClip, vol);
            }
        }

        /// <summary>
        /// Whoosh suave de golpe al aire cuando un ataque no conecta (ni acierta ni es bloqueado).
        /// </summary>
        public void PlayAttackWhiff(bool isKick = false, float relativeVolume = 0.26f)
        {
            float now = Time.unscaledTime;
            if (now - _lastWhiffTime < MinWhiffInterval) return;

            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            if (attackWhiffClip != null && sfxSource != null)
            {
                _lastWhiffTime = now;
                sfxSource.volume = 1f;
                sfxSource.pitch = isKick ? UnityEngine.Random.Range(0.92f, 0.97f) : UnityEngine.Random.Range(1.01f, 1.06f);
                sfxSource.PlayOneShot(attackWhiffClip, vol);
                sfxSource.pitch = 1.0f;
            }
        }

        /// <summary>
        /// Pasos muy sutiles, sincronizados con el contacto de los pies con el piso.
        /// Alterna entre step01 y step02 con pitch sutil.
        /// </summary>
        public void PlayFootstep(float relativeVolume = 0.20f)
        {
            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            AudioClip clip = _stepToggle ? step02Clip : step01Clip;
            _stepToggle = !_stepToggle;

            if (clip != null && sfxSource != null)
            {
                sfxSource.volume = 1f;
                sfxSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
                sfxSource.PlayOneShot(clip, vol);
                sfxSource.pitch = 1.0f;
            }
        }

        /// <summary>
        /// Golpe de mano notorio pero equilibrado, ejecutado al conectar.
        /// </summary>
        public void PlayPunchHit(float relativeVolume = 0.85f)
        {
            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            if (punchHitClip != null && sfxSource != null)
            {
                sfxSource.volume = 1f;
                sfxSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
                sfxSource.PlayOneShot(punchHitClip, vol);
                sfxSource.pitch = 1.0f;
            }
        }

        /// <summary>
        /// Patada notoria y contundente, ejecutada al conectar.
        /// </summary>
        public void PlayKickHit(float relativeVolume = 0.95f)
        {
            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            if (kickHitClip != null && sfxSource != null)
            {
                sfxSource.volume = 1f;
                sfxSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
                sfxSource.PlayOneShot(kickHitClip, vol);
                sfxSource.pitch = 1.0f;
            }
        }

        /// <summary>
        /// Sonido metálico nítido y distintivo al lograr una defensa perfecta (parry).
        /// </summary>
        public void PlayPerfectDefense(float relativeVolume = 0.95f)
        {
            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            if (perfectDefenseClip != null && sfxSource != null)
            {
                sfxSource.volume = 1f;
                sfxSource.pitch = UnityEngine.Random.Range(0.98f, 1.03f);
                sfxSource.PlayOneShot(perfectDefenseClip, vol);
                sfxSource.pitch = 1.0f;
            }
        }

        /// <summary>
        /// Impacto metálico resonante al lograr 3 defensas perfectas y aturdir al rival.
        /// </summary>
        public void PlayParryStun(float relativeVolume = 1.0f)
        {
            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            if (parryStunClip != null && sfxSource != null)
            {
                sfxSource.volume = 1f;
                sfxSource.pitch = 1.0f;
                sfxSource.PlayOneShot(parryStunClip, vol);
            }
        }

        /// <summary>
        /// Reproduce el sonido de salto una única vez al iniciar el salto.
        /// Si el personaje tiene un clip específico asignado (Luli, Pipi, Jumaay, Tatoon, Jere7em), reproduce ese clip;
        /// de lo contrario usa el sonido genérico de salto.
        /// </summary>
        public void PlayCharacterJump(string fighterName, CharacterVocalType fallbackVocalType, AudioClip assignedJumpClip = null, float relativeVolume = 0.90f)
        {
            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            AudioClip customClip = assignedJumpClip != null ? assignedJumpClip : GetCharacterJumpClip(fighterName);
            if (customClip != null)
            {
                if (sfxSource != null)
                {
                    sfxSource.volume = 1f;
                    sfxSource.pitch = 1.0f;
                    sfxSource.PlayOneShot(customClip, vol);
                }
                return;
            }

            PlayJumpVocal(fallbackVocalType, 0.65f);
        }

        /// <summary>
        /// Reproduce el sonido de ataque especial una única vez al iniciar el ataque especial,
        /// exclusivamente para los personajes que tienen clip asignado (Luli, Pipi, Jumaay, Tatoon, Jere7em).
        /// </summary>
        public void PlayCharacterSpecialAttack(string fighterName, AudioClip assignedSpecialClip = null, float relativeVolume = 0.95f)
        {
            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            AudioClip customClip = assignedSpecialClip != null ? assignedSpecialClip : GetCharacterSpecialClip(fighterName);
            if (customClip != null && sfxSource != null)
            {
                sfxSource.volume = 1f;
                sfxSource.pitch = 1.0f;
                sfxSource.PlayOneShot(customClip, vol);
            }
        }

        /// <summary>
        /// Sonido vocal breve al saltar para personajes sin clip personalizado.
        /// </summary>
        public void PlayJumpVocal(CharacterVocalType vocalType, float relativeVolume = 0.65f)
        {
            float vol = MasterSfxVolume * Mathf.Clamp01(relativeVolume);
            if (vol <= 0.001f) return;

            AudioClip clip = null;
            switch (vocalType)
            {
                case CharacterVocalType.FemaleHuman:
                    clip = jumpFemaleClip;
                    break;
                case CharacterVocalType.CanineAnimal:
                    clip = jumpCanineClip;
                    break;
                case CharacterVocalType.MaleHuman:
                default:
                    clip = jumpMaleClip;
                    break;
            }

            if (clip != null && sfxSource != null)
            {
                sfxSource.volume = 1f;
                sfxSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
                sfxSource.PlayOneShot(clip, vol);
                sfxSource.pitch = 1.0f;
            }
        }
    }
}
