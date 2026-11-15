#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Fightyub;

namespace Fightyub
{
    public static class TowerUISetup
    {
        [MenuItem("Fightyub/Build Tower and Pause UI")]
        public static void Build()
        {
            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null)
            {
                Debug.LogError("HUD_Canvas not found in scene!");
                return;
            }

            var matchManager = Object.FindFirstObjectByType<FightMatchManager>();
            if (matchManager == null)
            {
                Debug.LogError("FightMatchManager not found in scene!");
                return;
            }

            Sprite sTorre = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Menu/torre.png");
            Sprite sFondo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Menu/fondotorre.png");

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                var existingText = hudCanvas.GetComponentInChildren<Text>(true);
                if (existingText != null) defaultFont = existingText.font;
            }

            // Remove existing panels if any
            var oldTower = hudCanvas.transform.Find("Tower_Panel");
            if (oldTower != null) Object.DestroyImmediate(oldTower.gameObject);

            var oldPause = hudCanvas.transform.Find("Pause_Panel");
            if (oldPause != null) Object.DestroyImmediate(oldPause.gameObject);

            // =====================================================================
            // 1. TOWER PANEL
            // =====================================================================
            GameObject towerPanel = CreateUIObject("Tower_Panel", hudCanvas.transform);
            RectTransform towerPanelRect = towerPanel.GetComponent<RectTransform>();
            towerPanelRect.anchorMin = Vector2.zero;
            towerPanelRect.anchorMax = Vector2.one;
            towerPanelRect.offsetMin = Vector2.zero;
            towerPanelRect.offsetMax = Vector2.zero;

            var towerPanelImg = towerPanel.AddComponent<Image>();
            towerPanelImg.color = new Color(0.04f, 0.04f, 0.07f, 1f);

            // Scroll Viewport
            GameObject viewport = CreateUIObject("Tower_ScrollViewport", towerPanel.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(0f, 90f);
            viewportRect.offsetMax = new Vector2(0f, -80f);
            viewport.AddComponent<RectMask2D>();

            // Background Image (Parallax)
            GameObject fondoObj = CreateUIObject("Tower_Fondo_Image", viewport.transform);
            RectTransform fondoRect = fondoObj.GetComponent<RectTransform>();
            fondoRect.anchorMin = new Vector2(0.5f, 0.5f);
            fondoRect.anchorMax = new Vector2(0.5f, 0.5f);
            fondoRect.pivot = new Vector2(0.5f, 0.5f);
            fondoRect.sizeDelta = new Vector2(1280f, 2275f);
            fondoRect.anchoredPosition = Vector2.zero;

            var fondoImg = fondoObj.AddComponent<Image>();
            fondoImg.sprite = sFondo;
            fondoImg.preserveAspect = true;
            fondoImg.color = new Color(0.9f, 0.9f, 0.95f, 1f);

            // Tower Container
            GameObject towerContainerObj = CreateUIObject("Tower_Container", viewport.transform);
            RectTransform towerContainerRect = towerContainerObj.GetComponent<RectTransform>();
            towerContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            towerContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            towerContainerRect.pivot = new Vector2(0.5f, 0.5f);
            towerContainerRect.sizeDelta = new Vector2(1024f, 1536f);
            towerContainerRect.anchoredPosition = new Vector2(0f, 440f); // Start at Floor 0

            // Tower Sprite Image
            GameObject towerImgObj = CreateUIObject("Tower_Artwork", towerContainerObj.transform);
            RectTransform towerArtworkRect = towerImgObj.GetComponent<RectTransform>();
            towerArtworkRect.anchorMin = Vector2.zero;
            towerArtworkRect.anchorMax = Vector2.one;
            towerArtworkRect.offsetMin = Vector2.zero;
            towerArtworkRect.offsetMax = Vector2.zero;

            var towerImg = towerImgObj.AddComponent<Image>();
            towerImg.sprite = sTorre;
            towerImg.preserveAspect = false;

            // 7 Floor Slots - Exactly aligned with the 7 carved stone windows in torre.png (1024x1536)
            // Window openings are centered horizontally at X = 0 (512px) and vertically at these exact centers:
            // Floor 0 (Base / Piso 1): -440f
            // Floor 1 (Piso 2):        -283f
            // Floor 2 (Piso 3):        -126f
            // Floor 3 (Piso 4):        +32f
            // Floor 4 (Piso 5):        +190f
            // Floor 5 (Piso 6):        +348f
            // Floor 6 (Cima / Piso 7): +506f
            float[] floorSlotYs = new float[] { -440f, -283f, -126f, 32f, 190f, 348f, 506f };
            GameObject[] floorSlots = new GameObject[7];
            Image[] floorPortraits = new Image[7];
            Text[] floorNames = new Text[7];
            GameObject[] floorDefeatedBadges = new GameObject[7];
            GameObject[] floorActiveMarkers = new GameObject[7];

            for (int i = 0; i < 7; i++)
            {
                float y = floorSlotYs[i];
                string slotName = (i == 6) ? $"Floor_Slot_6_Summit" : $"Floor_Slot_{i}";
                GameObject slot = CreateUIObject(slotName, towerContainerObj.transform);
                RectTransform slotRect = slot.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.sizeDelta = new Vector2(108f, 122f);
                slotRect.anchoredPosition = new Vector2(0f, y);

                // Portrait Frame Box - Fits precisely inside the 108x122 opening
                GameObject portBorder = CreateUIObject("Portrait_Border", slot.transform);
                RectTransform pbRect = portBorder.GetComponent<RectTransform>();
                pbRect.anchorMin = new Vector2(0.5f, 0.5f);
                pbRect.anchorMax = new Vector2(0.5f, 0.5f);
                pbRect.pivot = new Vector2(0.5f, 0.5f);
                pbRect.sizeDelta = new Vector2(90f, 90f);
                pbRect.anchoredPosition = new Vector2(0f, 7f);
                var pbImg = portBorder.AddComponent<Image>();
                // Dark neutral stone border - no blue tint or colored highlights
                pbImg.color = (i == 6) ? new Color(0.40f, 0.35f, 0.15f, 1f) : new Color(0.12f, 0.12f, 0.14f, 1f);

                // Portrait Image - 100% pure original RGB sprite colors, NO filters, NO tint
                GameObject portObj = CreateUIObject("Portrait_Image", portBorder.transform);
                RectTransform portRect = portObj.GetComponent<RectTransform>();
                portRect.anchorMin = Vector2.zero;
                portRect.anchorMax = Vector2.one;
                portRect.offsetMin = new Vector2(3f, 3f);
                portRect.offsetMax = new Vector2(-3f, -3f);
                var portImg = portObj.AddComponent<Image>();
                portImg.preserveAspect = true;
                portImg.color = Color.white;

                // Name Box - Compact label plate fitted at the bottom of the window
                GameObject nameBox = CreateUIObject("Name_BG", slot.transform);
                RectTransform nameBoxRect = nameBox.GetComponent<RectTransform>();
                nameBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
                nameBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
                nameBoxRect.pivot = new Vector2(0.5f, 0.5f);
                nameBoxRect.sizeDelta = new Vector2(96f, 17f);
                nameBoxRect.anchoredPosition = new Vector2(0f, -44f);
                var nameBoxImg = nameBox.AddComponent<Image>();
                nameBoxImg.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);

                // Name Text
                GameObject nameTxtObj = CreateUIObject("Name_Text", nameBox.transform);
                RectTransform nameTxtRect = nameTxtObj.GetComponent<RectTransform>();
                nameTxtRect.anchorMin = Vector2.zero;
                nameTxtRect.anchorMax = Vector2.one;
                nameTxtRect.offsetMin = new Vector2(2f, 0f);
                nameTxtRect.offsetMax = new Vector2(-2f, 0f);
                var nameTxt = nameTxtObj.AddComponent<Text>();
                nameTxt.font = defaultFont;
                nameTxt.fontSize = 11;
                nameTxt.fontStyle = FontStyle.Bold;
                nameTxt.alignment = TextAnchor.MiddleCenter;
                nameTxt.color = (i == 6) ? new Color(1f, 0.9f, 0.3f, 1f) : new Color(0.92f, 0.94f, 0.98f, 1f);
                nameTxt.text = (i == 6) ? "P7 • CIMA" : $"P{i + 1} • RIVAL";

                // Defeated Badge - Placed below on the name tag, NEVER covering or tinting the portrait
                GameObject defBadge = CreateUIObject("Defeated_Badge", slot.transform);
                RectTransform defRect = defBadge.GetComponent<RectTransform>();
                defRect.anchorMin = new Vector2(0.5f, 0.5f);
                defRect.anchorMax = new Vector2(0.5f, 0.5f);
                defRect.pivot = new Vector2(0.5f, 0.5f);
                defRect.sizeDelta = new Vector2(96f, 17f);
                defRect.anchoredPosition = new Vector2(0f, -44f);
                var defImg = defBadge.AddComponent<Image>();
                defImg.color = new Color(0.45f, 0.08f, 0.08f, 0.95f);

                GameObject defTxtObj = CreateUIObject("Defeated_Text", defBadge.transform);
                RectTransform defTxtRect = defTxtObj.GetComponent<RectTransform>();
                defTxtRect.anchorMin = Vector2.zero;
                defTxtRect.anchorMax = Vector2.one;
                defTxtRect.offsetMin = Vector2.zero;
                defTxtRect.offsetMax = Vector2.zero;
                var defTxt = defTxtObj.AddComponent<Text>();
                defTxt.font = defaultFont;
                defTxt.fontSize = 11;
                defTxt.fontStyle = FontStyle.Bold;
                defTxt.alignment = TextAnchor.MiddleCenter;
                defTxt.color = new Color(1f, 0.90f, 0.90f, 1f);
                defTxt.text = "✓ DERROTADO";
                defBadge.SetActive(false);

                // Active Marker - External indicators only, ZERO overlays or tint on the portrait!
                GameObject actMarker = CreateUIObject("Active_Marker", slot.transform);
                RectTransform actRect = actMarker.GetComponent<RectTransform>();
                actRect.anchorMin = new Vector2(0.5f, 0.5f);
                actRect.anchorMax = new Vector2(0.5f, 0.5f);
                actRect.pivot = new Vector2(0.5f, 0.5f);
                actRect.sizeDelta = new Vector2(108f, 122f);
                actRect.anchoredPosition = new Vector2(0f, 0f);

                // Top "ACTUAL" tag above the window
                GameObject actTag = CreateUIObject("Active_Tag", actMarker.transform);
                RectTransform actTagRect = actTag.GetComponent<RectTransform>();
                actTagRect.anchorMin = new Vector2(0.5f, 1f);
                actTagRect.anchorMax = new Vector2(0.5f, 1f);
                actTagRect.pivot = new Vector2(0.5f, 0f);
                actTagRect.sizeDelta = new Vector2(68f, 16f);
                actTagRect.anchoredPosition = new Vector2(0f, 2f);
                var actTagImg = actTag.AddComponent<Image>();
                actTagImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

                GameObject actTagTxtObj = CreateUIObject("Active_Tag_Text", actTag.transform);
                RectTransform attRect = actTagTxtObj.GetComponent<RectTransform>();
                attRect.anchorMin = Vector2.zero;
                attRect.anchorMax = Vector2.one;
                attRect.offsetMin = Vector2.zero;
                attRect.offsetMax = Vector2.zero;
                var attTxt = actTagTxtObj.AddComponent<Text>();
                attTxt.font = defaultFont;
                attTxt.fontSize = 10;
                attTxt.fontStyle = FontStyle.Bold;
                attTxt.alignment = TextAnchor.MiddleCenter;
                attTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
                attTxt.text = "ACTUAL";

                // Left Arrow pointing to window from outside
                GameObject leftArrow = CreateUIObject("Active_Left_Arrow", actMarker.transform);
                RectTransform laRect = leftArrow.GetComponent<RectTransform>();
                laRect.anchorMin = new Vector2(0f, 0.5f);
                laRect.anchorMax = new Vector2(0f, 0.5f);
                laRect.pivot = new Vector2(1f, 0.5f);
                laRect.sizeDelta = new Vector2(20f, 20f);
                laRect.anchoredPosition = new Vector2(-5f, 7f);
                var laTxt = leftArrow.AddComponent<Text>();
                laTxt.font = defaultFont;
                laTxt.fontSize = 16;
                laTxt.fontStyle = FontStyle.Bold;
                laTxt.alignment = TextAnchor.MiddleCenter;
                laTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
                laTxt.text = "▶";

                // Right Arrow pointing to window from outside
                GameObject rightArrow = CreateUIObject("Active_Right_Arrow", actMarker.transform);
                RectTransform raRect = rightArrow.GetComponent<RectTransform>();
                raRect.anchorMin = new Vector2(1f, 0.5f);
                raRect.anchorMax = new Vector2(1f, 0.5f);
                raRect.pivot = new Vector2(0f, 0.5f);
                raRect.sizeDelta = new Vector2(20f, 20f);
                raRect.anchoredPosition = new Vector2(5f, 7f);
                var raTxt = rightArrow.AddComponent<Text>();
                raTxt.font = defaultFont;
                raTxt.fontSize = 16;
                raTxt.fontStyle = FontStyle.Bold;
                raTxt.alignment = TextAnchor.MiddleCenter;
                raTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
                raTxt.text = "◀";

                actMarker.SetActive(i == 0);

                floorSlots[i] = slot;
                floorPortraits[i] = portImg;
                floorNames[i] = nameTxt;
                floorDefeatedBadges[i] = defBadge;
                floorActiveMarkers[i] = actMarker;
            }

            // Header Bar
            GameObject headerBar = CreateUIObject("Tower_Header_Bar", towerPanel.transform);
            RectTransform hbRect = headerBar.GetComponent<RectTransform>();
            hbRect.anchorMin = new Vector2(0f, 1f);
            hbRect.anchorMax = new Vector2(1f, 1f);
            hbRect.pivot = new Vector2(0.5f, 1f);
            hbRect.sizeDelta = new Vector2(0f, 80f);
            hbRect.anchoredPosition = Vector2.zero;
            var hbImg = headerBar.AddComponent<Image>();
            hbImg.color = new Color(0.07f, 0.08f, 0.12f, 0.95f);

            GameObject titleObj = CreateUIObject("Tower_Title", headerBar.transform);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.45f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = defaultFont;
            titleTxt.fontSize = 30;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
            titleTxt.text = "MODO TORRE — ASCENSO AL CAMPEONATO";

            GameObject subtitleObj = CreateUIObject("Tower_Subtitle", headerBar.transform);
            RectTransform subRect = subtitleObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(1f, 0.45f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            var subTxt = subtitleObj.AddComponent<Text>();
            subTxt.font = defaultFont;
            subTxt.fontSize = 15;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.85f, 0.88f, 0.95f, 1f);
            subTxt.text = "Derrota a los 7 rivales consecutivos para conquistar la cima";

            // Footer Bar
            GameObject footerBar = CreateUIObject("Tower_Footer_Bar", towerPanel.transform);
            RectTransform fbRect = footerBar.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0f, 0f);
            fbRect.anchorMax = new Vector2(1f, 0f);
            fbRect.pivot = new Vector2(0.5f, 0f);
            fbRect.sizeDelta = new Vector2(0f, 90f);
            fbRect.anchoredPosition = Vector2.zero;
            var fbImg = footerBar.AddComponent<Image>();
            fbImg.color = new Color(0.07f, 0.08f, 0.12f, 0.96f);

            // Footer Info Text
            GameObject footerInfoObj = CreateUIObject("Tower_Footer_Text", footerBar.transform);
            RectTransform fiRect = footerInfoObj.GetComponent<RectTransform>();
            fiRect.anchorMin = new Vector2(0f, 0f);
            fiRect.anchorMax = new Vector2(0.6f, 1f);
            fiRect.offsetMin = new Vector2(30f, 0f);
            fiRect.offsetMax = new Vector2(-10f, 0f);
            var fiTxt = footerInfoObj.AddComponent<Text>();
            fiTxt.font = defaultFont;
            fiTxt.fontSize = 19;
            fiTxt.fontStyle = FontStyle.Bold;
            fiTxt.alignment = TextAnchor.MiddleLeft;
            fiTxt.color = new Color(0.2f, 0.9f, 0.95f, 1f);
            fiTxt.text = "PISO 1 DE 7:  RIVAL  —  AL MEJOR DE 3 ROUNDS";

            // Buttons Container
            GameObject startBtnObj = CreateButton("Button_StartFight", footerBar.transform, "¡A PELEAR!", 190f, 52f,
                new Color(0.18f, 0.65f, 0.32f, 1f), new Color(0.25f, 0.85f, 0.42f, 1f), defaultFont, 20);
            RectTransform startBtnRect = startBtnObj.GetComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(1f, 0.5f);
            startBtnRect.anchorMax = new Vector2(1f, 0.5f);
            startBtnRect.pivot = new Vector2(1f, 0.5f);
            startBtnRect.anchoredPosition = new Vector2(-25f, 0f);
            var startBtn = startBtnObj.GetComponent<Button>();

            GameObject backBtnObj = CreateButton("Button_BackMenu", footerBar.transform, "VOLVER AL MENÚ", 190f, 52f,
                new Color(0.45f, 0.18f, 0.18f, 1f), new Color(0.65f, 0.25f, 0.25f, 1f), defaultFont, 16);
            RectTransform backBtnRect = backBtnObj.GetComponent<RectTransform>();
            backBtnRect.anchorMin = new Vector2(1f, 0.5f);
            backBtnRect.anchorMax = new Vector2(1f, 0.5f);
            backBtnRect.pivot = new Vector2(1f, 0.5f);
            backBtnRect.anchoredPosition = new Vector2(-230f, 0f);
            var backBtn = backBtnObj.GetComponent<Button>();

            // =====================================================================
            // TOWER VICTORY MODAL ("FELICITACIONES, GANASTE")
            // =====================================================================
            GameObject victoryModal = CreateUIObject("Tower_Victory_Modal", towerPanel.transform);
            RectTransform vicModalRect = victoryModal.GetComponent<RectTransform>();
            vicModalRect.anchorMin = Vector2.zero;
            vicModalRect.anchorMax = Vector2.one;
            vicModalRect.offsetMin = Vector2.zero;
            vicModalRect.offsetMax = Vector2.zero;
            var vicModalImg = victoryModal.AddComponent<Image>();
            vicModalImg.color = new Color(0.02f, 0.02f, 0.04f, 0.88f);

            GameObject vicDialog = CreateUIObject("Victory_Dialog_Box", victoryModal.transform);
            RectTransform vdRect = vicDialog.GetComponent<RectTransform>();
            vdRect.anchorMin = new Vector2(0.5f, 0.5f);
            vdRect.anchorMax = new Vector2(0.5f, 0.5f);
            vdRect.pivot = new Vector2(0.5f, 0.5f);
            vdRect.sizeDelta = new Vector2(740f, 400f);
            var vdImg = vicDialog.AddComponent<Image>();
            vdImg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

            // Dialog Border
            GameObject vdBG = CreateUIObject("Dialog_Border", vicDialog.transform);
            RectTransform vdBgRect = vdBG.GetComponent<RectTransform>();
            vdBgRect.anchorMin = Vector2.zero;
            vdBgRect.anchorMax = Vector2.one;
            vdBgRect.offsetMin = new Vector2(6f, 6f);
            vdBgRect.offsetMax = new Vector2(-6f, -6f);
            var vdBgImg = vdBG.AddComponent<Image>();
            vdBgImg.color = new Color(0.06f, 0.07f, 0.12f, 1f);

            // Victory Title
            GameObject vicTitleObj = CreateUIObject("Victory_Title_Text", vdBG.transform);
            RectTransform vtRect = vicTitleObj.GetComponent<RectTransform>();
            vtRect.anchorMin = new Vector2(0f, 0.65f);
            vtRect.anchorMax = new Vector2(1f, 0.95f);
            vtRect.offsetMin = Vector2.zero;
            vtRect.offsetMax = Vector2.zero;
            var vtTxt = vicTitleObj.AddComponent<Text>();
            vtTxt.font = defaultFont;
            vtTxt.fontSize = 38;
            vtTxt.fontStyle = FontStyle.Bold;
            vtTxt.alignment = TextAnchor.MiddleCenter;
            vtTxt.color = new Color(1f, 0.85f, 0.15f, 1f);
            vtTxt.text = "FELICITACIONES, GANASTE";

            // Victory Subtitle
            GameObject vicSubObj = CreateUIObject("Victory_Subtitle_Text", vdBG.transform);
            RectTransform vsRect = vicSubObj.GetComponent<RectTransform>();
            vsRect.anchorMin = new Vector2(0f, 0.35f);
            vsRect.anchorMax = new Vector2(1f, 0.65f);
            vsRect.offsetMin = new Vector2(25f, 0f);
            vsRect.offsetMax = new Vector2(-25f, 0f);
            var vsTxt = vicSubObj.AddComponent<Text>();
            vsTxt.font = defaultFont;
            vsTxt.fontSize = 20;
            vsTxt.alignment = TextAnchor.MiddleCenter;
            vsTxt.color = new Color(0.9f, 0.95f, 1f, 1f);
            vsTxt.text = "¡Has derrotado a los 7 rivales de la torre!\nSos el campeón indiscutido de Fightyub.";

            // Victory Menu Button
            GameObject vicBtnObj = CreateButton("Button_VolverAlMenuPrincipal", vdBG.transform, "Volver al menú principal", 340f, 62f,
                new Color(0.18f, 0.6f, 0.3f, 1f), new Color(0.24f, 0.8f, 0.4f, 1f), defaultFont, 22);
            RectTransform vicBtnRect = vicBtnObj.GetComponent<RectTransform>();
            vicBtnRect.anchorMin = new Vector2(0.5f, 0.18f);
            vicBtnRect.anchorMax = new Vector2(0.5f, 0.18f);
            vicBtnRect.pivot = new Vector2(0.5f, 0.5f);
            vicBtnRect.anchoredPosition = Vector2.zero;
            var vicBtn = vicBtnObj.GetComponent<Button>();

            victoryModal.SetActive(false);
            towerPanel.SetActive(false);

            // =====================================================================
            // 2. PAUSE PANEL ("Seguir jugando" y "Salir al menú")
            // =====================================================================
            GameObject pausePanel = CreateUIObject("Pause_Panel", hudCanvas.transform);
            RectTransform pausePanelRect = pausePanel.GetComponent<RectTransform>();
            pausePanelRect.anchorMin = Vector2.zero;
            pausePanelRect.anchorMax = Vector2.one;
            pausePanelRect.offsetMin = Vector2.zero;
            pausePanelRect.offsetMax = Vector2.zero;
            var pausePanelImg = pausePanel.AddComponent<Image>();
            pausePanelImg.color = new Color(0f, 0f, 0f, 0.75f);

            // Dialog Box
            GameObject pDialog = CreateUIObject("Pause_Dialog_Box", pausePanel.transform);
            RectTransform pdRect = pDialog.GetComponent<RectTransform>();
            pdRect.anchorMin = new Vector2(0.5f, 0.5f);
            pdRect.anchorMax = new Vector2(0.5f, 0.5f);
            pdRect.pivot = new Vector2(0.5f, 0.5f);
            pdRect.sizeDelta = new Vector2(460f, 320f);
            var pdImg = pDialog.AddComponent<Image>();
            pdImg.color = new Color(0.12f, 0.13f, 0.18f, 0.98f);

            // Inner Frame
            GameObject pInner = CreateUIObject("Pause_Inner_Frame", pDialog.transform);
            RectTransform piRect = pInner.GetComponent<RectTransform>();
            piRect.anchorMin = Vector2.zero;
            piRect.anchorMax = Vector2.one;
            piRect.offsetMin = new Vector2(5f, 5f);
            piRect.offsetMax = new Vector2(-5f, -5f);
            var piImg = pInner.AddComponent<Image>();
            piImg.color = new Color(0.06f, 0.07f, 0.11f, 1f);

            // Title "PAUSA"
            GameObject pTitleObj = CreateUIObject("Pause_Title_Text", pInner.transform);
            RectTransform ptRect = pTitleObj.GetComponent<RectTransform>();
            ptRect.anchorMin = new Vector2(0f, 0.72f);
            ptRect.anchorMax = new Vector2(1f, 0.96f);
            ptRect.offsetMin = Vector2.zero;
            ptRect.offsetMax = Vector2.zero;
            var ptTxt = pTitleObj.AddComponent<Text>();
            ptTxt.font = defaultFont;
            ptTxt.fontSize = 36;
            ptTxt.fontStyle = FontStyle.Bold;
            ptTxt.alignment = TextAnchor.MiddleCenter;
            ptTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
            ptTxt.text = "PAUSA";

                        // Button 1: "SEGUIR"
            GameObject resumeBtnObj = CreateButton("Button_SeguirJugando", pInner.transform, "SEGUIR", 330f, 48f,
                new Color(0.16f, 0.46f, 0.24f, 1f), new Color(0.24f, 0.70f, 0.36f, 1f), defaultFont, 20);
            RectTransform resBtnRect = resumeBtnObj.GetComponent<RectTransform>();
            resBtnRect.anchorMin = new Vector2(0.5f, 0.58f);
            resBtnRect.anchorMax = new Vector2(0.5f, 0.58f);
            resBtnRect.pivot = new Vector2(0.5f, 0.5f);
            resBtnRect.anchoredPosition = Vector2.zero;
            var resumeBtn = resumeBtnObj.GetComponent<Button>();

            // Button 2: "REVANCHA"
            GameObject rematchBtnObj = CreateButton("Button_Revancha", pInner.transform, "REVANCHA", 330f, 48f,
                new Color(0.18f, 0.36f, 0.62f, 1f), new Color(0.26f, 0.52f, 0.88f, 1f), defaultFont, 20);
            RectTransform rematchBtnRect = rematchBtnObj.GetComponent<RectTransform>();
            rematchBtnRect.anchorMin = new Vector2(0.5f, 0.38f);
            rematchBtnRect.anchorMax = new Vector2(0.5f, 0.38f);
            rematchBtnRect.pivot = new Vector2(0.5f, 0.5f);
            rematchBtnRect.anchoredPosition = Vector2.zero;
            var rematchBtn = rematchBtnObj.GetComponent<Button>();

            // Button 3: "VOLVER AL MENÚ"
            GameObject exitBtnObj = CreateButton("Button_SalirAlMenu", pInner.transform, "VOLVER AL MENÚ", 330f, 48f,
                new Color(0.50f, 0.16f, 0.16f, 1f), new Color(0.72f, 0.22f, 0.22f, 1f), defaultFont, 20);
            RectTransform exitBtnRect = exitBtnObj.GetComponent<RectTransform>();
            exitBtnRect.anchorMin = new Vector2(0.5f, 0.18f);
            exitBtnRect.anchorMax = new Vector2(0.5f, 0.18f);
            exitBtnRect.pivot = new Vector2(0.5f, 0.5f);
            exitBtnRect.anchoredPosition = Vector2.zero;
            var exitBtn = exitBtnObj.GetComponent<Button>();

            pausePanel.SetActive(false);

            // =====================================================================
            // 3. WIRE SERIALIZED FIELDS TO FightMatchManager
            // =====================================================================
            matchManager.towerPanel = towerPanel;
            matchManager.towerScrollViewport = viewportRect;
            matchManager.towerContainer = towerContainerRect;
            matchManager.towerFondoRect = fondoRect;
            matchManager.towerImage = towerImg;
            matchManager.towerFondoImage = fondoImg;
            matchManager.towerFloorSlots = floorSlots;
            matchManager.towerFloorPortraits = floorPortraits;
            matchManager.towerFloorNames = floorNames;
            matchManager.towerFloorDefeatedBadges = floorDefeatedBadges;
            matchManager.towerFloorActiveMarkers = floorActiveMarkers;
            matchManager.towerFooterFloorText = fiTxt;
            matchManager.towerStartFightButton = startBtn;
            matchManager.towerBackToMenuButton = backBtn;

            matchManager.towerVictoryModal = victoryModal;
            matchManager.towerVictoryTitleText = vtTxt;
            matchManager.towerVictoryMenuButton = vicBtn;

            matchManager.pausePanel = pausePanel;
            matchManager.pauseResumeButton = resumeBtn;
            matchManager.pauseRematchButton = rematchBtn;
            matchManager.pauseExitMenuButton = exitBtn;

            EditorUtility.SetDirty(matchManager);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("Successfully created and wired Tower_Panel and Pause_Panel!");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, float width, float height,
            Color normalColor, Color highlightedColor, Font font, int fontSize)
        {
            var btnObj = CreateUIObject(name, parent);
            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            var img = btnObj.AddComponent<Image>();
            img.color = normalColor;

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = highlightedColor * 0.8f;
            btn.colors = colors;

            var textObj = CreateUIObject("Text", btnObj.transform);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var txt = textObj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;

            return btnObj;
        }
    }
}
#endif
