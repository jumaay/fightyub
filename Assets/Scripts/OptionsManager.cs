using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Fightyub
{
    public class OptionsManager : MonoBehaviour
    {
        public static OptionsManager Instance { get; private set; }

        public static readonly Vector2Int[] SupportedResolutions = new Vector2Int[]
        {
            new Vector2Int(640, 480),
            new Vector2Int(800, 600),
            new Vector2Int(1024, 768),
            new Vector2Int(1280, 720),
            new Vector2Int(1366, 768),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160)
        };

        public static readonly string[] ResolutionLabels = new string[]
        {
            "640×480",
            "800×600",
            "1024×768",
            "1280×720",
            "1366×768",
            "1600×900",
            "1920×1080",
            "2560×1440",
            "3840×2160 (4K)"
        };

        public static readonly string[] QualityLabels = new string[]
        {
            "Baja",
            "Media",
            "Alta"
        };

        public static readonly string[] ScreenModeLabels = new string[]
        {
            "Ventana",
            "Pantalla completa",
            "Pantalla completa sin bordes"
        };

        public static readonly string[] DifficultyLabels = new string[]
        {
            "Fácil",
            "Medio",
            "Difícil"
        };

        // PlayerPrefs keys for persistent settings across sessions
        public const string PrefResolutionIndex = "Fightyub_Opt_ResolutionIdx";
        public const string PrefQualityIndex = "Fightyub_Opt_QualityIdx";
        public const string PrefMusicVolume = "Fightyub_Opt_MusicVol";
        public const string PrefSfxVolume = "Fightyub_Opt_SfxVol";
        public const string PrefScreenModeIndex = "Fightyub_Opt_ScreenModeIdx";
        public const string PrefAIDifficulty = "Fightyub_Opt_AIDifficulty";

        // Default values
        public const int DefaultResolutionIndex = 3; // 1280x720
        public const int DefaultQualityIndex = 2;    // Alta
        public const float DefaultMusicVolume = 0.80f;
        public const float DefaultSfxVolume = 0.80f;
        public const int DefaultScreenModeIndex = 0; // Ventana
        public const int DefaultAIDifficulty = 1;    // 0 = Fácil, 1 = Medio, 2 = Difícil

        public const float TargetGameAspect = 16f / 9f;
        public const float ConfirmationTimeoutSeconds = 10f;

        // Committed (saved) settings
        public int committedResolutionIndex = DefaultResolutionIndex;
        public int committedQualityIndex = DefaultQualityIndex;
        public float committedMusicVolume = DefaultMusicVolume;
        public float committedSfxVolume = DefaultSfxVolume;
        public int committedScreenModeIndex = DefaultScreenModeIndex;
        public int committedAIDifficulty = DefaultAIDifficulty;

        // Pending (staged in UI before clicking Aplicar) settings
        public int pendingResolutionIndex = DefaultResolutionIndex;
        public int pendingQualityIndex = DefaultQualityIndex;
        public float pendingMusicVolume = DefaultMusicVolume;
        public float pendingSfxVolume = DefaultSfxVolume;
        public int pendingScreenModeIndex = DefaultScreenModeIndex;
        public int pendingAIDifficulty = DefaultAIDifficulty;

        // Backup of display settings prior to unconfirmed display change
        private int _preConfirmResolutionIndex = DefaultResolutionIndex;
        private int _preConfirmScreenModeIndex = DefaultScreenModeIndex;
        private int _preConfirmQualityIndex = DefaultQualityIndex;
        private float _preConfirmMusicVolume = DefaultMusicVolume;
        private float _preConfirmSfxVolume = DefaultSfxVolume;
        public bool isAwaitingDisplayConfirmation = false;
        public float confirmationRemainingTime = 0f;

        // Static audio volume & graphics quality accessors
        public static float SavedMusicVolume => PlayerPrefs.GetFloat(PrefMusicVolume, DefaultMusicVolume);
        public static float SavedSfxVolume => PlayerPrefs.GetFloat(PrefSfxVolume, DefaultSfxVolume);
        public static event Action<float, float> OnAudioVolumesChanged;

        public static int ActiveQualityIndex => Instance != null
            ? Mathf.Clamp(Instance.committedQualityIndex, 0, QualityLabels.Length - 1)
            : Mathf.Clamp(PlayerPrefs.GetInt(PrefQualityIndex, DefaultQualityIndex), 0, QualityLabels.Length - 1);

        // Efectos 2D (sombras de contacto, luz ambiental y destellos): apagados en Baja (0), activos en Media (1) y Alta (2)
        public static bool AreLightingAndShadowsEnabled => ActiveQualityIndex >= 1;
        public static float LightingQualityMultiplier => ActiveQualityIndex >= 2 ? 1.0f : (ActiveQualityIndex == 1 ? 0.78f : 0f);

        // AI Difficulty accessors
        public static int ActiveAIDifficulty => Instance != null
            ? Mathf.Clamp(Instance.committedAIDifficulty, 0, DifficultyLabels.Length - 1)
            : Mathf.Clamp(PlayerPrefs.GetInt(PrefAIDifficulty, DefaultAIDifficulty), 0, DifficultyLabels.Length - 1);
        public static string ActiveAIDifficultyLabel => DifficultyLabels[ActiveAIDifficulty];

        [Header("Scene & Camera References")]
        public FightMatchManager matchManager;
        public ControlBindingsManager controlsManager;
        public Camera mainCamera;
        public Camera letterboxCamera;
        public CanvasScaler hudCanvasScaler;

        [Header("Options Panel UI")]
        public GameObject optionsPanel;
        public Button mainMenuOptionsButton;

        // Resolution UI
        public Text resolutionValueText;
        public Button resPrevButton;
        public Button resNextButton;
        public Button[] resolutionDirectButtons = new Button[0];
        public Image[] resolutionDirectHighlights = new Image[0];

        // Quality UI
        public Text qualityValueText;
        public Button[] qualityButtons = new Button[0];
        public Image[] qualityHighlights = new Image[0];

        // Screen Mode UI
        public Text screenModeValueText;
        public Button[] screenModeButtons = new Button[0];
        public Image[] screenModeHighlights = new Image[0];

        // Audio Sliders & Buttons
        public Slider musicVolumeSlider;
        public Text musicVolumeValueText;
        public Button musicMinusButton;
        public Button musicPlusButton;

        public Slider sfxVolumeSlider;
        public Text sfxVolumeValueText;
        public Button sfxMinusButton;
        public Button sfxPlusButton;

        // AI Difficulty UI
        public Text difficultyValueText;
        public Button[] difficultyButtons = new Button[0];
        public Image[] difficultyHighlights = new Image[0];

        // Action Buttons
        public Button applyButton;
        public Button restoreDefaultsButton;
        public Button backButton;
        public Text statusFeedbackText;

        [Header("Confirmation Modal (Resolution / Screen Mode)")]
        public GameObject confirmModalPanel;
        public Text confirmModalDescriptionText;
        public Text confirmModalTimerText;
        public Button confirmKeepButton;
        public Button confirmRevertButton;

        private bool _suppressSliderCallback = false;
        private int _lastScreenW;
        private int _lastScreenH;

        private void Awake()
        {
            Instance = this;
            if (mainCamera == null) mainCamera = Camera.main;
            if (matchManager == null) matchManager = FindFirstObjectByType<FightMatchManager>();
            if (controlsManager == null) controlsManager = FindFirstObjectByType<ControlBindingsManager>();
            if (hudCanvasScaler == null)
            {
                var canvasGo = GameObject.Find("HUD_Canvas");
                if (canvasGo != null) hudCanvasScaler = canvasGo.GetComponent<CanvasScaler>();
            }

            EnsureLetterboxCamera();
            EnsureDifficultyUI();
            LoadSavedSettings();
            ApplyAllSettingsRuntime(
                committedResolutionIndex,
                committedScreenModeIndex,
                committedQualityIndex,
                committedMusicVolume,
                committedSfxVolume);
        }

        private void Start()
        {
            WireUIControls();
            SyncPendingFromCommitted();
            RefreshOptionsUI();

            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (confirmModalPanel != null) confirmModalPanel.SetActive(false);
        }

        private void Update()
        {
            if (Screen.width != _lastScreenW || Screen.height != _lastScreenH)
            {
                _lastScreenW = Screen.width;
                _lastScreenH = Screen.height;
                int activeResIdx = isAwaitingDisplayConfirmation ? pendingResolutionIndex : committedResolutionIndex;
                UpdateCameraViewportAndCanvas(activeResIdx);
            }

            if (isAwaitingDisplayConfirmation)
            {
                confirmationRemainingTime -= Time.unscaledDeltaTime;
                UpdateConfirmationTimerText();

                if (confirmationRemainingTime <= 0f)
                {
                    RevertDisplayChanges(true);
                }
                else if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    RevertDisplayChanges(false);
                }
                return;
            }

            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                if (controlsManager != null && controlsManager.isListeningForRebind)
                {
                    return;
                }
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseOptionsAndReturnToMenu();
                }
            }
        }

        private void EnsureLetterboxCamera()
        {
            if (letterboxCamera == null)
            {
                var existing = GameObject.Find("Letterbox_Camera");
                if (existing != null) letterboxCamera = existing.GetComponent<Camera>();
            }
            if (letterboxCamera == null)
            {
                var go = new GameObject("Letterbox_Camera");
                letterboxCamera = go.AddComponent<Camera>();
            }
            letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
            letterboxCamera.backgroundColor = Color.black;
            letterboxCamera.cullingMask = 0;
            letterboxCamera.depth = -100f;
            letterboxCamera.orthographic = true;
            letterboxCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }

        public void WireUIControls()
        {
            if (mainMenuOptionsButton == null)
            {
                var btnGo = GameObject.Find("HUD_Canvas/MainMenu_Panel/Button_Opciones");
                if (btnGo != null) mainMenuOptionsButton = btnGo.GetComponent<Button>();
            }
            if (mainMenuOptionsButton != null)
            {
                // The button was serialized as non-interactable (greyed out); make sure it is usable.
                mainMenuOptionsButton.interactable = true;
                mainMenuOptionsButton.enabled = true;
                mainMenuOptionsButton.onClick.RemoveAllListeners();
                mainMenuOptionsButton.onClick.AddListener(OpenOptionsPanel);
            }

            if (resPrevButton != null)
            {
                resPrevButton.onClick.RemoveAllListeners();
                resPrevButton.onClick.AddListener(() => SelectPendingResolution(
                    (pendingResolutionIndex - 1 + SupportedResolutions.Length) % SupportedResolutions.Length));
            }
            if (resNextButton != null)
            {
                resNextButton.onClick.RemoveAllListeners();
                resNextButton.onClick.AddListener(() => SelectPendingResolution(
                    (pendingResolutionIndex + 1) % SupportedResolutions.Length));
            }

            if (resolutionDirectButtons != null)
            {
                for (int i = 0; i < resolutionDirectButtons.Length; i++)
                {
                    int idx = i;
                    if (resolutionDirectButtons[idx] != null)
                    {
                        resolutionDirectButtons[idx].onClick.RemoveAllListeners();
                        resolutionDirectButtons[idx].onClick.AddListener(() => SelectPendingResolution(idx));
                    }
                }
            }

            if (qualityButtons != null)
            {
                for (int i = 0; i < qualityButtons.Length; i++)
                {
                    int idx = i;
                    if (qualityButtons[idx] != null)
                    {
                        qualityButtons[idx].onClick.RemoveAllListeners();
                        qualityButtons[idx].onClick.AddListener(() => SelectPendingQuality(idx));
                    }
                }
            }

            if (screenModeButtons != null)
            {
                for (int i = 0; i < screenModeButtons.Length; i++)
                {
                    int idx = i;
                    if (screenModeButtons[idx] != null)
                    {
                        screenModeButtons[idx].onClick.RemoveAllListeners();
                        screenModeButtons[idx].onClick.AddListener(() => SelectPendingScreenMode(idx));
                    }
                }
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
                musicVolumeSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }
            if (musicMinusButton != null)
            {
                musicMinusButton.onClick.RemoveAllListeners();
                musicMinusButton.onClick.AddListener(() => SetPendingMusicVolume(pendingMusicVolume - 0.10f));
            }
            if (musicPlusButton != null)
            {
                musicPlusButton.onClick.RemoveAllListeners();
                musicPlusButton.onClick.AddListener(() => SetPendingMusicVolume(pendingMusicVolume + 0.10f));
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveAllListeners();
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            }
            if (sfxMinusButton != null)
            {
                sfxMinusButton.onClick.RemoveAllListeners();
                sfxMinusButton.onClick.AddListener(() => SetPendingSfxVolume(pendingSfxVolume - 0.10f));
            }
            if (sfxPlusButton != null)
            {
                sfxPlusButton.onClick.RemoveAllListeners();
                sfxPlusButton.onClick.AddListener(() => SetPendingSfxVolume(pendingSfxVolume + 0.10f));
            }

            if (difficultyButtons != null)
            {
                for (int i = 0; i < difficultyButtons.Length; i++)
                {
                    int idx = i;
                    if (difficultyButtons[idx] != null)
                    {
                        difficultyButtons[idx].onClick.RemoveAllListeners();
                        difficultyButtons[idx].onClick.AddListener(() => SelectPendingDifficulty(idx));
                    }
                }
            }

            if (applyButton != null)
            {
                applyButton.onClick.RemoveAllListeners();
                applyButton.onClick.AddListener(ApplyPendingChanges);
            }
            if (restoreDefaultsButton != null)
            {
                restoreDefaultsButton.onClick.RemoveAllListeners();
                restoreDefaultsButton.onClick.AddListener(RestoreDefaultSettings);
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(CloseOptionsAndReturnToMenu);
            }

            if (confirmKeepButton != null)
            {
                confirmKeepButton.onClick.RemoveAllListeners();
                confirmKeepButton.onClick.AddListener(ConfirmDisplayChanges);
            }
            if (confirmRevertButton != null)
            {
                confirmRevertButton.onClick.RemoveAllListeners();
                confirmRevertButton.onClick.AddListener(() => RevertDisplayChanges(false));
            }
        }

        public void LoadSavedSettings()
        {
            committedResolutionIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(PrefResolutionIndex, DefaultResolutionIndex),
                0, SupportedResolutions.Length - 1);

            committedQualityIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(PrefQualityIndex, DefaultQualityIndex),
                0, QualityLabels.Length - 1);

            committedMusicVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(PrefMusicVolume, DefaultMusicVolume));

            committedSfxVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(PrefSfxVolume, DefaultSfxVolume));

            committedScreenModeIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(PrefScreenModeIndex, DefaultScreenModeIndex),
                0, ScreenModeLabels.Length - 1);

            committedAIDifficulty = Mathf.Clamp(
                PlayerPrefs.GetInt(PrefAIDifficulty, DefaultAIDifficulty),
                0, DifficultyLabels.Length - 1);

            SyncPendingFromCommitted();
        }

        public void SaveCommittedSettingsToDisk()
        {
            PlayerPrefs.SetInt(PrefResolutionIndex, committedResolutionIndex);
            PlayerPrefs.SetInt(PrefQualityIndex, committedQualityIndex);
            PlayerPrefs.SetFloat(PrefMusicVolume, committedMusicVolume);
            PlayerPrefs.SetFloat(PrefSfxVolume, committedSfxVolume);
            PlayerPrefs.SetInt(PrefScreenModeIndex, committedScreenModeIndex);
            PlayerPrefs.SetInt(PrefAIDifficulty, committedAIDifficulty);
            PlayerPrefs.Save();
        }

        private void SyncPendingFromCommitted()
        {
            pendingResolutionIndex = committedResolutionIndex;
            pendingQualityIndex = committedQualityIndex;
            pendingMusicVolume = committedMusicVolume;
            pendingSfxVolume = committedSfxVolume;
            pendingScreenModeIndex = committedScreenModeIndex;
            pendingAIDifficulty = committedAIDifficulty;
            if (controlsManager != null)
            {
                controlsManager.SyncPendingFromCommitted();
            }
        }

        public void OpenOptionsPanel()
        {
            SoundEffectsManager.Instance?.PlayUiConfirm();
            EnsureDifficultyUI();
            SyncPendingFromCommitted();
            if (controlsManager != null)
            {
                controlsManager.RefreshConnectedControllers();
            }
            isAwaitingDisplayConfirmation = false;
            if (confirmModalPanel != null) confirmModalPanel.SetActive(false);

            if (matchManager != null && matchManager.mainMenuPanel != null)
            {
                matchManager.mainMenuPanel.SetActive(false);
            }
            if (optionsPanel != null)
            {
                optionsPanel.SetActive(true);
                optionsPanel.transform.SetAsLastSibling();
            }

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "Configurá pantalla, audio o controles (P1/P2) y presioná APLICAR.";
            }

            RefreshOptionsUI();
        }

        public void CloseOptionsAndReturnToMenu()
        {
            SoundEffectsManager.Instance?.PlayUiConfirm();
            if (isAwaitingDisplayConfirmation)
            {
                RevertDisplayChanges(false);
            }

            SyncPendingFromCommitted();
            if (confirmModalPanel != null) confirmModalPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);

            if (matchManager != null)
            {
                matchManager.ShowMainMenu();
            }
        }

        public void SelectPendingResolution(int index)
        {
            int prev = pendingResolutionIndex;
            pendingResolutionIndex = Mathf.Clamp(index, 0, SupportedResolutions.Length - 1);
            if (pendingResolutionIndex != prev) SoundEffectsManager.Instance?.PlayUiMove();
            RefreshOptionsUI();
            SetStatusPreviewMessage();
        }

        public void SelectPendingQuality(int index)
        {
            int prev = pendingQualityIndex;
            pendingQualityIndex = Mathf.Clamp(index, 0, QualityLabels.Length - 1);
            if (pendingQualityIndex != prev) SoundEffectsManager.Instance?.PlayUiMove();
            RefreshOptionsUI();
            SetStatusPreviewMessage();
        }

        public void SelectPendingScreenMode(int index)
        {
            int prev = pendingScreenModeIndex;
            pendingScreenModeIndex = Mathf.Clamp(index, 0, ScreenModeLabels.Length - 1);
            if (pendingScreenModeIndex != prev) SoundEffectsManager.Instance?.PlayUiMove();
            RefreshOptionsUI();
            SetStatusPreviewMessage();
        }

        public void SelectPendingDifficulty(int index)
        {
            int prev = pendingAIDifficulty;
            pendingAIDifficulty = Mathf.Clamp(index, 0, DifficultyLabels.Length - 1);
            if (pendingAIDifficulty != prev) SoundEffectsManager.Instance?.PlayUiMove();
            RefreshOptionsUI();
            SetStatusPreviewMessage();
        }

        private void OnMusicSliderChanged(float val)
        {
            if (_suppressSliderCallback) return;
            pendingMusicVolume = Mathf.Clamp01(val);
            committedMusicVolume = pendingMusicVolume;
            PlayerPrefs.SetFloat(PrefMusicVolume, committedMusicVolume);
            PlayerPrefs.Save();

            ApplyAudioVolumesRuntime(committedMusicVolume, committedSfxVolume);
            RefreshOptionsUI();
            SetStatusPreviewMessage();
        }

        private void OnSfxSliderChanged(float val)
        {
            if (_suppressSliderCallback) return;
            pendingSfxVolume = Mathf.Clamp01(val);
            committedSfxVolume = pendingSfxVolume;
            PlayerPrefs.SetFloat(PrefSfxVolume, committedSfxVolume);
            PlayerPrefs.Save();

            ApplyAudioVolumesRuntime(committedMusicVolume, committedSfxVolume);
            RefreshOptionsUI();
            SetStatusPreviewMessage();
        }

        public void SetPendingMusicVolume(float val)
        {
            pendingMusicVolume = Mathf.Clamp01(Mathf.Round(val * 100f) / 100f);
            committedMusicVolume = pendingMusicVolume;
            PlayerPrefs.SetFloat(PrefMusicVolume, committedMusicVolume);
            PlayerPrefs.Save();

            ApplyAudioVolumesRuntime(committedMusicVolume, committedSfxVolume);
            RefreshOptionsUI();
            SetStatusPreviewMessage();
        }

        public void SetPendingSfxVolume(float val)
        {
            pendingSfxVolume = Mathf.Clamp01(Mathf.Round(val * 100f) / 100f);
            committedSfxVolume = pendingSfxVolume;
            PlayerPrefs.SetFloat(PrefSfxVolume, committedSfxVolume);
            PlayerPrefs.Save();

            ApplyAudioVolumesRuntime(committedMusicVolume, committedSfxVolume);
            RefreshOptionsUI();
            SetStatusPreviewMessage();
            SoundEffectsManager.Instance?.PlayUiConfirm();
        }

        private void SetStatusPreviewMessage()
        {
            if (statusFeedbackText == null) return;
            bool hasDisplayChange = (pendingResolutionIndex != committedResolutionIndex) ||
                                    (pendingScreenModeIndex != committedScreenModeIndex);
            if (hasDisplayChange)
            {
                statusFeedbackText.text = "Cambios pendientes de pantalla (requieren confirmación al presionar APLICAR).";
            }
            else
            {
                statusFeedbackText.text = "Cambios pendientes. Presioná APLICAR para guardarlos.";
            }
        }

        public void ApplyPendingChanges()
        {
            SoundEffectsManager.Instance?.PlayUiConfirm();
            bool displayChanged = (pendingResolutionIndex != committedResolutionIndex) ||
                                  (pendingScreenModeIndex != committedScreenModeIndex);

            // Always apply quality, audio, and control bindings immediately
            ApplyQualityRuntime(pendingQualityIndex);
            ApplyAudioVolumesRuntime(pendingMusicVolume, pendingSfxVolume);
            if (controlsManager != null)
            {
                controlsManager.CommitPendingBindings();
            }

            if (displayChanged)
            {
                // Store backup so we can revert if not confirmed
                _preConfirmResolutionIndex = committedResolutionIndex;
                _preConfirmScreenModeIndex = committedScreenModeIndex;
                _preConfirmQualityIndex = committedQualityIndex;
                _preConfirmMusicVolume = committedMusicVolume;
                _preConfirmSfxVolume = committedSfxVolume;

                // Commit non-display settings right away
                committedQualityIndex = pendingQualityIndex;
                committedMusicVolume = pendingMusicVolume;
                committedSfxVolume = pendingSfxVolume;
                committedAIDifficulty = pendingAIDifficulty;
                if (matchManager != null && matchManager.player2 != null && matchManager.player2.isAI)
                {
                    matchManager.player2.aiDifficulty = (FighterController.AIDifficulty)committedAIDifficulty;
                }
                SaveCommittedSettingsToDisk();

                // Apply the new display configuration provisionally and ask for confirmation
                ApplyDisplayRuntime(pendingResolutionIndex, pendingScreenModeIndex);
                OpenDisplayConfirmationModal();
            }
            else
            {
                committedQualityIndex = pendingQualityIndex;
                committedMusicVolume = pendingMusicVolume;
                committedSfxVolume = pendingSfxVolume;
                committedAIDifficulty = pendingAIDifficulty;
                if (matchManager != null && matchManager.player2 != null && matchManager.player2.isAI)
                {
                    matchManager.player2.aiDifficulty = (FighterController.AIDifficulty)committedAIDifficulty;
                }
                SaveCommittedSettingsToDisk();
                RefreshOptionsUI();

                if (statusFeedbackText != null)
                {
                    statusFeedbackText.text = "¡Opciones aplicadas y guardadas correctamente!";
                }
            }
        }

        private void OpenDisplayConfirmationModal()
        {
            isAwaitingDisplayConfirmation = true;
            confirmationRemainingTime = ConfirmationTimeoutSeconds;

            if (confirmModalPanel != null)
            {
                confirmModalPanel.SetActive(true);
                confirmModalPanel.transform.SetAsLastSibling();
            }

            if (confirmModalDescriptionText != null)
            {
                string resText = ResolutionLabels[pendingResolutionIndex];
                string modeText = ScreenModeLabels[pendingScreenModeIndex];
                confirmModalDescriptionText.text =
                    $"Se aplicó: {resText} — {modeText}\n¿Deseás mantener esta configuración de pantalla?";
            }

            UpdateConfirmationTimerText();
        }

        private void UpdateConfirmationTimerText()
        {
            if (confirmModalTimerText != null)
            {
                int secs = Mathf.CeilToInt(Mathf.Max(0f, confirmationRemainingTime));
                confirmModalTimerText.text = $"Volviendo a la configuración anterior en {secs} s...";
            }
        }

        public void ConfirmDisplayChanges()
        {
            SoundEffectsManager.Instance?.PlayUiConfirm();
            isAwaitingDisplayConfirmation = false;
            committedResolutionIndex = pendingResolutionIndex;
            committedScreenModeIndex = pendingScreenModeIndex;
            committedQualityIndex = pendingQualityIndex;
            committedMusicVolume = pendingMusicVolume;
            committedSfxVolume = pendingSfxVolume;
            committedAIDifficulty = pendingAIDifficulty;
            if (matchManager != null && matchManager.player2 != null && matchManager.player2.isAI)
            {
                matchManager.player2.aiDifficulty = (FighterController.AIDifficulty)committedAIDifficulty;
            }

            SaveCommittedSettingsToDisk();

            if (confirmModalPanel != null) confirmModalPanel.SetActive(false);
            RefreshOptionsUI();

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text =
                    $"¡Configuración confirmada y guardada ({ResolutionLabels[committedResolutionIndex]}, {ScreenModeLabels[committedScreenModeIndex]})!";
            }
        }

        public void RevertDisplayChanges(bool timedOut)
        {
            if (!timedOut) SoundEffectsManager.Instance?.PlayUiConfirm();
            isAwaitingDisplayConfirmation = false;
            committedResolutionIndex = _preConfirmResolutionIndex;
            committedScreenModeIndex = _preConfirmScreenModeIndex;
            pendingResolutionIndex = committedResolutionIndex;
            pendingScreenModeIndex = committedScreenModeIndex;

            ApplyDisplayRuntime(committedResolutionIndex, committedScreenModeIndex);
            SaveCommittedSettingsToDisk();

            if (confirmModalPanel != null) confirmModalPanel.SetActive(false);
            RefreshOptionsUI();

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = timedOut
                    ? "Tiempo agotado: se restauró la resolución y modo de pantalla anteriores."
                    : "Cambio cancelado: se restauró la resolución y modo de pantalla anteriores.";
            }
        }

        public void RestoreDefaultSettings()
        {
            SoundEffectsManager.Instance?.PlayUiConfirm();
            if (isAwaitingDisplayConfirmation)
            {
                isAwaitingDisplayConfirmation = false;
                if (confirmModalPanel != null) confirmModalPanel.SetActive(false);
            }

            pendingResolutionIndex = DefaultResolutionIndex;
            pendingQualityIndex = DefaultQualityIndex;
            pendingMusicVolume = DefaultMusicVolume;
            pendingSfxVolume = DefaultSfxVolume;
            pendingScreenModeIndex = DefaultScreenModeIndex;
            pendingAIDifficulty = DefaultAIDifficulty;

            committedResolutionIndex = DefaultResolutionIndex;
            committedQualityIndex = DefaultQualityIndex;
            committedMusicVolume = DefaultMusicVolume;
            committedSfxVolume = DefaultSfxVolume;
            committedScreenModeIndex = DefaultScreenModeIndex;
            committedAIDifficulty = DefaultAIDifficulty;

            if (matchManager != null && matchManager.player2 != null && matchManager.player2.isAI)
            {
                matchManager.player2.aiDifficulty = (FighterController.AIDifficulty)committedAIDifficulty;
            }

            ApplyAllSettingsRuntime(
                committedResolutionIndex,
                committedScreenModeIndex,
                committedQualityIndex,
                committedMusicVolume,
                committedSfxVolume);

            if (controlsManager != null)
            {
                controlsManager.RestoreDefaultBindingsAndCommit();
            }

            SaveCommittedSettingsToDisk();
            RefreshOptionsUI();

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "Valores predeterminados restaurados (Pantalla, Audio y Controles P1/P2).";
            }
        }

        private void ApplyAllSettingsRuntime(int resIdx, int modeIdx, int qualIdx, float musicVol, float sfxVol)
        {
            ApplyDisplayRuntime(resIdx, modeIdx);
            ApplyQualityRuntime(qualIdx);
            ApplyAudioVolumesRuntime(musicVol, sfxVol);
        }

        public void ApplyDisplayRuntime(int resIdx, int modeIdx)
        {
            resIdx = Mathf.Clamp(resIdx, 0, SupportedResolutions.Length - 1);
            modeIdx = Mathf.Clamp(modeIdx, 0, ScreenModeLabels.Length - 1);

            Vector2Int res = SupportedResolutions[resIdx];
            FullScreenMode fsMode = GetUnityFullScreenMode(modeIdx);

            Screen.SetResolution(res.x, res.y, fsMode);
            UpdateCameraViewportAndCanvas(resIdx);
        }

        public static FullScreenMode GetUnityFullScreenMode(int modeIdx)
        {
            switch (modeIdx)
            {
                case 0: return FullScreenMode.Windowed;
                case 1: return FullScreenMode.ExclusiveFullScreen;
                case 2: return FullScreenMode.FullScreenWindow;
                default: return FullScreenMode.Windowed;
            }
        }

        public void UpdateCameraViewportAndCanvas(int resIdx)
        {
            resIdx = Mathf.Clamp(resIdx, 0, SupportedResolutions.Length - 1);
            Vector2Int targetRes = SupportedResolutions[resIdx];

            // Compute target aspect from selected resolution so both Standalone builds and Editor GameView
            // accurately preserve the 16:9 image proportions (letterboxing 4:3 resolutions like 640x480, 800x600, 1024x768).
            float selectedAspect = (float)targetRes.x / targetRes.y;
            float scaleHeight = selectedAspect / TargetGameAspect;

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                EnsureLetterboxCamera();
                if (scaleHeight < 0.999f)
                {
                    // Letterbox (top & bottom black bars for 4:3 resolutions)
                    Rect rect = new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight);
                    mainCamera.rect = rect;
                }
                else if (scaleHeight > 1.001f)
                {
                    // Pillarbox (left & right black bars if wider than 16:9)
                    float scaleWidth = 1f / scaleHeight;
                    Rect rect = new Rect((1f - scaleWidth) * 0.5f, 0f, scaleWidth, 1f);
                    mainCamera.rect = rect;
                }
                else
                {
                    mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
                }
            }

            if (hudCanvasScaler != null)
            {
                hudCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                hudCanvasScaler.referenceResolution = new Vector2(1280f, 720f);
                hudCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                hudCanvasScaler.matchWidthOrHeight = 0.5f;
            }
        }

        public void ApplyQualityRuntime(int qualIdx)
        {
            qualIdx = Mathf.Clamp(qualIdx, 0, QualityLabels.Length - 1);
            int totalLevels = QualitySettings.names != null ? QualitySettings.names.Length : 0;
            if (totalLevels > 0)
            {
                int unityQualityIndex;
                if (qualIdx == 0)
                {
                    // Baja -> Low (index 1 if 6 levels exist, else 0)
                    unityQualityIndex = totalLevels > 3 ? 1 : 0;
                }
                else if (qualIdx == 1)
                {
                    // Media -> Medium
                    unityQualityIndex = totalLevels / 2;
                }
                else
                {
                    // Alta -> High / Ultra
                    unityQualityIndex = totalLevels - 1;
                }
                QualitySettings.SetQualityLevel(unityQualityIndex, true);
            }
            committedQualityIndex = qualIdx;
            if (matchManager == null) matchManager = FindFirstObjectByType<FightMatchManager>();
            if (matchManager != null)
            {
                matchManager.RefreshLightingAndShadowsForQuality(qualIdx);
            }
        }

        public void ApplyAudioVolumesRuntime(float musicVol, float sfxVol)
        {
            musicVol = Mathf.Clamp01(musicVol);
            sfxVol = Mathf.Clamp01(sfxVol);

            // Apply to any existing AudioSources in the scene based on name/tag convention
            var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var src in sources)
            {
                if (src == null) continue;
                if (SoundEffectsManager.Instance != null && (src == SoundEffectsManager.Instance.sfxSource || src == SoundEffectsManager.Instance.uiSource))
                {
                    src.volume = 1f; // SoundEffectsManager gestiona volumen individual y master por clip
                    continue;
                }
                if (FightMatchManager.Instance != null && src == FightMatchManager.Instance.bgmSource)
                {
                    src.volume = FightMatchManager.GetScaledBgmVolume(musicVol);
                    continue;
                }
                string n = src.gameObject.name.ToLowerInvariant();
                if (n.Contains("music") || n.Contains("bgm") || n.Contains("musica"))
                {
                    src.volume = FightMatchManager.GetScaledBgmVolume(musicVol);
                }
                else
                {
                    src.volume = sfxVol;
                }
            }

            OnAudioVolumesChanged?.Invoke(musicVol, sfxVol);
        }

        public void RefreshOptionsUI()
        {
            if (resolutionValueText != null)
            {
                resolutionValueText.text = ResolutionLabels[Mathf.Clamp(pendingResolutionIndex, 0, ResolutionLabels.Length - 1)];
            }

            if (resolutionDirectHighlights != null)
            {
                for (int i = 0; i < resolutionDirectHighlights.Length; i++)
                {
                    if (resolutionDirectHighlights[i] == null) continue;
                    bool sel = (i == pendingResolutionIndex);
                    resolutionDirectHighlights[i].color = sel
                        ? new Color(0.20f, 0.82f, 1f, 1f)
                        : new Color(0.16f, 0.21f, 0.32f, 0.95f);
                }
            }

            if (qualityValueText != null)
            {
                qualityValueText.text = QualityLabels[Mathf.Clamp(pendingQualityIndex, 0, QualityLabels.Length - 1)].ToUpper();
            }

            if (qualityHighlights != null)
            {
                for (int i = 0; i < qualityHighlights.Length; i++)
                {
                    if (qualityHighlights[i] == null) continue;
                    bool sel = (i == pendingQualityIndex);
                    qualityHighlights[i].color = sel
                        ? new Color(1f, 0.84f, 0.16f, 1f)
                        : new Color(0.16f, 0.21f, 0.32f, 0.95f);
                }
            }

            if (screenModeValueText != null)
            {
                screenModeValueText.text = ScreenModeLabels[Mathf.Clamp(pendingScreenModeIndex, 0, ScreenModeLabels.Length - 1)].ToUpper();
            }

            if (screenModeHighlights != null)
            {
                for (int i = 0; i < screenModeHighlights.Length; i++)
                {
                    if (screenModeHighlights[i] == null) continue;
                    bool sel = (i == pendingScreenModeIndex);
                    screenModeHighlights[i].color = sel
                        ? new Color(0.25f, 0.88f, 0.48f, 1f)
                        : new Color(0.16f, 0.21f, 0.32f, 0.95f);
                }
            }

            _suppressSliderCallback = true;
            if (musicVolumeSlider != null) musicVolumeSlider.value = pendingMusicVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = pendingSfxVolume;
            _suppressSliderCallback = false;

            if (musicVolumeValueText != null)
            {
                musicVolumeValueText.text = $"{Mathf.RoundToInt(pendingMusicVolume * 100f)}%";
            }
            if (sfxVolumeValueText != null)
            {
                sfxVolumeValueText.text = $"{Mathf.RoundToInt(pendingSfxVolume * 100f)}%";
            }

            if (difficultyValueText != null)
            {
                difficultyValueText.text = DifficultyLabels[Mathf.Clamp(pendingAIDifficulty, 0, DifficultyLabels.Length - 1)].ToUpper();
            }

            if (difficultyHighlights != null)
            {
                for (int i = 0; i < difficultyHighlights.Length; i++)
                {
                    if (difficultyHighlights[i] == null) continue;
                    bool sel = (i == pendingAIDifficulty);
                    if (sel)
                    {
                        if (i == 0) difficultyHighlights[i].color = new Color(0.25f, 0.88f, 0.48f, 1f); // Fácil (Verde)
                        else if (i == 1) difficultyHighlights[i].color = new Color(1f, 0.84f, 0.16f, 1f); // Medio (Amarillo/Oro)
                        else difficultyHighlights[i].color = new Color(1f, 0.32f, 0.32f, 1f); // Difícil (Rojo arcade)
                    }
                    else
                    {
                        difficultyHighlights[i].color = new Color(0.16f, 0.21f, 0.32f, 0.95f);
                    }
                }
            }
        }

        public void EnsureDifficultyUI()
        {
            Transform leftCol = optionsPanel != null ? optionsPanel.transform.Find("Container/LeftColumn_VideoAudio") : null;
            if (leftCol == null)
            {
                var panelGo = GameObject.Find("HUD_Canvas/Options_Panel");
                if (panelGo != null)
                {
                    if (optionsPanel == null) optionsPanel = panelGo;
                    leftCol = panelGo.transform.Find("Container/LeftColumn_VideoAudio");
                }
            }
            if (leftCol == null) return;

            // Reposition music & sfx cleanly to make room
            string[] musicNames = new[] { "Music_Label", "Music_Minus", "Music_Slider", "Music_Plus", "Music_Value" };
            foreach (var n in musicNames)
            {
                var tr = leftCol.Find(n) as RectTransform;
                if (tr != null) tr.anchoredPosition = new Vector2(tr.anchoredPosition.x, -128f);
            }
            string[] sfxNames = new[] { "SFX_Label", "SFX_Minus", "SFX_Slider", "SFX_Plus", "SFX_Value" };
            foreach (var n in sfxNames)
            {
                var tr = leftCol.Find(n) as RectTransform;
                if (tr != null) tr.anchoredPosition = new Vector2(tr.anchoredPosition.x, -168f);
            }

            var mTxt = leftCol.Find("Music_Label")?.GetComponent<Text>();
            if (mTxt != null) mTxt.text = "MÚSICA:";
            var sTxt = leftCol.Find("SFX_Label")?.GetComponent<Text>();
            if (sTxt != null) sTxt.text = "EFECTOS DE SONIDO:";

            // Ensure DifficultyLabel
            var diffLbl = leftCol.Find("DifficultyLabel");
            if (diffLbl == null)
            {
                var qLbl = leftCol.Find("QualityLabel");
                if (qLbl != null)
                {
                    var diffLblGo = Instantiate(qLbl.gameObject, leftCol);
                    diffLblGo.name = "DifficultyLabel";
                    var rt = diffLblGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0f, -204f);
                    var txt = diffLblGo.GetComponent<Text>();
                    if (txt != null) txt.text = "DIFICULTAD DE LA IA:";
                    diffLbl = diffLblGo.transform;
                }
            }

            // Ensure buttons
            string[] diffNames = new[] { "Diff_0_Facil", "Diff_1_Medio", "Diff_2_Dificil" };
            string[] diffLabels = new[] { "FÁCIL", "MEDIO", "DIFÍCIL" };
            float[] xs = new[] { -182f, 0f, 182f };

            if (difficultyButtons == null || difficultyButtons.Length != 3)
            {
                difficultyButtons = new Button[3];
                difficultyHighlights = new Image[3];
            }

            var srcBtnTr = leftCol.Find("Quality_0_Baja");
            for (int i = 0; i < 3; i++)
            {
                var bTr = leftCol.Find(diffNames[i]);
                if (bTr == null && srcBtnTr != null)
                {
                    var bGo = Instantiate(srcBtnTr.gameObject, leftCol);
                    bGo.name = diffNames[i];
                    var rt = bGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(xs[i], -232f);
                    rt.sizeDelta = new Vector2(174f, 30f);
                    var t = bGo.GetComponentInChildren<Text>();
                    if (t != null) t.text = diffLabels[i];
                    bTr = bGo.transform;
                }
                if (bTr != null)
                {
                    difficultyButtons[i] = bTr.GetComponent<Button>();
                    difficultyHighlights[i] = bTr.GetComponent<Image>();
                    int idx = i;
                    if (difficultyButtons[i] != null)
                    {
                        difficultyButtons[i].onClick.RemoveAllListeners();
                        difficultyButtons[i].onClick.AddListener(() => SelectPendingDifficulty(idx));
                    }
                }
            }
        }
    }
}
