using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Fightyub
{
    public enum FighterState
    {
        Idle,
        Walk,
        Run,
        Crouch,
        Jump,
        StandPunch,
        StandKick,
        CrouchPunch,
        CrouchKick,
        AirPunch,
        AirKick,
        StandBlock,
        CrouchBlock,
        AirBlock,
        Hitstun,
        KO,
        SpecialAttack
    }

    [System.Serializable]
    public class FighterAnimationSet
    {
        public Sprite portrait;
        public Sprite[] idle;
        public Sprite[] walk;
        public Sprite[] run;
        public Sprite[] crouch;
        public Sprite[] jump;
        public Sprite[] standPunch;
        public Sprite[] standKick;
        public Sprite[] crouchPunch;
        public Sprite[] crouchKick;
        public Sprite[] airPunch;
        public Sprite[] airKick;
        public Sprite[] standBlock;
        public Sprite[] crouchBlock;
        public Sprite[] airBlock;
        public Sprite[] hitReaction; // 2 reaction frames
        public Sprite[] hitRecovery; // 2 recovery frames
        public Sprite[] hurt;        // 4 frames: 0-1 reaction, 2-3 recovery
        public Sprite[] specialAttack; // 10 frames
        public AudioClip jumpAudioClip;
        public AudioClip specialAttackAudioClip;
    }

    public class FighterController : MonoBehaviour
    {
        public enum AIDifficulty
        {
            Easy = 0,
            Medium = 1,
            Hard = 2
        }
        [Header("Identity")]
        public string fighterName = "Jumaay";
        public int playerIndex = 1; // 1 = Left, 2 = Right
        public int inputPlayerIndex = 1; // 1 = reads P1 input, 2 = reads P2 input
        public FighterAnimationSet anims = new FighterAnimationSet();
        public AudioClip jumpClip;
        public AudioClip specialAttackClip;
        public FighterController opponent;

        [Header("Stats & Combat")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float currentSuper = 0f;
        public const float MaxSuper = 100f;
        public float groundY = -3.00f;
        public float minArenaX = -5.1f;
        public float maxArenaX = 5.1f;
        public float walkSpeed = 3.2f;
        public float runSpeed = 6.0f;
        public float jumpVelocity = 10.5f;
        public float gravity = 28.0f;

        public void AddSuper(float amount)
        {
            currentSuper = Mathf.Clamp(currentSuper + amount, 0f, MaxSuper);
        }

        public void ResetSuper()
        {
            currentSuper = 0f;
        }

        [Header("State")]
        public FighterState state = FighterState.Idle;
        public bool facingRight = true;
        public bool isAI = false;
        public AIDifficulty aiDifficulty = AIDifficulty.Medium;

        public bool AttackHitDealt => _attackHitDealt;
        public bool WhiffSoundPlayed => _whiffSoundPlayed;
        public bool IsParryStunned => _isParryStunned;

        [Header("Perfect Defense (Parry)")]
        public const float PerfectDefenseWindow = 0.15f; // Ventana de 150 ms para bloqueo justo antes del impacto
        public const int MaxPerfectDefenseStreak = 3;
        public const float ParryStunDuration = 0.80f;

        public int perfectDefenseStreak = 0;
        private float _currentBlockDuration = 0f;
        private bool _isParryStunned = false;
        private float _currentHitstunDuration = 0.28f;

        private SpriteRenderer _sr;
        private float _verticalVelocity;
        private float _horizontalVelocity;
        private float _knockbackVelocity;
        private float _stateTimer;
        private float _animTimer;
        private int _animFrame;
        private bool _attackHitDealt;
        private bool _whiffSoundPlayed;
        private int _lastFootstepFrame = -1;
        private bool _aiReactedToAttack = false;
        private int _specialHitStep = 0;
        private bool _specialConnected = false;

        // Combat polish: brief hit-stop, flash reaction & pooled pixel-art impact VFX
        private float _hitStopTimer = 0f;
        private float _flashTimer = 0f;
        private bool _flashIsBlock = false;
        private GameObject _impactVfxObj;
        private SpriteRenderer _impactVfxRenderer;
        private float _impactVfxTimer = 0f;
        private float _impactVfxDuration = 0.135f;
        private bool _impactVfxBlocked = false;
        private static Sprite[] _hitSparkSprites;
        private static Sprite[] _blockSparkSprites;
        private PixelVfxEmitter _pixelVfx;
        private float _dashDustTimer = 0f;
        private float _parryStunVfxTimer = 0f;
        private float _specialVfxTimer = 0f;

        // 2D Contact Shadow & Discrete 2D Impact/Special Light Flash
        private GameObject _contactShadowObj;
        private SpriteRenderer _contactShadowRenderer;
        private Light2D _impactLight2D;
        private float _impactLightBaseIntensity = 0f;
        private static Sprite _contactShadowSprite;
        private static Sprite _pixelLightCookieSprite;
        private static Material _unlitShadowMaterial;

        public float HitStopTimer => _hitStopTimer;
        public bool InHitStop => _hitStopTimer > 0f;
        public float FlashTimer => _flashTimer;
        public bool IsImpactVfxActive => _impactVfxRenderer != null && _impactVfxRenderer.enabled;
        public bool LastImpactWasBlocked => _impactVfxBlocked;
        public SpriteRenderer ImpactVfxRenderer => _impactVfxRenderer;
        public SpriteRenderer ContactShadowRenderer => _contactShadowRenderer;
        public Light2D ImpactLight2D => _impactLight2D;

        public CharacterVocalType VocalType
        {
            get
            {
                string n = (fighterName ?? "").ToLowerInvariant();
                if (n.Contains("tatoon") || n.Contains("pachi") || n.Contains("perla") || n.Contains("momo") || n.Contains("pipi"))
                    return CharacterVocalType.CanineAnimal;
                if (n.Contains("lila") || n.Contains("luli") || n.Contains("irma"))
                    return CharacterVocalType.FemaleHuman;
                return CharacterVocalType.MaleHuman;
            }
        }

        // AI state
        private float _aiDecisionTimer;
        private float _aiAttackCooldown;
        private int _aiMoveDir;
        private bool _aiRun;
        private bool _aiCrouch;
        private bool _aiBlock;
        private float _aiReactionDelayTimer;
        private bool _oppPrevAttacking;
        private bool _oppPrevHitDealt;
        private FighterState _lastAiAttackState = FighterState.Idle;
        private int _sameAttackRepeatCount = 0;

        // Double-tap run detection
        private int _lastTapDir;
        private float _lastTapTime = -10f;
        private bool _isRunning;
        private const float DoubleTapThreshold = 0.28f;

        public bool IsGrounded => transform.position.y <= groundY + 0.01f && _verticalVelocity <= 0f;
        public bool IsBlocking => state == FighterState.StandBlock || state == FighterState.CrouchBlock || state == FighterState.AirBlock;
        public bool IsAttacking => state == FighterState.StandPunch || state == FighterState.StandKick ||
                                   state == FighterState.CrouchPunch || state == FighterState.CrouchKick ||
                                   state == FighterState.AirPunch || state == FighterState.AirKick || state == FighterState.SpecialAttack;

        private float _baseVisualScale = 1.9f;
        private static System.Collections.Generic.Dictionary<string, Sprite[]> _damageCache;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _baseVisualScale = transform.localScale.x;
        }

        public void ApplyCharacterProfile(string newName, float visualScale, FighterAnimationSet newAnims)
        {
            fighterName = newName;
            anims = newAnims;
            _baseVisualScale = visualScale;
            EnsureDamageAnimation(fighterName, anims);
            EnsureSpecialAttackAnimation(fighterName, anims);
            if (SoundEffectsManager.Instance != null)
            {
                SoundEffectsManager.Instance.LoadClipsIfMissing();
                jumpClip = (anims != null && anims.jumpAudioClip != null)
                    ? anims.jumpAudioClip
                    : SoundEffectsManager.Instance.GetCharacterJumpClip(fighterName);
                specialAttackClip = (anims != null && anims.specialAttackAudioClip != null)
                    ? anims.specialAttackAudioClip
                    : SoundEffectsManager.Instance.GetCharacterSpecialClip(fighterName);
            }
            else if (anims != null)
            {
                jumpClip = anims.jumpAudioClip;
                specialAttackClip = anims.specialAttackAudioClip;
            }
            transform.localScale = new Vector3(visualScale, visualScale, 1f);
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && anims != null && anims.idle != null && anims.idle.Length > 0)
            {
                _sr.sprite = anims.idle[0];
            }
        }

        public float GetHitstunScaleRatio()
        {
            return GetNormalizedScaleRatio(FighterState.Hitstun);
        }

        public float GetNormalizedScaleRatio(FighterState st)
        {
            string n = (fighterName ?? "").ToLowerInvariant();

            if (st == FighterState.Hitstun)
            {
                if (n.Contains("tatoon")) return 105f / 181f;
                if (n.Contains("jumaay")) return 127f / 185f;
                if (n.Contains("lila")) return 92f / 137f;
                if (n.Contains("lalo")) return 123f / 188f;
                if (n.Contains("luli")) return 127f / 183f;
                if (n.Contains("pachi")) return 89f / 125f;
                if (n.Contains("perla")) return 96f / 153f;
                if (n.Contains("irma")) return 111f / 183f;
                if (n.Contains("momo")) return 91f / 155f;
                if (n.Contains("pipi")) return 117f / 161f;
                if (n.Contains("jere7em")) return 127f / 176f;
                return 1f;
            }

            if (st == FighterState.Walk)
            {
                if (n.Contains("lalo")) return 123f / 141f;
                if (n.Contains("jere7em")) return 127f / 103.5f;
                if (n.Contains("pipi")) return 117f / 132.5f;
                if (n.Contains("luli")) return 127f / 136.1f;
                if (n.Contains("irma")) return 111f / 99.9f;
                if (n.Contains("momo")) return 91f / 81.1f;
                if (n.Contains("jumaay")) return 127f / 113.9f;
                if (n.Contains("pachi")) return 89f / 84.9f;
                if (n.Contains("perla")) return 96f / 89.1f;
                return 1f;
            }

            if (st == FighterState.Run)
            {
                if (n.Contains("lalo")) return 123f / 138.8f;
                if (n.Contains("jere7em")) return 127f / 84.9f;
                if (n.Contains("pipi")) return 117f / 137.4f;
                if (n.Contains("luli")) return 127f / 142.4f;
                if (n.Contains("irma")) return 111f / 88.6f;
                if (n.Contains("momo")) return 91f / 81.9f;
                if (n.Contains("jumaay")) return 127f / 107.4f;
                if (n.Contains("pachi")) return 89f / 81.1f;
                if (n.Contains("perla")) return 96f / 100.6f;
                if (n.Contains("tatoon")) return 105f / 98.4f;
                return 1f;
            }

            if (st == FighterState.StandPunch)
            {
                if (n.Contains("pipi")) return 117f / 107.5f;
                if (n.Contains("lalo")) return 123f / 115.4f;
                return 1f;
            }

            if (st == FighterState.StandKick)
            {
                if (n.Contains("luli")) return 126f / 108.2f;
                if (n.Contains("pipi")) return 117f / 109.1f;
                if (n.Contains("lalo")) return 123f / 114.6f;
                return 1f;
            }

            return 1f;
        }

        public static void EnsureDamageAnimation(string charName, FighterAnimationSet animSet)
        {
            if (animSet == null || string.IsNullOrEmpty(charName)) return;
            if (animSet.hurt != null && animSet.hurt.Length >= 4) return;

            if (_damageCache == null)
            {
                _damageCache = new System.Collections.Generic.Dictionary<string, Sprite[]>(System.StringComparer.OrdinalIgnoreCase);
                Sprite[] allSprites = null;
#if UNITY_EDITOR
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Chars/Fightyub_Todos_Personajes_Dano_Recuperacion_16bit.png");
                var list = new System.Collections.Generic.List<Sprite>();
                foreach (var a in assets) if (a is Sprite sp) list.Add(sp);
                allSprites = list.ToArray();
#else
                allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
#endif
                if (allSprites != null && allSprites.Length > 0)
                {
                    string[] charNames = { "Tatoon", "Jumaay", "Lila", "Lalo", "Luli", "Pachi", "Perla", "Irma", "Momo", "Pipi", "Jere7em" };
                    foreach (var cn in charNames)
                    {
                        var frames = new Sprite[4];
                        for (int f = 0; f < 4; f++)
                        {
                            string targetName = $"{cn}_Damage_{f}";
                            frames[f] = System.Array.Find(allSprites, s => s != null && s.name == targetName);
                        }
                        if (frames[0] != null)
                        {
                            _damageCache[cn] = frames;
                        }
                    }
                }
            }

            if (_damageCache != null)
            {
                foreach (var kvp in _damageCache)
                {
                    if (charName.IndexOf(kvp.Key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        animSet.hurt = kvp.Value;
                        animSet.hitReaction = new Sprite[] { kvp.Value[0], kvp.Value[1] };
                        animSet.hitRecovery = new Sprite[] { kvp.Value[2], kvp.Value[3] };
                        break;
                    }
                }
            }
        }

        private static System.Collections.Generic.Dictionary<string, Sprite[]> _specialCache;
        private static readonly System.Collections.Generic.Dictionary<string, string> SpecialAssetPaths = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Jumaay", "Assets/Chars/Jumaay_AtaqueEspecial_Guitarra_16bit.png" },
            { "Pipi", "Assets/Chars/Pipi_AtaqueEspecial_Varita_Burbujas_16bit.png" },
            { "Irma", "Assets/Chars/Irma_AtaqueEspecial_EscobaTaladro_16bit.png" },
            { "Lila", "Assets/Chars/Lila_AtaqueEspecial_CacaVoladora_16bit.png" },
            { "Perla", "Assets/Chars/Perla_AtaqueEspecial_LadridoEnsordecedor_16bit.png" },
            { "Pachi", "Assets/Chars/Pachi_AtaqueEspecial_RosquillaJuguete_16bit.png" },
            { "Jere7em", "Assets/Chars/Jere7em_AtaqueEspecial_Monitor_3Golpes_16bit.png" },
            { "Tatoon", "Assets/Chars/Tatoon_AtaqueEspecial_PizzeraChef_16bit.png" },
            { "Lalo", "Assets/Chars/Lalo_AtaqueEspecial_Estetoscopio_16bit.png" },
            { "Luli", "Assets/Chars/Luli_AtaqueEspecial_CandelabroMickey_16bit.png" },
            { "Momo", "Assets/Chars/Momo_AtaqueEspecial_SoguitaJuguete_3Golpes_16bit.png" }
        };

        public static void EnsureSpecialAttackAnimation(string charName, FighterAnimationSet animSet)
        {
            if (animSet == null || string.IsNullOrEmpty(charName)) return;
            if (animSet.specialAttack != null && animSet.specialAttack.Length == 10 && animSet.specialAttack[0] != null) return;

            if (_specialCache == null)
            {
                _specialCache = new System.Collections.Generic.Dictionary<string, Sprite[]>(System.StringComparer.OrdinalIgnoreCase);
            }

            string matchedKey = null;
            foreach (var kvp in SpecialAssetPaths)
            {
                if (charName.IndexOf(kvp.Key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matchedKey = kvp.Key;
                    break;
                }
            }
            if (matchedKey == null) return;

            if (_specialCache.TryGetValue(matchedKey, out var cachedSprites))
            {
                animSet.specialAttack = cachedSprites;
                return;
            }

            Sprite[] frames = new Sprite[10];
#if UNITY_EDITOR
            string path = SpecialAssetPaths[matchedKey];
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < 10; i++)
            {
                string targetName = $"{matchedKey}_Special_{i}";
                foreach (var a in assets)
                {
                    if (a is Sprite sp && sp.name == targetName)
                    {
                        frames[i] = sp;
                        break;
                    }
                }
            }
#else
            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < 10; i++)
            {
                string targetName = $"{matchedKey}_Special_{i}";
                frames[i] = System.Array.Find(allSprites, s => s != null && s.name == targetName);
            }
#endif

            if (frames[0] != null)
            {
                _specialCache[matchedKey] = frames;
                animSet.specialAttack = frames;
            }
        }

        public bool IsThreeHitSpecial()
        {
            string n = (fighterName ?? "").ToLowerInvariant();
            return n.Contains("jere7em") || n.Contains("momo") || n.Contains("irma");
        }

        public float GetSpecialReach()
        {
            string n = (fighterName ?? "").ToLowerInvariant();
            if (n.Contains("lila")) return 4.8f;       // Caca voladora / proyectil a larga distancia
            if (n.Contains("perla")) return 3.4f;      // Ladrido ensordecedor onda sónica
            if (n.Contains("pipi")) return 3.4f;       // Varita y burbujas mágicas
            return 2.4f;                               // Alcance cuerpo a cuerpo con props
        }

        public void ClearCombatPolishEffects()
        {
            CancelInvoke(nameof(RestoreSpriteColor));
            _hitStopTimer = 0f;
            _flashTimer = 0f;
            _flashIsBlock = false;
            _impactVfxTimer = 0f;
            _impactLightBaseIntensity = 0f;
            _dashDustTimer = 0f;
            _parryStunVfxTimer = 0f;
            _specialVfxTimer = 0f;
            _pixelVfx?.Clear();
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _sr.color = Color.white;
            if (_impactVfxRenderer != null) _impactVfxRenderer.enabled = false;
            if (_impactLight2D != null)
            {
                _impactLight2D.intensity = 0f;
                _impactLight2D.enabled = false;
            }
        }

        public static Sprite EnsurePixelLightCookieSprite()
        {
            if (_pixelLightCookieSprite != null && _pixelLightCookieSprite.texture != null)
            {
                return _pixelLightCookieSprite;
            }

            const int size = 48;
            const float ppu = 24f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];

            float center = (size - 1) * 0.5f;
            float maxR = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / maxR;
                    float dy = (y - center) / maxR;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Bandas escalonadas limpias estilo pixel art 16-bit (sin degradado borroso)
                    float a = 0f;
                    if (dist <= 0.22f) a = 1.00f;
                    else if (dist <= 0.42f) a = 0.76f;
                    else if (dist <= 0.62f) a = 0.50f;
                    else if (dist <= 0.80f) a = 0.26f;
                    else if (dist <= 0.95f) a = 0.10f;

                    pixels[y * size + x] = new Color(a, a, a, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _pixelLightCookieSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
            _pixelLightCookieSprite.name = "PixelArtLightCookie_16Bit";
            return _pixelLightCookieSprite;
        }

        private static Sprite EnsureContactShadowSprite()
        {
            if (_contactShadowSprite != null && _contactShadowSprite.texture != null)
            {
                return _contactShadowSprite;
            }

            const int w = 48;
            const int h = 14;
            const float ppu = 32f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[w * h];

            float cx = (w - 1) * 0.5f;
            float cy = (h - 1) * 0.5f;
            float rx = w * 0.48f;
            float ry = h * 0.46f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - cx) / rx;
                    float ny = (y - cy) / ry;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);

                    // Sombra ovalada pixel art con 3 anillos escalonados limpios
                    float alpha = 0f;
                    if (d <= 0.48f) alpha = 0.56f;
                    else if (d <= 0.76f) alpha = 0.38f;
                    else if (d <= 0.96f) alpha = 0.18f;

                    pixels[y * w + x] = new Color(0.02f, 0.03f, 0.06f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _contactShadowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
            _contactShadowSprite.name = "FighterContactShadow_16Bit";
            return _contactShadowSprite;
        }

        private void EnsureContactShadowObject()
        {
            if (_contactShadowObj == null)
            {
                string shadowName = $"{gameObject.name}_ContactShadow";
                var existing = GameObject.Find(shadowName);
                if (existing != null)
                {
                    _contactShadowObj = existing;
                }
                else
                {
                    _contactShadowObj = new GameObject(shadowName);
                    if (transform.parent != null)
                    {
                        _contactShadowObj.transform.SetParent(transform.parent, false);
                    }
                }
            }

            if (_contactShadowObj != null)
            {
                _contactShadowObj.name = $"{gameObject.name}_ContactShadow";
            }

            if (_contactShadowRenderer == null)
            {
                _contactShadowRenderer = _contactShadowObj.GetComponent<SpriteRenderer>();
                if (_contactShadowRenderer == null)
                {
                    _contactShadowRenderer = _contactShadowObj.AddComponent<SpriteRenderer>();
                }
            }

            _contactShadowRenderer.sprite = EnsureContactShadowSprite();
            if (_unlitShadowMaterial == null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
                if (unlitShader != null) _unlitShadowMaterial = new Material(unlitShader);
            }
            if (_unlitShadowMaterial != null)
            {
                _contactShadowRenderer.sharedMaterial = _unlitShadowMaterial;
            }

            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                _contactShadowRenderer.sortingLayerID = _sr.sortingLayerID;
                _contactShadowRenderer.sortingOrder = Mathf.Max(-5, _sr.sortingOrder - 6); // Orden 4: sobre el suelo (-10) y bajo el luchador (10)
            }
            else
            {
                _contactShadowRenderer.sortingOrder = 4;
            }
        }

        public void UpdateContactShadow()
        {
            EnsureContactShadowObject();
            if (_contactShadowRenderer == null) return;

            bool effectsOn = OptionsManager.AreLightingAndShadowsEnabled;
            if (!effectsOn || !gameObject.activeInHierarchy)
            {
                _contactShadowRenderer.enabled = false;
                return;
            }

            _contactShadowRenderer.enabled = true;

            // La sombra permanece alineada con el suelo (groundY) y sigue en X al luchador sin tocar su escala ni posición
            float heightAboveGround = Mathf.Max(0f, transform.position.y - groundY);
            float jumpShrink = Mathf.Clamp(1f - (heightAboveGround / 2.8f) * 0.56f, 0.42f, 1.0f);

            // Ajuste suave de ancho según postura en suelo sin alterar el transform del luchador
            float stateWidthFactor = (state == FighterState.KO) ? 1.25f : 1.0f;
            float sx = 1.45f * jumpShrink * stateWidthFactor;
            float sy = 1.04f * jumpShrink;

            _contactShadowObj.transform.position = new Vector3(transform.position.x, groundY + 0.01f, 0f);
            _contactShadowObj.transform.localScale = new Vector3(sx, sy, 1f);

            float qualMult = Mathf.Clamp(OptionsManager.LightingQualityMultiplier, 0.75f, 1.0f);
            float alpha = Mathf.Lerp(0.52f, 1.0f, jumpShrink) * qualMult;
            _contactShadowRenderer.color = new Color(1f, 1f, 1f, alpha);
        }

        public void ApplyHitStop(float duration)
        {
            // Reemplazar (no acumular) el hit-stop para no congelar en golpes repetidos
            _hitStopTimer = Mathf.Clamp(duration, 0f, 0.12f);
        }

        private static void EnsureImpactSprites()
        {
            if (_hitSparkSprites != null && _hitSparkSprites.Length == 3 && _hitSparkSprites[0] != null &&
                _blockSparkSprites != null && _blockSparkSprites.Length == 3 && _blockSparkSprites[0] != null)
            {
                return;
            }

            _hitSparkSprites = new Sprite[3];
            _blockSparkSprites = new Sprite[3];
            const int size = 20;
            const float ppu = 44f;

            for (int frame = 0; frame < 3; frame++)
            {
                // Hit Spark (warm gold / orange / white pixel-art burst)
                var hitTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                hitTex.filterMode = FilterMode.Point;
                hitTex.wrapMode = TextureWrapMode.Clamp;
                var hitPixels = new Color[size * size];

                // Block Spark (crisp cyan / electric blue pixel-art guard shield)
                var blkTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                blkTex.filterMode = FilterMode.Point;
                blkTex.wrapMode = TextureWrapMode.Clamp;
                var blkPixels = new Color[size * size];

                Color hitCore = new Color(1f, 0.98f, 0.86f, 1f);
                Color hitMid = new Color(1f, 0.84f, 0.22f, 1f);
                Color hitOuter = new Color(1f, 0.42f, 0.08f, 0.95f);

                Color blkCore = new Color(0.90f, 0.99f, 1f, 1f);
                Color blkMid = new Color(0.30f, 0.88f, 1f, 1f);
                Color blkOuter = new Color(0.10f, 0.48f, 0.96f, 0.95f);

                int c = size / 2;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int dx = Mathf.Abs(x - c);
                        int dy = Mathf.Abs(y - c);
                        int manhattan = dx + dy;
                        int chebyshev = Mathf.Max(dx, dy);
                        bool onAxis = (dx <= 1 || dy <= 1);
                        bool onDiag = Mathf.Abs(dx - dy) <= 1;

                        // --- HIT SPARK FRAME PIXELS ---
                        if (frame == 0)
                        {
                            if (chebyshev <= 1) hitPixels[y * size + x] = hitCore;
                            else if ((onAxis || onDiag) && chebyshev <= 4) hitPixels[y * size + x] = hitMid;
                            else if ((dx == 0 || dy == 0 || dx == dy) && chebyshev <= 6) hitPixels[y * size + x] = hitOuter;
                        }
                        else if (frame == 1)
                        {
                            if (chebyshev == 2 && (onAxis || onDiag)) hitPixels[y * size + x] = hitCore;
                            else if ((dx == 0 || dy == 0 || dx == dy) && chebyshev >= 3 && chebyshev <= 6) hitPixels[y * size + x] = hitMid;
                            else if ((onAxis || onDiag) && chebyshev >= 6 && chebyshev <= 8) hitPixels[y * size + x] = hitOuter;
                        }
                        else
                        {
                            if ((dx == 0 || dy == 0 || dx == dy) && chebyshev >= 6 && chebyshev <= 8) hitPixels[y * size + x] = hitMid;
                            else if ((dx == 0 || dy == 0 || dx == dy) && chebyshev == 9) hitPixels[y * size + x] = hitOuter;
                        }

                        // --- BLOCK SPARK FRAME PIXELS (distinct diamond/octagonal shield) ---
                        if (frame == 0)
                        {
                            if (manhattan <= 2) blkPixels[y * size + x] = blkCore;
                            else if (manhattan >= 3 && manhattan <= 5) blkPixels[y * size + x] = blkMid;
                            else if (manhattan == 6) blkPixels[y * size + x] = blkOuter;
                        }
                        else if (frame == 1)
                        {
                            if (manhattan == 4 || manhattan == 5) blkPixels[y * size + x] = blkCore;
                            else if (manhattan >= 6 && manhattan <= 7 && chebyshev <= 5) blkPixels[y * size + x] = blkMid;
                            else if (manhattan == 8 && chebyshev <= 6) blkPixels[y * size + x] = blkOuter;
                        }
                        else
                        {
                            if (manhattan >= 7 && manhattan <= 8 && dx >= 2 && dy >= 2) blkPixels[y * size + x] = blkMid;
                            else if (manhattan == 9 && dx >= 2 && dy >= 2) blkPixels[y * size + x] = blkOuter;
                        }
                    }
                }

                hitTex.SetPixels(hitPixels);
                hitTex.Apply();
                _hitSparkSprites[frame] = Sprite.Create(hitTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
                _hitSparkSprites[frame].name = $"HitSpark_{frame}";

                blkTex.SetPixels(blkPixels);
                blkTex.Apply();
                _blockSparkSprites[frame] = Sprite.Create(blkTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
                _blockSparkSprites[frame].name = $"BlockSpark_{frame}";
            }
        }

        private void EnsureImpactVfxComponents()
        {
            EnsureImpactSprites();
            if (_impactVfxObj == null)
            {
                string vfxName = $"{gameObject.name}_ImpactVFX";
                var existing = GameObject.Find(vfxName);
                if (existing != null)
                {
                    _impactVfxObj = existing;
                }
                else
                {
                    _impactVfxObj = new GameObject(vfxName);
                    if (transform.parent != null) _impactVfxObj.transform.SetParent(transform.parent, false);
                }
            }
            if (_impactVfxRenderer == null)
            {
                _impactVfxRenderer = _impactVfxObj.GetComponent<SpriteRenderer>();
                if (_impactVfxRenderer == null) _impactVfxRenderer = _impactVfxObj.AddComponent<SpriteRenderer>();
            }
            if (_impactLight2D == null)
            {
                _impactLight2D = _impactVfxObj.GetComponent<Light2D>();
                if (_impactLight2D == null) _impactLight2D = _impactVfxObj.AddComponent<Light2D>();
                _impactLight2D.lightType = Light2D.LightType.Sprite;
                _impactLight2D.lightCookieSprite = EnsurePixelLightCookieSprite();
                _impactLight2D.overlapOperation = Light2D.OverlapOperation.Additive;
                _impactLight2D.volumetricEnabled = false;
                _impactLight2D.shadowsEnabled = false;
                _impactLight2D.enabled = false;
            }
            if (_pixelVfx == null)
            {
                _pixelVfx = new PixelVfxEmitter(gameObject.name, transform.parent);
            }
        }

        private void SpawnImpactEffect(Vector3 worldPos, bool blocked, bool isHeavy, bool isPerfectDefense = false, bool isKick = false, bool isSpecial = false)
        {
            EnsureImpactVfxComponents();

            int baseLayer = (_sr != null) ? _sr.sortingLayerID : 0;
            int baseOrder = (_sr != null) ? _sr.sortingOrder : 22;

            _impactVfxRenderer.sortingLayerID = baseLayer;
            _impactVfxRenderer.sortingOrder = baseOrder + 3;

            _impactVfxBlocked = blocked;
            _impactVfxTimer = 0f;
            _impactVfxDuration = isPerfectDefense ? 0.18f : (isHeavy ? 0.15f : 0.12f);
            _impactVfxObj.transform.position = worldPos;
            float scale = isPerfectDefense ? 1.40f : (isHeavy ? 1.25f : 0.95f);
            _impactVfxObj.transform.localScale = new Vector3(scale, scale, 1f);
            _impactVfxRenderer.sprite = blocked ? _blockSparkSprites[0] : _hitSparkSprites[0];

            PixelImpactKind kind = isPerfectDefense
                ? PixelImpactKind.PerfectBlock
                : (blocked ? PixelImpactKind.Block : (isSpecial ? PixelImpactKind.Special : (isKick ? PixelImpactKind.Kick : PixelImpactKind.Punch)));

            if (kind == PixelImpactKind.PerfectBlock)
                _impactVfxRenderer.color = new Color(0.40f, 1f, 1f, 1f);
            else if (kind == PixelImpactKind.Block)
                _impactVfxRenderer.color = new Color(0.85f, 0.96f, 1f, 1f);
            else if (kind == PixelImpactKind.Kick)
                _impactVfxRenderer.color = new Color(1f, 0.56f, 0.42f, 1f);
            else if (kind == PixelImpactKind.Special)
                _impactVfxRenderer.color = GetSpecialPrimaryColor();
            else
                _impactVfxRenderer.color = Color.white;

            _impactVfxRenderer.enabled = true;

            EmitImpactPixelBurst(worldPos, kind, baseLayer, baseOrder + 2);

            if (_impactLight2D != null)
            {
                if (OptionsManager.AreLightingAndShadowsEnabled)
                {
                    float qualMult = OptionsManager.LightingQualityMultiplier;
                    _impactLight2D.lightCookieSprite = EnsurePixelLightCookieSprite();
                    _impactLight2D.color = isPerfectDefense
                        ? new Color(0.20f, 1f, 0.95f, 1f)
                        : (blocked
                            ? new Color(0.32f, 0.88f, 1f, 1f)
                            : (isKick ? new Color(1f, 0.42f, 0.25f, 1f) : new Color(1f, 0.85f, 0.38f, 1f)));
                    _impactLightBaseIntensity = (isPerfectDefense ? 0.30f : (blocked ? 0.15f : (isHeavy ? 0.22f : 0.17f))) * qualMult;
                    _impactLight2D.intensity = _impactLightBaseIntensity;
                    _impactLight2D.enabled = true;
                }
                else
                {
                    _impactLightBaseIntensity = 0f;
                    _impactLight2D.intensity = 0f;
                    _impactLight2D.enabled = false;
                }
            }
        }

        private void EmitImpactPixelBurst(Vector3 worldPos, PixelImpactKind kind, int sortingLayerId, int sortingOrder)
        {
            if (_pixelVfx == null) return;

            int count = 8;
            Color cStart = Color.white;
            Color cEnd = Color.white;
            float speedMin = 2.0f;
            float speedMax = 4.2f;
            float life = 0.16f;
            float startScale = 0.70f;
            PixelShape shape = PixelShape.Square;

            switch (kind)
            {
                case PixelImpactKind.Punch:
                    count = 9;
                    cStart = new Color(1f, 0.96f, 0.55f, 1f);
                    cEnd = new Color(1f, 0.48f, 0.10f, 0f);
                    speedMin = 2.2f;
                    speedMax = 4.4f;
                    life = 0.15f;
                    startScale = 0.72f;
                    shape = PixelShape.Square;
                    break;

                case PixelImpactKind.Kick:
                    count = 11;
                    cStart = new Color(1f, 0.48f, 0.24f, 1f);
                    cEnd = new Color(0.88f, 0.12f, 0.42f, 0f);
                    speedMin = 2.8f;
                    speedMax = 5.4f;
                    life = 0.18f;
                    startScale = 0.85f;
                    shape = PixelShape.Diamond;
                    break;

                case PixelImpactKind.Block:
                    count = 8;
                    cStart = new Color(0.85f, 0.99f, 1f, 1f);
                    cEnd = new Color(0.15f, 0.58f, 0.98f, 0f);
                    speedMin = 1.8f;
                    speedMax = 3.5f;
                    life = 0.14f;
                    startScale = 0.68f;
                    shape = PixelShape.Diamond;
                    break;

                case PixelImpactKind.PerfectBlock:
                    count = 14;
                    cStart = new Color(0.55f, 1f, 1f, 1f);
                    cEnd = new Color(1f, 0.92f, 0.35f, 0f);
                    speedMin = 2.8f;
                    speedMax = 5.8f;
                    life = 0.22f;
                    startScale = 0.90f;
                    shape = PixelShape.Diamond;
                    _pixelVfx.Emit(worldPos, Vector3.zero, new Color(0.5f, 1f, 1f, 0.95f), new Color(1f, 0.95f, 0.4f, 0f), 0.20f, 1.0f, 2.3f, PixelShape.Ring, sortingLayerId, sortingOrder);
                    break;

                case PixelImpactKind.Special:
                    count = 12;
                    cStart = GetSpecialPrimaryColor();
                    cEnd = GetSpecialSecondaryColor();
                    cEnd.a = 0f;
                    speedMin = 2.6f;
                    speedMax = 5.5f;
                    life = 0.20f;
                    startScale = 0.88f;
                    shape = PixelShape.Diamond;
                    break;
            }

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.22f, 0.22f);
                float spd = Random.Range(speedMin, speedMax);
                Vector3 vel = new Vector3(Mathf.Cos(angle) * spd, Mathf.Sin(angle) * spd, 0f);
                PixelShape s = (i % 3 == 0 && kind == PixelImpactKind.Kick) ? PixelShape.Streak : shape;
                float rot = (s == PixelShape.Streak) ? angle * Mathf.Rad2Deg : 0f;
                _pixelVfx.Emit(
                    worldPos + vel * 0.015f,
                    vel,
                    cStart,
                    cEnd,
                    life * Random.Range(0.8f, 1.15f),
                    startScale,
                    0.22f,
                    s,
                    sortingLayerId,
                    sortingOrder,
                    gravity: 2.5f,
                    drag: 4.0f,
                    rotationDeg: rot
                );
            }
        }

        private void SpawnWhiffMotionEffect(bool isKick)
        {
            EnsureImpactVfxComponents();
            if (_pixelVfx == null) return;

            int baseLayer = (_sr != null) ? _sr.sortingLayerID : 0;
            int behindOrder = (_sr != null) ? Mathf.Max(-5, _sr.sortingOrder - 1) : 10;
            float dir = facingRight ? 1f : -1f;
            float yOff = (state == FighterState.CrouchPunch || state == FighterState.CrouchKick) ? 0.62f : 1.08f;

            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = new Vector3(
                    transform.position.x + dir * (0.45f + i * 0.14f),
                    transform.position.y + yOff + (i - 1) * 0.10f,
                    0f
                );
                Vector3 vel = new Vector3(dir * (isKick ? 2.6f : 2.0f), (i - 1) * 0.25f, 0f);
                _pixelVfx.Emit(
                    pos,
                    vel,
                    new Color(0.92f, 0.96f, 1f, 0.45f),
                    new Color(0.92f, 0.96f, 1f, 0f),
                    0.085f,
                    0.55f,
                    0.25f,
                    PixelShape.Streak,
                    baseLayer,
                    behindOrder
                );
            }
        }

        private Color GetSpecialPrimaryColor()
        {
            string n = (fighterName ?? "").ToLowerInvariant();
            if (n.Contains("jumaay")) return new Color(1f, 0.88f, 0.20f, 1f);      // Electric gold
            if (n.Contains("pipi")) return new Color(0.44f, 0.96f, 1f, 1f);        // Magic cyan
            if (n.Contains("irma")) return new Color(0.95f, 0.90f, 0.78f, 1f);     // Drill wind cream
            if (n.Contains("lila")) return new Color(0.55f, 0.88f, 0.18f, 1f);     // Toxic lime
            if (n.Contains("perla")) return new Color(0.36f, 0.91f, 1f, 1f);      // Sonic cyan
            if (n.Contains("pachi")) return new Color(1f, 0.25f, 0.69f, 1f);       // Toy magenta
            if (n.Contains("jere7em")) return new Color(0.0f, 0.96f, 1f, 1f);      // CRT cyan
            if (n.Contains("tatoon")) return new Color(1f, 0.35f, 0.16f, 1f);      // Oven ember
            if (n.Contains("lalo")) return new Color(1f, 0.18f, 0.30f, 1f);        // Cardiac red
            if (n.Contains("luli")) return new Color(1f, 0.82f, 0.25f, 1f);        // Candle gold
            if (n.Contains("momo")) return new Color(0.28f, 0.72f, 1f, 1f);        // Rope sky blue
            return new Color(1f, 0.85f, 0.35f, 1f);
        }

        private Color GetSpecialSecondaryColor()
        {
            string n = (fighterName ?? "").ToLowerInvariant();
            if (n.Contains("jumaay")) return new Color(1f, 0.40f, 0.08f, 1f);      // Rock orange
            if (n.Contains("pipi")) return new Color(1f, 0.52f, 0.85f, 1f);        // Pastel pink
            if (n.Contains("irma")) return new Color(1f, 0.85f, 0.25f, 1f);        // Drill spark gold
            if (n.Contains("lila")) return new Color(0.48f, 0.32f, 0.16f, 1f);     // Comic brown
            if (n.Contains("perla")) return new Color(0.92f, 0.99f, 1f, 1f);       // Sonic white
            if (n.Contains("pachi")) return new Color(1f, 0.90f, 0.20f, 1f);       // Toy gold
            if (n.Contains("jere7em")) return new Color(0.22f, 1f, 0.42f, 1f);     // Matrix green
            if (n.Contains("tatoon")) return new Color(1f, 0.95f, 0.84f, 1f);      // Flour cream
            if (n.Contains("lalo")) return new Color(0.25f, 0.94f, 1f, 1f);        // Defib cyan
            if (n.Contains("luli")) return new Color(1f, 0.47f, 0.10f, 1f);        // Warm amber
            if (n.Contains("momo")) return new Color(1f, 0.88f, 0.25f, 1f);        // Bright yellow
            return new Color(1f, 0.50f, 0.15f, 1f);
        }

        private void EmitCharacterSpecialParticles(bool isInitialBurst)
        {
            EnsureImpactVfxComponents();
            if (_pixelVfx == null) return;

            int baseLayer = (_sr != null) ? _sr.sortingLayerID : 0;
            // Always draw special aura/trail behind the fighter and their prop/projectile so nothing is obscured
            int behindOrder = (_sr != null) ? Mathf.Max(-5, _sr.sortingOrder - 1) : 9;
            float dir = facingRight ? 1f : -1f;
            string n = (fighterName ?? "").ToLowerInvariant();

            Color c1 = GetSpecialPrimaryColor();
            Color c2 = GetSpecialSecondaryColor();
            Color c1Fade = new Color(c1.r, c1.g, c1.b, 0f);
            Color c2Fade = new Color(c2.r, c2.g, c2.b, 0f);

            int count = isInitialBurst ? 8 : 3;
            for (int i = 0; i < count; i++)
            {
                Color startCol = (i % 2 == 0) ? c1 : c2;
                Color endCol = (i % 2 == 0) ? c1Fade : c2Fade;

                if (n.Contains("pipi"))
                {
                    // Magic wand bubbles & sparkles drifting forward-up without hiding the wand
                    Vector3 pos = transform.position + new Vector3(dir * Random.Range(0.2f, 0.9f), Random.Range(0.75f, 1.45f), 0f);
                    Vector3 vel = new Vector3(dir * Random.Range(0.8f, 2.0f), Random.Range(0.5f, 1.3f), 0f);
                    PixelShape shape = (i % 2 == 0) ? PixelShape.Ring : PixelShape.Diamond;
                    _pixelVfx.Emit(pos, vel, startCol, endCol, 0.30f, 0.65f, 0.85f, shape, baseLayer, behindOrder);
                }
                else if (n.Contains("perla"))
                {
                    // Sonic bark pulses expanding forward-down/up in open arcs
                    Vector3 pos = transform.position + new Vector3(dir * 0.55f, 0.95f + Random.Range(-0.22f, 0.22f), 0f);
                    Vector3 vel = new Vector3(dir * Random.Range(2.8f, 4.4f), Random.Range(-0.6f, 0.6f), 0f);
                    PixelShape shape = (i == 0 && isInitialBurst) ? PixelShape.Ring : PixelShape.Streak;
                    _pixelVfx.Emit(pos, vel, startCol, endCol, 0.24f, 0.80f, 1.25f, shape, baseLayer, behindOrder);
                }
                else if (n.Contains("lila"))
                {
                    // Comic stink/fly puffs trailing behind/below so the flying projectile stays 100% visible
                    Vector3 pos = transform.position + new Vector3(-dir * Random.Range(0.15f, 0.55f), Random.Range(0.35f, 1.15f), 0f);
                    Vector3 vel = new Vector3(-dir * Random.Range(0.3f, 0.9f), Random.Range(0.5f, 1.2f), 0f);
                    PixelShape shape = (i % 2 == 0) ? PixelShape.Square : PixelShape.Ring;
                    _pixelVfx.Emit(pos, vel, startCol, endCol, 0.28f, 0.55f, 0.30f, shape, baseLayer, behindOrder);
                }
                else if (n.Contains("jere7em"))
                {
                    // Digital CRT glitch pixels snapping around perimeter
                    Vector3 pos = transform.position + new Vector3(Random.Range(-0.65f, 0.65f), Random.Range(0.25f, 1.55f), 0f);
                    Vector3 vel = new Vector3(dir * Random.Range(-0.6f, 1.4f), Random.Range(-0.4f, 1.0f), 0f);
                    _pixelVfx.Emit(pos, vel, startCol, endCol, 0.22f, 0.65f, 0.20f, PixelShape.Square, baseLayer, behindOrder);
                }
                else if (n.Contains("irma"))
                {
                    // Drill-broom swirling wind & dust kicking backward behind broom
                    Vector3 pos = transform.position + new Vector3(-dir * Random.Range(0.2f, 0.6f), Random.Range(0.25f, 1.25f), 0f);
                    Vector3 vel = new Vector3(-dir * Random.Range(1.5f, 3.0f), Random.Range(-0.3f, 0.8f), 0f);
                    PixelShape shape = (i % 2 == 0) ? PixelShape.Streak : PixelShape.Diamond;
                    _pixelVfx.Emit(pos, vel, startCol, endCol, 0.22f, 0.72f, 0.25f, shape, baseLayer, behindOrder);
                }
                else if (n.Contains("momo"))
                {
                    // Fast rope-whip wind streaks trailing behind
                    Vector3 pos = transform.position + new Vector3(Random.Range(-0.55f, 0.45f), Random.Range(0.40f, 1.35f), 0f);
                    Vector3 vel = new Vector3(-dir * Random.Range(1.4f, 2.8f), Random.Range(-0.2f, 0.5f), 0f);
                    _pixelVfx.Emit(pos, vel, startCol, endCol, 0.19f, 0.75f, 0.30f, PixelShape.Streak, baseLayer, behindOrder);
                }
                else if (n.Contains("luli") || n.Contains("tatoon"))
                {
                    // Rising warm embers / candlelight / oven flour sparks behind the fighter
                    Vector3 pos = transform.position + new Vector3(Random.Range(-0.60f, 0.60f), Random.Range(0.20f, 1.15f), 0f);
                    Vector3 vel = new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(1.1f, 2.2f), 0f);
                    PixelShape shape = (i % 2 == 0) ? PixelShape.Diamond : PixelShape.Square;
                    _pixelVfx.Emit(pos, vel, startCol, endCol, 0.28f, 0.62f, 0.20f, shape, baseLayer, behindOrder);
                }
                else
                {
                    // Jumaay (rock electric), Pachi (toy confetti), Lalo (medical pulse): perimeter sparks behind fighter
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 offset = new Vector3(Mathf.Cos(ang) * 0.55f, 0.85f + Mathf.Sin(ang) * 0.55f, 0f);
                    Vector3 vel = new Vector3(Mathf.Cos(ang) * 1.6f, Mathf.Sin(ang) * 1.4f + 0.4f, 0f);
                    PixelShape shape = (n.Contains("jumaay") && i % 2 == 0) ? PixelShape.Streak : PixelShape.Diamond;
                    _pixelVfx.Emit(transform.position + offset, vel, startCol, endCol, 0.24f, 0.72f, 0.25f, shape, baseLayer, behindOrder);
                }
            }
        }

        private void SpawnSpecialActivationFlash()
        {
            EmitCharacterSpecialParticles(true);
            _specialVfxTimer = 0f;
            if (!OptionsManager.AreLightingAndShadowsEnabled) return;
            EnsureImpactVfxComponents();
            if (_impactLight2D != null)
            {
                float qualMult = OptionsManager.LightingQualityMultiplier;
                _impactLight2D.lightCookieSprite = EnsurePixelLightCookieSprite();
                _impactLight2D.color = GetSpecialPrimaryColor();
                _impactLightBaseIntensity = 0.22f * qualMult;
                _impactLight2D.intensity = _impactLightBaseIntensity;
                _impactLight2D.enabled = true;
            }
        }

        private void UpdateCombatPolishVisuals(float dt)
        {
            _pixelVfx?.Tick(dt);

            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f)
                {
                    _flashTimer = 0f;
                    if (_sr != null) _sr.color = Color.white;
                }
                else if (_sr != null)
                {
                    if (_flashIsBlock)
                    {
                        _sr.color = (_flashTimer > 0.055f)
                            ? new Color(0.84f, 0.96f, 1f, 1f)
                            : new Color(0.60f, 0.86f, 1f, 1f);
                    }
                    else
                    {
                        _sr.color = (_flashTimer > 0.06f)
                            ? new Color(1f, 0.72f, 0.68f, 1f)
                            : new Color(1f, 0.45f, 0.45f, 1f);
                    }
                }
            }

            // Dash dust trail (only while grounded and moving in Run/Dash)
            if (state == FighterState.Run && IsGrounded && Mathf.Abs(_horizontalVelocity) > 0.1f)
            {
                _dashDustTimer += dt;
                if (_dashDustTimer >= 0.045f)
                {
                    _dashDustTimer = 0f;
                    EnsureImpactVfxComponents();
                    int baseLayer = (_sr != null) ? _sr.sortingLayerID : 0;
                    int behindOrder = (_sr != null) ? Mathf.Max(-5, _sr.sortingOrder - 1) : 10;
                    float moveSign = Mathf.Sign(_horizontalVelocity);
                    Vector3 dustPos = new Vector3(transform.position.x - moveSign * 0.32f, transform.position.y + 0.08f, 0f);
                    for (int i = 0; i < 2; i++)
                    {
                        Vector3 vel = new Vector3(-moveSign * Random.Range(0.9f, 1.9f), Random.Range(0.25f, 0.75f), 0f);
                        _pixelVfx?.Emit(
                            dustPos + new Vector3(0f, i * 0.06f, 0f),
                            vel,
                            new Color(0.90f, 0.86f, 0.78f, 0.85f),
                            new Color(0.68f, 0.64f, 0.58f, 0f),
                            0.18f,
                            0.60f,
                            0.20f,
                            i == 0 ? PixelShape.Square : PixelShape.Streak,
                            baseLayer,
                            behindOrder,
                            drag: 3.5f
                        );
                    }
                }
            }
            else
            {
                _dashDustTimer = 0.045f; // Ready to emit immediately on next dash start
            }

            // Parry Stun stars above head while stunned
            if (state == FighterState.Hitstun && _isParryStunned)
            {
                _parryStunVfxTimer += dt;
                if (_parryStunVfxTimer >= 0.095f)
                {
                    _parryStunVfxTimer = 0f;
                    EnsureImpactVfxComponents();
                    int baseLayer = (_sr != null) ? _sr.sortingLayerID : 0;
                    int frontOrder = (_sr != null) ? _sr.sortingOrder + 2 : 24;
                    float angle = Time.time * 6f;
                    Vector3 starPos = new Vector3(
                        transform.position.x + Mathf.Cos(angle) * 0.34f,
                        transform.position.y + 1.58f + Mathf.Sin(angle) * 0.08f,
                        0f
                    );
                    _pixelVfx?.Emit(
                        starPos,
                        new Vector3(Mathf.Cos(angle) * 0.35f, 0.35f, 0f),
                        new Color(1f, 0.95f, 0.50f, 1f),
                        new Color(1f, 0.62f, 0.10f, 0f),
                        0.26f,
                        0.70f,
                        0.25f,
                        PixelShape.Diamond,
                        baseLayer,
                        frontOrder
                    );
                }
            }
            else
            {
                _parryStunVfxTimer = 0.095f;
            }

            // Character-themed SUPER particles during active frames
            if (state == FighterState.SpecialAttack && _stateTimer >= 0.08f && _stateTimer <= 0.86f)
            {
                _specialVfxTimer += dt;
                if (_specialVfxTimer >= 0.065f)
                {
                    _specialVfxTimer = 0f;
                    EmitCharacterSpecialParticles(false);
                }
            }

            if (_impactVfxRenderer != null && _impactVfxRenderer.enabled)
            {
                _impactVfxTimer += dt;
                if (_impactVfxTimer >= _impactVfxDuration)
                {
                    _impactVfxRenderer.enabled = false;
                    if (_impactLight2D != null)
                    {
                        _impactLight2D.intensity = 0f;
                        _impactLight2D.enabled = false;
                    }
                }
                else
                {
                    EnsureImpactSprites();
                    Sprite[] frames = _impactVfxBlocked ? _blockSparkSprites : _hitSparkSprites;
                    int idx = Mathf.Clamp(Mathf.FloorToInt((_impactVfxTimer / _impactVfxDuration) * frames.Length), 0, frames.Length - 1);
                    _impactVfxRenderer.sprite = frames[idx];

                    if (_impactLight2D != null && _impactLight2D.enabled)
                    {
                        // Escalones discretos sincronizados con los 3 cuadros pixel art (sin parpadeo continuo)
                        float stepFactor = (idx == 0) ? 1.0f : (idx == 1 ? 0.65f : 0.32f);
                        _impactLight2D.intensity = _impactLightBaseIntensity * stepFactor;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            UpdateContactShadow();
        }

        private void OnDisable()
        {
            if (_impactVfxRenderer != null) _impactVfxRenderer.enabled = false;
            if (_impactLight2D != null)
            {
                _impactLight2D.intensity = 0f;
                _impactLight2D.enabled = false;
            }
            if (_contactShadowRenderer != null) _contactShadowRenderer.enabled = false;
            _pixelVfx?.Clear();
            _hitStopTimer = 0f;
            _flashTimer = 0f;
        }

        private void OnDestroy()
        {
            if (_impactVfxObj != null)
            {
                Destroy(_impactVfxObj);
            }
            if (_contactShadowObj != null)
            {
                Destroy(_contactShadowObj);
            }
            _pixelVfx?.DestroyPool();
        }

        public void ResetFighter(Vector3 startPos, bool startFacingRight)
        {
            ClearCombatPolishEffects();
            transform.position = startPos;
            currentHealth = maxHealth;
            facingRight = startFacingRight;
            _verticalVelocity = 0f;
            _horizontalVelocity = 0f;
            _knockbackVelocity = 0f;
            _isRunning = false;
            _aiDecisionTimer = 0.15f;
            _aiAttackCooldown = 0.40f;
            _aiMoveDir = 0;
            _aiRun = false;
            _aiCrouch = false;
            _aiBlock = false;
            _aiReactedToAttack = false;
            _aiReactionDelayTimer = 0f;
            _oppPrevAttacking = false;
            _oppPrevHitDealt = false;
            _lastAiAttackState = FighterState.Idle;
            _sameAttackRepeatCount = 0;
            perfectDefenseStreak = 0;
            _currentBlockDuration = 0f;
            _isParryStunned = false;
            _currentHitstunDuration = 0.28f;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.color = Color.white;
            _sr.flipX = !facingRight;
            state = FighterState.Walk; // Force state change on SetState(Idle)
            SetState(FighterState.Idle);
            UpdateSpriteAnimation(0f);
            UpdateContactShadow();
        }

        public void TickFighter(float dt, bool allowInput)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();

            UpdateCombatPolishVisuals(dt);
            UpdateContactShadow();

            if (_hitStopTimer > 0f)
            {
                if (dt > _hitStopTimer)
                {
                    dt -= _hitStopTimer;
                    _hitStopTimer = 0f;
                }
                else
                {
                    _hitStopTimer -= dt;
                    return;
                }
            }

            _stateTimer += dt;
            _animTimer += dt;

            if (IsBlocking)
            {
                _currentBlockDuration += dt;
            }
            else
            {
                _currentBlockDuration = 0f;
            }

            // Always face opponent when neutral (not attacking or in hitstun) and not in KO
            if (opponent != null && state != FighterState.KO && !IsAttacking && state != FighterState.Hitstun)
            {
                if (Mathf.Abs(opponent.transform.position.x - transform.position.x) > 0.05f)
                {
                    facingRight = opponent.transform.position.x > transform.position.x;
                }
                _sr.flipX = !facingRight;
            }

            if (state == FighterState.KO)
            {
                ApplyPhysics(dt);
                UpdateSpriteAnimation(dt);
                return;
            }

            if (state == FighterState.Hitstun)
            {
                float hitstunLimit = _currentHitstunDuration > 0f ? _currentHitstunDuration : 0.28f;
                if (_stateTimer >= hitstunLimit)
                {
                    _isParryStunned = false;
                    _currentHitstunDuration = 0.28f;
                    _sr.color = Color.white;
                    transform.localScale = new Vector3(_baseVisualScale, _baseVisualScale, 1f);
                    SetState(IsGrounded ? FighterState.Idle : FighterState.Jump);
                }
                ApplyPhysics(dt);
                UpdateSpriteAnimation(dt);
                return;
            }

            if (IsAttacking)
            {
                UpdateAttack(dt);
                ApplyPhysics(dt);
                UpdateSpriteAnimation(dt);
                return;
            }

            if (allowInput)
            {
                if (isAI)
                {
                    HandleAI(dt);
                }
                else
                {
                    HandleInput(dt);
                }
            }
            else
            {
                _horizontalVelocity = 0f;
                if (IsGrounded && state != FighterState.Idle)
                    SetState(FighterState.Idle);
            }

            ApplyPhysics(dt);
            UpdateSpriteAnimation(dt);
            UpdateContactShadow();
        }

        private void HandleInput(float dt)
        {
            int readIndex = (inputPlayerIndex > 0) ? inputPlayerIndex : playerIndex;
            FighterInputFrame input;
            if (ControlBindingsManager.Instance != null)
            {
                input = ControlBindingsManager.Instance.ReadPlayerInput(readIndex);
            }
            else
            {
                var kb = Keyboard.current;
                if (kb == null) return;
                if (readIndex == 2)
                {
                    input = new FighterInputFrame
                    {
                        leftHeld = kb.leftArrowKey.isPressed,
                        rightHeld = kb.rightArrowKey.isPressed,
                        leftPressedThisFrame = kb.leftArrowKey.wasPressedThisFrame,
                        rightPressedThisFrame = kb.rightArrowKey.wasPressedThisFrame,
                        upPressedThisFrame = kb.upArrowKey.wasPressedThisFrame,
                        downHeld = kb.downArrowKey.isPressed,
                        downPressedThisFrame = kb.downArrowKey.wasPressedThisFrame,
                        punchPressedThisFrame = kb.bKey.wasPressedThisFrame,
                        kickPressedThisFrame = kb.nKey.wasPressedThisFrame,
                        blockHeld = kb.mKey.isPressed,
                        blockPressedThisFrame = kb.mKey.wasPressedThisFrame,
                        specialPressedThisFrame = kb.enterKey.wasPressedThisFrame,
                        runHeld = kb.rightShiftKey.isPressed
                    };
                }
                else
                {
                    input = new FighterInputFrame
                    {
                        leftHeld = kb.aKey.isPressed,
                        rightHeld = kb.dKey.isPressed,
                        leftPressedThisFrame = kb.aKey.wasPressedThisFrame,
                        rightPressedThisFrame = kb.dKey.wasPressedThisFrame,
                        upPressedThisFrame = kb.wKey.wasPressedThisFrame,
                        downHeld = kb.sKey.isPressed,
                        downPressedThisFrame = kb.sKey.wasPressedThisFrame,
                        punchPressedThisFrame = kb.jKey.wasPressedThisFrame,
                        kickPressedThisFrame = kb.kKey.wasPressedThisFrame,
                        blockHeld = kb.lKey.isPressed,
                        blockPressedThisFrame = kb.lKey.wasPressedThisFrame,
                        specialPressedThisFrame = kb.spaceKey.wasPressedThisFrame,
                        runHeld = kb.leftShiftKey.isPressed
                    };
                }
            }

            bool left = input.leftHeld;
            bool right = input.rightHeld;
            bool upPressed = input.upPressedThisFrame;
            bool down = input.downHeld;
            bool punchPressed = input.punchPressedThisFrame;
            bool kickPressed = input.kickPressedThisFrame;
            bool blockHeld = input.blockHeld;

            if (input.leftPressedThisFrame) RegisterDirectionTap(-1);
            if (input.rightPressedThisFrame) RegisterDirectionTap(1);

            int moveDir = (right ? 1 : 0) - (left ? 1 : 0);
            if (moveDir == 0 || moveDir != _lastTapDir)
            {
                _isRunning = false;
            }

            // Priority 0: Special Attack (only when Super is 100%)
            if (input.specialPressedThisFrame)
            {
                if (currentSuper >= MaxSuper && !IsAttacking && state != FighterState.Hitstun && state != FighterState.KO)
                {
                    currentSuper = 0f;
                    FightMatchManager.Instance?.UpdateHUD();
                    _horizontalVelocity = 0f;
                    SetState(FighterState.SpecialAttack);
                    return;
                }
            }

            // Priority 1: Attacks (Punch / Kick)
            if (punchPressed)
            {
                _horizontalVelocity = IsGrounded ? 0f : _horizontalVelocity;
                if (!IsGrounded) SetState(FighterState.AirPunch);
                else if (down) SetState(FighterState.CrouchPunch);
                else SetState(FighterState.StandPunch);
                return;
            }

            if (kickPressed)
            {
                _horizontalVelocity = IsGrounded ? 0f : _horizontalVelocity;
                if (!IsGrounded) SetState(FighterState.AirKick);
                else if (down) SetState(FighterState.CrouchKick);
                else SetState(FighterState.StandKick);
                return;
            }

            // Priority 2: Blocking (Stand, Crouch, Air)
            if (blockHeld)
            {
                if (IsGrounded) _horizontalVelocity = 0f;
                if (!IsGrounded) SetState(FighterState.AirBlock);
                else if (down) SetState(FighterState.CrouchBlock);
                else SetState(FighterState.StandBlock);
                return;
            }

            // Priority 3: Jump
            if (IsGrounded && upPressed && !down)
            {
                _verticalVelocity = jumpVelocity;
                float airSpeed = _isRunning ? runSpeed * 0.85f : walkSpeed;
                _horizontalVelocity = moveDir * airSpeed;
                SetState(FighterState.Jump);
                return;
            }

            // Priority 4: Airborne movement
            if (!IsGrounded)
            {
                if (moveDir != 0)
                {
                    _horizontalVelocity = moveDir * walkSpeed * 0.9f;
                }
                if (state != FighterState.Jump)
                {
                    SetState(FighterState.Jump);
                }
                return;
            }

            // Priority 5: Crouch
            if (down)
            {
                _horizontalVelocity = 0f;
                SetState(FighterState.Crouch);
                return;
            }

            // Priority 6: Walk / Run / Idle
            if (moveDir != 0)
            {
                bool runActive = _isRunning || input.runHeld;
                _horizontalVelocity = moveDir * (runActive ? runSpeed : walkSpeed);
                SetState(runActive ? FighterState.Run : FighterState.Walk);
            }
            else
            {
                _horizontalVelocity = 0f;
                SetState(FighterState.Idle);
            }
        }

        private void HandleAI(float dt)
        {
            if (opponent == null || opponent.state == FighterState.KO)
            {
                _horizontalVelocity = 0f;
                if (IsGrounded) SetState(FighterState.Idle);
                _oppPrevAttacking = false;
                _oppPrevHitDealt = false;
                return;
            }

            _aiDecisionTimer -= dt;
            _aiAttackCooldown -= dt;
            if (_aiReactionDelayTimer > 0f) _aiReactionDelayTimer -= dt;

            float dx = opponent.transform.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);
            int dirToOpp = dx >= 0f ? 1 : -1;

            bool oppIsAttacking = opponent.IsAttacking;
            bool oppIsAirborne = !opponent.IsGrounded;
            bool oppIsLow = opponent.state == FighterState.CrouchPunch || opponent.state == FighterState.CrouchKick;
            bool oppIsBlocking = opponent.IsBlocking;
            bool oppInHitstun = opponent.state == FighterState.Hitstun;

            // Detección de ataque fallido (whiff) del oponente:
            // Terminó de atacar sin impactar o sonó el aire sin conectar golpe
            bool oppJustWhiffed = (!oppIsAttacking && _oppPrevAttacking && !_oppPrevHitDealt) ||
                                  (oppIsAttacking && opponent.WhiffSoundPlayed && !opponent.AttackHitDealt);

            // Inicio de tiempo de reacción humano al ver comenzar un ataque rival (sin lectura tramposa de inputs)
            if (oppIsAttacking && !_oppPrevAttacking)
            {
                _aiReactedToAttack = false;
                switch (aiDifficulty)
                {
                    case AIDifficulty.Easy:
                        _aiReactionDelayTimer = Random.Range(0.34f, 0.46f);
                        break;
                    case AIDifficulty.Medium:
                        _aiReactionDelayTimer = Random.Range(0.20f, 0.28f);
                        break;
                    case AIDifficulty.Hard:
                    default:
                        _aiReactionDelayTimer = Random.Range(0.11f, 0.16f);
                        break;
                }
            }
            if (!oppIsAttacking)
            {
                _aiReactedToAttack = false;
            }

            // Ratio de salud de ambos luchadores
            float myHpRatio = maxHealth > 0f ? (currentHealth / maxHealth) : 1f;
            float oppHpRatio = (opponent.maxHealth > 0f) ? (opponent.currentHealth / opponent.maxHealth) : 1f;
            bool amLowHp = myHpRatio < 0.35f;
            bool oppLowHp = oppHpRatio < 0.25f;

            // =========================================================================
            // 1. CASTIGO DE ATAQUES FALLIDOS (WHIFF PUNISH)
            // =========================================================================
            if (oppJustWhiffed && dist <= 1.8f && !IsAttacking && state != FighterState.Hitstun && state != FighterState.KO && _aiAttackCooldown <= 0.10f)
            {
                float whiffPunishChance;
                switch (aiDifficulty)
                {
                    case AIDifficulty.Easy: whiffPunishChance = 0.10f; break;
                    case AIDifficulty.Medium: whiffPunishChance = 0.45f; break;
                    case AIDifficulty.Hard: default: whiffPunishChance = 0.82f; break;
                }

                if (Random.value < whiffPunishChance)
                {
                    _aiBlock = false;
                    _horizontalVelocity = 0f;

                    // En difícil o medio con super cargado al 100%, castiga el whiff con Súper
                    if (currentSuper >= MaxSuper && dist <= GetSpecialReach() &&
                        (aiDifficulty == AIDifficulty.Hard || (aiDifficulty == AIDifficulty.Medium && Random.value < 0.5f)))
                    {
                        currentSuper = 0f;
                        FightMatchManager.Instance?.UpdateHUD();
                        SetState(FighterState.SpecialAttack);
                        _aiAttackCooldown = Random.Range(0.8f, 1.4f);
                        RecordAiAttack(FighterState.SpecialAttack);
                        UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                        return;
                    }

                    // Castigo normal con patada o puño según distancia
                    FighterState punishState = (dist > 1.25f)
                        ? FighterState.StandKick
                        : (Random.value < 0.5f ? FighterState.StandPunch : FighterState.StandKick);

                    SetState(punishState);
                    _aiAttackCooldown = GetAttackCooldownForDifficulty();
                    RecordAiAttack(punishState);
                    UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                    return;
                }
            }

            // =========================================================================
            // 2. ACTIVACIÓN DEL SÚPER / ATAQUE ESPECIAL (100% BARRA)
            // =========================================================================
            if (currentSuper >= MaxSuper && !IsAttacking && state != FighterState.Hitstun && state != FighterState.KO && _aiAttackCooldown <= 0f)
            {
                float specialReach = GetSpecialReach();
                if (dist <= specialReach)
                {
                    bool shouldFireSpecial = false;
                    switch (aiDifficulty)
                    {
                        case AIDifficulty.Easy:
                            // En fácil se activa de forma errática y con baja frecuencia (35%)
                            shouldFireSpecial = Random.value < 0.35f;
                            break;
                        case AIDifficulty.Medium:
                            // En medio aprovecha aperturas o rival en recuperación (65%), raro ante bloqueo sólido
                            if (!oppIsBlocking || oppInHitstun || oppJustWhiffed)
                                shouldFireSpecial = Random.value < 0.65f;
                            else
                                shouldFireSpecial = Random.value < 0.20f;
                            break;
                        case AIDifficulty.Hard:
                        default:
                            // En difícil castiga aperturas y nunca desperdicia el súper contra guardia cerrada
                            if (!oppIsBlocking || oppInHitstun || oppJustWhiffed)
                                shouldFireSpecial = Random.value < 0.85f;
                            else
                                shouldFireSpecial = false;
                            break;
                    }

                    if (shouldFireSpecial)
                    {
                        currentSuper = 0f;
                        FightMatchManager.Instance?.UpdateHUD();
                        _horizontalVelocity = 0f;
                        SetState(FighterState.SpecialAttack);
                        _aiAttackCooldown = (aiDifficulty == AIDifficulty.Hard) ? Random.Range(0.6f, 1.1f) : Random.Range(1.0f, 1.8f);
                        RecordAiAttack(FighterState.SpecialAttack);
                        UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                        return;
                    }
                }
            }

            // =========================================================================
            // 3. DEFENSA REACTIVA (BLOQUEO Y EVASIÓN SEGÚN DIFICULTAD Y TIEMPO DE REACCIÓN)
            // =========================================================================
            if (oppIsAttacking && dist <= 2.2f && !IsAttacking)
            {
                // Solo decide defensa una vez transcurrido su retardo de reacción humano
                if (!_aiReactedToAttack && _aiReactionDelayTimer <= 0f)
                {
                    _aiReactedToAttack = true;

                    float defenseChance;
                    float evadeChance;
                    float correctGuardChance;

                    switch (aiDifficulty)
                    {
                        case AIDifficulty.Easy:
                            defenseChance = 0.35f;      // 35% bloqueo
                            evadeChance = 0.15f;        // 15% evasión
                            correctGuardChance = 0.60f; // 40% error en guardia alta/baja
                            break;
                        case AIDifficulty.Medium:
                            defenseChance = 0.62f;      // 62% bloqueo
                            evadeChance = 0.22f;        // 22% evasión
                            correctGuardChance = 0.80f; // 20% error en guardia
                            break;
                        case AIDifficulty.Hard:
                        default:
                            defenseChance = 0.82f;      // 82% bloqueo (deja 18% aperturas legítimas)
                            evadeChance = 0.25f;        // 25% evasión/retroceso
                            correctGuardChance = 0.92f; // 8% error en guardia
                            break;
                    }

                    float roll = Random.value;
                    if (roll < defenseChance)
                    {
                        _aiBlock = true;
                        bool pickLowGuard = (Random.value < correctGuardChance) ? oppIsLow : !oppIsLow;
                        _aiCrouch = pickLowGuard;
                        _horizontalVelocity = 0f;
                        _aiDecisionTimer = Random.Range(0.24f, 0.38f);

                        if (!IsGrounded) SetState(FighterState.AirBlock);
                        else if (_aiCrouch) SetState(FighterState.CrouchBlock);
                        else SetState(FighterState.StandBlock);

                        UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                        return;
                    }
                    else if (roll < (defenseChance + evadeChance) && IsGrounded)
                    {
                        _aiBlock = false;
                        if (Random.value < 0.50f)
                        {
                            _verticalVelocity = jumpVelocity;
                            _horizontalVelocity = -dirToOpp * (walkSpeed * 0.85f);
                            SetState(FighterState.Jump);
                            _aiDecisionTimer = 0.45f;
                            UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                            return;
                        }
                        else
                        {
                            _aiMoveDir = -dirToOpp;
                            _horizontalVelocity = -dirToOpp * walkSpeed;
                            SetState(FighterState.Walk);
                            _aiDecisionTimer = Random.Range(0.25f, 0.45f);
                            UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                            return;
                        }
                    }
                    else
                    {
                        // Error de decisión / lentitud de reflejos: no se cubre
                        _aiBlock = false;
                    }
                }
            }

            // =========================================================================
            // 4. OFENSIVA Y COMBOS (VARIACIÓN DE GOLPES SIN SPAM REPETITIVO)
            // =========================================================================
            if (_aiAttackCooldown <= 0f && !oppIsAttacking)
            {
                // En el aire (alcance aéreo <= 1.55m)
                if (!IsGrounded && dist <= 1.55f)
                {
                    _aiBlock = false;
                    FighterState airAtk = (Random.value < 0.5f) ? FighterState.AirKick : FighterState.AirPunch;
                    SetState(airAtk);
                    _aiAttackCooldown = GetAttackCooldownForDifficulty();
                    _aiDecisionTimer = 0.35f;
                    RecordAiAttack(airAtk);
                    UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                    return;
                }

                // En el suelo:
                if (IsGrounded)
                {
                    // Rango corto (<= 1.25m): alternancia puño / patada / guardia baja
                    if (dist <= 1.25f)
                    {
                        _aiBlock = false;
                        _horizontalVelocity = 0f;

                        FighterState chosenAtk;
                        float mixupChance = (aiDifficulty == AIDifficulty.Hard) ? 0.75f : ((aiDifficulty == AIDifficulty.Medium) ? 0.50f : 0.20f);

                        // Si el rival se cubre alto, ataca abajo para romper la guardia
                        if (opponent.state == FighterState.StandBlock && Random.value < mixupChance)
                        {
                            chosenAtk = FighterState.CrouchKick;
                        }
                        // Si el rival se cubre agachado, castiga con patada alta o salto
                        else if (opponent.state == FighterState.CrouchBlock && Random.value < mixupChance)
                        {
                            chosenAtk = FighterState.StandKick;
                        }
                        else
                        {
                            chosenAtk = PickDiverseGroundAttack(true);
                        }

                        SetState(chosenAtk);
                        RecordAiAttack(chosenAtk);
                        _aiAttackCooldown = GetAttackCooldownForDifficulty();

                        ApplyPostAttackDisengage(dirToOpp, amLowHp);
                        UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                        return;
                    }
                    // Rango medio de patada (1.25m < dist <= 1.55m)
                    else if (dist <= 1.55f)
                    {
                        _aiBlock = false;
                        _horizontalVelocity = 0f;

                        FighterState chosenAtk;
                        if (opponent.state == FighterState.StandBlock && (aiDifficulty != AIDifficulty.Easy || Random.value < 0.3f))
                        {
                            chosenAtk = FighterState.CrouchKick;
                        }
                        else
                        {
                            chosenAtk = PickDiverseGroundAttack(false); // Solo patadas por alcance
                        }

                        SetState(chosenAtk);
                        RecordAiAttack(chosenAtk);
                        _aiAttackCooldown = GetAttackCooldownForDifficulty();

                        ApplyPostAttackDisengage(dirToOpp, amLowHp);
                        UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                        return;
                    }
                }
            }

            // =========================================================================
            // 5. MOVIMIENTO TÁCTICO, FOOTSIES Y ESPACIAMIENTO
            // =========================================================================
            if (_aiDecisionTimer <= 0f)
            {
                _aiBlock = false;
                _aiCrouch = false;
                _aiRun = false;

                // Reacción antiaérea si el rival salta hacia ella
                if (oppIsAirborne && dist <= 2.2f && IsGrounded)
                {
                    float antiAirChance = (aiDifficulty == AIDifficulty.Hard) ? 0.75f : ((aiDifficulty == AIDifficulty.Medium) ? 0.50f : 0.20f);
                    float roll = Random.value;
                    if (roll < antiAirChance && _aiAttackCooldown <= 0f && dist <= 1.45f)
                    {
                        // Puño antiaéreo
                        _horizontalVelocity = 0f;
                        SetState(FighterState.StandPunch);
                        _aiAttackCooldown = GetAttackCooldownForDifficulty();
                        _aiDecisionTimer = 0.35f;
                        RecordAiAttack(FighterState.StandPunch);
                        UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                        return;
                    }
                    else if (roll < antiAirChance + 0.30f)
                    {
                        // Retroceder para que el salto rival caiga en el vacío (whiff bait)
                        _aiMoveDir = -dirToOpp;
                        _aiDecisionTimer = (aiDifficulty == AIDifficulty.Hard) ? Random.Range(0.20f, 0.35f) : Random.Range(0.35f, 0.55f);
                    }
                    else
                    {
                        // Bloquear el ataque aéreo
                        _aiBlock = true;
                        _aiCrouch = false;
                        _aiDecisionTimer = Random.Range(0.30f, 0.50f);
                    }
                }
                // Si la IA tiene poca vida en modo Difícil: juega a la defensiva y distancia de footsies
                else if (amLowHp && aiDifficulty == AIDifficulty.Hard)
                {
                    _aiDecisionTimer = Random.Range(0.18f, 0.32f);
                    if (dist < 1.45f)
                    {
                        // Retroceder para mantenerse en el límite del alcance de patada
                        _aiMoveDir = -dirToOpp;
                    }
                    else if (dist <= 1.70f)
                    {
                        // Espacio óptimo de baiting: finta o bloqueo defensivo
                        if (Random.value < 0.55f) _aiBlock = true;
                        else _aiCrouch = true;
                        _aiMoveDir = 0;
                    }
                    else
                    {
                        _aiMoveDir = dirToOpp;
                    }
                }
                // Distancia Larga (dist > 2.8m): aproximación controlada
                else if (dist > 2.8f)
                {
                    _aiDecisionTimer = (aiDifficulty == AIDifficulty.Hard) ? Random.Range(0.18f, 0.35f) : ((aiDifficulty == AIDifficulty.Medium) ? Random.Range(0.28f, 0.48f) : Random.Range(0.45f, 0.75f));
                    float r = Random.value;
                    if (r < 0.50f)
                    {
                        _aiMoveDir = dirToOpp;
                        _aiRun = (aiDifficulty != AIDifficulty.Easy) && (Random.value < 0.60f);
                    }
                    else if (r < 0.75f && IsGrounded)
                    {
                        // Salto ofensivo hacia adelante
                        _verticalVelocity = jumpVelocity;
                        _horizontalVelocity = dirToOpp * walkSpeed;
                        SetState(FighterState.Jump);
                        _aiDecisionTimer = 0.50f;
                        UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                        return;
                    }
                    else
                    {
                        // Pausa neutral observadora
                        _aiMoveDir = 0;
                    }
                }
                // Distancia Media / Footsies (1.55m < dist <= 2.8m): fintas, retrocesos y tanteo
                else if (dist > 1.55f)
                {
                    _aiDecisionTimer = (aiDifficulty == AIDifficulty.Hard) ? Random.Range(0.14f, 0.28f) : ((aiDifficulty == AIDifficulty.Medium) ? Random.Range(0.25f, 0.45f) : Random.Range(0.40f, 0.65f));
                    float r = Random.value;
                    if (r < 0.40f)
                    {
                        // Retroceso táctico para generar espacio
                        _aiMoveDir = -dirToOpp;
                    }
                    else if (r < 0.62f && IsGrounded)
                    {
                        // Salto neutral o salto hacia atrás táctico
                        _verticalVelocity = jumpVelocity;
                        float jumpDir = Random.value < 0.60f ? -dirToOpp : 0f;
                        _horizontalVelocity = jumpDir * (walkSpeed * 0.75f);
                        SetState(FighterState.Jump);
                        _aiDecisionTimer = 0.45f;
                        UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                        return;
                    }
                    else if (r < 0.85f)
                    {
                        // Acercamiento mesurado
                        _aiMoveDir = dirToOpp;
                        _aiRun = false;
                    }
                    else
                    {
                        // Finta de agachado
                        _aiCrouch = true;
                        _aiMoveDir = 0;
                    }
                }
                // Distancia Corta (dist <= 1.55m) en cooldown de ataque: no quedarse quieto
                else
                {
                    _aiDecisionTimer = (aiDifficulty == AIDifficulty.Hard) ? Random.Range(0.12f, 0.24f) : ((aiDifficulty == AIDifficulty.Medium) ? Random.Range(0.20f, 0.40f) : Random.Range(0.35f, 0.60f));
                    float r = Random.value;
                    if (r < 0.45f)
                    {
                        _aiMoveDir = -dirToOpp;
                    }
                    else if (r < 0.68f && IsGrounded)
                    {
                        _verticalVelocity = jumpVelocity;
                        _horizontalVelocity = -dirToOpp * walkSpeed;
                        SetState(FighterState.Jump);
                        _aiDecisionTimer = 0.45f;
                        UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);
                        return;
                    }
                    else if (r < 0.88f)
                    {
                        _aiBlock = true;
                        _aiCrouch = Random.value < 0.40f;
                        _aiMoveDir = 0;
                    }
                    else
                    {
                        _aiCrouch = true;
                        _aiMoveDir = 0;
                    }
                }
            }

            // =========================================================================
            // 6. APLICACIÓN DE ESTADOS
            // =========================================================================
            UpdateOpponentTracker(oppIsAttacking, opponent.AttackHitDealt);

            if (_aiBlock)
            {
                if (IsGrounded) _horizontalVelocity = 0f;
                if (!IsGrounded) SetState(FighterState.AirBlock);
                else if (_aiCrouch) SetState(FighterState.CrouchBlock);
                else SetState(FighterState.StandBlock);
                return;
            }

            if (!IsGrounded)
            {
                if (state != FighterState.Jump) SetState(FighterState.Jump);
                return;
            }

            if (_aiCrouch)
            {
                _horizontalVelocity = 0f;
                SetState(FighterState.Crouch);
                return;
            }

            if (_aiMoveDir != 0)
            {
                _horizontalVelocity = _aiMoveDir * (_aiRun ? runSpeed : walkSpeed);
                SetState(_aiRun ? FighterState.Run : FighterState.Walk);
            }
            else
            {
                _horizontalVelocity = 0f;
                SetState(FighterState.Idle);
            }
        }

        private float GetAttackCooldownForDifficulty()
        {
            switch (aiDifficulty)
            {
                case AIDifficulty.Easy:
                    return Random.Range(1.10f, 1.80f);
                case AIDifficulty.Medium:
                    return Random.Range(0.65f, 1.05f);
                case AIDifficulty.Hard:
                default:
                    return Random.Range(0.35f, 0.65f);
            }
        }

        private FighterState PickDiverseGroundAttack(bool allowPunch)
        {
            var candidates = new System.Collections.Generic.List<FighterState>();
            if (allowPunch)
            {
                if (_lastAiAttackState != FighterState.StandPunch || _sameAttackRepeatCount < 1)
                    candidates.Add(FighterState.StandPunch);
                if (_lastAiAttackState != FighterState.CrouchPunch || _sameAttackRepeatCount < 1)
                    candidates.Add(FighterState.CrouchPunch);
            }
            if (_lastAiAttackState != FighterState.StandKick || _sameAttackRepeatCount < 1)
                candidates.Add(FighterState.StandKick);
            if (_lastAiAttackState != FighterState.CrouchKick || _sameAttackRepeatCount < 1)
                candidates.Add(FighterState.CrouchKick);

            if (candidates.Count == 0)
            {
                return allowPunch ? FighterState.StandPunch : FighterState.StandKick;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private void RecordAiAttack(FighterState atk)
        {
            if (_lastAiAttackState == atk)
            {
                _sameAttackRepeatCount++;
            }
            else
            {
                _lastAiAttackState = atk;
                _sameAttackRepeatCount = 1;
            }
        }

        private void ApplyPostAttackDisengage(int dirToOpp, bool amLowHp)
        {
            float roll = Random.value;
            float retreatChance = amLowHp ? 0.65f : ((aiDifficulty == AIDifficulty.Hard) ? 0.45f : 0.35f);

            if (roll < retreatChance)
            {
                _aiMoveDir = -dirToOpp;
                _aiRun = false;
                _aiDecisionTimer = (aiDifficulty == AIDifficulty.Hard) ? Random.Range(0.18f, 0.35f) : Random.Range(0.35f, 0.65f);
            }
            else if (roll < retreatChance + 0.20f)
            {
                _verticalVelocity = jumpVelocity;
                _horizontalVelocity = -dirToOpp * (walkSpeed * 0.85f);
                SetState(FighterState.Jump);
                _aiDecisionTimer = 0.45f;
            }
            else if (roll < retreatChance + 0.40f)
            {
                _aiBlock = true;
                _aiMoveDir = 0;
                _aiDecisionTimer = (aiDifficulty == AIDifficulty.Hard) ? Random.Range(0.16f, 0.30f) : Random.Range(0.30f, 0.50f);
            }
            else
            {
                _aiMoveDir = 0;
                _aiDecisionTimer = (aiDifficulty == AIDifficulty.Hard) ? Random.Range(0.12f, 0.22f) : Random.Range(0.30f, 0.55f);
            }
        }

        private void UpdateOpponentTracker(bool oppIsAttacking, bool oppHitDealt)
        {
            _oppPrevAttacking = oppIsAttacking;
            _oppPrevHitDealt = oppHitDealt;
        }

        private void RegisterDirectionTap(int dir)
        {
            float now = Time.time;
            if (_lastTapDir == dir && (now - _lastTapTime) <= DoubleTapThreshold)
            {
                _isRunning = true;
            }
            _lastTapDir = dir;
            _lastTapTime = now;
        }

        private void UpdateAttack(float dt)
        {
            if (state == FighterState.SpecialAttack)
            {
                UpdateSpecialAttack(dt);
                return;
            }

            bool isKick = state == FighterState.StandKick || state == FighterState.CrouchKick || state == FighterState.AirKick;
            Sprite[] frames = GetFramesForState(state);
            int frameCount = (frames != null && frames.Length > 0) ? frames.Length : (isKick ? 4 : 3);
            float fps = GetFpsForState(state);
            float duration = (float)frameCount / fps;
            float activeStart = duration * 0.25f;
            float activeEnd = duration * 0.70f;

            if (!_attackHitDealt && _stateTimer >= activeStart && _stateTimer <= activeEnd)
            {
                CheckAttackHit(isKick);
            }
            else if (!_attackHitDealt && !_whiffSoundPlayed && _stateTimer > activeEnd)
            {
                _whiffSoundPlayed = true;
                SoundEffectsManager.Instance?.PlayAttackWhiff(isKick);
                SpawnWhiffMotionEffect(isKick);
            }

            if (_stateTimer >= duration)
            {
                SetState(IsGrounded ? FighterState.Idle : FighterState.Jump);
            }
        }


        private void UpdateSpecialAttack(float dt)
        {
            _horizontalVelocity = 0f;
            bool threeHit = IsThreeHitSpecial();
            float stepDamage = threeHit ? 10f : 15f; // Exact 30% of max health divided evenly

            if (threeHit)
            {
                if (_specialHitStep == 0 && _stateTimer >= 0.38f)
                {
                    _specialHitStep = 1;
                    CheckSpecialHit(stepDamage, false);
                }
                else if (_specialHitStep == 1 && _stateTimer >= 0.58f)
                {
                    _specialHitStep = 2;
                    CheckSpecialHit(stepDamage, false);
                }
                else if (_specialHitStep == 2 && _stateTimer >= 0.78f)
                {
                    _specialHitStep = 3;
                    CheckSpecialHit(stepDamage, true);
                }
            }
            else
            {
                if (_specialHitStep == 0 && _stateTimer >= 0.48f)
                {
                    _specialHitStep = 1;
                    CheckSpecialHit(stepDamage, false);
                }
                else if (_specialHitStep == 1 && _stateTimer >= 0.68f)
                {
                    _specialHitStep = 2;
                    CheckSpecialHit(stepDamage, true);
                }
            }

            if (_stateTimer >= 1.0f)
            {
                SetState(IsGrounded ? FighterState.Idle : FighterState.Jump);
            }
        }

        private void CheckSpecialHit(float stepDamage, bool isFinalHit)
        {
            if (opponent == null || opponent.state == FighterState.KO) return;

            float dir = facingRight ? 1f : -1f;
            float reach = GetSpecialReach();
            float dx = opponent.transform.position.x - transform.position.x;
            float forwardDist = dir * dx;

            // If not connected yet, verify reach and facing direction
            if (!_specialConnected)
            {
                if (forwardDist < -0.15f || forwardDist > reach)
                {
                    return; // Whiff! Deals 0 damage
                }

                float oppY = opponent.transform.position.y;
                if (Mathf.Abs(oppY - transform.position.y) > 1.6f)
                {
                    return; // Whiff! Deals 0 damage
                }

                _specialConnected = true;
            }

            SoundEffectsManager.Instance?.PlayPunchHit();
            opponent.ReceiveSpecialHit(stepDamage, dir, isFinalHit);
        }

        public void ReceiveSpecialHit(float damage, float attackerDir, bool isFinalHit)
        {
            if (state == FighterState.KO) return;

            perfectDefenseStreak = 0;
            _isParryStunned = false;
            _currentHitstunDuration = 0.28f;
            FightMatchManager.Instance?.UpdateHUD();

            // Quien recibe un golpe gana 10% de SUPER
            AddSuper(10f);

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            _knockbackVelocity = attackerDir * (isFinalHit ? 7.5f : 1.8f);
            _flashTimer = 0.12f;
            _flashIsBlock = false;
            if (_sr != null) _sr.color = new Color(1f, 0.68f, 0.64f, 1f);

            float stopDur = isFinalHit ? 0.075f : 0.045f;
            ApplyHitStop(stopDur);
            opponent?.ApplyHitStop(stopDur);

            float contactX = (opponent != null)
                ? Mathf.Lerp(transform.position.x, opponent.transform.position.x, 0.45f)
                : (transform.position.x - attackerDir * 0.35f);
            float contactY = transform.position.y + 1.10f;
            SpawnImpactEffect(new Vector3(contactX, contactY, 0f), false, true, false, false, true);

            // Actualizar inmediatamente la barra visible de vida en el HUD
            FightMatchManager.Instance?.UpdateHUD();

            if (currentHealth <= 0f)
            {
                SetState(FighterState.KO);
            }
            else
            {
                if (state == FighterState.Hitstun)
                {
                    _stateTimer = 0f;
                    _animTimer = 0f;
                    _animFrame = 0;
                    _attackHitDealt = false;
            _whiffSoundPlayed = false;
                    float ratio = GetHitstunScaleRatio();
                    transform.localScale = new Vector3(_baseVisualScale * ratio, _baseVisualScale * ratio, 1f);
                }
                else
                {
                    SetState(FighterState.Hitstun);
                }
            }
        }

        private void CheckAttackHit(bool isKick)
        {
            if (opponent == null || opponent.state == FighterState.KO) return;

            float dir = facingRight ? 1f : -1f;
            float reach = isKick ? 1.55f : 1.30f;
            // Valores de daño estrictamente equitativos para jugador e IA: 10f puño, 12f patada
            float damage = isKick ? 12f : 10f;
            float hitHeightOffset = (state == FighterState.CrouchPunch || state == FighterState.CrouchKick) ? 0.55f : 1.15f;

            // Verificación direccional estricta: el rival debe estar frente al atacante (evita daño fantasma por la espalda)
            float dx = opponent.transform.position.x - transform.position.x;
            float forwardDist = dir * dx;
            if (forwardDist < -0.10f)
            {
                return;
            }

            Vector2 attackCenter = new Vector2(transform.position.x + dir * (reach * 0.55f), transform.position.y + hitHeightOffset);
            Vector2 attackHalfExtents = new Vector2(reach * 0.5f, 0.70f);

            float oppHeight = (opponent.state == FighterState.Crouch || opponent.state == FighterState.CrouchBlock ||
                               opponent.state == FighterState.CrouchPunch || opponent.state == FighterState.CrouchKick)
                              ? 1.15f : 1.85f;
            Vector2 oppCenter = new Vector2(opponent.transform.position.x, opponent.transform.position.y + oppHeight * 0.5f);
            Vector2 oppHalfExtents = new Vector2(0.55f, oppHeight * 0.5f);

            bool overlapX = Mathf.Abs(attackCenter.x - oppCenter.x) <= (attackHalfExtents.x + oppHalfExtents.x);
            bool overlapY = Mathf.Abs(attackCenter.y - oppCenter.y) <= (attackHalfExtents.y + oppHalfExtents.y);

            if (overlapX && overlapY)
            {
                _attackHitDealt = true;
                opponent.ReceiveHit(damage, dir, state);
            }
        }

        public void ReceiveHit(float damage, float attackerDir, FighterState attackType)
        {
            if (state == FighterState.KO) return;

            bool isSpecial = (attackType == FighterState.SpecialAttack);
            bool blocked = false;
            if (IsBlocking && !isSpecial)
            {
                // Low attacks require crouch block; air attacks require stand or air block
                bool isLowAttack = attackType == FighterState.CrouchKick || attackType == FighterState.CrouchPunch;
                bool isAirAttack = attackType == FighterState.AirPunch || attackType == FighterState.AirKick;

                if (isLowAttack && state == FighterState.CrouchBlock) blocked = true;
                else if (isAirAttack && (state == FighterState.StandBlock || state == FighterState.AirBlock)) blocked = true;
                else if (!isLowAttack && !isAirAttack) blocked = true;
            }

            bool isKick = (attackType == FighterState.StandKick || attackType == FighterState.CrouchKick || attackType == FighterState.AirKick);

            float hitHeight = (attackType == FighterState.CrouchPunch || attackType == FighterState.CrouchKick ||
                               state == FighterState.Crouch || state == FighterState.CrouchBlock) ? 0.68f : 1.12f;
            float contactX = (opponent != null)
                ? Mathf.Lerp(transform.position.x, opponent.transform.position.x, 0.45f)
                : (transform.position.x - attackerDir * 0.35f);
            Vector3 impactPos = new Vector3(contactX, transform.position.y + hitHeight, 0f);

            if (blocked)
            {
                bool isPerfectDefense = (_currentBlockDuration <= PerfectDefenseWindow);

                if (isPerfectDefense)
                {
                    // ==========================================
                    // DEFENSA PERFECTA (PARRY)
                    // ==========================================
                    perfectDefenseStreak++;
                    // "Una defensa perfecta válida reinicia el contador del rival"
                    if (opponent != null) opponent.perfectDefenseStreak = 0;

                    // Sonido metálico nítido y distintivo
                    SoundEffectsManager.Instance?.PlayPerfectDefense();

                    // Quien bloquea perfecto gana 2% de Super
                    AddSuper(2f);

                    // Firme en el suelo: retroceso mínimo (0.6f en vez de 3.2f)
                    _knockbackVelocity = attackerDir * 0.6f;
                    _flashTimer = 0.14f;
                    _flashIsBlock = true;
                    if (_sr != null) _sr.color = new Color(0.40f, 1f, 1f, 1f); // Destello cian eléctrico

                    // Breve hitstop para defensor, mayor freeze para atacante para dar ventana de respuesta
                    float blockStop = 0.030f;
                    ApplyHitStop(blockStop);
                    opponent?.ApplyHitStop(0.065f);
                    SpawnImpactEffect(impactPos, true, true, true);

                    // Si se alcanzan 3 defensas perfectas consecutivas, aturdir al rival
                    if (perfectDefenseStreak >= MaxPerfectDefenseStreak)
                    {
                        perfectDefenseStreak = 0;
                        opponent?.ApplyParryStun(ParryStunDuration);
                    }

                    FightMatchManager.Instance?.UpdateHUD();
                }
                else
                {
                    // ==========================================
                    // BLOQUEO NORMAL (Llegó tarde / ya venía bloqueando > 0.15s)
                    // ==========================================
                    // "Si una defensa falla, llega tarde, recibe daño o deja de bloquear antes del impacto, reinicia la cadena."
                    perfectDefenseStreak = 0;

                    if (isKick) SoundEffectsManager.Instance?.PlayKickHit();
                    else SoundEffectsManager.Instance?.PlayPunchHit();

                    AddSuper(2f);
                    _knockbackVelocity = attackerDir * 3.2f;
                    _flashTimer = 0.11f;
                    _flashIsBlock = true;
                    if (_sr != null) _sr.color = new Color(0.82f, 0.95f, 1f, 1f);

                    float blockStop = 0.032f;
                    ApplyHitStop(blockStop);
                    opponent?.ApplyHitStop(blockStop);
                    SpawnImpactEffect(impactPos, true, false, false);

                    FightMatchManager.Instance?.UpdateHUD();
                }
            }
            else
            {
                // ==========================================
                // GOLPE RECIBIDO (Defensa falló o rompió guardia)
                // ==========================================
                perfectDefenseStreak = 0; // Reinicia cadena al recibir daño

                // Si estaba en stun de parry y recibe un contraataque, limpiar flag de parry stun y volver a duración estándar
                _isParryStunned = false;
                _currentHitstunDuration = 0.28f;

                if (isKick) SoundEffectsManager.Instance?.PlayKickHit();
                else SoundEffectsManager.Instance?.PlayPunchHit();

                // El atacante gana 5% por cada golpe que conecta (no durante ataque especial)
                if (!isSpecial)
                {
                    opponent?.AddSuper(5f);
                }

                // Quien recibe un golpe gana 10%
                AddSuper(10f);

                currentHealth = Mathf.Max(0f, currentHealth - damage);
                _knockbackVelocity = attackerDir * (isSpecial ? 7.5f : 6.5f);
                _flashTimer = 0.12f;
                _flashIsBlock = false;
                if (_sr != null) _sr.color = new Color(1f, 0.68f, 0.64f, 1f);

                float hitStop = isSpecial ? 0.075f : (isKick ? 0.052f : 0.040f);
                ApplyHitStop(hitStop);
                opponent?.ApplyHitStop(hitStop);
                SpawnImpactEffect(impactPos, false, isSpecial || isKick, false, isKick, isSpecial);

                // Actualizar inmediatamente la barra visible de vida en el HUD
                FightMatchManager.Instance?.UpdateHUD();

                if (currentHealth <= 0f)
                {
                    SetState(FighterState.KO);
                }
                else
                {
                    if (state == FighterState.Hitstun)
                    {
                        // Si ya estaba en hitstun, reiniciar timer y animación de daño para registrar el nuevo impacto
                        _stateTimer = 0f;
                        _animTimer = 0f;
                        _animFrame = 0;
                        _attackHitDealt = false;
            _whiffSoundPlayed = false;
                        float ratio = GetHitstunScaleRatio();
                        transform.localScale = new Vector3(_baseVisualScale * ratio, _baseVisualScale * ratio, 1f);
                    }
                    else
                    {
                        SetState(FighterState.Hitstun);
                    }
                }
            }
        }

        /// <summary>
        /// Aplica aturdimiento de defensa perfecta (parry stun): 0.8 s usando la animación existente de daño.
        /// No inflige daño ni suma SUPER por daño recibido. El rival queda indefenso permitiendo el contraataque.
        /// </summary>
        public void ApplyParryStun(float duration = ParryStunDuration)
        {
            if (state == FighterState.KO) return;

            _isParryStunned = true;
            _currentHitstunDuration = duration;
            _stateTimer = 0f;
            _animTimer = 0f;
            _animFrame = 0;
            _attackHitDealt = false;
            _whiffSoundPlayed = false;
            _horizontalVelocity = 0f;
            _verticalVelocity = 0f;
            _knockbackVelocity = 0f;

            // El stun no causa daño ni suma SUPER por daño recibido
            SetState(FighterState.Hitstun);

            _flashTimer = 0.22f;
            _flashIsBlock = false;
            if (_sr != null) _sr.color = new Color(1f, 0.90f, 0.40f, 1f); // Destello dorado de aturdimiento

            SoundEffectsManager.Instance?.PlayParryStun();
            FightMatchManager.Instance?.UpdateHUD();
        }

        private void RestoreSpriteColor()
        {
            if (_sr != null && state != FighterState.Hitstun)
            {
                _sr.color = Color.white;
                float r = GetNormalizedScaleRatio(state);
                transform.localScale = new Vector3(_baseVisualScale * r, _baseVisualScale * r, 1f);
            }
        }

        private void ApplyPhysics(float dt)
        {
            Vector3 pos = transform.position;

            // Horizontal movement + knockback decay
            float totalVelX = _horizontalVelocity + _knockbackVelocity;
            pos.x += totalVelX * dt;
            _knockbackVelocity = Mathf.MoveTowards(_knockbackVelocity, 0f, 18f * dt);

            // Vertical movement & gravity
            if (pos.y > groundY || _verticalVelocity > 0f)
            {
                pos.y += _verticalVelocity * dt;
                _verticalVelocity -= gravity * dt;
                if (pos.y <= groundY)
                {
                    pos.y = groundY;
                    _verticalVelocity = 0f;
                    if (state == FighterState.Jump || state == FighterState.AirPunch ||
                        state == FighterState.AirKick || state == FighterState.AirBlock)
                    {
                        SetState(FighterState.Idle);
                    }
                }
            }
            else
            {
                pos.y = groundY;
                _verticalVelocity = 0f;
            }

            // Pushbox collision so fighters don't overlap on the same spot or cross each other on the ground
            if (opponent != null)
            {
                float minSeparation = 0.90f;
                float dx = pos.x - opponent.transform.position.x;
                float absDx = Mathf.Abs(dx);
                float dy = Mathf.Abs(pos.y - opponent.transform.position.y);

                // Only block horizontally when both are near ground level (allow jump cross-ups when high in air)
                if (dy < 0.90f && absDx < minSeparation)
                {
                    float pushDir = dx != 0f ? Mathf.Sign(dx) : (playerIndex == 1 ? -1f : 1f);
                    float overlap = minSeparation - absDx;

                    // If opponent is backed against the arena wall, this fighter absorbs 100% of the separation
                    if (opponent.transform.position.x <= minArenaX + 0.04f && pushDir < 0f)
                    {
                        pos.x = opponent.transform.position.x + minSeparation;
                    }
                    else if (opponent.transform.position.x >= maxArenaX - 0.04f && pushDir > 0f)
                    {
                        pos.x = opponent.transform.position.x - minSeparation;
                    }
                    else
                    {
                        pos.x += pushDir * overlap;
                    }
                }
            }

            // Clamp within arena boundaries
            pos.x = Mathf.Clamp(pos.x, minArenaX, maxArenaX);
            transform.position = pos;
        }

        public void SetState(FighterState newState)
        {
            if (state == newState) return;

            // Reproducir sonido de salto una única vez al despegar del suelo (no al volver a Jump tras un golpe o bloqueo aéreo)
            bool wasInAirState = (state == FighterState.Jump || state == FighterState.AirPunch ||
                                  state == FighterState.AirKick || state == FighterState.AirBlock);
            if (newState == FighterState.Jump && !wasInAirState && _verticalVelocity > 0f)
            {
                SoundEffectsManager.Instance?.PlayCharacterJump(fighterName, VocalType, jumpClip);
            }
            else if (newState == FighterState.SpecialAttack && state != FighterState.SpecialAttack)
            {
                SoundEffectsManager.Instance?.PlayCharacterSpecialAttack(fighterName, specialAttackClip);
                SpawnSpecialActivationFlash();
            }

            state = newState;
            _stateTimer = 0f;
            _animTimer = 0f;
            _animFrame = 0;
            _attackHitDealt = false;
            _whiffSoundPlayed = false;
            _lastFootstepFrame = -1;
            _specialHitStep = 0;
            _specialConnected = false;

            float ratio = GetNormalizedScaleRatio(state);
            transform.localScale = new Vector3(_baseVisualScale * ratio, _baseVisualScale * ratio, 1f);
            if (newState != FighterState.Hitstun)
            {
                _hitStopTimer = 0f;
            }
            if (state != FighterState.Hitstun && _flashTimer <= 0f && _sr != null)
            {
                _sr.color = Color.white;
            }
        }

        private void UpdateSpriteAnimation(float dt)
        {
            Sprite[] frames = GetFramesForState(state);
            if (frames == null || frames.Length == 0) return;

            float fps = GetFpsForState(state);

            if (state == FighterState.Walk)
            {
                // Alternar piernas claramente en el ciclo. Al retroceder, reproducir en reversa para no moonwalkear ni cruzar piernas
                bool movingBackward = (facingRight && _horizontalVelocity < -0.05f) || (!facingRight && _horizontalVelocity > 0.05f);
                int frameIndex = Mathf.FloorToInt(_animTimer * fps);
                int baseIndex = frameIndex % frames.Length;
                _animFrame = movingBackward ? ((frames.Length - 1) - baseIndex) : baseIndex;
            }
            else if (state == FighterState.Run)
            {
                int frameIndex = Mathf.FloorToInt(_animTimer * fps);
                _animFrame = frameIndex % frames.Length;
            }
            else if (state == FighterState.Idle)
            {
                int frameIndex = Mathf.FloorToInt(_animTimer * fps);
                _animFrame = frameIndex % frames.Length;
            }
            else if (state == FighterState.Crouch)
            {
                // Transicion al agacharse (C00..C01) y mantener la postura baja sin saltar a cuadros de pararse
                if (frames.Length >= 4)
                {
                    int rawFrame = Mathf.FloorToInt(_animTimer * fps);
                    if (rawFrame <= 1)
                    {
                        _animFrame = rawFrame;
                    }
                    else
                    {
                        int crouchLoop = (rawFrame - 2) % 2;
                        _animFrame = Mathf.Clamp(2 + crouchLoop, 0, Mathf.Min(3, frames.Length - 1));
                    }
                }
                else
                {
                    _animFrame = Mathf.Clamp(Mathf.FloorToInt(_animTimer * fps), 0, frames.Length - 1);
                }
            }
            else if (state == FighterState.Jump)
            {
                // Sincronizar cuadros de salto con la fisica vertical para no flotar ni caminar en el aire
                if (frames.Length >= 5)
                {
                    if (_verticalVelocity > 4.5f)
                    {
                        _animFrame = 1; // Impulso ascendente
                    }
                    else if (_verticalVelocity > 1.0f)
                    {
                        _animFrame = Mathf.Min(2, frames.Length - 1); // Ascenso rapido
                    }
                    else if (_verticalVelocity >= -2.5f)
                    {
                        _animFrame = Mathf.Min(3, frames.Length - 1); // Vertice / apice
                    }
                    else
                    {
                        _animFrame = Mathf.Min(4, frames.Length - 1); // Caida
                    }
                }
                else
                {
                    _animFrame = Mathf.Clamp(Mathf.FloorToInt(_animTimer * fps), 0, frames.Length - 1);
                }
            }
            else if (state == FighterState.Hitstun)
            {
                // 4 cuadros: 0-1 reaccion al impacto, 2-3 recuperacion progresiva
                float hitstunDur = (_currentHitstunDuration > 0f) ? _currentHitstunDuration : 0.28f;
                if (frames.Length >= 4)
                {
                    float progress = Mathf.Clamp01(_stateTimer / hitstunDur);
                    _animFrame = Mathf.Clamp(Mathf.FloorToInt(progress * 4f), 0, 3);
                }
                else
                {
                    _animFrame = Mathf.Clamp(Mathf.FloorToInt(_animTimer * fps), 0, frames.Length - 1);
                }
            }
            else if (state == FighterState.KO)
            {
                // Al caer K.O., usar solo los cuadros de descenso (0..2) de la fila R02 y mantener la postura baja sin levantarse
                int rawFrame = Mathf.FloorToInt(_animTimer * fps);
                int maxDownFrame = Mathf.Min(2, frames.Length - 1);
                _animFrame = Mathf.Clamp(rawFrame, 0, maxDownFrame);
            }
            else
            {
                int frameIndex = Mathf.FloorToInt(_animTimer * fps);
                _animFrame = Mathf.Min(frameIndex, frames.Length - 1);
            }

            float animRatio = GetNormalizedScaleRatio(state);
            transform.localScale = new Vector3(_baseVisualScale * animRatio, _baseVisualScale * animRatio, 1f);
            _sr.sprite = frames[_animFrame];

            // Pasos muy sutiles sincronizados con el contacto de los pies
            if (IsGrounded && (state == FighterState.Walk || state == FighterState.Run))
            {
                if ((_animFrame == 1 || _animFrame == 5) && _lastFootstepFrame != _animFrame)
                {
                    _lastFootstepFrame = _animFrame;
                    SoundEffectsManager.Instance?.PlayFootstep(state == FighterState.Run ? 0.26f : 0.18f);
                }
                else if (_animFrame != 1 && _animFrame != 5)
                {
                    _lastFootstepFrame = -1;
                }
            }
            else
            {
                _lastFootstepFrame = -1;
            }
        }

        private Sprite[] GetFramesForState(FighterState st)
        {
            switch (st)
            {
                case FighterState.Idle: return anims.idle;
                case FighterState.Walk: return anims.walk;
                case FighterState.Run: return anims.run;
                case FighterState.Crouch: return anims.crouch;
                case FighterState.Jump: return anims.jump;
                case FighterState.StandPunch: return anims.standPunch;
                case FighterState.StandKick: return anims.standKick;
                case FighterState.CrouchPunch: return anims.crouchPunch;
                case FighterState.CrouchKick: return anims.crouchKick;
                case FighterState.AirPunch: return anims.airPunch;
                case FighterState.AirKick: return anims.airKick;
                case FighterState.StandBlock: return anims.standBlock;
                case FighterState.CrouchBlock: return anims.crouchBlock;
                case FighterState.AirBlock: return anims.airBlock;
                case FighterState.SpecialAttack:
                    if (anims != null && anims.specialAttack != null && anims.specialAttack.Length > 0)
                        return anims.specialAttack;
                    return anims.standPunch;
                case FighterState.Hitstun:
                    if (anims != null && anims.hurt != null && anims.hurt.Length > 0)
                        return anims.hurt;
                    return anims.standBlock;
                case FighterState.KO: return anims.crouch;
                default: return anims.idle;
            }
        }

        private float GetFpsForState(FighterState st)
        {
            switch (st)
            {
                case FighterState.Idle: return 8f;
                case FighterState.Walk: return 10f;
                case FighterState.Run: return 14f;
                case FighterState.Hitstun: return 14f;
                case FighterState.StandPunch:
                case FighterState.CrouchPunch:
                case FighterState.AirPunch: return 18f;
                case FighterState.StandKick:
                case FighterState.CrouchKick:
                case FighterState.AirKick: return 16f;
                case FighterState.SpecialAttack: return 10f;
                default: return 12f;
            }
        }
    }
}
