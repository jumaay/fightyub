using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Fightyub.Editor
{
    public static class SetupExitButton
    {
        [MenuItem("Fightyub/Setup Exit Button in Main Menu")]
        public static void Setup()
        {
            var fmm = UnityEngine.Object.FindFirstObjectByType<FightMatchManager>();
            if (fmm == null)
            {
                Debug.LogError("FightMatchManager not found!");
                return;
            }

            var mainMenuPanel = GameObject.Find("HUD_Canvas/MainMenu_Panel");
            if (mainMenuPanel == null)
            {
                Debug.LogError("HUD_Canvas/MainMenu_Panel not found!");
                return;
            }

            AssetDatabase.ImportAsset("Assets/Menu/botones.png", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Menu/Btn_Torre.png", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Menu/Btn_Salir.png", ImportAssetOptions.ForceUpdate);

            Undo.RecordObject(mainMenuPanel, "Setup Main Menu Buttons");
            Undo.RecordObject(fmm, "Setup Main Menu Buttons FMM");

            // Load sprites from Assets/Menu/botones.png, Btn_Torre.png, Btn_Salir.png
            Sprite sPelea = null, sVS = null, sOpciones = null;
            var allBotonesAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Menu/botones.png");
            foreach (var obj in allBotonesAssets)
            {
                if (obj is Sprite sp)
                {
                    if (sp.name == "Btn_Pelea") sPelea = sp;
                    else if (sp.name == "Btn_VS") sVS = sp;
                    else if (sp.name == "Btn_Opciones") sOpciones = sp;
                }
            }

            Sprite sTorre = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Menu/Btn_Torre.png");
            Sprite sSalir = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Menu/Btn_Salir.png");

            // Find or create Button_Salir
            Transform exitT = mainMenuPanel.transform.Find("Button_Salir");
            GameObject exitObj;
            if (exitT == null)
            {
                exitObj = new GameObject("Button_Salir", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                exitObj.transform.SetParent(mainMenuPanel.transform, false);
            }
            else
            {
                exitObj = exitT.gameObject;
            }

            string[] btnNames = new string[] { "Button_Pelea", "Button_Torre", "Button_VS", "Button_Opciones", "Button_Salir" };
            Sprite[] btnSprites = new Sprite[] { sPelea, sTorre, sVS, sOpciones, sSalir };
            // Uniform size (246 x 72 matches 1123:329 aspect ratio) and uniform 84px step (12px clear gap between buttons)
            Vector2 unifiedSize = new Vector2(246f, 72f);
            float[] yPositions = new float[] { 36f, -48f, -132f, -216f, -300f };

            Button[] btns = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                Transform t = mainMenuPanel.transform.Find(btnNames[i]);
                if (t == null) continue;

                var rt = t.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = unifiedSize;
                    rt.anchoredPosition = new Vector2(0f, yPositions[i]);
                    rt.localScale = Vector3.one;
                }

                var img = t.GetComponent<Image>();
                if (img != null)
                {
                    if (btnSprites[i] != null) img.sprite = btnSprites[i];
                    img.color = Color.white;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                }

                // Disable any legacy child Text/TMP objects so only the unified arcade sprite is shown
                for (int c = 0; c < t.childCount; c++)
                {
                    t.GetChild(c).gameObject.SetActive(false);
                }

                var btn = t.GetComponent<Button>();
                if (btn != null)
                {
                    btn.targetGraphic = img;
                    btn.interactable = true;
                    btn.transition = Selectable.Transition.None;
                    btns[i] = btn;
                }
            }

            fmm.mainMenuFightButton = btns[0];
            fmm.mainMenuTowerButton = btns[1];
            fmm.mainMenuVsButton = btns[2];
            fmm.mainMenuExitButton = btns[4];

            for (int i = 0; i < btns.Length; i++)
            {
                if (btns[i] == null) continue;
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = btns[(i - 1 + btns.Length) % btns.Length],
                    selectOnDown = btns[(i + 1) % btns.Length]
                };
                btns[i].navigation = nav;
            }

            EditorUtility.SetDirty(mainMenuPanel);
            EditorUtility.SetDirty(fmm);
            EditorSceneManager.MarkSceneDirty(fmm.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=green>Successfully unified all 5 Main Menu buttons (Pelea, Torre, VS, Opciones, Salir del juego)!</color>");
        }
    }
}
