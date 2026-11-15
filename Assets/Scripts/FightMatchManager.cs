using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.UI;

using UnityEngine.InputSystem;

using UnityEngine.Rendering.Universal;

namespace Fightyub

{

    [System.Serializable]

    public class CharacterRosterEntry

    {

        public string characterName;

        public float visualScale = 1.9f;

        public FighterAnimationSet anims = new FighterAnimationSet();

    }

    [System.Serializable]

    public class StageEntry

    {

        public string stageName;

        public string subtitle;

        public GameObject stageRoot;

        public Sprite previewSprite;

        public float groundY = -3.00f;

        public AudioClip stageBgmClip;

    }

    public class FightMatchManager : MonoBehaviour

    {

        public static FightMatchManager Instance { get; private set; }

        private string _originalConfirmCharButtonText;
        private bool _suppressUiSfx = false;

        private void Awake()

        {

            Instance = this;

        }

        [Header("Fighters")]

        public FighterController player1;

        public FighterController player2;

        public FighterController jumaayFighter;

        public FighterController tatoonFighter;

        public Vector3 p1StartPos = new Vector3(-2.6f, -3.00f, 0f);

        public Vector3 p2StartPos = new Vector3(2.6f, -3.00f, 0f);

        [Header("Roster (11 Playable Characters) & Stages (5 Scenarios)")]

        public CharacterRosterEntry[] roster = new CharacterRosterEntry[0];

        public StageEntry[] stages = new StageEntry[0];

        public int selectedCharIndex = 0;

        public int selectedRivalIndex = 1;

        public int selectedStageIndex = 0;

        [Header("Background Music")]
        public AudioSource bgmSource;
        public AudioClip obeliscoBgmClip;
        public AudioClip triunviratoBgmClip;
        public AudioClip casaRosadaBgmClip;
        public AudioClip puenteMujerBgmClip;
        public AudioClip floralisBgmClip;
        public AudioClip menuBgmClip;

        [Header("Mode & State Flags")]

        public bool isVSMode = false;

        public bool isTowerMode = false;

        public bool inMainMenu = true;

        public bool inSideSelect = false;

        public bool inCharSelect = false;

        public bool inStageSelect = false;

        public bool inTowerScreen = false;

        public bool isPaused = false;

        public bool isTowerScrolling = false;

        public int currentTowerFloor = 0; // 0 to 6

        public int[] towerRivalIndices = new int[7];

        public bool[] towerFloorsDefeated = new bool[7];

        [Header("Camera Tracking")]

        public Camera mainCamera;

        public float minCamX = -4.2f;

        public float maxCamX = 4.2f;

        public float camSmoothSpeed = 7f;

        public float manualCamPanOffset = 0f;

        [Header("Match & Round Rules (Best of 3)")]

        public float roundDuration = 99f;

        public float currentTimer = 99f;

        public int currentRound = 1;

        public int p1RoundWins = 0;

        public int p2RoundWins = 0;

        public const int RoundsToWin = 2;

        public bool roundTransitionActive;

        public bool matchEnded;

        private float _roundTransitionTimer;

        [Header("HUD References")]

        public Image p1HealthFill;

        public Image p2HealthFill;

        public Image p1PortraitImage;

        public Image p2PortraitImage;

        public Text p1NameText;

        public Text p2NameText;

        public Text timerText;

        public Text roundInfoText;

        public GameObject resultPanel;

        public Text resultTitleText;

        public Button restartButton;

        public Button returnToMenuButton;

        public GameObject controlsOverlay;

        [Header("Super Meter UI")]

        public GameObject superMeterRoot;

        public Image p1SuperFill;

        public Image p2SuperFill;

        public Text p1SuperText;

        public Text p2SuperText;

        [Header("Perfect Defense (Parry) HUD")]
        public GameObject p1ParryTrackerRoot;
        public Image[] p1ParryPips = new Image[3];
        public Text p1ParryCounterText;
        public GameObject p2ParryTrackerRoot;
        public Image[] p2ParryPips = new Image[3];
        public Text p2ParryCounterText;

        [Header("Main Menu UI")]

        public GameObject mainMenuPanel;

        public Button mainMenuFightButton;

        public Button mainMenuTowerButton;

        public Button mainMenuVsButton;

        public Button mainMenuExitButton;

        public int mainMenuSelectedIndex = 0;

        public OptionsManager optionsManager;

        [Header("Side Select UI (VS Mode)")]

        public GameObject sideSelectPanel;

        public RectTransform p1SideTokenRect;

        public RectTransform p2SideTokenRect;

        public Image p1SideDeviceIcon;

        public Image p2SideDeviceIcon;

        public Text p1SideDeviceNameText;

        public Text p2SideDeviceNameText;

        public Text p1SideControlsPromptText;

        public Text p2SideControlsPromptText;

        public Text p1SideStatusText;

        public Text p2SideStatusText;

        public Image p1SideHighlight;

        public Image p2SideHighlight;

        public Button sideSelectBackButton;

        public Button sideSelectContinueButton;

        // Side slots: -1 = Left (1P side), 0 = Center (unassigned), 1 = Right (2P side)

        public int p1SideSlot = 0;

        public int p2SideSlot = 0;

        public bool p1SideConfirmed = false;

        public bool p2SideConfirmed = false;

        public int leftSidePlayerIndex = 1;  // which player (1 or 2) is on the left

        public int rightSidePlayerIndex = 2; // which player (1 or 2) is on the right

        [Header("Character Select UI (11 Characters)")]

        public GameObject charSelectPanel;

        public Button[] charSelectButtons = new Button[0];

        public Image[] charSelectHighlights = new Image[0];

        public Image[] charSelectP2Highlights = new Image[0];

        public Text[] charSelectP1P2Tags = new Text[0];

        public Image selectedCharPreviewImage;

        public Text selectedCharNameText;

        public Button confirmCharButton;

        public Button charSelectBackButton;

        // VS Mode character selection dual preview

        public GameObject vsCharPreviewContainer;

        public Image vsLeftCharPreviewImage;

        public Text vsLeftCharNameText;

        public Text vsLeftCharStatusText;

        public Text vsLeftPlayerHeader;

        public Image vsRightCharPreviewImage;

        public Text vsRightCharNameText;

        public Text vsRightCharStatusText;

        public Text vsRightPlayerHeader;

        public int vsLeftCharIndex = 0;

        public int vsRightCharIndex = 1;

        public bool vsLeftCharConfirmed = false;

        public bool vsRightCharConfirmed = false;

        // Legacy fields kept for compatibility

        public Button selectJumaayButton;

        public Button selectTatoonButton;

        public Image jumaaySelectHighlight;

        public Image tatoonSelectHighlight;

        public Text jumaayRoleLabel;

        public Text tatoonRoleLabel;

        [Header("Stage Select UI (5 Scenarios)")]

        public GameObject stageSelectPanel;

        public Button[] stageSelectButtons = new Button[0];

        public Image[] stageSelectHighlights = new Image[0];

        public Text selectedStageNameText;

        public Text stageMatchupPreviewText;

        public Button confirmStageButton;

        public Button stageSelectBackButton;

        [Header("Tower Mode UI")]

        public GameObject towerPanel;

        public RectTransform towerScrollViewport;

        public RectTransform towerContainer;

        public RectTransform towerFondoRect;

        public Image towerImage;

        public Image towerFondoImage;

        public GameObject[] towerFloorSlots = new GameObject[7];

        public Image[] towerFloorPortraits = new Image[7];

        public Text[] towerFloorNames = new Text[7];

        public GameObject[] towerFloorDefeatedBadges = new GameObject[7];

        public GameObject[] towerFloorActiveMarkers = new GameObject[7];

        public Text towerFooterFloorText;

        public Button towerStartFightButton;

        public Button towerBackToMenuButton;

        [Header("Tower Victory Modal")]

        public GameObject towerVictoryModal;

        public Text towerVictoryTitleText;

        public Button towerVictoryMenuButton;

        [Header("Pause Menu UI")]
        public GameObject pausePanel;
        public Button pauseResumeButton;
        public Button pauseRematchButton;
        public Button pauseExitMenuButton;
        public int pauseSelectedIndex = 0;

        private void Start()

        {

            if (mainCamera == null) mainCamera = Camera.main;

            if (player1 == null)

            {

                var go = GameObject.Find("P1_Jumaay");

                if (go != null) player1 = go.GetComponent<FighterController>();

            }

            if (player2 == null)

            {

                var go = GameObject.Find("P2_Tatoon");

                if (go != null) player2 = go.GetComponent<FighterController>();

            }

            if (jumaayFighter == null) jumaayFighter = player1;

            if (tatoonFighter == null) tatoonFighter = player2;

            if (mainMenuTowerButton == null)

            {

                var towerGo = GameObject.Find("HUD_Canvas/MainMenu_Panel/Button_Torre");

                if (towerGo != null) mainMenuTowerButton = towerGo.GetComponent<Button>();

            }

            if (mainMenuVsButton == null)

            {

                var vsGo = GameObject.Find("HUD_Canvas/MainMenu_Panel/Button_VS");

                if (vsGo != null) mainMenuVsButton = vsGo.GetComponent<Button>();

            }
            if (mainMenuExitButton == null)
            {
                var exitGo = GameObject.Find("HUD_Canvas/MainMenu_Panel/Button_Salir");
                if (exitGo != null) mainMenuExitButton = exitGo.GetComponent<Button>();
            }

            if (mainMenuVsButton != null)

            {

                mainMenuVsButton.interactable = true;

            }

            if (confirmCharButton != null)

            {

                var txt = confirmCharButton.GetComponentInChildren<Text>();

                if (txt != null)

                {

                    _originalConfirmCharButtonText = txt.text;

                }

            }

            WireUIButtons();

            if (ControlBindingsManager.Instance != null)

            {

                ControlBindingsManager.Instance.OnBindingsOrDevicesChanged += OnDevicesOrBindingsUpdated;

            }

            if (roster != null)

            {

                foreach (var entry in roster)

                {

                    if (entry != null && entry.anims != null)

                    {

                        FighterController.EnsureSpecialAttackAnimation(entry.characterName, entry.anims);

                    }

                }

            }

            _suppressUiSfx = true;
            EnsureBgmSource();
            selectedRivalIndex = PickRandomRivalIndex(selectedCharIndex);

            ApplyStageSelection(selectedStageIndex);

            ApplyCharacterSelectionToFighters();

            ResetRoundState();

            if (mainMenuPanel != null)
            {
                ShowMainMenu();
                _suppressUiSfx = false;
                string[] cmdArgs = System.Environment.GetCommandLineArgs();
                if (System.Array.IndexOf(cmdArgs, "-test-quit-button") >= 0)
                {
                    mainMenuSelectedIndex = 4;
                    ActivateMainMenuSelection();
                }
            }
            else
            {
                inMainMenu = false;
                inSideSelect = false;
                inCharSelect = false;
                inStageSelect = false;
                SetFightersActiveForCombat(true);
                _suppressUiSfx = false;
            }

        }

        private void OnDestroy()

        {

            if (ControlBindingsManager.Instance != null)

            {

                ControlBindingsManager.Instance.OnBindingsOrDevicesChanged -= OnDevicesOrBindingsUpdated;

            }

            OptionsManager.OnAudioVolumesChanged -= HandleAudioVolumesChanged;

        }

        public const float BgmRelativeVolume = 0.10f; // 10% del nivel nominal, asegurando que la música acompañe claramente detrás del combate sin competir con golpes, voces ni efectos

        public static float GetScaledBgmVolume(float rawMusicVolume)
        {
            return Mathf.Clamp01(rawMusicVolume) * BgmRelativeVolume;
        }

        public void EnsureBgmSource()
        {
            if (bgmSource == null)
            {
                var child = transform.Find("BGM_AudioSource");
                if (child != null)
                {
                    bgmSource = child.GetComponent<AudioSource>();
                }
                else
                {
                    var go = new GameObject("BGM_AudioSource");
                    go.transform.SetParent(transform);
                    bgmSource = go.AddComponent<AudioSource>();
                }
            }

            if (bgmSource != null)
            {
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
                bgmSource.spatialBlend = 0f;
                bgmSource.volume = GetScaledBgmVolume(OptionsManager.SavedMusicVolume);
            }

            if (obeliscoBgmClip == null)
            {
#if UNITY_EDITOR
                obeliscoBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/bgm_obelisco_tango.wav");
#endif
            }

            if (casaRosadaBgmClip == null)
            {
#if UNITY_EDITOR
                casaRosadaBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/bgm_casarosada_cumbia.wav");
#endif
            }

            if (triunviratoBgmClip == null)
            {
#if UNITY_EDITOR
                triunviratoBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/bgm_triunvirato_progmetal.wav");
#endif
            }

            if (puenteMujerBgmClip == null)
            {
#if UNITY_EDITOR
                puenteMujerBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/bgm_puentemujer_electronic.wav");
#endif
            }

            if (floralisBgmClip == null)
            {
#if UNITY_EDITOR
                floralisBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/bgm_floralis_rock.wav");
#endif
            }

            if (menuBgmClip == null)
            {
#if UNITY_EDITOR
                menuBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/bgm_menu_arcade.wav");
#endif
            }

            if (stages != null)
            {
                if (stages.Length > 0 && stages[0] != null && stages[0].stageBgmClip == null && obeliscoBgmClip != null)
                {
                    stages[0].stageBgmClip = obeliscoBgmClip;
                }
                if (stages.Length > 1 && stages[1] != null && stages[1].stageBgmClip == null && casaRosadaBgmClip != null)
                {
                    stages[1].stageBgmClip = casaRosadaBgmClip;
                }
                if (stages.Length > 2 && stages[2] != null && stages[2].stageBgmClip == null && puenteMujerBgmClip != null)
                {
                    stages[2].stageBgmClip = puenteMujerBgmClip;
                }
                if (stages.Length > 3 && stages[3] != null && stages[3].stageBgmClip == null && triunviratoBgmClip != null)
                {
                    stages[3].stageBgmClip = triunviratoBgmClip;
                }
                if (stages.Length > 4 && stages[4] != null && stages[4].stageBgmClip == null && floralisBgmClip != null)
                {
                    stages[4].stageBgmClip = floralisBgmClip;
                }
            }

            OptionsManager.OnAudioVolumesChanged -= HandleAudioVolumesChanged;
            OptionsManager.OnAudioVolumesChanged += HandleAudioVolumesChanged;
        }

        private void HandleAudioVolumesChanged(float musicVol, float sfxVol)
        {
            if (bgmSource != null)
            {
                bgmSource.volume = GetScaledBgmVolume(musicVol);
            }
        }

        public void PlayStageMusic(AudioClip clip)
        {
            EnsureBgmSource();
            if (bgmSource == null || clip == null) return;

            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                bgmSource.loop = true;
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.volume = GetScaledBgmVolume(OptionsManager.SavedMusicVolume);
            bgmSource.time = 0f;
            bgmSource.Play();
        }

        public void PlayMenuMusic()
        {
            EnsureBgmSource();
            if (bgmSource == null) return;

            if (menuBgmClip == null)
            {
#if UNITY_EDITOR
                menuBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/bgm_menu_arcade.wav");
#endif
            }

            if (menuBgmClip == null) return;

            if (bgmSource.clip == menuBgmClip && bgmSource.isPlaying)
            {
                bgmSource.loop = true;
                return;
            }

            bgmSource.clip = menuBgmClip;
            bgmSource.loop = true;
            bgmSource.volume = GetScaledBgmVolume(OptionsManager.SavedMusicVolume);
            bgmSource.time = 0f;
            bgmSource.Play();
        }

        public void StopStageMusic()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }
        }

        public void PauseStageMusic()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Pause();
            }
        }

        public void ResumeStageMusic()
        {
            if (bgmSource != null)
            {
                bgmSource.UnPause();
            }
        }

        public AudioClip GetCurrentStageTargetClip()
        {
            if (stages != null && selectedStageIndex >= 0 && selectedStageIndex < stages.Length)
            {
                var st = stages[selectedStageIndex];
                if (st != null)
                {
                    if (st.stageBgmClip != null)
                    {
                        return st.stageBgmClip;
                    }
                    if (st.stageName != null)
                    {
                        if (st.stageName.IndexOf("Obelisco", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return obeliscoBgmClip;
                        if (st.stageName.IndexOf("Casa Rosada", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            st.stageName.IndexOf("Rosada", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return casaRosadaBgmClip;
                        if (st.stageName.IndexOf("Triunvirato", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            st.stageName.IndexOf("Olazabal", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return triunviratoBgmClip;
                        if (st.stageName.IndexOf("Puente", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            st.stageName.IndexOf("Mujer", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return puenteMujerBgmClip;
                        if (st.stageName.IndexOf("Floralis", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            st.stageName.IndexOf("Generica", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            st.stageName.IndexOf("Genérica", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return floralisBgmClip;
                    }
                }
            }
            return null;
        }

        public void UpdateStageMusic()
        {
            EnsureBgmSource();

            if (inMainMenu || inSideSelect || inCharSelect || inStageSelect || inTowerScreen)
            {
                PlayMenuMusic();
                return;
            }

            AudioClip targetClip = GetCurrentStageTargetClip();

            if (targetClip != null)
            {
                PlayStageMusic(targetClip);
            }
            else
            {
                StopStageMusic();
            }
        }

        private void OnDevicesOrBindingsUpdated()

        {

            if (inSideSelect)

            {

                UpdateSideSelectVisuals();

            }

        }

        private void WireUIButtons()

        {

            if (restartButton != null)

            {

                restartButton.onClick.RemoveAllListeners();

                restartButton.onClick.AddListener(RestartCurrentModeMatch);

            }

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveAllListeners();
                returnToMenuButton.onClick.AddListener(() =>
                {
                    SoundEffectsManager.Instance?.PlayUiConfirm();
                    ShowMainMenu();
                });
            }

            if (mainMenuFightButton != null)

            {

                mainMenuFightButton.onClick.RemoveAllListeners();

                mainMenuFightButton.onClick.AddListener(StartPeleaMode);

            }

            if (mainMenuTowerButton != null)

            {

                mainMenuTowerButton.onClick.RemoveAllListeners();

                mainMenuTowerButton.onClick.AddListener(StartTowerModeFromMenu);

            }

            if (mainMenuVsButton != null)

            {

                mainMenuVsButton.interactable = true;

                mainMenuVsButton.onClick.RemoveAllListeners();

                mainMenuVsButton.onClick.AddListener(OpenSideSelect);

            }
            if (mainMenuExitButton != null)
            {
                mainMenuExitButton.onClick.RemoveAllListeners();
                mainMenuExitButton.onClick.AddListener(QuitGame);
            }

            if (sideSelectBackButton != null)

            {

                sideSelectBackButton.onClick.RemoveAllListeners();

                sideSelectBackButton.onClick.AddListener(() =>
                {
                    SoundEffectsManager.Instance?.PlayUiConfirm();
                    ShowMainMenu();
                });

            }

            if (sideSelectContinueButton != null)

            {

                sideSelectContinueButton.onClick.RemoveAllListeners();

                sideSelectContinueButton.onClick.AddListener(() =>

                {

                    if (CanProceedFromSideSelect())

                    {

                        ConfirmSideSelectionAndProceed();

                    }

                });

            }

            if (charSelectButtons != null)

            {

                for (int i = 0; i < charSelectButtons.Length; i++)

                {

                    int idx = i;

                    if (charSelectButtons[idx] != null)

                    {

                        charSelectButtons[idx].onClick.RemoveAllListeners();

                        charSelectButtons[idx].onClick.AddListener(() => OnCharButtonClicked(idx));

                    }

                }

            }

            if (confirmCharButton != null)

            {

                confirmCharButton.onClick.RemoveAllListeners();

                confirmCharButton.onClick.AddListener(OnConfirmCharButtonClicked);

            }

            if (charSelectBackButton != null)

            {

                charSelectBackButton.onClick.RemoveAllListeners();

                charSelectBackButton.onClick.AddListener(() =>

                {
                    SoundEffectsManager.Instance?.PlayUiConfirm();

                    if (isVSMode) OpenSideSelect();

                    else ShowMainMenu();

                });

            }

            if (stageSelectButtons != null)

            {

                for (int i = 0; i < stageSelectButtons.Length; i++)

                {

                    int idx = i;

                    if (stageSelectButtons[idx] != null)

                    {

                        stageSelectButtons[idx].onClick.RemoveAllListeners();

                        stageSelectButtons[idx].onClick.AddListener(() => SelectStage(idx));

                    }

                }

            }

            if (confirmStageButton != null)

            {

                confirmStageButton.onClick.RemoveAllListeners();

                confirmStageButton.onClick.AddListener(StartFightFromStageSelect);

            }

            if (stageSelectBackButton != null)

            {

                stageSelectBackButton.onClick.RemoveAllListeners();

                stageSelectBackButton.onClick.AddListener(() =>
                {
                    SoundEffectsManager.Instance?.PlayUiConfirm();
                    OpenCharSelect(isVSMode);
                });

            }

            // Pause Menu buttons
            EnsurePauseMenuUI();

            // Tower Mode buttons

            if (towerStartFightButton != null)

            {

                towerStartFightButton.onClick.RemoveAllListeners();

                towerStartFightButton.onClick.AddListener(() => StartTowerFight(currentTowerFloor));

            }

            if (towerBackToMenuButton != null)
            {
                towerBackToMenuButton.onClick.RemoveAllListeners();
                towerBackToMenuButton.onClick.AddListener(() =>
                {
                    SoundEffectsManager.Instance?.PlayUiConfirm();
                    ShowMainMenu();
                });
            }

            if (towerVictoryMenuButton != null)
            {
                towerVictoryMenuButton.onClick.RemoveAllListeners();
                towerVictoryMenuButton.onClick.AddListener(() =>
                {
                    SoundEffectsManager.Instance?.PlayUiConfirm();
                    ShowMainMenu();
                });
            }

        }

        public void SetFightersActiveForCombat(bool active)
        {
            if (player1 != null)
            {
                player1.CancelInvoke();
                if (!active)
                {
                    player1.ResetFighter(p1StartPos, true);
                }
                player1.gameObject.SetActive(active);
            }
            if (player2 != null)
            {
                player2.CancelInvoke();
                if (!active)
                {
                    player2.ResetFighter(p2StartPos, false);
                }
                player2.gameObject.SetActive(active);
            }
        }

        public void ShowMainMenu()

        {

            StopAllCoroutines();

            PlayMenuMusic();

            isPaused = false;

            Time.timeScale = 1f;

            inMainMenu = true;

            isTowerMode = false;

            inTowerScreen = false;

            isTowerScrolling = false;

            inSideSelect = false;

            inCharSelect = false;

            inStageSelect = false;

            matchEnded = false;

            roundTransitionActive = false;

            currentTimer = roundDuration;

            SetFightersActiveForCombat(false);

            UpdateHUD();

            if (pausePanel != null) pausePanel.SetActive(false);

            if (towerPanel != null) towerPanel.SetActive(false);

            if (towerVictoryModal != null) towerVictoryModal.SetActive(false);

            if (resultPanel != null) resultPanel.SetActive(false);

            if (sideSelectPanel != null) sideSelectPanel.SetActive(false);

            if (charSelectPanel != null) charSelectPanel.SetActive(false);

            if (stageSelectPanel != null) stageSelectPanel.SetActive(false);

            if (optionsManager == null) optionsManager = FindFirstObjectByType<OptionsManager>();

            if (optionsManager != null && optionsManager.optionsPanel != null)

            {

                optionsManager.optionsPanel.SetActive(false);

            }

            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

            mainMenuSelectedIndex = 0;

            UpdateMainMenuSelectionVisuals();

        }

        // =========================================================================

        // 1. MODO VS - SELECTOR DE LADO

        // =========================================================================

        public void OpenSideSelect()

        {
            if (!_suppressUiSfx) SoundEffectsManager.Instance?.PlayUiConfirm();
            PlayMenuMusic();

            isVSMode = true;

            isTowerMode = false;

            inTowerScreen = false;

            inMainMenu = false;

            inSideSelect = true;

            inCharSelect = false;

            inStageSelect = false;

            matchEnded = false;

            roundTransitionActive = false;

            p1SideSlot = 0;

            p2SideSlot = 0;

            p1SideConfirmed = false;

            p2SideConfirmed = false;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            if (charSelectPanel != null) charSelectPanel.SetActive(false);

            if (stageSelectPanel != null) stageSelectPanel.SetActive(false);

            if (resultPanel != null) resultPanel.SetActive(false);

            if (sideSelectPanel != null)

            {

                sideSelectPanel.SetActive(true);

                sideSelectPanel.transform.SetAsLastSibling();

            }

            UpdateSideSelectVisuals();

        }

        public bool CanProceedFromSideSelect()

        {

            bool differentSides = (p1SideSlot == -1 && p2SideSlot == 1) || (p1SideSlot == 1 && p2SideSlot == -1);

            return differentSides && p1SideConfirmed && p2SideConfirmed;

        }

        public void ConfirmSideSelectionAndProceed()

        {
            if (!_suppressUiSfx) SoundEffectsManager.Instance?.PlayUiConfirm();

            leftSidePlayerIndex = (p1SideSlot == -1) ? 1 : 2;

            rightSidePlayerIndex = (p1SideSlot == 1) ? 1 : 2;

            if (sideSelectPanel != null) sideSelectPanel.SetActive(false);

            OpenCharSelect(true);

        }

        public void UpdateSideSelectVisuals()

        {

            float leftX = -360f;

            float centerX = 0f;

            float rightX = 360f;

            // P1 Token Position & Highlight

            if (p1SideTokenRect != null)

            {

                float targetX = (p1SideSlot == -1) ? leftX : (p1SideSlot == 1 ? rightX : centerX);

                float targetY = (p1SideSlot == 0 && p2SideSlot == 0) ? 46f : 0f;

                p1SideTokenRect.anchoredPosition = new Vector2(targetX, targetY);

            }

            if (p1SideHighlight != null)

            {

                p1SideHighlight.color = p1SideConfirmed

                    ? new Color(0.18f, 0.88f, 0.35f, 1f) // Green: locked in

                    : (p1SideSlot != 0 ? new Color(0.25f, 0.85f, 1f, 1f) : new Color(0.20f, 0.28f, 0.45f, 0.95f));

            }

            if (p1SideStatusText != null)

            {

                if (p1SideConfirmed)

                {

                    string sideName = p1SideSlot == -1 ? "IZQUIERDA (1P)" : "DERECHA (2P)";

                    p1SideStatusText.text = $"¡LISTO! — {sideName} (Cubrirse para cancelar)";

                    p1SideStatusText.color = new Color(0.25f, 0.95f, 0.45f, 1f);

                }

                else if (p1SideSlot != 0)

                {

                    string sideName = p1SideSlot == -1 ? "LADO IZQUIERDO" : "LADO DERECHO";

                    p1SideStatusText.text = $"{sideName} (Presioná Piña o Patada para confirmar)";

                    p1SideStatusText.color = new Color(1f, 0.88f, 0.25f, 1f);

                }

                else

                {

                    p1SideStatusText.text = "EN EL CENTRO (Mové a Izquierda o Derecha)";

                    p1SideStatusText.color = new Color(0.80f, 0.88f, 0.98f, 1f);

                }

            }

            // P2 Token Position & Highlight

            if (p2SideTokenRect != null)

            {

                float targetX = (p2SideSlot == -1) ? leftX : (p2SideSlot == 1 ? rightX : centerX);

                float targetY = (p1SideSlot == 0 && p2SideSlot == 0) ? -74f : 0f;

                p2SideTokenRect.anchoredPosition = new Vector2(targetX, targetY);

            }

            if (p2SideHighlight != null)

            {

                p2SideHighlight.color = p2SideConfirmed

                    ? new Color(0.18f, 0.88f, 0.35f, 1f) // Green: locked in

                    : (p2SideSlot != 0 ? new Color(1f, 0.55f, 0.15f, 1f) : new Color(0.35f, 0.24f, 0.20f, 0.95f));

            }

            if (p2SideStatusText != null)

            {

                if (p2SideConfirmed)

                {

                    string sideName = p2SideSlot == -1 ? "IZQUIERDA (1P)" : "DERECHA (2P)";

                    p2SideStatusText.text = $"¡LISTO! — {sideName} (Cubrirse para cancelar)";

                    p2SideStatusText.color = new Color(0.25f, 0.95f, 0.45f, 1f);

                }

                else if (p2SideSlot != 0)

                {

                    string sideName = p2SideSlot == -1 ? "LADO IZQUIERDO" : "LADO DERECHO";

                    p2SideStatusText.text = $"{sideName} (Presioná Piña o Patada para confirmar)";

                    p2SideStatusText.color = new Color(1f, 0.88f, 0.25f, 1f);

                }

                else

                {

                    p2SideStatusText.text = "EN EL CENTRO (Mové a Izquierda o Derecha)";

                    p2SideStatusText.color = new Color(0.80f, 0.88f, 0.98f, 1f);

                }

            }

            // Update Device Icons & Names dynamically

            if (ControlBindingsManager.Instance != null)

            {

                if (p1SideDeviceIcon != null)

                {

                    p1SideDeviceIcon.sprite = ControlBindingsManager.Instance.GetDeviceIcon(1);

                }

                if (p2SideDeviceIcon != null)

                {

                    p2SideDeviceIcon.sprite = ControlBindingsManager.Instance.GetDeviceIcon(2);

                }

                if (p1SideDeviceNameText != null)

                {

                    p1SideDeviceNameText.text = ControlBindingsManager.Instance.p1DeviceDisplayName;

                }

                if (p2SideDeviceNameText != null)

                {

                    p2SideDeviceNameText.text = ControlBindingsManager.Instance.p2DeviceDisplayName;

                }

                if (p1SideControlsPromptText != null)

                {

                    p1SideControlsPromptText.text = ControlBindingsManager.GetAdaptedPromptsForFamily(

                        ControlBindingsManager.Instance.p1ControllerFamily, ControlBindingsManager.Instance.p1CommittedKeys);

                }

                if (p2SideControlsPromptText != null)

                {

                    p2SideControlsPromptText.text = ControlBindingsManager.GetAdaptedPromptsForFamily(

                        ControlBindingsManager.Instance.p2ControllerFamily, ControlBindingsManager.Instance.p2CommittedKeys);

                }

            }

            if (sideSelectContinueButton != null)

            {

                bool canProceed = CanProceedFromSideSelect();

                sideSelectContinueButton.interactable = canProceed;

                var img = sideSelectContinueButton.GetComponent<Image>();

                if (img != null)

                {

                    img.color = canProceed ? new Color(0.16f, 0.72f, 0.32f, 1f) : new Color(0.28f, 0.34f, 0.44f, 0.75f);

                }

            }

        }

        private void HandleSideSelectInput()

        {

            FighterInputFrame p1In = (ControlBindingsManager.Instance != null)

                ? ControlBindingsManager.Instance.ReadPlayerInput(1)

                : default;

            FighterInputFrame p2In = (ControlBindingsManager.Instance != null)

                ? ControlBindingsManager.Instance.ReadPlayerInput(2)

                : default;

            var kb = Keyboard.current;

            bool cancelGlobal = (kb != null && kb.escapeKey.wasPressedThisFrame);

            // --- P1 Side Selection ---
            if (!p1SideConfirmed)
            {
                int prevP1Slot = p1SideSlot;
                if (p1In.leftPressedThisFrame)
                {
                    if (p1SideSlot == 0)
                    {
                        if (p2SideSlot != -1) p1SideSlot = -1; // Move to Left (if not occupied by P2)
                    }
                    else if (p1SideSlot == 1)
                    {
                        p1SideSlot = 0; // Return to Center
                    }
                    if (p1SideSlot != prevP1Slot) SoundEffectsManager.Instance?.PlayUiMove();
                    UpdateSideSelectVisuals();
                }
                else if (p1In.rightPressedThisFrame)
                {
                    if (p1SideSlot == 0)
                    {
                        if (p2SideSlot != 1) p1SideSlot = 1; // Move to Right (if not occupied by P2)
                    }
                    else if (p1SideSlot == -1)
                    {
                        p1SideSlot = 0; // Return to Center
                    }
                    if (p1SideSlot != prevP1Slot) SoundEffectsManager.Instance?.PlayUiMove();
                    UpdateSideSelectVisuals();
                }

                if (p1In.punchPressedThisFrame || p1In.kickPressedThisFrame)
                {
                    if (p1SideSlot != 0)
                    {
                        p1SideConfirmed = true;
                        SoundEffectsManager.Instance?.PlayUiConfirm();
                        UpdateSideSelectVisuals();
                        if (CanProceedFromSideSelect()) ConfirmSideSelectionAndProceed();
                        return;
                    }
                }

                if (p1In.blockPressedThisFrame || cancelGlobal)
                {
                    if (p1SideSlot != 0)
                    {
                        p1SideSlot = 0;
                        SoundEffectsManager.Instance?.PlayUiMove();
                        UpdateSideSelectVisuals();
                    }
                    else
                    {
                        SoundEffectsManager.Instance?.PlayUiConfirm();
                        ShowMainMenu();
                        return;
                    }
                }
            }
            else // P1 already confirmed
            {
                if (p1In.blockPressedThisFrame || cancelGlobal)
                {
                    p1SideConfirmed = false;
                    SoundEffectsManager.Instance?.PlayUiMove();
                    UpdateSideSelectVisuals();
                }
            }

            // --- P2 Side Selection ---
            if (!p2SideConfirmed)
            {
                int prevP2Slot = p2SideSlot;
                if (p2In.leftPressedThisFrame)
                {
                    if (p2SideSlot == 0)
                    {
                        if (p1SideSlot != -1) p2SideSlot = -1; // Move to Left (if not occupied by P1)
                    }
                    else if (p2SideSlot == 1)
                    {
                        p2SideSlot = 0; // Return to Center
                    }
                    if (p2SideSlot != prevP2Slot) SoundEffectsManager.Instance?.PlayUiMove();
                    UpdateSideSelectVisuals();
                }
                else if (p2In.rightPressedThisFrame)
                {
                    if (p2SideSlot == 0)
                    {
                        if (p1SideSlot != 1) p2SideSlot = 1; // Move to Right (if not occupied by P1)
                    }
                    else if (p2SideSlot == -1)
                    {
                        p2SideSlot = 0; // Return to Center
                    }
                    if (p2SideSlot != prevP2Slot) SoundEffectsManager.Instance?.PlayUiMove();
                    UpdateSideSelectVisuals();
                }

                if (p2In.punchPressedThisFrame || p2In.kickPressedThisFrame)
                {
                    if (p2SideSlot != 0)
                    {
                        p2SideConfirmed = true;
                        SoundEffectsManager.Instance?.PlayUiConfirm();
                        UpdateSideSelectVisuals();
                        if (CanProceedFromSideSelect()) ConfirmSideSelectionAndProceed();
                        return;
                    }
                }

                if (p2In.blockPressedThisFrame)
                {
                    if (p2SideSlot != 0)
                    {
                        p2SideSlot = 0;
                        SoundEffectsManager.Instance?.PlayUiMove();
                        UpdateSideSelectVisuals();
                    }
                    else
                    {
                        SoundEffectsManager.Instance?.PlayUiConfirm();
                        ShowMainMenu();
                        return;
                    }
                }
            }
            else // P2 already confirmed
            {
                if (p2In.blockPressedThisFrame)
                {
                    p2SideConfirmed = false;
                    SoundEffectsManager.Instance?.PlayUiMove();
                    UpdateSideSelectVisuals();
                }
            }

        }

        // =========================================================================

        // 2. SELECTOR DE PERSONAJES (PELEA VS IA y MODO VS)

        // =========================================================================

        public void StartPeleaMode()
        {
            if (!_suppressUiSfx) SoundEffectsManager.Instance?.PlayUiConfirm();
            isVSMode = false;
            isTowerMode = false;
            inTowerScreen = false;
            OpenCharSelect(false);
        }

        public void StartTowerModeFromMenu()
        {
            if (!_suppressUiSfx) SoundEffectsManager.Instance?.PlayUiConfirm();
            isTowerMode = true;
            isVSMode = false;
            inTowerScreen = false;
            OpenCharSelect(false);
        }

        public void OpenCharSelect()

        {

            OpenCharSelect(isVSMode);

        }


        public void EnsureCardDecorations(Transform cardTr)
        {
            if (cardTr == null) return;
            Transform frameTr = cardTr.Find("Pixel_Frame");
            if (frameTr == null)
            {
                var go = new GameObject("Pixel_Frame", typeof(RectTransform));
                go.transform.SetParent(cardTr, false);
                frameTr = go.transform;
                var frRT = frameTr.GetComponent<RectTransform>();
                frRT.anchorMin = new Vector2(0.5f, 0.5f);
                frRT.anchorMax = new Vector2(0.5f, 0.5f);
                frRT.pivot = new Vector2(0.5f, 0.5f);
                frRT.anchoredPosition = new Vector2(0f, 11f);
                frRT.sizeDelta = new Vector2(98f, 112f);

                CreateCornerBracket(frameTr, "Corner_TL", new Vector2(-49f, 56f), true, true);
                CreateCornerBracket(frameTr, "Corner_TR", new Vector2(49f, 56f), false, true);
                CreateCornerBracket(frameTr, "Corner_BL", new Vector2(-49f, -56f), true, false);
                CreateCornerBracket(frameTr, "Corner_BR", new Vector2(49f, -56f), false, false);
            }

            Transform badgesTr = cardTr.Find("Selection_Badges");
            if (badgesTr == null)
            {
                var bgo = new GameObject("Selection_Badges", typeof(RectTransform));
                bgo.transform.SetParent(cardTr, false);
                badgesTr = bgo.transform;
                var bRT = badgesTr.GetComponent<RectTransform>();
                bRT.anchorMin = new Vector2(0.5f, 0.5f);
                bRT.anchorMax = new Vector2(0.5f, 0.5f);
                bRT.pivot = new Vector2(0.5f, 0.5f);
                bRT.anchoredPosition = new Vector2(0f, 65f);
                bRT.sizeDelta = new Vector2(104f, 24f);

                CreatePlayerBadge(badgesTr, "Badge_P1", "1P", new Color(0f, 0.88f, 1f, 1f));
                CreatePlayerBadge(badgesTr, "Badge_P2", "2P", new Color(1f, 0.38f, 0.12f, 1f));
                CreatePlayerBadge(badgesTr, "Badge_AI", "IA", new Color(1f, 0.72f, 0.15f, 1f));
            }
        }

        private static void CreateCornerBracket(Transform parent, string name, Vector2 pos, bool left, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(14f, 14f);

            var hGo = new GameObject("H", typeof(RectTransform), typeof(Image));
            hGo.transform.SetParent(go.transform, false);
            var hRT = hGo.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
            hRT.anchorMax = hRT.anchorMin;
            hRT.pivot = hRT.anchorMin;
            hRT.anchoredPosition = Vector2.zero;
            hRT.sizeDelta = new Vector2(14f, 3f);
            var hImg = hGo.GetComponent<Image>();
            hImg.color = Color.white;

            var vGo = new GameObject("V", typeof(RectTransform), typeof(Image));
            vGo.transform.SetParent(go.transform, false);
            var vRT = vGo.GetComponent<RectTransform>();
            vRT.anchorMin = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
            vRT.anchorMax = vRT.anchorMin;
            vRT.pivot = vRT.anchorMin;
            vRT.anchoredPosition = Vector2.zero;
            vRT.sizeDelta = new Vector2(3f, 14f);
            var vImg = vGo.GetComponent<Image>();
            vImg.color = Color.white;
        }

        private static void CreatePlayerBadge(Transform parent, string name, string defaultText, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(44f, 20f);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = bgColor;

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var txtRT = txtGo.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero;
            txtRT.anchoredPosition = Vector2.zero;
            var txt = txtGo.GetComponent<Text>();
            txt.text = defaultText;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            go.SetActive(false);
        }

        public void UpdateCardSelectionBadge(int i, bool isP1, bool isP2, bool isAI, string p1Label, string p2Label)
        {
            if (charSelectButtons == null || i < 0 || i >= charSelectButtons.Length || charSelectButtons[i] == null) return;
            Transform cardTr = charSelectButtons[i].transform;
            EnsureCardDecorations(cardTr);

            Transform legacyTag = cardTr.Find("P1P2_Tag");
            if (legacyTag != null) legacyTag.gameObject.SetActive(false);

            Transform frameTr = cardTr.Find("Pixel_Frame");
            Transform badgesTr = cardTr.Find("Selection_Badges");
            if (frameTr == null || badgesTr == null) return;

            Transform bP1 = badgesTr.Find("Badge_P1");
            Transform bP2 = badgesTr.Find("Badge_P2");
            Transform bAI = badgesTr.Find("Badge_AI");

            Color colP1 = new Color(0f, 0.88f, 1f, 1f);     // Vivid arcade cyan
            Color colP2 = new Color(1f, 0.38f, 0.12f, 1f);   // Vivid arcade orange
            Color colAI = new Color(1f, 0.72f, 0.15f, 1f);   // Arcade amber gold

            bool anySelected = isP1 || isP2 || isAI;
            frameTr.gameObject.SetActive(anySelected);

            cardTr.localScale = (isP1 || isP2) ? new Vector3(1.05f, 1.05f, 1f) : (isAI ? new Vector3(1.02f, 1.02f, 1f) : Vector3.one);

            void SetCornerColor(string cName, Color c)
            {
                var tr = frameTr.Find(cName);
                if (tr != null)
                {
                    foreach (var img in tr.GetComponentsInChildren<Image>()) img.color = c;
                }
            }

            if (isP1 && isP2)
            {
                if (bP1 != null)
                {
                    var rt = bP1.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(-24f, 0f);
                    rt.sizeDelta = new Vector2(40f, 20f);
                    var t = bP1.GetComponentInChildren<Text>();
                    if (t != null) t.text = p1Label;
                    bP1.gameObject.SetActive(true);
                }
                if (bP2 != null)
                {
                    var rt = bP2.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(24f, 0f);
                    rt.sizeDelta = new Vector2(40f, 20f);
                    var t = bP2.GetComponentInChildren<Text>();
                    if (t != null) t.text = p2Label;
                    bP2.gameObject.SetActive(true);
                }
                if (bAI != null) bAI.gameObject.SetActive(false);

                SetCornerColor("Corner_TL", colP1);
                SetCornerColor("Corner_BL", colP1);
                SetCornerColor("Corner_TR", colP2);
                SetCornerColor("Corner_BR", colP2);
            }
            else if (isP1)
            {
                if (bP1 != null)
                {
                    var rt = bP1.GetComponent<RectTransform>();
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(46f, 20f);
                    var t = bP1.GetComponentInChildren<Text>();
                    if (t != null) t.text = p1Label;
                    bP1.gameObject.SetActive(true);
                }
                if (bP2 != null) bP2.gameObject.SetActive(false);
                if (bAI != null) bAI.gameObject.SetActive(false);

                SetCornerColor("Corner_TL", colP1);
                SetCornerColor("Corner_TR", colP1);
                SetCornerColor("Corner_BL", colP1);
                SetCornerColor("Corner_BR", colP1);
            }
            else if (isP2)
            {
                if (bP2 != null)
                {
                    var rt = bP2.GetComponent<RectTransform>();
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(46f, 20f);
                    var t = bP2.GetComponentInChildren<Text>();
                    if (t != null) t.text = p2Label;
                    bP2.gameObject.SetActive(true);
                }
                if (bP1 != null) bP1.gameObject.SetActive(false);
                if (bAI != null) bAI.gameObject.SetActive(false);

                SetCornerColor("Corner_TL", colP2);
                SetCornerColor("Corner_TR", colP2);
                SetCornerColor("Corner_BL", colP2);
                SetCornerColor("Corner_BR", colP2);
            }
            else if (isAI)
            {
                if (bAI != null)
                {
                    var rt = bAI.GetComponent<RectTransform>();
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(46f, 20f);
                    bAI.gameObject.SetActive(true);
                }
                if (bP1 != null) bP1.gameObject.SetActive(false);
                if (bP2 != null) bP2.gameObject.SetActive(false);

                SetCornerColor("Corner_TL", colAI);
                SetCornerColor("Corner_TR", colAI);
                SetCornerColor("Corner_BL", colAI);
                SetCornerColor("Corner_BR", colAI);
            }
            else
            {
                if (bP1 != null) bP1.gameObject.SetActive(false);
                if (bP2 != null) bP2.gameObject.SetActive(false);
                if (bAI != null) bAI.gameObject.SetActive(false);
            }
        }

        public void ApplyCharSelectResponsiveLayout()
        {
            if (charSelectPanel == null) return;
            Transform container = charSelectPanel.transform.Find("Container");
            if (container == null) return;

            var containerRT = container.GetComponent<RectTransform>();
            if (containerRT != null)
            {
                containerRT.anchorMin = new Vector2(0.5f, 0.5f);
                containerRT.anchorMax = new Vector2(0.5f, 0.5f);
                containerRT.pivot = new Vector2(0.5f, 0.5f);
                containerRT.sizeDelta = new Vector2(1200f, 650f);
                containerRT.anchoredPosition = Vector2.zero;
            }

            // Hide legacy single-box preview on the right so both Left (P1) and Right (IA / P2) side panels are always symmetric and clear
            if (selectedCharPreviewImage != null && selectedCharPreviewImage.transform.parent != null)
            {
                selectedCharPreviewImage.transform.parent.gameObject.SetActive(false);
            }

            if (vsCharPreviewContainer != null)
            {
                vsCharPreviewContainer.SetActive(true);
                Transform leftBox = vsCharPreviewContainer.transform.Find("LeftPlayerBox");
                if (leftBox != null)
                {
                    var rt = leftBox.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.sizeDelta = new Vector2(190f, 344f);
                        rt.anchoredPosition = new Vector2(-478f, 6f);
                    }
                }
                Transform rightBox = vsCharPreviewContainer.transform.Find("RightPlayerBox");
                if (rightBox != null)
                {
                    var rt = rightBox.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.sizeDelta = new Vector2(190f, 344f);
                        rt.anchoredPosition = new Vector2(478f, 6f);
                    }
                }
            }

            // Center all 11 character cards in the middle zone (x in [-354..+354]), completely clear of side panels [-573..-383] and [+383..+573]
            float[] row0X = new float[] { -300f, -180f, -60f, 60f, 180f, 300f };
            float[] row1X = new float[] { -240f, -120f, 0f, 120f, 240f };
            Vector2 cardSize = new Vector2(108f, 150f);

            if (charSelectButtons != null)
            {
                for (int i = 0; i < charSelectButtons.Length; i++)
                {
                    if (charSelectButtons[i] == null) continue;
                    var cardRT = charSelectButtons[i].GetComponent<RectTransform>();
                    if (cardRT != null)
                    {
                        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
                        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                        cardRT.pivot = new Vector2(0.5f, 0.5f);
                        cardRT.sizeDelta = cardSize;
                        float cx = (i < 6) ? row0X[i] : row1X[i - 6];
                        float cy = (i < 6) ? 94f : -78f;
                        cardRT.anchoredPosition = new Vector2(cx, cy);
                    }

                    Transform innerBG = charSelectButtons[i].transform.Find("InnerBG");
                    if (innerBG != null)
                    {
                        var bgRT = innerBG.GetComponent<RectTransform>();
                        if (bgRT != null)
                        {
                            bgRT.sizeDelta = new Vector2(96f, 110f);
                            bgRT.anchoredPosition = new Vector2(0f, 11f);
                        }
                        Transform port = innerBG.Find("Portrait");
                        if (port != null)
                        {
                            var pRT = port.GetComponent<RectTransform>();
                            if (pRT != null)
                            {
                                pRT.sizeDelta = new Vector2(90f, 104f);
                                pRT.anchoredPosition = Vector2.zero;
                            }
                            var pImg = port.GetComponent<Image>();
                            if (pImg != null) pImg.preserveAspect = true;
                        }
                    }

                    Transform nameLbl = charSelectButtons[i].transform.Find("NameLabel");
                    if (nameLbl != null)
                    {
                        var nRT = nameLbl.GetComponent<RectTransform>();
                        if (nRT != null)
                        {
                            nRT.sizeDelta = new Vector2(104f, 24f);
                            nRT.anchoredPosition = new Vector2(0f, -57f);
                        }
                        var nTxt = nameLbl.GetComponent<Text>();
                        if (nTxt != null) nTxt.fontSize = 14;
                    }

                    EnsureCardDecorations(charSelectButtons[i].transform);
                    Transform tagLbl = charSelectButtons[i].transform.Find("P1P2_Tag");
                    if (tagLbl != null)
                    {
                        var tRT = tagLbl.GetComponent<RectTransform>();
                        if (tRT != null)
                        {
                            tRT.sizeDelta = new Vector2(102f, 22f);
                            tRT.anchoredPosition = new Vector2(0f, 60f);
                        }
                        var tTxt = tagLbl.GetComponent<Text>();
                        if (tTxt != null) tTxt.fontSize = 13;
                    }
                }
            }
        }

        public void OpenCharSelect(bool vsMode)
        {
            PlayMenuMusic();
            isVSMode = vsMode;
            inMainMenu = false;
            inSideSelect = false;
            inCharSelect = true;
            inStageSelect = false;
            matchEnded = false;
            roundTransitionActive = false;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (sideSelectPanel != null) sideSelectPanel.SetActive(false);
            if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (optionsManager != null && optionsManager.optionsPanel != null) optionsManager.optionsPanel.SetActive(false);
            if (charSelectPanel != null) charSelectPanel.SetActive(true);

            ApplyCharSelectResponsiveLayout();

            Transform titleTr = charSelectPanel != null ? charSelectPanel.transform.Find("Container/Title") : null;
            Text titleTxt = titleTr != null ? titleTr.GetComponent<Text>() : null;
            if (titleTxt != null)
            {
                if (isVSMode) titleTxt.text = "MODO VERSUS — SELECCIÓN DE PERSONAJES";
                else if (isTowerMode) titleTxt.text = "MODO TORRE — SELECCIONÁ TU PERSONAJE";
                else titleTxt.text = "MODO PELEA — SELECCIONÁ TU PERSONAJE";
            }

            if (confirmCharButton != null)
            {
                var txt = confirmCharButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    if (isTowerMode) txt.text = "SUBIR A LA TORRE →";
                    else txt.text = "ELEGIR ESCENARIO →";
                }
            }

            if (isVSMode)
            {
                vsLeftCharConfirmed = false;
                vsRightCharConfirmed = false;
                vsLeftCharIndex = Mathf.Clamp(vsLeftCharIndex, 0, roster.Length - 1);
                vsRightCharIndex = Mathf.Clamp(vsRightCharIndex, 0, roster.Length - 1);
                if (vsRightCharIndex == vsLeftCharIndex && roster.Length > 1) vsRightCharIndex = (vsLeftCharIndex + 1) % roster.Length;
                UpdateVSCharSelectVisuals();
            }
            else
            {
                bool prevSuppress = _suppressUiSfx;
                _suppressUiSfx = true;
                selectedCharIndex = Mathf.Clamp(selectedCharIndex, 0, roster.Length - 1);
                selectedRivalIndex = PickRandomRivalIndex(selectedCharIndex);
                SelectCharacter(selectedCharIndex);
                _suppressUiSfx = prevSuppress;
            }
        }

        public void SelectCharacter(int index)
        {
            int count = (roster != null && roster.Length > 0) ? roster.Length : 11;
            int prevCharIndex = selectedCharIndex;
            selectedCharIndex = Mathf.Clamp(index, 0, count - 1);
            if (inCharSelect && !_suppressUiSfx && selectedCharIndex != prevCharIndex)
            {
                SoundEffectsManager.Instance?.PlayUiMove();
            }

            // Ensure the AI rival is randomly assigned from the remaining characters (never equal to P1, never fixed to Tatoon)
            if (selectedRivalIndex < 0 || selectedRivalIndex >= count || selectedRivalIndex == selectedCharIndex)
            {
                selectedRivalIndex = PickRandomRivalIndex(selectedCharIndex);
            }

            for (int i = 0; i < count; i++)
            {
                bool isP1 = (i == selectedCharIndex);
                bool isAI = (!isTowerMode && i == selectedRivalIndex);

                if (charSelectHighlights != null && i < charSelectHighlights.Length && charSelectHighlights[i] != null)
                {
                    if (isP1)
                        charSelectHighlights[i].color = new Color(0.15f, 0.65f, 0.85f, 1f);
                    else if (isAI)
                        charSelectHighlights[i].color = new Color(0.85f, 0.45f, 0.15f, 1f);
                    else
                        charSelectHighlights[i].color = new Color(0.18f, 0.23f, 0.35f, 0.92f);
                }

                UpdateCardSelectionBadge(i, isP1, false, isAI, "1P", "");
            }

            if (roster != null && selectedCharIndex >= 0 && selectedCharIndex < roster.Length)
            {
                var p1Entry = roster[selectedCharIndex];
                var aiEntry = (selectedRivalIndex >= 0 && selectedRivalIndex < roster.Length) ? roster[selectedRivalIndex] : p1Entry;

                if (selectedCharPreviewImage != null && p1Entry.anims != null && p1Entry.anims.portrait != null)
                {
                    selectedCharPreviewImage.sprite = p1Entry.anims.portrait;
                }

                // Update Left Panel (P1)
                if (vsLeftPlayerHeader != null) vsLeftPlayerHeader.text = "JUGADOR 1 (P1)";
                if (vsLeftCharPreviewImage != null && p1Entry.anims != null) vsLeftCharPreviewImage.sprite = p1Entry.anims.portrait;
                if (vsLeftCharNameText != null) vsLeftCharNameText.text = p1Entry.characterName.ToUpper();
                if (vsLeftCharStatusText != null)
                {
                    vsLeftCharStatusText.text = "TU PERSONAJE";
                    vsLeftCharStatusText.color = new Color(0.35f, 0.92f, 1f, 1f);
                }

                // Update Right Panel (RIVAL IA)
                if (vsRightPlayerHeader != null) vsRightPlayerHeader.text = isTowerMode ? "MODO TORRE (IA)" : "RIVAL IA (AL AZAR)";
                if (vsRightCharPreviewImage != null && aiEntry.anims != null) vsRightCharPreviewImage.sprite = aiEntry.anims.portrait;
                if (vsRightCharNameText != null) vsRightCharNameText.text = isTowerMode ? "7 RIVALES IA" : $"IA: {aiEntry.characterName.ToUpper()}";
                if (vsRightCharStatusText != null)
                {
                    vsRightCharStatusText.text = isTowerMode ? "TORRE DE 7 PISOS" : "ASIGNADO AL AZAR";
                    vsRightCharStatusText.color = new Color(1f, 0.68f, 0.25f, 1f);
                }

                if (selectedCharNameText != null)
                {
                    selectedCharNameText.text = isTowerMode
                        ? $"P1: {p1Entry.characterName.ToUpper()}   —   MODO TORRE (7 RIVALES IA)"
                        : $"P1: {p1Entry.characterName.ToUpper()}   VS   IA: {aiEntry.characterName.ToUpper()}";
                }
            }
        }

        public void OnCharButtonClicked(int index)
        {
            if (isVSMode)
            {
                int prevL = vsLeftCharIndex;
                int prevR = vsRightCharIndex;
                if (!vsLeftCharConfirmed)
                {
                    vsLeftCharIndex = index;
                }
                else if (!vsRightCharConfirmed)
                {
                    vsRightCharIndex = index;
                }
                if (vsLeftCharIndex != prevL || vsRightCharIndex != prevR)
                {
                    SoundEffectsManager.Instance?.PlayUiMove();
                }
                UpdateVSCharSelectVisuals();
            }
            else
            {
                if (index == selectedRivalIndex)
                {
                    selectedRivalIndex = PickRandomRivalIndex(index);
                }
                SelectCharacter(index);
            }
        }

        public void OnConfirmCharButtonClicked()
        {
            if (!_suppressUiSfx) SoundEffectsManager.Instance?.PlayUiConfirm();
            if (isVSMode)
            {
                vsLeftCharConfirmed = true;
                vsRightCharConfirmed = true;
                OpenStageSelect();
            }
            else if (isTowerMode)
            {
                StartTowerMode();
            }
            else
            {
                // Preserve flow: after selecting character in Modo Pelea, proceed to Stage Select!
                int rosterCount = (roster != null && roster.Length > 0) ? roster.Length : 11;
                if (selectedRivalIndex < 0 || selectedRivalIndex >= rosterCount || selectedRivalIndex == selectedCharIndex)
                {
                    selectedRivalIndex = PickRandomRivalIndex(selectedCharIndex);
                }
                OpenStageSelect();
            }
        }

        public void UpdateVSCharSelectVisuals()

        {

            int count = (roster != null && roster.Length > 0) ? roster.Length : 11;

            // Update card borders and tags

            for (int i = 0; i < count; i++)

            {

                bool isLeftHover = (i == vsLeftCharIndex);

                bool isRightHover = (i == vsRightCharIndex);

                if (charSelectHighlights != null && i < charSelectHighlights.Length && charSelectHighlights[i] != null)

                {

                    if (isLeftHover && isRightHover)

                    {

                        charSelectHighlights[i].color = new Color(0.95f, 0.95f, 0.20f, 1f); // Both

                    }

                    else if (isLeftHover)

                    {

                        charSelectHighlights[i].color = new Color(0.20f, 0.85f, 1f, 1f); // Left Player (Cyan)

                    }

                    else if (isRightHover)

                    {

                        charSelectHighlights[i].color = new Color(1f, 0.55f, 0.15f, 1f); // Right Player (Orange)

                    }

                    else

                    {

                        charSelectHighlights[i].color = new Color(0.18f, 0.23f, 0.35f, 0.92f);

                    }

                }

                UpdateCardSelectionBadge(i, isLeftHover, isRightHover, false, $"P{leftSidePlayerIndex}", $"P{rightSidePlayerIndex}");

            }

            // Left Player Preview

            if (roster != null && vsLeftCharIndex >= 0 && vsLeftCharIndex < roster.Length)

            {

                var entry = roster[vsLeftCharIndex];

                if (vsLeftCharPreviewImage != null && entry.anims != null)

                {

                    vsLeftCharPreviewImage.sprite = entry.anims.portrait;

                }

                if (vsLeftCharNameText != null)

                {

                    vsLeftCharNameText.text = entry.characterName.ToUpper();

                }

                if (vsLeftPlayerHeader != null)

                {

                    vsLeftPlayerHeader.text = $"LADO IZQUIERDO (P{leftSidePlayerIndex})";

                }

                if (vsLeftCharStatusText != null)

                {

                    vsLeftCharStatusText.text = vsLeftCharConfirmed ? "¡LISTO!" : "SELECCIONANDO...";

                    vsLeftCharStatusText.color = vsLeftCharConfirmed ? new Color(0.20f, 0.95f, 0.40f, 1f) : new Color(1f, 0.88f, 0.25f, 1f);

                }

            }

            // Right Player Preview

            if (roster != null && vsRightCharIndex >= 0 && vsRightCharIndex < roster.Length)

            {

                var entry = roster[vsRightCharIndex];

                if (vsRightCharPreviewImage != null && entry.anims != null)

                {

                    vsRightCharPreviewImage.sprite = entry.anims.portrait;

                }

                if (vsRightCharNameText != null)

                {

                    vsRightCharNameText.text = entry.characterName.ToUpper();

                }

                if (vsRightPlayerHeader != null)

                {

                    vsRightPlayerHeader.text = $"LADO DERECHO (P{rightSidePlayerIndex})";

                }

                if (vsRightCharStatusText != null)

                {

                    vsRightCharStatusText.text = vsRightCharConfirmed ? "¡LISTO!" : "SELECCIONANDO...";

                    vsRightCharStatusText.color = vsRightCharConfirmed ? new Color(0.20f, 0.95f, 0.40f, 1f) : new Color(1f, 0.88f, 0.25f, 1f);

                }

            }

            if (selectedCharNameText != null)

            {

                string p1Name = roster[vsLeftCharIndex].characterName.ToUpper();

                string p2Name = roster[vsRightCharIndex].characterName.ToUpper();

                selectedCharNameText.text = $"P{leftSidePlayerIndex}: {p1Name}   VS   P{rightSidePlayerIndex}: {p2Name}";

            }

        }

        private void HandleVSCharSelectInput()

        {

            int count = (roster != null && roster.Length > 0) ? roster.Length : 11;

            var kb = Keyboard.current;

            bool cancelGlobal = (kb != null && kb.escapeKey.wasPressedThisFrame);

            FighterInputFrame leftIn = (ControlBindingsManager.Instance != null)

                ? ControlBindingsManager.Instance.ReadPlayerInput(leftSidePlayerIndex)

                : default;

            FighterInputFrame rightIn = (ControlBindingsManager.Instance != null)

                ? ControlBindingsManager.Instance.ReadPlayerInput(rightSidePlayerIndex)

                : default;

            // --- Left Player Selection ---
            if (!vsLeftCharConfirmed)
            {
                int prevL = vsLeftCharIndex;
                if (leftIn.leftPressedThisFrame)
                {
                    vsLeftCharIndex = (vsLeftCharIndex - 1 + count) % count;
                }
                else if (leftIn.rightPressedThisFrame)
                {
                    vsLeftCharIndex = (vsLeftCharIndex + 1) % count;
                }
                else if (leftIn.upPressedThisFrame)
                {
                    vsLeftCharIndex = (vsLeftCharIndex >= 6) ? (vsLeftCharIndex - 6) : Mathf.Min(count - 1, vsLeftCharIndex + 6);
                }
                else if (leftIn.downPressedThisFrame)
                {
                    vsLeftCharIndex = (vsLeftCharIndex < 6) ? Mathf.Min(count - 1, vsLeftCharIndex + 6) : (vsLeftCharIndex - 6);
                }

                if (vsLeftCharIndex != prevL)
                {
                    SoundEffectsManager.Instance?.PlayUiMove();
                    UpdateVSCharSelectVisuals();
                }

                if (leftIn.punchPressedThisFrame || leftIn.kickPressedThisFrame)
                {
                    vsLeftCharConfirmed = true;
                    SoundEffectsManager.Instance?.PlayUiConfirm();
                    UpdateVSCharSelectVisuals();
                    CheckBothVSCharsConfirmed();
                    return;
                }

                if (leftIn.blockPressedThisFrame || cancelGlobal)
                {
                    SoundEffectsManager.Instance?.PlayUiConfirm();
                    OpenSideSelect();
                    return;
                }
            }
            else // Left player already confirmed
            {
                if (leftIn.blockPressedThisFrame)
                {
                    vsLeftCharConfirmed = false;
                    SoundEffectsManager.Instance?.PlayUiMove();
                    UpdateVSCharSelectVisuals();
                }
            }

            // --- Right Player Selection ---
            if (!vsRightCharConfirmed)
            {
                int prevR = vsRightCharIndex;
                if (rightIn.leftPressedThisFrame)
                {
                    vsRightCharIndex = (vsRightCharIndex - 1 + count) % count;
                }
                else if (rightIn.rightPressedThisFrame)
                {
                    vsRightCharIndex = (vsRightCharIndex + 1) % count;
                }
                else if (rightIn.upPressedThisFrame)
                {
                    vsRightCharIndex = (vsRightCharIndex >= 6) ? (vsRightCharIndex - 6) : Mathf.Min(count - 1, vsRightCharIndex + 6);
                }
                else if (rightIn.downPressedThisFrame)
                {
                    vsRightCharIndex = (vsRightCharIndex < 6) ? Mathf.Min(count - 1, vsRightCharIndex + 6) : (vsRightCharIndex - 6);
                }

                if (vsRightCharIndex != prevR)
                {
                    SoundEffectsManager.Instance?.PlayUiMove();
                    UpdateVSCharSelectVisuals();
                }

                if (rightIn.punchPressedThisFrame || rightIn.kickPressedThisFrame)
                {
                    vsRightCharConfirmed = true;
                    SoundEffectsManager.Instance?.PlayUiConfirm();
                    UpdateVSCharSelectVisuals();
                    CheckBothVSCharsConfirmed();
                    return;
                }

                if (rightIn.blockPressedThisFrame || cancelGlobal)
                {
                    SoundEffectsManager.Instance?.PlayUiConfirm();
                    OpenSideSelect();
                    return;
                }
            }
            else // Right player already confirmed
            {
                if (rightIn.blockPressedThisFrame)
                {
                    vsRightCharConfirmed = false;
                    SoundEffectsManager.Instance?.PlayUiMove();
                    UpdateVSCharSelectVisuals();
                }
            }

        }

        private void CheckBothVSCharsConfirmed()

        {

            if (vsLeftCharConfirmed && vsRightCharConfirmed)

            {

                OpenStageSelect();

            }

        }

        // =========================================================================

        // 3. SELECTOR DE ESCENARIO (PELEA VS IA y MODO VS)

        // =========================================================================

        public void OpenStageSelect()

        {
            PlayMenuMusic();

            inMainMenu = false;

            inSideSelect = false;

            inCharSelect = false;

            inStageSelect = true;

            matchEnded = false;

            roundTransitionActive = false;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            if (sideSelectPanel != null) sideSelectPanel.SetActive(false);

            if (charSelectPanel != null) charSelectPanel.SetActive(false);

            if (resultPanel != null) resultPanel.SetActive(false);

            if (stageSelectPanel != null) stageSelectPanel.SetActive(true);

            bool prevSuppress = _suppressUiSfx;
            _suppressUiSfx = true;
            SelectStage(selectedStageIndex);
            _suppressUiSfx = prevSuppress;

        }

        public void SelectStage(int index)

        {

            int count = (stages != null && stages.Length > 0) ? stages.Length : 5;
            int prevStageIndex = selectedStageIndex;

            selectedStageIndex = Mathf.Clamp(index, 0, count - 1);
            if (inStageSelect && !_suppressUiSfx && selectedStageIndex != prevStageIndex)
            {
                SoundEffectsManager.Instance?.PlayUiMove();
            }

            if (stageSelectHighlights != null)

            {

                for (int i = 0; i < stageSelectHighlights.Length; i++)

                {

                    if (stageSelectHighlights[i] == null) continue;

                    bool isSel = (i == selectedStageIndex);

                    stageSelectHighlights[i].color = isSel

                        ? new Color(0.25f, 0.88f, 1f, 1f)

                        : new Color(0.18f, 0.23f, 0.35f, 0.92f);

                }

            }

            ApplyStageSelection(selectedStageIndex);

            if (stages != null && selectedStageIndex >= 0 && selectedStageIndex < stages.Length)

            {

                var st = stages[selectedStageIndex];

                if (selectedStageNameText != null)

                {

                    selectedStageNameText.text = $"ESCENARIO: {st.stageName.ToUpper()}  ({st.subtitle})";

                }

            }

            if (stageMatchupPreviewText != null && roster != null)

            {

                if (isVSMode)

                {

                    string pLeft = roster[vsLeftCharIndex].characterName.ToUpper();

                    string pRight = roster[vsRightCharIndex].characterName.ToUpper();

                    stageMatchupPreviewText.text = $"MODO VS:  {pLeft} (P{leftSidePlayerIndex})   VS   {pRight} (P{rightSidePlayerIndex})";

                }

                else if (selectedCharIndex >= 0 && selectedCharIndex < roster.Length)
                {
                    string pName = roster[selectedCharIndex].characterName.ToUpper();
                    string aiName = (selectedRivalIndex >= 0 && selectedRivalIndex < roster.Length && selectedRivalIndex != selectedCharIndex)
                        ? roster[selectedRivalIndex].characterName.ToUpper()
                        : roster[PickRandomRivalIndex(selectedCharIndex)].characterName.ToUpper();
                    stageMatchupPreviewText.text = isTowerMode
                        ? $"MODO TORRE: {pName} — ELEGÍ EL ESCENARIO INICIAL DE LA TORRE"
                        : $"P1: {pName}   VS   IA: {aiName}";
                }

            }

        }

        public void ApplyStageSelection(int stageIdx)

        {

            if (stages == null || stages.Length == 0) return;

            selectedStageIndex = Mathf.Clamp(stageIdx, 0, stages.Length - 1);

            for (int i = 0; i < stages.Length; i++)

            {

                if (stages[i].stageRoot != null)

                {

                    stages[i].stageRoot.SetActive(i == selectedStageIndex);

                }

            }

            float gY = stages[selectedStageIndex].groundY;

            p1StartPos.y = gY;

            p2StartPos.y = gY;

            if (player1 != null) player1.groundY = gY;

            if (player2 != null) player2.groundY = gY;

            if (mainCamera != null)

            {

                mainCamera.transform.position = new Vector3(0f, 0f, -10f);

            }

            var activeRoot = stages[selectedStageIndex].stageRoot;

            if (selectedStageIndex == 3 && activeRoot != null)
            {
                stages[3].groundY = -3.00f;
                gY = -3.00f;
                p1StartPos.y = gY;
                p2StartPos.y = gY;
                if (player1 != null) player1.groundY = gY;
                if (player2 != null) player2.groundY = gY;

                var t03 = activeRoot.transform.Find("03_Esquina_comercial");
                var t04 = activeRoot.transform.Find("04_Avenida_y_senales");
                var t05 = activeRoot.transform.Find("05_Piso_de_lucha");
                if (t05 != null)
                {
                    var pl = t05.GetComponent<ParallaxLayer>();
                    if (pl != null) pl.SetOrigin(new Vector3(0f, -1.05f, 0f));
                    else t05.localPosition = new Vector3(0f, -1.05f, 0f);
                }
                if (t03 != null)
                {
                    var pl = t03.GetComponent<ParallaxLayer>();
                    if (pl != null) pl.SetOrigin(new Vector3(0f, 0.25f, 0f));
                    else t03.localPosition = new Vector3(0f, 0.25f, 0f);
                }
                if (t04 != null)
                {
                    var pl = t04.GetComponent<ParallaxLayer>();
                    if (pl != null) pl.SetOrigin(new Vector3(0f, 0.25f, 0f));
                    else t04.localPosition = new Vector3(0f, 0.25f, 0f);
                }
            }

            if (activeRoot != null)

            {

                var layers = activeRoot.GetComponentsInChildren<ParallaxLayer>(true);

                foreach (var l in layers)

                {

                    l.UpdateParallax();

                }

            }

            ApplyStageAmbientLighting(selectedStageIndex);
            UpdateStageMusic();
            if (player1 != null && player1.gameObject.activeInHierarchy) player1.UpdateContactShadow();
            if (player2 != null && player2.gameObject.activeInHierarchy) player2.UpdateContactShadow();

        }

        private Light2D _cachedGlobalLight2D;
        private Light2D[] _stageAmbientLights;

        public void RefreshLightingAndShadowsForQuality(int qualityIdx)
        {
            ApplyStageAmbientLighting(selectedStageIndex);
            if (player1 != null)
            {
                if (qualityIdx <= 0) player1.ClearCombatPolishEffects();
                player1.UpdateContactShadow();
            }
            if (player2 != null)
            {
                if (qualityIdx <= 0) player2.ClearCombatPolishEffects();
                player2.UpdateContactShadow();
            }
        }

        public void ApplyStageAmbientLighting(int stageIdx)
        {
            if (stages == null || stages.Length == 0) return;
            stageIdx = Mathf.Clamp(stageIdx, 0, stages.Length - 1);

            if (_cachedGlobalLight2D == null)
            {
                var allLights = FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var l in allLights)
                {
                    if (l != null && l.lightType == Light2D.LightType.Global)
                    {
                        _cachedGlobalLight2D = l;
                        break;
                    }
                }
            }

            if (_stageAmbientLights == null || _stageAmbientLights.Length != stages.Length)
            {
                _stageAmbientLights = new Light2D[stages.Length];
            }

            bool lightingEnabled = OptionsManager.AreLightingAndShadowsEnabled;
            float qualMult = OptionsManager.LightingQualityMultiplier;

            // Tinte global muy sutil (todos los canales >= 0.975f e intensidad 1.0f para no oscurecer ni lavar colores)
            Color globalTint = Color.white;
            Color accentColor = new Color(1f, 0.85f, 0.55f, 1f);
            float accentBaseIntensity = 0.11f;
            Vector3 accentLocalPos = new Vector3(0f, 1.0f, 0f);
            Vector3 accentLocalScale = new Vector3(9.5f, 5.2f, 1f);

            switch (stageIdx)
            {
                case 0: // Obelisco (atardecer cálido urbano)
                    globalTint = new Color(1.00f, 0.985f, 0.975f, 1f);
                    accentColor = new Color(1.00f, 0.82f, 0.50f, 1f);
                    accentBaseIntensity = 0.11f;
                    accentLocalPos = new Vector3(0f, 1.15f, 0f);
                    break;
                case 1: // Casa Rosada (resplandor cálido rosado/dorado de plaza)
                    globalTint = new Color(1.00f, 0.982f, 0.986f, 1f);
                    accentColor = new Color(1.00f, 0.78f, 0.72f, 1f);
                    accentBaseIntensity = 0.10f;
                    accentLocalPos = new Vector3(0f, 0.95f, 0f);
                    break;
                case 2: // Puente de la Mujer (reflejo costero cian suave)
                    globalTint = new Color(0.978f, 0.992f, 1.00f, 1f);
                    accentColor = new Color(0.58f, 0.88f, 1.00f, 1f);
                    accentBaseIntensity = 0.10f;
                    accentLocalPos = new Vector3(0f, 0.65f, 0f);
                    accentLocalScale = new Vector3(10.0f, 5.0f, 1f);
                    break;
                case 3: // Triunvirato y Olazábal (sol urbano de tarde)
                    globalTint = new Color(1.00f, 0.990f, 0.975f, 1f);
                    accentColor = new Color(1.00f, 0.88f, 0.58f, 1f);
                    accentBaseIntensity = 0.10f;
                    accentLocalPos = new Vector3(-0.4f, 1.20f, 0f);
                    break;
                case 4: // Floralis Genérica (luz de faroles y reflejo metálico de parque)
                    globalTint = new Color(0.985f, 0.985f, 1.00f, 1f);
                    accentColor = new Color(0.90f, 0.78f, 1.00f, 1f);
                    accentBaseIntensity = 0.11f;
                    accentLocalPos = new Vector3(0f, 1.05f, 0f);
                    break;
            }

            if (_cachedGlobalLight2D != null)
            {
                _cachedGlobalLight2D.color = lightingEnabled ? globalTint : Color.white;
                _cachedGlobalLight2D.intensity = 1.0f;
            }

            for (int i = 0; i < stages.Length; i++)
            {
                var root = stages[i].stageRoot;
                if (root == null) continue;

                Light2D amb = _stageAmbientLights[i];
                if (amb == null)
                {
                    var childTrans = root.transform.Find("StageAmbientLight2D");
                    if (childTrans != null)
                    {
                        amb = childTrans.GetComponent<Light2D>();
                    }
                    if (amb == null)
                    {
                        var go = new GameObject("StageAmbientLight2D");
                        go.transform.SetParent(root.transform, false);
                        amb = go.AddComponent<Light2D>();
                    }
                    amb.lightType = Light2D.LightType.Sprite;
                    amb.lightCookieSprite = FighterController.EnsurePixelLightCookieSprite();
                    amb.overlapOperation = Light2D.OverlapOperation.Additive;
                    amb.volumetricEnabled = false;
                    amb.shadowsEnabled = false;
                    _stageAmbientLights[i] = amb;
                }

                if (i == stageIdx)
                {
                    amb.lightCookieSprite = FighterController.EnsurePixelLightCookieSprite();
                    amb.transform.localPosition = accentLocalPos;
                    amb.transform.localScale = accentLocalScale;
                    amb.color = accentColor;
                    amb.intensity = lightingEnabled ? (accentBaseIntensity * qualMult) : 0f;
                    amb.enabled = lightingEnabled;
                }
                else
                {
                    amb.intensity = 0f;
                    amb.enabled = false;
                }
            }
        }

        public int PickRandomRivalIndex(int playerCharIndex)

        {

            int count = (roster != null && roster.Length > 1) ? roster.Length : 11;

            List<int> candidates = new List<int>(count - 1);

            for (int i = 0; i < count; i++)

            {

                if (i != playerCharIndex)

                {

                    candidates.Add(i);

                }

            }

            if (candidates.Count == 0) return 0;

            int randomPick = Random.Range(0, candidates.Count);

            return candidates[randomPick];

        }

        public void StartFightFromStageSelect()
        {
            if (!_suppressUiSfx) SoundEffectsManager.Instance?.PlayUiConfirm();
            if (isVSMode)
            {
                StartVSMatch();
            }
            else if (isTowerMode)
            {
                StartTowerMode();
            }
            else
            {
                isVSMode = false;
                isTowerMode = false;
                inMainMenu = false;
                inSideSelect = false;
                inCharSelect = false;
                inStageSelect = false;
                inTowerScreen = false;
                isPaused = false;
                Time.timeScale = 1f;

                if (charSelectPanel != null) charSelectPanel.SetActive(false);
                if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                if (resultPanel != null) resultPanel.SetActive(false);
                if (towerPanel != null) towerPanel.SetActive(false);

                int stageCount = (stages != null && stages.Length > 0) ? stages.Length : 5;
                selectedStageIndex = Mathf.Clamp(selectedStageIndex, 0, stageCount - 1);
                ApplyStageSelection(selectedStageIndex);

                int rosterCount = (roster != null && roster.Length > 0) ? roster.Length : 11;
                if (selectedRivalIndex < 0 || selectedRivalIndex >= rosterCount || selectedRivalIndex == selectedCharIndex)
                {
                    selectedRivalIndex = PickRandomRivalIndex(selectedCharIndex);
                }

                ApplyCharacterSelectionToFighters();
                RestartMatch();
            }
        }

        // =========================================================================

        // MODO TORRE (Tower Mode - 7 Rivales)

        // =========================================================================

        public void StartTowerMode()

        {

            isTowerMode = true;

            isVSMode = false;

            inMainMenu = false;

            inSideSelect = false;

            inCharSelect = false;

            inStageSelect = false;

            isPaused = false;

            Time.timeScale = 1f;

            if (charSelectPanel != null) charSelectPanel.SetActive(false);

            if (stageSelectPanel != null) stageSelectPanel.SetActive(false);

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            if (resultPanel != null) resultPanel.SetActive(false);

            GenerateTowerRivals();

            currentTowerFloor = 0;

            towerFloorsDefeated = new bool[7];

            OpenTowerScreen(instantScroll: true);

        }

        public void GenerateTowerRivals()

        {

            int count = (roster != null && roster.Length > 0) ? roster.Length : 11;

            List<int> pool = new List<int>();

            for (int i = 0; i < count; i++)

            {

                if (i != selectedCharIndex)

                {

                    pool.Add(i);

                }

            }

            towerRivalIndices = new int[7];

            for (int f = 0; f < 7; f++)

            {

                if (pool.Count == 0)

                {

                    towerRivalIndices[f] = 0;

                    continue;

                }

                int r = Random.Range(0, pool.Count);

                towerRivalIndices[f] = pool[r];

                pool.RemoveAt(r);

            }

        }

        public float GetTowerYForFloor(int floor)

        {

            // Exact offset to center each of the 7 floor windows of torre.png (1024x1536) in the viewport

            // Window localY centers: [-440f, -283f, -126f, +32f, +190f, +348f, +506f]

            // Target container Y = -windowLocalY

            float[] floorYs = new float[] { 440f, 283f, 126f, -32f, -190f, -348f, -506f };

            floor = Mathf.Clamp(floor, 0, floorYs.Length - 1);

            return floorYs[floor];

        }

        public void OpenTowerScreen(bool instantScroll)

        {
            PlayMenuMusic();

            inTowerScreen = true;

            isPaused = false;

            Time.timeScale = 1f;

            if (towerVictoryModal != null) towerVictoryModal.SetActive(false);

            if (resultPanel != null) resultPanel.SetActive(false);

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            if (charSelectPanel != null) charSelectPanel.SetActive(false);

            if (stageSelectPanel != null) stageSelectPanel.SetActive(false);

            if (towerPanel != null)

            {

                towerPanel.SetActive(true);

                towerPanel.transform.SetAsLastSibling();

            }

            UpdateTowerSlotsVisuals();

            float targetY = GetTowerYForFloor(currentTowerFloor);

            if (instantScroll)

            {

                if (towerContainer != null) towerContainer.anchoredPosition = new Vector2(0f, targetY);

                if (towerFondoRect != null) towerFondoRect.anchoredPosition = new Vector2(0f, targetY * 0.35f);

                if (towerStartFightButton != null) towerStartFightButton.interactable = true;

            }

            else

            {

                StartCoroutine(ScrollTowerToFloorRoutine(currentTowerFloor, 1.8f, autoStartFight: true));

            }

        }

        public void UpdateTowerSlotsVisuals()

        {

            if (towerFloorSlots == null) return;

            for (int i = 0; i < 7; i++)

            {

                if (i >= towerFloorSlots.Length || towerFloorSlots[i] == null) continue;

                int rivalIdx = (towerRivalIndices != null && i < towerRivalIndices.Length) ? towerRivalIndices[i] : 0;

                string rivalName = (roster != null && rivalIdx >= 0 && rivalIdx < roster.Length)

                    ? roster[rivalIdx].characterName.ToUpper()

                    : $"RIVAL {i + 1}";

                Sprite portrait = (roster != null && rivalIdx >= 0 && rivalIdx < roster.Length && roster[rivalIdx].anims != null)

                    ? roster[rivalIdx].anims.portrait

                    : null;

                if (towerFloorPortraits != null && i < towerFloorPortraits.Length && towerFloorPortraits[i] != null)

                {

                    if (portrait != null) towerFloorPortraits[i].sprite = portrait;

                    // Los retratos de la torre conservan sus colores originales sin ningún tinte ni filtro

                    towerFloorPortraits[i].color = Color.white;

                }

                if (towerFloorNames != null && i < towerFloorNames.Length && towerFloorNames[i] != null)

                {

                    string floorPrefix = (i == 6) ? "P7 (CIMA)" : $"P{i + 1}";

                    towerFloorNames[i].text = $"{floorPrefix} • {rivalName}";

                }

                bool isDefeated = (towerFloorsDefeated != null && i < towerFloorsDefeated.Length && towerFloorsDefeated[i]);

                bool isActive = (i == currentTowerFloor) && !isDefeated;

                if (towerFloorDefeatedBadges != null && i < towerFloorDefeatedBadges.Length && towerFloorDefeatedBadges[i] != null)

                {

                    towerFloorDefeatedBadges[i].SetActive(isDefeated);

                }

                if (towerFloorActiveMarkers != null && i < towerFloorActiveMarkers.Length && towerFloorActiveMarkers[i] != null)

                {

                    towerFloorActiveMarkers[i].SetActive(isActive);

                }

            }

            if (towerFooterFloorText != null)

            {

                int rivalIdx = (towerRivalIndices != null && currentTowerFloor < towerRivalIndices.Length)

                    ? towerRivalIndices[currentTowerFloor]

                    : 0;

                string rivalName = (roster != null && rivalIdx >= 0 && rivalIdx < roster.Length)

                    ? roster[rivalIdx].characterName.ToUpper()

                    : $"RIVAL {currentTowerFloor + 1}";

                string floorTag = (currentTowerFloor == 6) ? "CIMA (PISO 7 DE 7)" : $"PISO {currentTowerFloor + 1} DE 7";

                towerFooterFloorText.text = $"{floorTag}:  {rivalName}  —  AL MEJOR DE 3 ROUNDS";

            }

        }

        public IEnumerator ScrollTowerToFloorRoutine(int targetFloor, float duration, bool autoStartFight)

        {

            isTowerScrolling = true;

            if (towerStartFightButton != null) towerStartFightButton.interactable = false;

            float startTowerY = towerContainer != null ? towerContainer.anchoredPosition.y : 0f;

            float targetTowerY = GetTowerYForFloor(targetFloor);

            float startFondoY = towerFondoRect != null ? towerFondoRect.anchoredPosition.y : 0f;

            float targetFondoY = targetTowerY * 0.35f;

            float elapsed = 0f;

            while (elapsed < duration)

            {

                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (towerContainer != null)

                {

                    towerContainer.anchoredPosition = new Vector2(0f, Mathf.Lerp(startTowerY, targetTowerY, t));

                }

                if (towerFondoRect != null)

                {

                    towerFondoRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(startFondoY, targetFondoY, t));

                }

                yield return null;

            }

            if (towerContainer != null) towerContainer.anchoredPosition = new Vector2(0f, targetTowerY);

            if (towerFondoRect != null) towerFondoRect.anchoredPosition = new Vector2(0f, targetFondoY);

            isTowerScrolling = false;

            UpdateTowerSlotsVisuals();

            if (towerStartFightButton != null) towerStartFightButton.interactable = true;

            if (autoStartFight)

            {

                yield return new WaitForSecondsRealtime(1.1f);

                if (inTowerScreen && !isPaused)

                {

                    StartTowerFight(targetFloor);

                }

            }

        }

        public void StartTowerFight(int floor)

        {
            if (!_suppressUiSfx) SoundEffectsManager.Instance?.PlayUiConfirm();

            StopAllCoroutines();

            isTowerScrolling = false;

            inTowerScreen = false;

            inMainMenu = false;

            inSideSelect = false;

            inCharSelect = false;

            inStageSelect = false;

            isPaused = false;

            Time.timeScale = 1f;

            if (towerPanel != null) towerPanel.SetActive(false);

            if (resultPanel != null) resultPanel.SetActive(false);

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            currentTowerFloor = Mathf.Clamp(floor, 0, 6);

            selectedRivalIndex = (towerRivalIndices != null && currentTowerFloor < towerRivalIndices.Length)

                ? towerRivalIndices[currentTowerFloor]

                : 1;

            // Sorteo aleatorio de escenario en cada combate de la torre (5 escenarios disponibles)

            int stageCount = (stages != null && stages.Length > 0) ? stages.Length : 5;

            int nextStage = Random.Range(0, stageCount);

            if (stageCount > 1 && nextStage == selectedStageIndex)

            {

                nextStage = (nextStage + 1) % stageCount;

            }

            selectedStageIndex = nextStage;

            ApplyStageSelection(selectedStageIndex);

            ApplyCharacterSelectionToFighters();

            RestartMatch();

        }

        private int _lastRematchFrame = -100;
        private float _lastRematchTime = -10f;

        public void ReturnFromMatchResult()
        {
            if (_lastRematchFrame == Time.frameCount || (Time.unscaledTime - _lastRematchTime) < 0.08f) return;
            _lastRematchFrame = Time.frameCount;
            _lastRematchTime = Time.unscaledTime;
            if (!_suppressUiSfx) SoundEffectsManager.Instance?.PlayUiConfirm();
            ShowMainMenu();
        }

        public void OnTowerFloorCleared()

        {
            if (_lastRematchFrame == Time.frameCount || (Time.unscaledTime - _lastRematchTime) < 0.08f) return;
            _lastRematchFrame = Time.frameCount;
            _lastRematchTime = Time.unscaledTime;
            matchEnded = false;

            StopAllCoroutines();

            if (towerFloorsDefeated != null && currentTowerFloor < towerFloorsDefeated.Length)

            {

                towerFloorsDefeated[currentTowerFloor] = true;

            }

            if (currentTowerFloor >= 6)

            {

                ShowTowerVictoryModal();

            }

            else

            {

                currentTowerFloor++;

                OpenTowerScreen(instantScroll: false);

            }

        }

        public IEnumerator AutoAdvanceTowerFloorAfterDelay(float delay)

        {

            yield return new WaitForSecondsRealtime(delay);

            if (matchEnded && isTowerMode && !inMainMenu && !isPaused)

            {

                OnTowerFloorCleared();

            }

        }

        public void ShowTowerVictoryModal()

        {

            inTowerScreen = true;

            isPaused = false;

            Time.timeScale = 1f;

            if (resultPanel != null) resultPanel.SetActive(false);

            if (towerPanel != null)

            {

                towerPanel.SetActive(true);

                towerPanel.transform.SetAsLastSibling();

            }

            UpdateTowerSlotsVisuals();

            float targetY = GetTowerYForFloor(6);

            if (towerContainer != null) towerContainer.anchoredPosition = new Vector2(0f, targetY);

            if (towerFondoRect != null) towerFondoRect.anchoredPosition = new Vector2(0f, targetY * 0.35f);

            if (towerVictoryModal != null)

            {

                towerVictoryModal.SetActive(true);

                towerVictoryModal.transform.SetAsLastSibling();

            }

        }

        // =========================================================================

        // =========================================================================
        // MENÚ DE PAUSA (Pause Menu: SEGUIR, REVANCHA, VOLVER AL MENÚ)
        // =========================================================================

        private float _lastPauseInputTime = -10f;
        private float _lastPauseToggleTime = -10f;
        private float _ignoreInputTimerAfterUnpause = 0f;

        public void TogglePause()
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        public void PauseGame()
        {
            if (isPaused) return;
            if (matchEnded || inMainMenu || inSideSelect || inCharSelect || inStageSelect || inTowerScreen) return;

            float now = Time.unscaledTime;
            if (now - _lastPauseToggleTime < 0.15f) return;
            _lastPauseToggleTime = now;

            SoundEffectsManager.Instance?.PlayUiConfirm();
            SoundEffectsManager.Instance?.PauseCombatAudio();
            PauseStageMusic();

            isPaused = true;
            Time.timeScale = 0f;

            EnsurePauseMenuUI();

            pauseSelectedIndex = 0; // Por defecto resaltar "SEGUIR"
            UpdatePauseMenuSelectionVisuals();

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
                pausePanel.transform.SetAsLastSibling();
            }
        }

        public void ResumeGame()
        {
            float now = Time.unscaledTime;
            _lastPauseToggleTime = now;

            SoundEffectsManager.Instance?.PlayUiConfirm();
            SoundEffectsManager.Instance?.ResumeCombatAudio();
            ResumeStageMusic();

            isPaused = false;
            Time.timeScale = 1f;

            if (pausePanel != null) pausePanel.SetActive(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

            _ignoreInputTimerAfterUnpause = 0.08f;
        }

        public void RematchFromPause()
        {
            float now = Time.unscaledTime;
            _lastPauseToggleTime = now;

            SoundEffectsManager.Instance?.PlayUiConfirm();
            SoundEffectsManager.Instance?.StopCombatAudio();

            isPaused = false;
            Time.timeScale = 1f;

            if (pausePanel != null) pausePanel.SetActive(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

            _ignoreInputTimerAfterUnpause = 0.08f;

            RestartCurrentModeMatch();
        }

        public void ExitMatchToMenu()
        {
            float now = Time.unscaledTime;
            _lastPauseToggleTime = now;

            SoundEffectsManager.Instance?.PlayUiConfirm();
            SoundEffectsManager.Instance?.StopCombatAudio();

            isPaused = false;
            Time.timeScale = 1f;

            if (pausePanel != null) pausePanel.SetActive(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

            _ignoreInputTimerAfterUnpause = 0.08f;

            ShowMainMenu();
        }

        public void EnsurePauseMenuUI()
        {
            if (pausePanel == null)
            {
                var hud = GameObject.Find("HUD_Canvas");
                if (hud != null)
                {
                    var pTrans = hud.transform.Find("Pause_Panel");
                    if (pTrans != null) pausePanel = pTrans.gameObject;
                }
            }
            if (pausePanel == null) return;

            Transform dialog = pausePanel.transform.Find("Pause_Dialog_Box");
            if (dialog == null) return;
            Transform inner = dialog.Find("Pause_Inner_Frame");
            if (inner == null) inner = dialog;

            // 1. Título
            Transform titleTrans = inner.Find("Pause_Title_Text");
            if (titleTrans != null)
            {
                var titleTxt = titleTrans.GetComponent<Text>();
                if (titleTxt != null)
                {
                    titleTxt.text = "PAUSA";
                    titleTxt.alignment = TextAnchor.MiddleCenter;
                    titleTxt.fontStyle = FontStyle.Bold;
                    titleTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
                }
                var titleRT = titleTrans.GetComponent<RectTransform>();
                if (titleRT != null)
                {
                    titleRT.anchorMin = new Vector2(0f, 0.76f);
                    titleRT.anchorMax = new Vector2(1f, 0.98f);
                    titleRT.offsetMin = Vector2.zero;
                    titleRT.offsetMax = Vector2.zero;
                }
            }

            // 2. Botón 0: Seguir
            if (pauseResumeButton == null)
            {
                var b = inner.Find("Button_SeguirJugando");
                if (b == null) b = inner.Find("Button_Seguir");
                if (b != null) pauseResumeButton = b.GetComponent<Button>();
            }

            // 3. Botón 2: Volver al menú
            if (pauseExitMenuButton == null)
            {
                var b = inner.Find("Button_SalirAlMenu");
                if (b == null) b = inner.Find("Button_VolverAlMenu");
                if (b != null) pauseExitMenuButton = b.GetComponent<Button>();
            }

            // 4. Botón 1: Revancha
            if (pauseRematchButton == null)
            {
                var b = inner.Find("Button_Revancha");
                if (b != null)
                {
                    pauseRematchButton = b.GetComponent<Button>();
                }
                else if (pauseResumeButton != null)
                {
                    GameObject rematchObj = GameObject.Instantiate(pauseResumeButton.gameObject, inner);
                    rematchObj.name = "Button_Revancha";
                    pauseRematchButton = rematchObj.GetComponent<Button>();
                    rematchObj.transform.SetSiblingIndex(pauseResumeButton.transform.GetSiblingIndex() + 1);
                }
            }

            // Ajustar tamaño del cuadro de diálogo
            var dRT = dialog.GetComponent<RectTransform>();
            if (dRT != null)
            {
                dRT.sizeDelta = new Vector2(460f, 340f);
            }

            // Formatear botones: posiciones, tamaños, textos y eventos
            Vector2 btnSize = new Vector2(330f, 48f);
            string[] labels = new string[] { "SEGUIR", "REVANCHA", "VOLVER AL MENÚ" };
            float[] yAnchors = new float[] { 0.58f, 0.38f, 0.18f };
            Button[] btns = new Button[] { pauseResumeButton, pauseRematchButton, pauseExitMenuButton };

            for (int i = 0; i < 3; i++)
            {
                if (btns[i] == null) continue;
                var rt = btns[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, yAnchors[i]);
                    rt.anchorMax = new Vector2(0.5f, yAnchors[i]);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = btnSize;
                }

                var txt = btns[i].GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = labels[i];
                    txt.fontSize = 20;
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.fontStyle = FontStyle.Bold;
                }

                // Soporte para hover de mouse
                int btnIndex = i;
                var trigger = btns[i].gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null) trigger = btns[i].gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                trigger.triggers.Clear();

                var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
                entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
                entry.callback.AddListener((data) =>
                {
                    if (pauseSelectedIndex != btnIndex)
                    {
                        pauseSelectedIndex = btnIndex;
                        SoundEffectsManager.Instance?.PlayUiMove();
                        UpdatePauseMenuSelectionVisuals();
                    }
                });
                trigger.triggers.Add(entry);
            }

            // Listeners onClick
            if (pauseResumeButton != null)
            {
                pauseResumeButton.onClick.RemoveAllListeners();
                pauseResumeButton.onClick.AddListener(ResumeGame);
            }
            if (pauseRematchButton != null)
            {
                pauseRematchButton.onClick.RemoveAllListeners();
                pauseRematchButton.onClick.AddListener(RematchFromPause);
            }
            if (pauseExitMenuButton != null)
            {
                pauseExitMenuButton.onClick.RemoveAllListeners();
                pauseExitMenuButton.onClick.AddListener(ExitMatchToMenu);
            }
        }

        public void EnsureParryTrackerUI()
        {
            if (superMeterRoot == null)
            {
                var hud = GameObject.Find("HUD_Canvas");
                if (hud != null)
                {
                    var smTrans = hud.transform.Find("Super_Meters_Root");
                    if (smTrans != null) superMeterRoot = smTrans.gameObject;
                }
            }
            if (superMeterRoot == null) return;

            Transform root = superMeterRoot.transform;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. P1 Parry Tracker
            Transform p1Tr = root.Find("P1_Parry_Tracker");
            if (p1Tr == null)
            {
                var p1Go = new GameObject("P1_Parry_Tracker", typeof(RectTransform), typeof(Image));
                p1Go.transform.SetParent(root, false);
                var rt = p1Go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(35f, 48f);
                rt.sizeDelta = new Vector2(200f, 18f);

                var bg = p1Go.GetComponent<Image>();
                bg.color = new Color(0.06f, 0.09f, 0.15f, 0.82f);

                var lbl = new GameObject("Label", typeof(RectTransform), typeof(Text));
                lbl.transform.SetParent(p1Go.transform, false);
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(0f, 1f);
                lrt.pivot = new Vector2(0f, 0.5f);
                lrt.anchoredPosition = new Vector2(8f, 0f);
                lrt.sizeDelta = new Vector2(95f, 0f);
                var txt = lbl.GetComponent<Text>();
                txt.font = font;
                txt.fontSize = 11;
                txt.fontStyle = FontStyle.Bold;
                txt.text = "DEF. PERFECTA";
                txt.color = new Color(0.75f, 0.88f, 1.0f, 0.95f);
                txt.alignment = TextAnchor.MiddleLeft;

                for (int i = 0; i < 3; i++)
                {
                    var pip = new GameObject($"Pip_{i}", typeof(RectTransform), typeof(Image));
                    pip.transform.SetParent(p1Go.transform, false);
                    var prt = pip.GetComponent<RectTransform>();
                    prt.anchorMin = new Vector2(0f, 0.5f);
                    prt.anchorMax = new Vector2(0f, 0.5f);
                    prt.pivot = new Vector2(0.5f, 0.5f);
                    prt.anchoredPosition = new Vector2(115f + i * 16f, 0f);
                    prt.sizeDelta = new Vector2(11f, 11f);
                    var img = pip.GetComponent<Image>();
                    img.color = new Color(0.18f, 0.24f, 0.35f, 0.90f);
                }

                var cnt = new GameObject("CounterText", typeof(RectTransform), typeof(Text));
                cnt.transform.SetParent(p1Go.transform, false);
                var crt = cnt.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0f, 0f);
                crt.anchorMax = new Vector2(1f, 1f);
                crt.pivot = new Vector2(1f, 0.5f);
                crt.anchoredPosition = new Vector2(-8f, 0f);
                crt.sizeDelta = new Vector2(0f, 0f);
                var ctxt = cnt.GetComponent<Text>();
                ctxt.font = font;
                ctxt.fontSize = 11;
                ctxt.fontStyle = FontStyle.Bold;
                ctxt.text = "0/3";
                ctxt.color = new Color(0.55f, 0.65f, 0.80f, 0.85f);
                ctxt.alignment = TextAnchor.MiddleRight;

                p1Tr = p1Go.transform;
            }

            p1ParryTrackerRoot = p1Tr.gameObject;
            if (p1ParryPips == null || p1ParryPips.Length != 3) p1ParryPips = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var pipTr = p1Tr.Find($"Pip_{i}");
                if (pipTr != null) p1ParryPips[i] = pipTr.GetComponent<Image>();
            }
            var p1CntTr = p1Tr.Find("CounterText");
            if (p1CntTr != null) p1ParryCounterText = p1CntTr.GetComponent<Text>();

            // 2. P2 Parry Tracker
            Transform p2Tr = root.Find("P2_Parry_Tracker");
            if (p2Tr == null)
            {
                var p2Go = new GameObject("P2_Parry_Tracker", typeof(RectTransform), typeof(Image));
                p2Go.transform.SetParent(root, false);
                var rt = p2Go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-35f, 48f);
                rt.sizeDelta = new Vector2(200f, 18f);

                var bg = p2Go.GetComponent<Image>();
                bg.color = new Color(0.06f, 0.09f, 0.15f, 0.82f);

                var cnt = new GameObject("CounterText", typeof(RectTransform), typeof(Text));
                cnt.transform.SetParent(p2Go.transform, false);
                var crt = cnt.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0f, 0f);
                crt.anchorMax = new Vector2(0f, 1f);
                crt.pivot = new Vector2(0f, 0.5f);
                crt.anchoredPosition = new Vector2(8f, 0f);
                crt.sizeDelta = new Vector2(32f, 0f);
                var ctxt = cnt.GetComponent<Text>();
                ctxt.font = font;
                ctxt.fontSize = 11;
                ctxt.fontStyle = FontStyle.Bold;
                ctxt.text = "0/3";
                ctxt.color = new Color(0.55f, 0.65f, 0.80f, 0.85f);
                ctxt.alignment = TextAnchor.MiddleLeft;

                for (int i = 0; i < 3; i++)
                {
                    var pip = new GameObject($"Pip_{i}", typeof(RectTransform), typeof(Image));
                    pip.transform.SetParent(p2Go.transform, false);
                    var prt = pip.GetComponent<RectTransform>();
                    prt.anchorMin = new Vector2(0f, 0.5f);
                    prt.anchorMax = new Vector2(0f, 0.5f);
                    prt.pivot = new Vector2(0.5f, 0.5f);
                    prt.anchoredPosition = new Vector2(50f + i * 16f, 0f);
                    prt.sizeDelta = new Vector2(11f, 11f);
                    var img = pip.GetComponent<Image>();
                    img.color = new Color(0.18f, 0.24f, 0.35f, 0.90f);
                }

                var lbl = new GameObject("Label", typeof(RectTransform), typeof(Text));
                lbl.transform.SetParent(p2Go.transform, false);
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(1f, 1f);
                lrt.pivot = new Vector2(1f, 0.5f);
                lrt.anchoredPosition = new Vector2(-8f, 0f);
                lrt.sizeDelta = new Vector2(0f, 0f);
                var txt = lbl.GetComponent<Text>();
                txt.font = font;
                txt.fontSize = 11;
                txt.fontStyle = FontStyle.Bold;
                txt.text = "DEF. PERFECTA";
                txt.color = new Color(0.75f, 0.88f, 1.0f, 0.95f);
                txt.alignment = TextAnchor.MiddleRight;

                p2Tr = p2Go.transform;
            }

            p2ParryTrackerRoot = p2Tr.gameObject;
            if (p2ParryPips == null || p2ParryPips.Length != 3) p2ParryPips = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var pipTr = p2Tr.Find($"Pip_{i}");
                if (pipTr != null) p2ParryPips[i] = pipTr.GetComponent<Image>();
            }
            var p2CntTr = p2Tr.Find("CounterText");
            if (p2CntTr != null) p2ParryCounterText = p2CntTr.GetComponent<Text>();
        }

        private Button[] GetPauseMenuButtons()
        {
            EnsurePauseMenuUI();
            return new Button[] { pauseResumeButton, pauseRematchButton, pauseExitMenuButton };
        }

        public void UpdatePauseMenuSelectionVisuals()
        {
            Button[] btns = GetPauseMenuButtons();
            if (btns == null || btns.Length < 3) return;

            for (int i = 0; i < btns.Length; i++)
            {
                if (btns[i] == null) continue;
                bool isSelected = (i == pauseSelectedIndex);
                btns[i].transform.localScale = isSelected ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;

                var img = btns[i].GetComponent<Image>();
                if (img != null)
                {
                    Color baseColor;
                    switch (i)
                    {
                        case 0: // SEGUIR (verde arcade)
                            baseColor = isSelected ? new Color(0.24f, 0.70f, 0.36f, 1f) : new Color(0.16f, 0.46f, 0.24f, 1f);
                            break;
                        case 1: // REVANCHA (azul acero arcade)
                            baseColor = isSelected ? new Color(0.26f, 0.52f, 0.88f, 1f) : new Color(0.18f, 0.36f, 0.62f, 1f);
                            break;
                        case 2: // VOLVER AL MENÚ (carmesí arcade)
                        default:
                            baseColor = isSelected ? new Color(0.72f, 0.22f, 0.22f, 1f) : new Color(0.50f, 0.16f, 0.16f, 1f);
                            break;
                    }
                    img.color = baseColor;
                }

                var txt = btns[i].GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.color = isSelected ? new Color(1f, 0.95f, 0.45f, 1f) : Color.white;
                    txt.fontStyle = FontStyle.Bold;
                }
            }

            if (btns.Length > pauseSelectedIndex && btns[pauseSelectedIndex] != null)
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(btns[pauseSelectedIndex].gameObject);
            }
        }

        private void HandlePauseMenuInput()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            float now = Time.unscaledTime;

            FighterInputFrame p1In = (ControlBindingsManager.Instance != null)
                ? ControlBindingsManager.Instance.ReadPlayerInput(1)
                : default;

            bool up = (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame))
                      || (gp != null && (gp.dpad.up.wasPressedThisFrame || (gp.leftStick.up.isPressed && now - _lastPauseInputTime > 0.22f)))
                      || p1In.upPressedThisFrame;

            bool down = (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame))
                        || (gp != null && (gp.dpad.down.wasPressedThisFrame || (gp.leftStick.down.isPressed && now - _lastPauseInputTime > 0.22f)))
                        || p1In.downPressedThisFrame;

            bool submit = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                          || (gp != null && gp.buttonSouth.wasPressedThisFrame)
                          || p1In.punchPressedThisFrame || p1In.kickPressedThisFrame;

            bool escapeOrBack = (kb != null && kb.escapeKey.wasPressedThisFrame)
                                || (gp != null && (gp.startButton.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame))
                                || p1In.blockPressedThisFrame;

            if (escapeOrBack)
            {
                if (now - _lastPauseToggleTime >= 0.15f)
                {
                    ResumeGame();
                    return;
                }
            }

            int totalButtons = 3;
            int prevIdx = pauseSelectedIndex;

            if (up)
            {
                _lastPauseInputTime = now;
                pauseSelectedIndex = (pauseSelectedIndex - 1 + totalButtons) % totalButtons;
                if (pauseSelectedIndex != prevIdx)
                {
                    SoundEffectsManager.Instance?.PlayUiMove();
                    UpdatePauseMenuSelectionVisuals();
                }
            }
            else if (down)
            {
                _lastPauseInputTime = now;
                pauseSelectedIndex = (pauseSelectedIndex + 1) % totalButtons;
                if (pauseSelectedIndex != prevIdx)
                {
                    SoundEffectsManager.Instance?.PlayUiMove();
                    UpdatePauseMenuSelectionVisuals();
                }
            }

            if (submit)
            {
                ActivatePauseMenuSelection();
            }
        }

        public void ActivatePauseMenuSelection()
        {
            switch (pauseSelectedIndex)
            {
                case 0:
                    ResumeGame();
                    break;
                case 1:
                    RematchFromPause();
                    break;
                case 2:
                    ExitMatchToMenu();
                    break;
            }
        }

        public void StartMatchWithRandomRival()

        {

            isVSMode = false;

            inMainMenu = false;

            inSideSelect = false;

            inCharSelect = false;

            inStageSelect = false;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            if (sideSelectPanel != null) sideSelectPanel.SetActive(false);

            if (charSelectPanel != null) charSelectPanel.SetActive(false);

            if (stageSelectPanel != null) stageSelectPanel.SetActive(false);

            if (roster != null)

            {

                foreach (var entry in roster)

                {

                    if (entry != null && entry.anims != null)

                    {

                        FighterController.EnsureSpecialAttackAnimation(entry.characterName, entry.anims);

                    }

                }

            }

            selectedRivalIndex = PickRandomRivalIndex(selectedCharIndex);

            ApplyStageSelection(selectedStageIndex);

            ApplyCharacterSelectionToFighters();

            RestartMatch();

        }

        public void StartVSMatch()

        {

            isVSMode = true;

            inMainMenu = false;

            inSideSelect = false;

            inCharSelect = false;

            inStageSelect = false;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            if (sideSelectPanel != null) sideSelectPanel.SetActive(false);

            if (charSelectPanel != null) charSelectPanel.SetActive(false);

            if (stageSelectPanel != null) stageSelectPanel.SetActive(false);

            ApplyStageSelection(selectedStageIndex);

            if (player1 != null && player2 != null && roster != null && roster.Length >= 2)

            {

                var pLeftEntry = roster[vsLeftCharIndex];

                var pRightEntry = roster[vsRightCharIndex];

                player1.ApplyCharacterProfile(pLeftEntry.characterName, pLeftEntry.visualScale, pLeftEntry.anims);

                player2.ApplyCharacterProfile(pRightEntry.characterName, pRightEntry.visualScale, pRightEntry.anims);

                player1.gameObject.name = $"P{leftSidePlayerIndex}_{pLeftEntry.characterName}";

                player2.gameObject.name = $"P{rightSidePlayerIndex}_{pRightEntry.characterName}";

                player1.playerIndex = 1;

                player1.inputPlayerIndex = leftSidePlayerIndex;

                player1.isAI = false;

                player1.opponent = player2;

                player2.playerIndex = 2;

                player2.inputPlayerIndex = rightSidePlayerIndex;

                player2.isAI = false; // IN VS MODE: BOTH PLAYERS ARE HUMAN CONTROLLED!

                player2.opponent = player1;

                if (p1PortraitImage != null && player1.anims != null)

                {

                    p1PortraitImage.sprite = player1.anims.portrait;

                }

                if (p2PortraitImage != null && player2.anims != null)

                {

                    p2PortraitImage.sprite = player2.anims.portrait;

                }

            }

            RestartMatch();

        }

        public void RestartCurrentModeMatch()

        {
            if (_lastRematchFrame == Time.frameCount || (Time.unscaledTime - _lastRematchTime) < 0.08f)
            {
                return;
            }
            if (inMainMenu || inSideSelect || inCharSelect || inStageSelect || inTowerScreen)
            {
                return;
            }
            _lastRematchFrame = Time.frameCount;
            _lastRematchTime = Time.unscaledTime;

            if (!_suppressUiSfx) SoundEffectsManager.Instance?.PlayUiConfirm();

            if (isVSMode)

            {

                StartVSMatch();

            }

            else if (isTowerMode)

            {

                StartTowerFight(currentTowerFloor);

            }

            else

            {

                int stageCount = (stages != null && stages.Length > 0) ? stages.Length : 5;

                selectedStageIndex = Mathf.Clamp(selectedStageIndex, 0, stageCount - 1);

                // En Modo Pelea: revancha contra el rival y escenario actuales
                if (selectedRivalIndex < 0 || selectedRivalIndex == selectedCharIndex)
                {
                    selectedRivalIndex = PickRandomRivalIndex(selectedCharIndex);
                }

                ApplyStageSelection(selectedStageIndex);

                ApplyCharacterSelectionToFighters();

                RestartMatch();

            }

        }

        public void ConfirmCharacterAndStartMatch()

        {

            StartFightFromStageSelect();

        }

        public void StartFightFromMenu()

        {

            StartPeleaMode();

        }

        public void ApplyCharacterSelectionToFighters()

        {

            if (player1 == null || player2 == null) return;

            if (roster != null && roster.Length >= 2)

            {
                foreach (var entry in roster)
                {
                    if (entry != null && entry.anims != null)
                    {
                        FighterController.EnsureDamageAnimation(entry.characterName, entry.anims);
                        FighterController.EnsureSpecialAttackAnimation(entry.characterName, entry.anims);
                    }
                }
                selectedCharIndex = Mathf.Clamp(selectedCharIndex, 0, roster.Length - 1);

                if (selectedRivalIndex < 0 || selectedRivalIndex >= roster.Length || selectedRivalIndex == selectedCharIndex)

                {

                    if (roster != null)

            {

                foreach (var entry in roster)

                {

                    if (entry != null && entry.anims != null)

                    {

                        FighterController.EnsureSpecialAttackAnimation(entry.characterName, entry.anims);

                    }

                }

            }

            selectedRivalIndex = PickRandomRivalIndex(selectedCharIndex);

                }

                var p1Entry = roster[selectedCharIndex];

                var p2Entry = roster[selectedRivalIndex];

                player1.ApplyCharacterProfile(p1Entry.characterName, p1Entry.visualScale, p1Entry.anims);

                player2.ApplyCharacterProfile(p2Entry.characterName, p2Entry.visualScale, p2Entry.anims);

                player1.gameObject.name = $"P1_{p1Entry.characterName}";

                player2.gameObject.name = $"P2_{p2Entry.characterName}";

            }

            player1.playerIndex = 1;

            player1.inputPlayerIndex = 1;

            player1.isAI = false;

            player1.opponent = player2;

            player2.playerIndex = 2;

            player2.inputPlayerIndex = 2;

            player2.isAI = true; // In Pelea mode: Rival is AI!

            player2.aiDifficulty = (FighterController.AIDifficulty)OptionsManager.ActiveAIDifficulty;

            player2.opponent = player1;

            if (p1PortraitImage != null && player1.anims != null && player1.anims.portrait != null)

            {

                p1PortraitImage.sprite = player1.anims.portrait;

            }

            if (p2PortraitImage != null && player2.anims != null && player2.anims.portrait != null)

            {

                p2PortraitImage.sprite = player2.anims.portrait;

            }

        }

        public void RestartMatch()

        {
            StopAllCoroutines();
            isTowerScrolling = false;
            isPaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

            SetFightersActiveForCombat(true);

            currentRound = 1;

            p1RoundWins = 0;

            p2RoundWins = 0;

            matchEnded = false;

            roundTransitionActive = false;

            if (player1 != null)
            {
                player1.ResetSuper();
                player1.ClearCombatPolishEffects();
                player1.perfectDefenseStreak = 0;
            }

            if (player2 != null)
            {
                if (player2.isAI)
                {
                    player2.aiDifficulty = (FighterController.AIDifficulty)OptionsManager.ActiveAIDifficulty;
                }
                player2.ResetSuper();
                player2.ClearCombatPolishEffects();
                player2.perfectDefenseStreak = 0;
            }

            EnsureParryTrackerUI();
            UpdateHUD();
            UpdateStageMusic();

            ResetRoundState();

        }

        public void StartNextRound()

        {

            currentRound++;

            roundTransitionActive = false;

            matchEnded = false;

            ResetRoundState();

        }

        private void ResetRoundState()

        {

            currentTimer = roundDuration;

            manualCamPanOffset = 0f;

            if (resultPanel != null) resultPanel.SetActive(false);

            if (player1 != null && player2 != null)

            {

                player1.opponent = player2;

                player2.opponent = player1;

                player1.groundY = p1StartPos.y;

                player2.groundY = p2StartPos.y;

                player1.ResetFighter(p1StartPos, true);

                player2.ResetFighter(p2StartPos, false);

            }

            if (mainCamera != null)

            {

                mainCamera.transform.position = new Vector3(0f, 0f, -10f);

            }

            UpdateHUD();

        }

        private void Update()
        {
            if (bgmSource != null && !bgmSource.loop)
            {
                bgmSource.loop = true;
            }

            var kb = Keyboard.current;

            var gp = Gamepad.current;

            bool pausePressed = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.startButton.wasPressedThisFrame);

            if (isPaused)
            {
                HandlePauseMenuInput();
                return;
            }

            if (inTowerScreen)

            {

                if (towerVictoryModal != null && towerVictoryModal.activeSelf)

                {

                    if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))

                    {

                        ShowMainMenu();

                    }

                    return;

                }

                if (!isTowerScrolling)

                {

                    if (kb != null)

                    {

                        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)

                        {

                            StartTowerFight(currentTowerFloor);

                        }

                        else if (kb.escapeKey.wasPressedThisFrame)

                        {

                            ShowMainMenu();

                        }

                    }

                }

                return;

            }

            if (mainMenuPanel != null && mainMenuPanel.activeInHierarchy)
            {
                inMainMenu = true;
            }

            if (inMainMenu)
            {
                if ((player1 != null && player1.gameObject.activeSelf) || (player2 != null && player2.gameObject.activeSelf))
                {
                    SetFightersActiveForCombat(false);
                }
                bool optionsOpen = optionsManager != null && optionsManager.optionsPanel != null && optionsManager.optionsPanel.activeSelf;
                if (!optionsOpen)
                {
                    HandleMainMenuInput();
                }
                return;
            }

            if (inSideSelect)

            {

                HandleSideSelectInput();

                return;

            }

            if (inCharSelect)

            {

                if (isVSMode)

                {

                    HandleVSCharSelectInput();

                }

                else
                {
                    int count = (roster != null && roster.Length > 0) ? roster.Length : 11;
                    FighterInputFrame p1In = (ControlBindingsManager.Instance != null)
                        ? ControlBindingsManager.Instance.ReadPlayerInput(1)
                        : default;

                    bool navLeft = p1In.leftPressedThisFrame || (kb != null && (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)) || (gp != null && (gp.dpad.left.wasPressedThisFrame || gp.leftStick.left.wasPressedThisFrame));
                    bool navRight = p1In.rightPressedThisFrame || (kb != null && (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)) || (gp != null && (gp.dpad.right.wasPressedThisFrame || gp.leftStick.right.wasPressedThisFrame));
                    bool navUp = p1In.upPressedThisFrame || (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) || (gp != null && (gp.dpad.up.wasPressedThisFrame || gp.leftStick.up.wasPressedThisFrame));
                    bool navDown = p1In.downPressedThisFrame || (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) || (gp != null && (gp.dpad.down.wasPressedThisFrame || gp.leftStick.down.wasPressedThisFrame));
                    bool navConfirm = p1In.punchPressedThisFrame || p1In.kickPressedThisFrame || (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) || (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame));
                    bool navCancel = p1In.blockPressedThisFrame || (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);

                    if (navLeft)
                    {
                        SelectCharacter((selectedCharIndex - 1 + count) % count);
                    }
                    else if (navRight)
                    {
                        SelectCharacter((selectedCharIndex + 1) % count);
                    }
                    else if (navUp)
                    {
                        int nextIdx = (selectedCharIndex >= 6)
                            ? (selectedCharIndex - 6)
                            : Mathf.Min(count - 1, selectedCharIndex + 6);
                        SelectCharacter(nextIdx);
                    }
                    else if (navDown)
                    {
                        int nextIdx = (selectedCharIndex < 6)
                            ? Mathf.Min(count - 1, selectedCharIndex + 6)
                            : (selectedCharIndex - 6);
                        SelectCharacter(nextIdx);
                    }
                    else if (navConfirm)
                    {
                        OnConfirmCharButtonClicked();
                    }
                    else if (navCancel)
                    {
                        SoundEffectsManager.Instance?.PlayUiConfirm();
                        ShowMainMenu();
                    }
                }

                return;

            }

            if (inStageSelect)

            {

                int count = (stages != null && stages.Length > 0) ? stages.Length : 5;

                FighterInputFrame p1In = (ControlBindingsManager.Instance != null)

                    ? ControlBindingsManager.Instance.ReadPlayerInput(1)

                    : default;

                FighterInputFrame p2In = (ControlBindingsManager.Instance != null)

                    ? ControlBindingsManager.Instance.ReadPlayerInput(2)

                    : default;

                bool leftNav = p1In.leftPressedThisFrame || p2In.leftPressedThisFrame || (kb != null && (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame));

                bool rightNav = p1In.rightPressedThisFrame || p2In.rightPressedThisFrame || (kb != null && (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame));

                bool confirmNav = p1In.punchPressedThisFrame || p1In.kickPressedThisFrame || p2In.punchPressedThisFrame || p2In.kickPressedThisFrame || (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame));

                bool cancelNav = p1In.blockPressedThisFrame || p2In.blockPressedThisFrame || (kb != null && kb.escapeKey.wasPressedThisFrame);

                if (leftNav)

                {

                    SelectStage((selectedStageIndex - 1 + count) % count);

                }

                else if (rightNav)

                {

                    SelectStage((selectedStageIndex + 1) % count);

                }

                else if (confirmNav)

                {

                    StartFightFromStageSelect();

                }

                else if (cancelNav)

                {
                    SoundEffectsManager.Instance?.PlayUiConfirm();

                    OpenCharSelect(isVSMode);

                }

                return;

            }

            // Escape or Gamepad Start during active fight triggers Pause

            if (pausePressed)

            {

                if (!matchEnded)

                {

                    PauseGame();

                    return;

                }

                else

                {

                    ShowMainMenu();

                    return;

                }

            }

            float dt = Time.deltaTime;

            if (roundTransitionActive)

            {

                _roundTransitionTimer -= dt;

                if (player1 != null && player2 != null)

                {

                    player1.TickFighter(dt, false);

                    player2.TickFighter(dt, false);

                }

                UpdateCamera(dt);

                UpdateHUD();

                if (_roundTransitionTimer <= 0f || (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)))

                {

                    StartNextRound();

                }

                return;

            }

            if (!matchEnded)

            {

                currentTimer -= dt;

                if (currentTimer <= 0f)

                {

                    currentTimer = 0f;

                    EndRoundByTimeout();

                }

            }

            if (kb != null)

            {

                // Toggle controls help overlay with Tab or F1

                if ((kb.tabKey.wasPressedThisFrame || kb.f1Key.wasPressedThisFrame) && controlsOverlay != null)

                {

                    controlsOverlay.SetActive(!controlsOverlay.activeSelf);

                }

                // Manual camera pan test keys: Q / E or [ / ] or 1 / 2 (0 to recenter)

                float panInput = 0f;

                if (kb.qKey.isPressed || kb.digit1Key.isPressed || kb.leftBracketKey.isPressed) panInput -= 1f;

                if (kb.eKey.isPressed || kb.digit2Key.isPressed || kb.rightBracketKey.isPressed) panInput += 1f;

                if (panInput != 0f)

                {

                    manualCamPanOffset = Mathf.Clamp(manualCamPanOffset + panInput * 4.5f * dt, -4.2f, 4.2f);

                }

                if (kb.digit0Key.wasPressedThisFrame)

                {

                    manualCamPanOffset = 0f;

                }

            }

            if (player1 != null && player2 != null)

            {

                float midX = (player1.transform.position.x + player2.transform.position.x) * 0.5f + manualCamPanOffset;

                float camTargetX = Mathf.Clamp(midX, minCamX, maxCamX);

                player1.minArenaX = Mathf.Max(-8.5f, camTargetX - 5.2f);

                player1.maxArenaX = Mathf.Min(8.5f, camTargetX + 5.2f);

                player2.minArenaX = player1.minArenaX;

                player2.maxArenaX = player1.maxArenaX;

                bool allowCombat = !matchEnded && !roundTransitionActive;
                if (_ignoreInputTimerAfterUnpause > 0f)
                {
                    _ignoreInputTimerAfterUnpause -= Time.unscaledDeltaTime;
                    allowCombat = false;
                }

                player1.TickFighter(dt, allowCombat);

                player2.TickFighter(dt, allowCombat);

                if (allowCombat)

                {

                    if (player1.currentHealth <= 0f || player2.currentHealth <= 0f)

                    {

                        EndRoundByKO();

                    }

                }

            }

            UpdateCamera(dt);

            UpdateHUD();

            if (matchEnded && kb != null)

            {

                if (kb.rKey.wasPressedThisFrame)

                {
                    if (isTowerMode && p1RoundWins >= RoundsToWin)
                    {
                        if (currentTowerFloor < 6) OnTowerFloorCleared();
                    }
                    else
                    {
                        RestartCurrentModeMatch();
                    }
                }

                else if (kb.escapeKey.wasPressedThisFrame)

                {

                    ReturnFromMatchResult();

                }

            }

        }

        private void UpdateCamera(float dt)

        {

            if (mainCamera == null || player1 == null || player2 == null) return;

            float midX = (player1.transform.position.x + player2.transform.position.x) * 0.5f + manualCamPanOffset;

            float targetX = Mathf.Clamp(midX, minCamX, maxCamX);

            Vector3 camPos = mainCamera.transform.position;

            camPos.x = Mathf.Lerp(camPos.x, targetX, camSmoothSpeed * dt);

            mainCamera.transform.position = camPos;

        }

        private string FormatWins(int wins)

        {

            if (wins >= 2) return "[★★]";

            if (wins == 1) return "[★☆]";

            return "[☆☆]";

        }

        public void UpdateHUD()

        {

            if (player1 != null && p1HealthFill != null)

            {

                p1HealthFill.fillAmount = Mathf.Clamp01(player1.currentHealth / player1.maxHealth);

            }

            if (player2 != null && p2HealthFill != null)

            {

                p2HealthFill.fillAmount = Mathf.Clamp01(player2.currentHealth / player2.maxHealth);

            }

            if (isVSMode)

            {

                if (player1 != null && p1NameText != null)

                {

                    p1NameText.text = $"{player1.fighterName.ToUpper()} (P{leftSidePlayerIndex}) {FormatWins(p1RoundWins)}";

                }

                if (player2 != null && p2NameText != null)

                {

                    p2NameText.text = $"{FormatWins(p2RoundWins)} {player2.fighterName.ToUpper()} (P{rightSidePlayerIndex})";

                }

            }

            else if (isTowerMode)

            {

                if (player1 != null && p1NameText != null)

                {

                    p1NameText.text = $"{player1.fighterName.ToUpper()} (TÚ) {FormatWins(p1RoundWins)}";

                }

                if (player2 != null && p2NameText != null)

                {

                    string floorTag = (currentTowerFloor == 6) ? "P7 CIMA" : $"PISO {currentTowerFloor + 1}/7";

                    p2NameText.text = $"{FormatWins(p2RoundWins)} {player2.fighterName.ToUpper()} ({floorTag})";

                }

            }

            else

            {

                if (player1 != null && p1NameText != null)

                {

                    p1NameText.text = $"{player1.fighterName.ToUpper()} (TÚ) {FormatWins(p1RoundWins)}";

                }

                if (player2 != null && p2NameText != null)

                {

                    p2NameText.text = $"{FormatWins(p2RoundWins)} {player2.fighterName.ToUpper()} (IA)";

                }

            }

            if (timerText != null)

            {

                timerText.text = Mathf.CeilToInt(currentTimer).ToString("00");

            }

            if (roundInfoText != null)

            {

                if (roundInfoText.rectTransform.sizeDelta.x < 500f)
                {
                    roundInfoText.rectTransform.sizeDelta = new Vector2(520f, 26f);
                }

                string stName = (stages != null && selectedStageIndex >= 0 && selectedStageIndex < stages.Length)

                    ? stages[selectedStageIndex].stageName.ToUpper()

                    : "OBELISCO";

                string modeTag = isVSMode ? "MODO VS" : (isTowerMode ? $"TORRE PISO {currentTowerFloor + 1}/7" : "PELEA");

                roundInfoText.text = $"{modeTag} · {stName} · ROUND {currentRound} ({p1RoundWins} - {p2RoundWins})";

            }

            // Super meter HUD update

            bool showSuperMeters = !inMainMenu && !inSideSelect && !inCharSelect && !inStageSelect && !inTowerScreen;

            if (superMeterRoot != null) superMeterRoot.SetActive(showSuperMeters);

            if (showSuperMeters)

            {

                if (player1 != null && p1SuperFill != null)

                {

                    float p1Ratio = Mathf.Clamp01(player1.currentSuper / FighterController.MaxSuper);

                    p1SuperFill.fillAmount = p1Ratio;

                    bool p1Ready = (player1.currentSuper >= FighterController.MaxSuper);

                    if (p1SuperText != null)

                    {

                        p1SuperText.gameObject.SetActive(p1Ready);

                        if (p1Ready)

                        {

                            int p1DevIdx = isVSMode ? leftSidePlayerIndex : 1;

                            string p1Btn = ControlBindingsManager.Instance != null

                                ? ControlBindingsManager.Instance.GetSpecialButtonPrompt(p1DevIdx)

                                : "Espacio";

                            p1SuperText.text = $"Ataque Especial Listo [{p1Btn}]";

                        }

                    }

                    if (p1Ready)

                    {

                        float pulse = Mathf.PingPong(Time.time * 4f, 1f);

                        p1SuperFill.color = Color.Lerp(Color.white, new Color(0.7f, 0.95f, 1f, 1f), pulse);

                    }

                    else

                    {

                        p1SuperFill.color = Color.white;

                    }

                }

                if (player2 != null && p2SuperFill != null)

                {

                    float p2Ratio = Mathf.Clamp01(player2.currentSuper / FighterController.MaxSuper);

                    p2SuperFill.fillAmount = p2Ratio;

                    bool p2Ready = (player2.currentSuper >= FighterController.MaxSuper);

                    if (p2SuperText != null)

                    {

                        p2SuperText.gameObject.SetActive(p2Ready);

                        if (p2Ready)

                        {

                            int p2DevIdx = isVSMode ? rightSidePlayerIndex : 2;

                            string p2Btn = ControlBindingsManager.Instance != null

                                ? ControlBindingsManager.Instance.GetSpecialButtonPrompt(p2DevIdx)

                                : (isVSMode ? "Enter" : "LT");

                            p2SuperText.text = $"Ataque Especial Listo [{p2Btn}]";

                        }

                    }

                    if (p2Ready)

                    {

                        float pulse = Mathf.PingPong(Time.time * 4f, 1f);

                        p2SuperFill.color = Color.Lerp(Color.white, new Color(0.7f, 0.95f, 1f, 1f), pulse);

                    }

                    else

                    {

                        p2SuperFill.color = Color.white;

                    }

                }

            }

            // Perfect Defense (Parry) Tracker update
            if (p1ParryTrackerRoot == null || p2ParryTrackerRoot == null)
            {
                EnsureParryTrackerUI();
            }

            if (p1ParryTrackerRoot != null) p1ParryTrackerRoot.SetActive(showSuperMeters);
            if (p2ParryTrackerRoot != null) p2ParryTrackerRoot.SetActive(showSuperMeters);

            if (showSuperMeters)
            {
                int s1 = player1 != null ? Mathf.Clamp(player1.perfectDefenseStreak, 0, 3) : 0;
                if (p1ParryCounterText != null)
                {
                    p1ParryCounterText.text = $"{s1}/3";
                    p1ParryCounterText.color = (s1 >= 3)
                        ? new Color(1f, 0.92f, 0.25f, 1f)
                        : ((s1 > 0) ? new Color(0.20f, 0.90f, 1f, 1f) : new Color(0.55f, 0.65f, 0.80f, 0.85f));
                }
                if (p1ParryPips != null)
                {
                    for (int i = 0; i < p1ParryPips.Length; i++)
                    {
                        if (p1ParryPips[i] == null) continue;
                        p1ParryPips[i].color = (i < s1)
                            ? ((s1 >= 3) ? new Color(1f, 0.92f, 0.25f, 1f) : new Color(0.20f, 0.90f, 1f, 1f))
                            : new Color(0.18f, 0.24f, 0.35f, 0.90f);
                    }
                }

                int s2 = player2 != null ? Mathf.Clamp(player2.perfectDefenseStreak, 0, 3) : 0;
                if (p2ParryCounterText != null)
                {
                    p2ParryCounterText.text = $"{s2}/3";
                    p2ParryCounterText.color = (s2 >= 3)
                        ? new Color(1f, 0.92f, 0.25f, 1f)
                        : ((s2 > 0) ? new Color(0.20f, 0.90f, 1f, 1f) : new Color(0.55f, 0.65f, 0.80f, 0.85f));
                }
                if (p2ParryPips != null)
                {
                    for (int i = 0; i < p2ParryPips.Length; i++)
                    {
                        if (p2ParryPips[i] == null) continue;
                        p2ParryPips[i].color = (i < s2)
                            ? ((s2 >= 3) ? new Color(1f, 0.92f, 0.25f, 1f) : new Color(0.20f, 0.90f, 1f, 1f))
                            : new Color(0.18f, 0.24f, 0.35f, 0.90f);
                    }
                }
            }

        }

        public void EndRoundByKO()

        {

            int winner = 0; // 0 = tie, 1 = player1, 2 = player2

            if (player1.currentHealth <= 0f && player2.currentHealth <= 0f)

            {

                winner = 0;

            }

            else if (player2.currentHealth <= 0f)

            {

                winner = 1;

            }

            else

            {

                winner = 2;

            }

            ResolveRoundResult(winner, "¡K.O.!");

        }

        private void EndRoundByTimeout()

        {

            int winner = 0;

            if (Mathf.Approximately(player1.currentHealth, player2.currentHealth))

            {

                winner = 0;

            }

            else if (player1.currentHealth > player2.currentHealth)

            {

                winner = 1;

            }

            else

            {

                winner = 2;

            }

            ResolveRoundResult(winner, "¡TIEMPO!");

        }

        public void ResolveRoundResult(int roundWinner, string reasonPrefix)

        {

            string roundWinnerName = "";

            if (isVSMode)

            {

                int winnerPlayerNum = (roundWinner == 1) ? leftSidePlayerIndex : rightSidePlayerIndex;

                string winnerChar = (roundWinner == 1) ? player1.fighterName : player2.fighterName;

                if (roundWinner == 1)

                {

                    p1RoundWins++;

                    roundWinnerName = $"{winnerChar.ToUpper()} (P{winnerPlayerNum})";

                }

                else if (roundWinner == 2)

                {

                    p2RoundWins++;

                    roundWinnerName = $"{winnerChar.ToUpper()} (P{winnerPlayerNum})";

                }

            }

            else

            {

                if (roundWinner == 1)

                {

                    p1RoundWins++;

                    roundWinnerName = $"{player1.fighterName.ToUpper()} (TÚ)";

                }

                else if (roundWinner == 2)

                {

                    p2RoundWins++;

                    roundWinnerName = $"{player2.fighterName.ToUpper()} (IA)";

                }

            }

            UpdateHUD();

            // Check if match is won (first to 2 rounds)

            if (p1RoundWins >= RoundsToWin || p2RoundWins >= RoundsToWin)

            {

                matchEnded = true;

                roundTransitionActive = false;

                string matchWinner;

                if (isVSMode)

                {

                    int matchWinnerPlayer = (p1RoundWins >= RoundsToWin) ? leftSidePlayerIndex : rightSidePlayerIndex;

                    string matchWinnerChar = (p1RoundWins >= RoundsToWin) ? player1.fighterName.ToUpper() : player2.fighterName.ToUpper();

                    matchWinner = $"¡GANÓ EL JUGADOR {matchWinnerPlayer} CON {matchWinnerChar}!\nResultado: {p1RoundWins} - {p2RoundWins}";

                }

                else if (isTowerMode)

                {

                    if (p1RoundWins >= RoundsToWin)

                    {

                        if (currentTowerFloor >= 6)

                        {

                            matchWinner = $"¡VENCISTE AL RIVAL FINAL ({player2.fighterName.ToUpper()})!\n¡TORRE COMPLETADA!";

                            if (resultTitleText != null) resultTitleText.text = $"{reasonPrefix}\n{matchWinner}";

                            if (resultPanel != null) resultPanel.SetActive(true);

                            if (restartButton != null) restartButton.gameObject.SetActive(false);

                            if (returnToMenuButton != null) returnToMenuButton.gameObject.SetActive(false);

                            StartCoroutine(AutoAdvanceTowerFloorAfterDelay(1.8f));

                            return;

                        }

                        else

                        {

                            matchWinner = $"¡PISO {currentTowerFloor + 1} SUPERADO!\nDerrotaste a {player2.fighterName.ToUpper()} ({p1RoundWins} - {p2RoundWins})\nSubiendo al siguiente piso...";

                            if (resultTitleText != null) resultTitleText.text = $"{reasonPrefix}\n{matchWinner}";

                            if (restartButton != null)

                            {

                                restartButton.gameObject.SetActive(true);

                                var restartText = restartButton.GetComponentInChildren<Text>();

                                if (restartText != null) restartText.text = "SIGUIENTE PISO";

                                restartButton.onClick.RemoveAllListeners();

                                restartButton.onClick.AddListener(OnTowerFloorCleared);

                            }

                            if (player1 != null) player1.ClearCombatPolishEffects();
                            if (player2 != null) player2.ClearCombatPolishEffects();

                            if (returnToMenuButton != null)
                            {
                                returnToMenuButton.gameObject.SetActive(true);
                                var menuText = returnToMenuButton.GetComponentInChildren<Text>();
                                if (menuText != null) menuText.text = "VOLVER AL MENÚ";
                                returnToMenuButton.onClick.RemoveAllListeners();
                                returnToMenuButton.onClick.AddListener(ReturnFromMatchResult);
                            }

                            if (resultPanel != null) resultPanel.SetActive(true);

                            StartCoroutine(AutoAdvanceTowerFloorAfterDelay(2.2f));

                            return;

                        }

                    }

                    else

                    {

                        matchWinner = $"¡CAÍSTE EN EL PISO {currentTowerFloor + 1} ANTE {player2.fighterName.ToUpper()}!\nResultado: {p1RoundWins} - {p2RoundWins}";

                        if (restartButton != null)

                        {

                            restartButton.gameObject.SetActive(true);

                            var restartText = restartButton.GetComponentInChildren<Text>();

                            if (restartText != null) restartText.text = "REVANCHA";

                            restartButton.onClick.RemoveAllListeners();

                            restartButton.onClick.AddListener(RestartCurrentModeMatch);

                        }

                    }

                }

                else

                {

                    matchWinner = p1RoundWins >= RoundsToWin

                        ? $"¡GANASTE LA PARTIDA CON {player1.fighterName.ToUpper()}!\nResultado: {p1RoundWins} - {p2RoundWins}"

                        : $"¡GANA LA IA ({player2.fighterName.ToUpper()})!\nResultado: {p1RoundWins} - {p2RoundWins}";

                }

                if (resultTitleText != null) resultTitleText.text = $"{reasonPrefix}\n{matchWinner}";

                if (player1 != null) player1.ClearCombatPolishEffects();
                if (player2 != null) player2.ClearCombatPolishEffects();

                if (restartButton != null)
                {
                    restartButton.gameObject.SetActive(true);
                    if (!isTowerMode)
                    {
                        var restartText = restartButton.GetComponentInChildren<Text>();
                        if (restartText != null) restartText.text = "REVANCHA";
                        restartButton.onClick.RemoveAllListeners();
                        restartButton.onClick.AddListener(RestartCurrentModeMatch);
                    }
                }

                if (returnToMenuButton != null)
                {
                    returnToMenuButton.gameObject.SetActive(true);
                    var menuText = returnToMenuButton.GetComponentInChildren<Text>();
                    if (menuText != null) menuText.text = "VOLVER AL MENÚ";
                    returnToMenuButton.onClick.RemoveAllListeners();
                    returnToMenuButton.onClick.AddListener(ReturnFromMatchResult);
                }

                if (resultPanel != null) resultPanel.SetActive(true);

            }

            else

            {

                roundTransitionActive = true;

                _roundTransitionTimer = 2.0f;

                string roundMsg = roundWinner == 0

                    ? $"{reasonPrefix} ¡ROUND {currentRound} EMPATADO!\nPreparando siguiente round..."

                    : $"{reasonPrefix} ¡ROUND {currentRound} PARA {roundWinnerName}!\nPreparando Round {currentRound + 1}...";

                if (resultTitleText != null) resultTitleText.text = roundMsg;

                if (restartButton != null) restartButton.gameObject.SetActive(false);

                if (returnToMenuButton != null) returnToMenuButton.gameObject.SetActive(false);

                if (resultPanel != null) resultPanel.SetActive(true);

            }

        }

    

        public void HandleMainMenuInput()

        {

            var kb = Keyboard.current;

            var pad = Gamepad.current;

            bool up = false;

            bool down = false;

            bool submit = false;

            if (kb != null)

            {

                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) up = true;

                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) down = true;

                if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) submit = true;

            }

            if (pad != null)

            {

                if (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame) up = true;

                if (pad.dpad.down.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame) down = true;

                if (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame) submit = true;

            }

            int totalButtons = 5;
            int prevMenuIdx = mainMenuSelectedIndex;

            if (up)
            {
                mainMenuSelectedIndex = (mainMenuSelectedIndex - 1 + totalButtons) % totalButtons;
                if (mainMenuSelectedIndex != prevMenuIdx) SoundEffectsManager.Instance?.PlayUiMove();
                UpdateMainMenuSelectionVisuals();
            }
            else if (down)
            {
                mainMenuSelectedIndex = (mainMenuSelectedIndex + 1) % totalButtons;
                if (mainMenuSelectedIndex != prevMenuIdx) SoundEffectsManager.Instance?.PlayUiMove();
                UpdateMainMenuSelectionVisuals();
            }

            if (submit)
            {
                SoundEffectsManager.Instance?.PlayUiConfirm();
                ActivateMainMenuSelection();
            }

        }

        public void ActivateMainMenuSelection()

        {

            switch (mainMenuSelectedIndex)

            {

                case 0:

                    StartPeleaMode();

                    break;

                case 1:

                    StartTowerModeFromMenu();

                    break;

                case 2:

                    OpenSideSelect();

                    break;

                case 3:

                    if (optionsManager != null) optionsManager.OpenOptionsPanel();

                    break;

                case 4:

                    QuitGame();

                    break;

            }

        }

        public void UpdateMainMenuSelectionVisuals()
        {
            Button[] btns = GetMainMenuButtons();
            Vector2 unifiedSize = new Vector2(246f, 72f);
            float[] yPositions = new float[] { 36f, -48f, -132f, -216f, -300f };

            for (int i = 0; i < btns.Length; i++)
            {
                if (btns[i] == null) continue;

                var rt = btns[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = unifiedSize;
                    rt.anchoredPosition = new Vector2(0f, yPositions[i]);
                }

                bool isSelected = (i == mainMenuSelectedIndex);
                btns[i].transform.localScale = isSelected ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;

                var img = btns[i].GetComponent<Image>();
                if (img != null)
                {
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                    img.color = isSelected ? new Color(1f, 1f, 0.78f, 1f) : Color.white;
                }
            }

            if (btns.Length > mainMenuSelectedIndex && btns[mainMenuSelectedIndex] != null)
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(btns[mainMenuSelectedIndex].gameObject);
            }
        }

        private Button[] GetMainMenuButtons()

        {

            Button b0 = mainMenuFightButton != null ? mainMenuFightButton : GameObject.Find("HUD_Canvas/MainMenu_Panel/Button_Pelea")?.GetComponent<Button>();

            Button b1 = mainMenuTowerButton != null ? mainMenuTowerButton : GameObject.Find("HUD_Canvas/MainMenu_Panel/Button_Torre")?.GetComponent<Button>();

            Button b2 = mainMenuVsButton != null ? mainMenuVsButton : GameObject.Find("HUD_Canvas/MainMenu_Panel/Button_VS")?.GetComponent<Button>();

            Button b3 = GameObject.Find("HUD_Canvas/MainMenu_Panel/Button_Opciones")?.GetComponent<Button>();

            Button b4 = mainMenuExitButton != null ? mainMenuExitButton : GameObject.Find("HUD_Canvas/MainMenu_Panel/Button_Salir")?.GetComponent<Button>();

            return new Button[] { b0, b1, b2, b3, b4 };

        }

        public void QuitGame()

        {
            if (!_suppressUiSfx) SoundEffectsManager.Instance?.PlayUiConfirm();

            Debug.Log("[FightMatchManager] Saliendo del juego...");

#if UNITY_EDITOR

            UnityEditor.EditorApplication.isPlaying = false;

#else

            Application.Quit();

#endif

        }

    }

}

