using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Fightyub
{
    public enum ControlAction
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        Punch = 4,
        Kick = 5,
        Block = 6,
        Special = 7
    }

    public enum ControllerFamily
    {
        None_Keyboard = 0,
        Xbox360 = 1,
        XboxOneSeries = 2,
        PS5_PlayStation = 3,
        NintendoSwitch = 4,
        NintendoSwitch2 = 5,
        GenericUSBBluetooth = 6
    }

    [Serializable]
    public class PlayerKeyBindings
    {
        public Key up = Key.W;
        public Key down = Key.S;
        public Key left = Key.A;
        public Key right = Key.D;
        public Key punch = Key.J;
        public Key kick = Key.K;
        public Key block = Key.L;
        public Key special = Key.Space;

        public Key GetKey(ControlAction action)
        {
            switch (action)
            {
                case ControlAction.Up: return up;
                case ControlAction.Down: return down;
                case ControlAction.Left: return left;
                case ControlAction.Right: return right;
                case ControlAction.Punch: return punch;
                case ControlAction.Kick: return kick;
                case ControlAction.Block: return block;
                case ControlAction.Special: return special;
                default: return Key.None;
            }
        }

        public void SetKey(ControlAction action, Key key)
        {
            switch (action)
            {
                case ControlAction.Up: up = key; break;
                case ControlAction.Down: down = key; break;
                case ControlAction.Left: left = key; break;
                case ControlAction.Right: right = key; break;
                case ControlAction.Punch: punch = key; break;
                case ControlAction.Kick: kick = key; break;
                case ControlAction.Block: block = key; break;
                case ControlAction.Special: special = key; break;
            }
        }

        public PlayerKeyBindings Clone()
        {
            return new PlayerKeyBindings
            {
                up = this.up,
                down = this.down,
                left = this.left,
                right = this.right,
                punch = this.punch,
                kick = this.kick,
                block = this.block,
                special = this.special
            };
        }
    }

    public struct FighterInputFrame
    {
        public bool leftHeld;
        public bool rightHeld;
        public bool leftPressedThisFrame;
        public bool rightPressedThisFrame;
        public bool upPressedThisFrame;
        public bool downHeld;
        public bool downPressedThisFrame;
        public bool punchPressedThisFrame;
        public bool kickPressedThisFrame;
        public bool blockHeld;
        public bool blockPressedThisFrame;
        public bool specialPressedThisFrame;
        public bool runHeld;
    }

    public class ControlBindingsManager : MonoBehaviour
    {
        public static ControlBindingsManager Instance { get; private set; }

        public static readonly ControlAction[] AllActions = new ControlAction[]
        {
            ControlAction.Up,
            ControlAction.Down,
            ControlAction.Left,
            ControlAction.Right,
            ControlAction.Punch,
            ControlAction.Kick,
            ControlAction.Block
        };

        public static readonly string[] ActionDisplayNames = new string[]
        {
            "Saltar / Arriba",
            "Agacharse / Abajo",
            "Mover Izquierda",
            "Mover Derecha",
            "Piña (Ataque 1)",
            "Patada (Ataque 2)",
            "Cubrirse (Defensa)"
        };

        // Committed key bindings
        public PlayerKeyBindings p1CommittedKeys = CreateDefaultP1Keys();
        public PlayerKeyBindings p2CommittedKeys = CreateDefaultP2Keys();

        // Pending key bindings (in Options screen before clicking Aplicar)
        public PlayerKeyBindings p1PendingKeys = CreateDefaultP1Keys();
        public PlayerKeyBindings p2PendingKeys = CreateDefaultP2Keys();

        // Connected & assigned controller states
        public InputDevice p1AssignedController;
        public InputDevice p2AssignedController;
        public ControllerFamily p1ControllerFamily = ControllerFamily.None_Keyboard;
        public ControllerFamily p2ControllerFamily = ControllerFamily.None_Keyboard;
        public string p1DeviceDisplayName = "Teclado";
        public string p2DeviceDisplayName = "Teclado";

        // Key rebinding state
        public bool isListeningForRebind = false;
        public int rebindPlayerIndex = 1;
        public ControlAction rebindAction = ControlAction.Punch;

        // Edge-detection state for analog sticks & triggers on P1 & P2 controllers
        private bool _p1StickUpWasActive;
        private bool _p1StickDownWasActive;
        private bool _p1StickLeftWasActive;
        private bool _p1StickRightWasActive;
        private bool _p1RtWasActive;
        private bool _p1LtWasActive;
        private bool _p2StickUpWasActive;
        private bool _p2StickDownWasActive;
        private bool _p2StickLeftWasActive;
        private bool _p2StickRightWasActive;
        private bool _p2RtWasActive;
        private bool _p2LtWasActive;

        // Edge-detection state for keyboard on P1 & P2
        private bool _p1KbUpWasActive, _p1KbDownWasActive, _p1KbLeftWasActive, _p1KbRightWasActive;
        private bool _p1KbPunchWasActive, _p1KbKickWasActive, _p1KbBlockWasActive, _p1KbSpecialWasActive;
        private bool _p2KbUpWasActive, _p2KbDownWasActive, _p2KbLeftWasActive, _p2KbRightWasActive;
        private bool _p2KbPunchWasActive, _p2KbKickWasActive, _p2KbBlockWasActive, _p2KbSpecialWasActive;

        [Header("Device Icons")]
        public Sprite keyboardIconSprite;
        public Sprite gamepadIconSprite;

        public bool IsGamepad(int playerIndex)
        {
            return (playerIndex == 2) ? (p2AssignedController != null) : (p1AssignedController != null);
        }

        public Sprite GetDeviceIcon(int playerIndex)
        {
            if (keyboardIconSprite == null)
            {
                keyboardIconSprite = Resources.Load<Sprite>("Settings/Icon_Keyboard");
#if UNITY_EDITOR
                if (keyboardIconSprite == null)
                    keyboardIconSprite = (Sprite)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Settings/Icon_Keyboard.png", typeof(Sprite));
#endif
            }
            if (gamepadIconSprite == null)
            {
                gamepadIconSprite = Resources.Load<Sprite>("Settings/Icon_Gamepad");
#if UNITY_EDITOR
                if (gamepadIconSprite == null)
                    gamepadIconSprite = (Sprite)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Settings/Icon_Gamepad.png", typeof(Sprite));
#endif
            }
            return IsGamepad(playerIndex) ? gamepadIconSprite : keyboardIconSprite;
        }

        [Header("UI References in Options Panel")]
        public Text connectedDevicesSummaryText;
        public Text controllerPromptsLegendText;
        public Text hudControlsOverlayText;
        public Button[] p1KeyBindButtons = new Button[7];
        public Text[] p1KeyBindButtonTexts = new Text[7];
        public Image[] p1KeyBindButtonImages = new Image[7];
        public Button[] p2KeyBindButtons = new Button[7];
        public Text[] p2KeyBindButtonTexts = new Text[7];
        public Image[] p2KeyBindButtonImages = new Image[7];

        public event Action OnBindingsOrDevicesChanged;

        public static PlayerKeyBindings CreateDefaultP1Keys()
        {
            return new PlayerKeyBindings
            {
                up = Key.W,
                down = Key.S,
                left = Key.A,
                right = Key.D,
                punch = Key.J,
                kick = Key.K,
                block = Key.L,
                special = Key.Space
            };
        }

        public static PlayerKeyBindings CreateDefaultP2Keys()
        {
            return new PlayerKeyBindings
            {
                up = Key.UpArrow,
                down = Key.DownArrow,
                left = Key.LeftArrow,
                right = Key.RightArrow,
                punch = Key.B,
                kick = Key.N,
                block = Key.M,
                special = Key.Enter
            };
        }

        public string GetSpecialButtonPrompt(int playerIndex)
        {
            bool isP1 = (playerIndex == 1);
            var controller = isP1 ? p1AssignedController : p2AssignedController;
            if (controller != null)
            {
                var fam = isP1 ? p1ControllerFamily : p2ControllerFamily;
                switch (fam)
                {
                    case ControllerFamily.PS5_PlayStation: return "L2";
                    case ControllerFamily.NintendoSwitch:
                    case ControllerFamily.NintendoSwitch2: return "ZL";
                    default: return "LT";
                }
            }

            var bindings = isP1 ? p1CommittedKeys : p2CommittedKeys;
            Key k = (bindings != null) ? bindings.special : (isP1 ? Key.Space : Key.Enter);
            return FormatKeyName(k);
        }

        private void Awake()
        {
            Instance = this;
            LoadSavedBindings();
            RefreshConnectedControllers();
        }

        private void OnEnable()
        {
            Instance = this;
            InputSystem.onDeviceChange += OnInputDeviceChanged;
            RefreshConnectedControllers();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnInputDeviceChanged;
        }

        private void Start()
        {
            WireRebindButtons();
            RefreshControlsUI();
            UpdateAdaptivePromptTexts();
        }

        private void Update()
        {
            if (!isListeningForRebind) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                CancelRebind();
                return;
            }

            foreach (KeyControl kc in kb.allKeys)
            {
                if (kc == null || kc.keyCode == Key.None || kc.keyCode == Key.Escape) continue;
                if (kc.wasPressedThisFrame)
                {
                    CompleteRebind(kc.keyCode);
                    break;
                }
            }
        }

        private void OnInputDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added ||
                change == InputDeviceChange.Removed ||
                change == InputDeviceChange.Reconnected ||
                change == InputDeviceChange.Disconnected ||
                change == InputDeviceChange.ConfigurationChanged)
            {
                RefreshConnectedControllers();
            }
        }

        public void WireRebindButtons()
        {
            if (p1KeyBindButtons != null)
            {
                for (int i = 0; i < p1KeyBindButtons.Length && i < AllActions.Length; i++)
                {
                    ControlAction act = AllActions[i];
                    if (p1KeyBindButtons[i] != null)
                    {
                        p1KeyBindButtons[i].onClick.RemoveAllListeners();
                        p1KeyBindButtons[i].onClick.AddListener(() => StartRebind(1, act));
                    }
                }
            }

            if (p2KeyBindButtons != null)
            {
                for (int i = 0; i < p2KeyBindButtons.Length && i < AllActions.Length; i++)
                {
                    ControlAction act = AllActions[i];
                    if (p2KeyBindButtons[i] != null)
                    {
                        p2KeyBindButtons[i].onClick.RemoveAllListeners();
                        p2KeyBindButtons[i].onClick.AddListener(() => StartRebind(2, act));
                    }
                }
            }
        }

        public void StartRebind(int playerIdx, ControlAction action)
        {
            isListeningForRebind = true;
            rebindPlayerIndex = playerIdx;
            rebindAction = action;
            RefreshControlsUI();

            if (OptionsManager.Instance != null && OptionsManager.Instance.statusFeedbackText != null)
            {
                OptionsManager.Instance.statusFeedbackText.text =
                    $"Presioná una tecla para asignar a P{playerIdx} [{ActionDisplayNames[(int)action]}] (ESC para cancelar)...";
            }
        }

        public void CancelRebind()
        {
            isListeningForRebind = false;
            RefreshControlsUI();

            if (OptionsManager.Instance != null && OptionsManager.Instance.statusFeedbackText != null)
            {
                OptionsManager.Instance.statusFeedbackText.text = "Reasignación de tecla cancelada.";
            }
        }

        public void CompleteRebind(Key newKey)
        {
            isListeningForRebind = false;
            var target = (rebindPlayerIndex == 1) ? p1PendingKeys : p2PendingKeys;
            target.SetKey(rebindAction, newKey);

            RefreshControlsUI();

            if (OptionsManager.Instance != null && OptionsManager.Instance.statusFeedbackText != null)
            {
                OptionsManager.Instance.statusFeedbackText.text =
                    $"P{rebindPlayerIndex} [{ActionDisplayNames[(int)rebindAction]}] asignado a [{FormatKeyName(newKey)}]. Presioná APLICAR para guardar.";
            }
        }

        public void LoadSavedBindings()
        {
            p1CommittedKeys = CreateDefaultP1Keys();
            p2CommittedKeys = CreateDefaultP2Keys();

            foreach (ControlAction act in AllActions)
            {
                string k1 = $"Fightyub_Key_P1_{act}";
                if (PlayerPrefs.HasKey(k1))
                {
                    int v1 = PlayerPrefs.GetInt(k1, (int)p1CommittedKeys.GetKey(act));
                    if (Enum.IsDefined(typeof(Key), v1) && (Key)v1 != Key.None)
                    {
                        p1CommittedKeys.SetKey(act, (Key)v1);
                    }
                }

                string k2 = $"Fightyub_Key_P2_{act}";
                if (PlayerPrefs.HasKey(k2))
                {
                    int v2 = PlayerPrefs.GetInt(k2, (int)p2CommittedKeys.GetKey(act));
                    if (Enum.IsDefined(typeof(Key), v2) && (Key)v2 != Key.None)
                    {
                        p2CommittedKeys.SetKey(act, (Key)v2);
                    }
                }
            }

            SyncPendingFromCommitted();
        }

        public void SaveCommittedBindingsToDisk()
        {
            foreach (ControlAction act in AllActions)
            {
                PlayerPrefs.SetInt($"Fightyub_Key_P1_{act}", (int)p1CommittedKeys.GetKey(act));
                PlayerPrefs.SetInt($"Fightyub_Key_P2_{act}", (int)p2CommittedKeys.GetKey(act));
            }
            PlayerPrefs.Save();
            UpdateAdaptivePromptTexts();
            OnBindingsOrDevicesChanged?.Invoke();
        }

        public void SyncPendingFromCommitted()
        {
            isListeningForRebind = false;
            p1PendingKeys = p1CommittedKeys.Clone();
            p2PendingKeys = p2CommittedKeys.Clone();
            RefreshControlsUI();
        }

        public void CommitPendingBindings()
        {
            isListeningForRebind = false;
            p1CommittedKeys = p1PendingKeys.Clone();
            p2CommittedKeys = p2PendingKeys.Clone();
            SaveCommittedBindingsToDisk();
            RefreshControlsUI();
        }

        public void RestoreDefaultBindingsAndCommit()
        {
            isListeningForRebind = false;
            p1CommittedKeys = CreateDefaultP1Keys();
            p2CommittedKeys = CreateDefaultP2Keys();
            p1PendingKeys = p1CommittedKeys.Clone();
            p2PendingKeys = p2CommittedKeys.Clone();
            SaveCommittedBindingsToDisk();
            RefreshControlsUI();
        }

        public void RefreshConnectedControllers()
        {
            List<InputDevice> controllers = new List<InputDevice>();

            foreach (var gp in Gamepad.all)
            {
                if (gp != null && gp.added)
                {
                    controllers.Add(gp);
                }
            }

            foreach (var js in Joystick.all)
            {
                if (js != null && js.added && !controllers.Contains(js))
                {
                    controllers.Add(js);
                }
            }

            // Rule:
            // - 0 controllers: P1 = Keyboard, P2 = Keyboard
            // - 1 controller:  P1 = Controller[0], P2 = Keyboard
            // - 2+ controllers: P1 = Controller[0], P2 = Controller[1]
            if (controllers.Count == 0)
            {
                p1AssignedController = null;
                p1ControllerFamily = ControllerFamily.None_Keyboard;
                p1DeviceDisplayName = "Teclado";

                p2AssignedController = null;
                p2ControllerFamily = ControllerFamily.None_Keyboard;
                p2DeviceDisplayName = "Teclado";
            }
            else if (controllers.Count == 1)
            {
                p1AssignedController = controllers[0];
                p1ControllerFamily = ClassifyDevice(controllers[0]);
                p1DeviceDisplayName = GetFamilyDisplayName(p1ControllerFamily, controllers[0].displayName);

                p2AssignedController = null;
                p2ControllerFamily = ControllerFamily.None_Keyboard;
                p2DeviceDisplayName = "Teclado";
            }
            else
            {
                p1AssignedController = controllers[0];
                p1ControllerFamily = ClassifyDevice(controllers[0]);
                p1DeviceDisplayName = GetFamilyDisplayName(p1ControllerFamily, controllers[0].displayName);

                p2AssignedController = controllers[1];
                p2ControllerFamily = ClassifyDevice(controllers[1]);
                p2DeviceDisplayName = GetFamilyDisplayName(p2ControllerFamily, controllers[1].displayName);
            }

            UpdateAdaptivePromptTexts();
            RefreshControlsUI();
            OnBindingsOrDevicesChanged?.Invoke();
        }

        public static ControllerFamily ClassifyDevice(InputDevice device)
        {
            if (device == null) return ControllerFamily.None_Keyboard;

            string combined = $"{device.name} {device.displayName} {device.description.product} {device.description.manufacturer} {device.description.interfaceName}".ToLowerInvariant();

            if (combined.Contains("switch 2") || combined.Contains("switch2") || combined.Contains("pro controller 2") || combined.Contains("nsw2"))
            {
                return ControllerFamily.NintendoSwitch2;
            }
            if (combined.Contains("switch") || combined.Contains("joy-con") || combined.Contains("joycon") || combined.Contains("nintendo"))
            {
                return ControllerFamily.NintendoSwitch;
            }
            if (combined.Contains("dualsense") || combined.Contains("ps5") || combined.Contains("dualshock") ||
                combined.Contains("playstation") || combined.Contains("sony") || combined.Contains("wireless controller"))
            {
                return ControllerFamily.PS5_PlayStation;
            }
            if (combined.Contains("360"))
            {
                return ControllerFamily.Xbox360;
            }
            if (combined.Contains("xbox") || combined.Contains("xinput"))
            {
                return ControllerFamily.XboxOneSeries;
            }
            return ControllerFamily.GenericUSBBluetooth;
        }

        public static string GetFamilyDisplayName(ControllerFamily family, string rawName = "")
        {
            switch (family)
            {
                case ControllerFamily.None_Keyboard: return "Teclado";
                case ControllerFamily.Xbox360: return "Mando Xbox 360";
                case ControllerFamily.XboxOneSeries: return "Mando Xbox One/Series";
                case ControllerFamily.PS5_PlayStation: return "Mando PS5 DualSense";
                case ControllerFamily.NintendoSwitch: return "Mando Nintendo Switch";
                case ControllerFamily.NintendoSwitch2: return "Mando Nintendo Switch 2";
                case ControllerFamily.GenericUSBBluetooth:
                    return string.IsNullOrEmpty(rawName) ? "Mando Genérico USB/BT" : $"Mando USB/BT ({rawName})";
                default: return "Teclado";
            }
        }

        public static string GetAdaptedPromptsForFamily(ControllerFamily family, PlayerKeyBindings kbBindings)
        {
            switch (family)
            {
                case ControllerFamily.Xbox360:
                case ControllerFamily.XboxOneSeries:
                    return "Mover: Stick Izq. / Cruceta  |  Piña: X / Y  |  Patada: A / B  |  Cubrirse: RT";

                case ControllerFamily.PS5_PlayStation:
                    return "Mover: Stick Izq. / Cruceta  |  Piña: □ / △  |  Patada: ✕ / ○  |  Cubrirse: R2";

                case ControllerFamily.NintendoSwitch:
                case ControllerFamily.NintendoSwitch2:
                    return "Mover: Stick Izq. / Cruceta  |  Piña: Y (Izq.) / X (Sup.)  |  Patada: B (Inf.) / A (Der.)  |  Cubrirse: ZR";

                case ControllerFamily.GenericUSBBluetooth:
                    return "Mover: Stick Izq. / Cruceta  |  Piña: Botón Izq./Sup.  |  Patada: Botón Inf./Der.  |  Cubrirse: Trigger Der. (RT/R2)";

                case ControllerFamily.None_Keyboard:
                default:
                    if (kbBindings == null) kbBindings = CreateDefaultP1Keys();
                    return $"Mover: {FormatKeyName(kbBindings.up)}/{FormatKeyName(kbBindings.left)}/{FormatKeyName(kbBindings.down)}/{FormatKeyName(kbBindings.right)}  |  Piña: {FormatKeyName(kbBindings.punch)}  |  Patada: {FormatKeyName(kbBindings.kick)}  |  Cubrirse: {FormatKeyName(kbBindings.block)}";
            }
        }

        public void UpdateAdaptivePromptTexts()
        {
            string p1Prompts = GetAdaptedPromptsForFamily(p1ControllerFamily, p1CommittedKeys);
            string p2Prompts = GetAdaptedPromptsForFamily(p2ControllerFamily, p2CommittedKeys);

            if (connectedDevicesSummaryText != null)
            {
                connectedDevicesSummaryText.text =
                    $"P1: {p1DeviceDisplayName}  ({p1Prompts})\n" +
                    $"P2: {p2DeviceDisplayName}  ({p2Prompts})";
            }

            if (controllerPromptsLegendText != null)
            {
                controllerPromptsLegendText.text =
                    "MANDOS SOPORTADOS (Xbox 360, Xbox One/Series, PS5, Switch, Switch 2, USB/BT):\n" +
                    "• Stick Izq. / Cruceta: Moverse   • Botón Físico Izq./Sup. (X/Y | □/△ | Y/X): Piña   • Inf./Der. (A/B | ✕/○ | B/A): Patada   • Trigger Der. (RT/R2/ZR): Cubrirse";
            }

            if (hudControlsOverlayText != null)
            {
                hudControlsOverlayText.text =
                    $"CONTROLES P1 ({p1DeviceDisplayName}): {p1Prompts}\n" +
                    $"CONTROLES P2 ({p2DeviceDisplayName}): {p2Prompts}";
            }
        }

        public void RefreshControlsUI()
        {
            UpdateAdaptivePromptTexts();

            if (p1KeyBindButtonTexts != null)
            {
                for (int i = 0; i < AllActions.Length && i < p1KeyBindButtonTexts.Length; i++)
                {
                    ControlAction act = AllActions[i];
                    bool waiting = isListeningForRebind && rebindPlayerIndex == 1 && rebindAction == act;
                    if (p1KeyBindButtonTexts[i] != null)
                    {
                        p1KeyBindButtonTexts[i].text = waiting
                            ? "PRESIONÁ..."
                            : FormatKeyName(p1PendingKeys.GetKey(act));
                    }
                    if (p1KeyBindButtonImages != null && i < p1KeyBindButtonImages.Length && p1KeyBindButtonImages[i] != null)
                    {
                        p1KeyBindButtonImages[i].color = waiting
                            ? new Color(0.95f, 0.55f, 0.12f, 1f)
                            : new Color(0.18f, 0.25f, 0.40f, 1f);
                    }
                }
            }

            if (p2KeyBindButtonTexts != null)
            {
                for (int i = 0; i < AllActions.Length && i < p2KeyBindButtonTexts.Length; i++)
                {
                    ControlAction act = AllActions[i];
                    bool waiting = isListeningForRebind && rebindPlayerIndex == 2 && rebindAction == act;
                    if (p2KeyBindButtonTexts[i] != null)
                    {
                        p2KeyBindButtonTexts[i].text = waiting
                            ? "PRESIONÁ..."
                            : FormatKeyName(p2PendingKeys.GetKey(act));
                    }
                    if (p2KeyBindButtonImages != null && i < p2KeyBindButtonImages.Length && p2KeyBindButtonImages[i] != null)
                    {
                        p2KeyBindButtonImages[i].color = waiting
                            ? new Color(0.95f, 0.55f, 0.12f, 1f)
                            : new Color(0.18f, 0.25f, 0.40f, 1f);
                    }
                }
            }
        }

        public FighterInputFrame ReadPlayerInput(int playerIndex)
        {
            FighterInputFrame frame = new FighterInputFrame();

            PlayerKeyBindings keys = (playerIndex == 2) ? p2CommittedKeys : p1CommittedKeys;
            InputDevice assignedDevice = (playerIndex == 2) ? p2AssignedController : p1AssignedController;

            // 1. Read keyboard bindings for this player (always active when assigned to keyboard, and also available as fallback for P1)
            var kb = Keyboard.current;
            if (kb != null && (assignedDevice == null || playerIndex == 1))
            {
                KeyControl upCtrl = GetKeyControl(kb, keys.up);
                KeyControl downCtrl = GetKeyControl(kb, keys.down);
                KeyControl leftCtrl = GetKeyControl(kb, keys.left);
                KeyControl rightCtrl = GetKeyControl(kb, keys.right);
                KeyControl punchCtrl = GetKeyControl(kb, keys.punch);
                KeyControl kickCtrl = GetKeyControl(kb, keys.kick);
                KeyControl blockCtrl = GetKeyControl(kb, keys.block);
                KeyControl specialCtrl = GetKeyControl(kb, keys.special);

                ref bool prevKbUp = ref (playerIndex == 2 ? ref _p2KbUpWasActive : ref _p1KbUpWasActive);
                ref bool prevKbDown = ref (playerIndex == 2 ? ref _p2KbDownWasActive : ref _p1KbDownWasActive);
                ref bool prevKbLeft = ref (playerIndex == 2 ? ref _p2KbLeftWasActive : ref _p1KbLeftWasActive);
                ref bool prevKbRight = ref (playerIndex == 2 ? ref _p2KbRightWasActive : ref _p1KbRightWasActive);
                ref bool prevKbPunch = ref (playerIndex == 2 ? ref _p2KbPunchWasActive : ref _p1KbPunchWasActive);
                ref bool prevKbKick = ref (playerIndex == 2 ? ref _p2KbKickWasActive : ref _p1KbKickWasActive);
                ref bool prevKbBlock = ref (playerIndex == 2 ? ref _p2KbBlockWasActive : ref _p1KbBlockWasActive);
                ref bool prevKbSpecial = ref (playerIndex == 2 ? ref _p2KbSpecialWasActive : ref _p1KbSpecialWasActive);

                bool isLeft = leftCtrl != null && leftCtrl.isPressed;
                frame.leftHeld |= isLeft;
                frame.leftPressedThisFrame |= (leftCtrl != null && leftCtrl.wasPressedThisFrame) || (isLeft && !prevKbLeft);
                prevKbLeft = isLeft;

                bool isRight = rightCtrl != null && rightCtrl.isPressed;
                frame.rightHeld |= isRight;
                frame.rightPressedThisFrame |= (rightCtrl != null && rightCtrl.wasPressedThisFrame) || (isRight && !prevKbRight);
                prevKbRight = isRight;

                bool isUp = upCtrl != null && upCtrl.isPressed;
                frame.upPressedThisFrame |= (upCtrl != null && upCtrl.wasPressedThisFrame) || (isUp && !prevKbUp);
                prevKbUp = isUp;

                bool isDown = downCtrl != null && downCtrl.isPressed;
                frame.downHeld |= isDown;
                frame.downPressedThisFrame |= (downCtrl != null && downCtrl.wasPressedThisFrame) || (isDown && !prevKbDown);
                prevKbDown = isDown;

                bool isPunch = punchCtrl != null && punchCtrl.isPressed;
                frame.punchPressedThisFrame |= (punchCtrl != null && punchCtrl.wasPressedThisFrame) || (isPunch && !prevKbPunch);
                prevKbPunch = isPunch;

                bool isKick = kickCtrl != null && kickCtrl.isPressed;
                frame.kickPressedThisFrame |= (kickCtrl != null && kickCtrl.wasPressedThisFrame) || (isKick && !prevKbKick);
                prevKbKick = isKick;

                bool isBlock = blockCtrl != null && blockCtrl.isPressed;
                frame.blockHeld |= isBlock;
                frame.blockPressedThisFrame |= (blockCtrl != null && blockCtrl.wasPressedThisFrame) || (isBlock && !prevKbBlock);
                prevKbBlock = isBlock;

                bool isSpecial = specialCtrl != null && specialCtrl.isPressed;
                frame.specialPressedThisFrame |= (specialCtrl != null && specialCtrl.wasPressedThisFrame) || (isSpecial && !prevKbSpecial);
                prevKbSpecial = isSpecial;

                if (kb.leftShiftKey != null && playerIndex == 1) frame.runHeld |= kb.leftShiftKey.isPressed;
            }

            // 2. Read assigned controller (Gamepad or generic HID Joystick) using physical button positions
            if (assignedDevice != null && assignedDevice.added)
            {
                ReadControllerDeviceIntoFrame(assignedDevice, playerIndex, ref frame);
            }

            return frame;
        }

        private void ReadControllerDeviceIntoFrame(InputDevice device, int playerIndex, ref FighterInputFrame frame)
        {
            ref bool prevUp = ref (playerIndex == 2 ? ref _p2StickUpWasActive : ref _p1StickUpWasActive);
            ref bool prevDown = ref (playerIndex == 2 ? ref _p2StickDownWasActive : ref _p1StickDownWasActive);
            ref bool prevLeft = ref (playerIndex == 2 ? ref _p2StickLeftWasActive : ref _p1StickLeftWasActive);
            ref bool prevRight = ref (playerIndex == 2 ? ref _p2StickRightWasActive : ref _p1StickRightWasActive);
            ref bool prevRT = ref (playerIndex == 2 ? ref _p2RtWasActive : ref _p1RtWasActive);
            ref bool prevLT = ref (playerIndex == 2 ? ref _p2LtWasActive : ref _p1LtWasActive);

            if (device is Gamepad gp)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                Vector2 dpad = gp.dpad.ReadValue();

                bool stickLeft = stick.x < -0.35f;
                bool stickRight = stick.x > 0.35f;
                bool stickUp = stick.y > 0.55f;
                bool stickDown = stick.y < -0.45f;
                bool rtPressed = gp.rightTrigger.isPressed || gp.rightTrigger.ReadValue() > 0.30f;
                bool ltPressed = gp.leftTrigger.isPressed || gp.leftTrigger.ReadValue() > 0.30f;

                frame.leftHeld |= stickLeft || dpad.x < -0.35f || gp.dpad.left.isPressed;
                frame.rightHeld |= stickRight || dpad.x > 0.35f || gp.dpad.right.isPressed;
                frame.downHeld |= stickDown || dpad.y < -0.35f || gp.dpad.down.isPressed;

                frame.leftPressedThisFrame |= gp.dpad.left.wasPressedThisFrame || (stickLeft && !prevLeft);
                frame.rightPressedThisFrame |= gp.dpad.right.wasPressedThisFrame || (stickRight && !prevRight);
                frame.upPressedThisFrame |= gp.dpad.up.wasPressedThisFrame || (stickUp && !prevUp);
                frame.downPressedThisFrame |= gp.dpad.down.wasPressedThisFrame || (stickDown && !prevDown);

                prevLeft = stickLeft;
                prevRight = stickRight;
                prevUp = stickUp;
                prevDown = stickDown;

                // Physical positions:
                // - West (Left face button) or North (Top face button) -> Punch (Piña)
                // - South (Bottom face button) or East (Right face button) -> Kick (Patada)
                // - Right Trigger (RT / R2 / ZR) -> Block (Cubrirse)
                // - Left Trigger (LT / L2 / ZL) -> Special Attack (Ataque Especial)
                frame.punchPressedThisFrame |= gp.buttonWest.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame;
                frame.kickPressedThisFrame |= gp.buttonSouth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame;
                frame.blockHeld |= rtPressed;
                frame.blockPressedThisFrame |= gp.rightTrigger.wasPressedThisFrame || (rtPressed && !prevRT);
                prevRT = rtPressed;

                frame.specialPressedThisFrame |= gp.leftTrigger.wasPressedThisFrame || (ltPressed && !prevLT);
                prevLT = ltPressed;

                // Allow Left Stick click or Left Shoulder or full stick tilt to assist running
                frame.runHeld |= gp.leftStickButton.isPressed || gp.leftShoulder.isPressed;
            }
            else if (device is Joystick js)
            {
                Vector2 stick = js.stick.ReadValue();
                Vector2 hat = js.hatswitch != null ? js.hatswitch.ReadValue() : Vector2.zero;

                bool stickLeft = stick.x < -0.35f || hat.x < -0.35f;
                bool stickRight = stick.x > 0.35f || hat.x > 0.35f;
                bool stickUp = stick.y > 0.55f || hat.y > 0.55f;
                bool stickDown = stick.y < -0.45f || hat.y < -0.45f;

                frame.leftHeld |= stickLeft;
                frame.rightHeld |= stickRight;
                frame.downHeld |= stickDown;

                frame.leftPressedThisFrame |= (stickLeft && !prevLeft);
                frame.rightPressedThisFrame |= (stickRight && !prevRight);
                frame.upPressedThisFrame |= (stickUp && !prevUp);
                frame.downPressedThisFrame |= (stickDown && !prevDown);

                prevLeft = stickLeft;
                prevRight = stickRight;
                prevUp = stickUp;
                prevDown = stickDown;

                // Generic HID Joystick physical buttons:
                // Standard HID layout: 0=South, 1=East, 2=West, 3=North, 5/7=Right Trigger, 4/6=Left Trigger
                var btns = new List<ButtonControl>();
                foreach (var ctrl in js.allControls)
                {
                    if (ctrl is ButtonControl bc && ctrl != js.trigger)
                    {
                        btns.Add(bc);
                    }
                }
                if (btns.Count >= 4)
                {
                    frame.kickPressedThisFrame |= btns[0].wasPressedThisFrame || btns[1].wasPressedThisFrame;
                    frame.punchPressedThisFrame |= btns[2].wasPressedThisFrame || btns[3].wasPressedThisFrame;
                }
                else if (js.trigger != null)
                {
                    frame.punchPressedThisFrame |= js.trigger.wasPressedThisFrame;
                }

                if (btns.Count >= 8)
                {
                    frame.blockHeld |= btns[7].isPressed || btns[5].isPressed;
                    frame.specialPressedThisFrame |= btns[6].wasPressedThisFrame || btns[4].wasPressedThisFrame;
                }
                else if (btns.Count >= 6)
                {
                    frame.blockHeld |= btns[5].isPressed;
                    frame.specialPressedThisFrame |= btns[4].wasPressedThisFrame;
                }
                else if (btns.Count >= 5)
                {
                    frame.specialPressedThisFrame |= btns[4].wasPressedThisFrame;
                }
            }
        }

        public static KeyControl GetKeyControl(Keyboard kb, Key key)
        {
            if (kb == null || key == Key.None) return null;
            try
            {
                return kb[key];
            }
            catch
            {
                return null;
            }
        }

        public static string FormatKeyName(Key key)
        {
            switch (key)
            {
                case Key.UpArrow: return "Flecha Arriba";
                case Key.DownArrow: return "Flecha Abajo";
                case Key.LeftArrow: return "Flecha Izq.";
                case Key.RightArrow: return "Flecha Der.";
                case Key.Space: return "Espacio";
                case Key.LeftShift: return "Shift Izq.";
                case Key.RightShift: return "Shift Der.";
                case Key.LeftCtrl: return "Ctrl Izq.";
                case Key.RightCtrl: return "Ctrl Der.";
                case Key.LeftAlt: return "Alt Izq.";
                case Key.RightAlt: return "Alt Der.";
                case Key.Enter: return "Enter";
                default: return key.ToString().ToUpperInvariant();
            }
        }
    }
}
