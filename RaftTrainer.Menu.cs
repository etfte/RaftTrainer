using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RaftTrainer
{
    // ==========================================
    // V14 МЕНЮ: ТАБЫ, СТИЛИ, HUD-ОВЕРЛЕИ
    // ==========================================
    public partial class TrainerGUI : MonoBehaviour
    {
        private string cachedCompassStr = "";
        private float nextCompassUpdateTime = 0f;
        private static readonly Color FpsHighColor = new Color(0.3f, 1f, 0.55f);
        private static readonly Color FpsMidColor = new Color(1f, 0.75f, 0.2f);
        private static readonly Color FpsLowColor = new Color(1f, 0.3f, 0.35f);
        private static readonly Color FpsGraphHighColor = new Color(0.3f, 1f, 0.55f, 0.9f);
        private static readonly Color FpsGraphMidColor = new Color(1f, 0.75f, 0.2f, 0.9f);
        private static readonly Color FpsGraphLowColor = new Color(1f, 0.3f, 0.35f, 0.9f);
        private static readonly Color FpsGraphBgColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color AlarmBaseColor = new Color(1f, 0.25f, 0.3f, 1f);
        private static readonly Color MinimapNorthColor = new Color(1f, 0.9f, 0.4f);
        private static readonly GUILayoutOption[] OptToggleRow = new GUILayoutOption[] { GUILayout.ExpandWidth(true) };
        private static readonly GUILayoutOption[] OptHeight18 = new GUILayoutOption[] { GUILayout.Height(18) };
        private static readonly GUILayoutOption[] OptHeight20 = new GUILayoutOption[] { GUILayout.Height(20) };
        private static readonly GUILayoutOption[] OptHeight22 = new GUILayoutOption[] { GUILayout.Height(22) };
        private static readonly GUILayoutOption[] OptHeight24 = new GUILayoutOption[] { GUILayout.Height(24) };
        private static readonly GUILayoutOption[] OptHeight26 = new GUILayoutOption[] { GUILayout.Height(26) };
        private string cachedWeatherTxt = "";
        private float nextWeatherTxtTime = 0f;
        // ТАБЫ
        private static readonly string[] tabNames = new string[]
        {
            "⚔ БОЙ", "🏃 БЕГ", "👁 ВИЗУАЛ", "🛡 ИГРОК", "⚓ ПЛОТ",
            "🌾 ФЕРМА", "🤖 АВТО", "⚙ СИСТЕМА", "🎒 СПАВНЕР", "📜 ЛОГ"
        };
        private int selectedTab = 0;
        private readonly Vector2[] tabScrolls = new Vector2[10];
        private Vector2 logScroll = Vector2.zero;

        // STYLING
        private GUIStyle topBarStyle;
        private GUIStyle titleBadgeStyle;
        private GUIStyle columnCardStyle;
        private GUIStyle columnHeaderStyle;
        private GUIStyle itemLabelStyle;
        private GUIStyle itemLabelActiveStyle;
        private GUIStyle subLabelStyle;
        private GUIStyle actionBtnStyle;
        private GUIStyle actionBtnGreenStyle;
        private GUIStyle actionBtnRedStyle;
        private GUIStyle actionBtnBlueStyle;
        private GUIStyle actionBtnAmberStyle;
        private GUIStyle actionBtnPurpleStyle;
        private GUIStyle searchBoxStyle;
        private GUIStyle statusBoxStyle;
        private GUIStyle statusTextStyle;
        private GUIStyle espBoxStyle;
        private GUIStyle itemRowStyle;
        private GUIStyle itemRowSelectedStyle;
        private GUIStyle iconBoxStyle;
        private GUIStyle rowStyle;
        private GUIStyle rowHoverStyle;
        private GUIStyle versionBadgeStyle;
        private GUIStyle sliderStyle;
        private GUIStyle sliderThumbStyle;
        private GUIStyle scrollTrackStyle;
        private GUIStyle scrollThumbStyle;
        private GUIStyle hiddenScrollButtonStyle;
        private GUIStyle hudPanelStyle;
        private GUIStyle hudTitleStyle;
        private GUIStyle hudTextStyle;
        private GUISkin trainerSkin;

        private Texture2D texTopBarBg;
        private Texture2D texCardBg;
        private Texture2D texHeaderBg;
        private Texture2D texIconBoxBg;
        private Texture2D texBtnNormal;
        private Texture2D texBtnHover;
        private Texture2D texBtnBlue;
        private Texture2D texBtnGreen;
        private Texture2D texBtnRed;
        private Texture2D texBtnAmber;
        private Texture2D texBtnPurple;
        private Texture2D texEspBg;
        private Texture2D texRowSelected;
        private Texture2D texRowBg;
        private Texture2D texRowHover;
        private Texture2D texSwitchOn;
        private Texture2D texSwitchOff;
        private Texture2D texKnob;
        private Texture2D texSliderTrack;
        private Texture2D texSliderThumb;
        private Texture2D texSliderThumbHover;
        private Texture2D texScrollTrack;
        private Texture2D texScrollThumb;
        private Texture2D texScrollThumbHover;
        private Texture2D texSearchBg;
        private Texture2D texSearchFocus;
        private Texture2D texWhiteDot;
        private Texture2D texAccentLine;
        private Texture2D texHudPanel;

        private void InitStyles()
        {
            if (stylesInitialized) return;

            // Deep navy glass palette with cyan / mint / violet accents
            Color panel = new Color(0.040f, 0.052f, 0.088f, 0.98f);
            Color card = new Color(0.062f, 0.078f, 0.128f, 0.97f);
            Color raised = new Color(0.094f, 0.116f, 0.182f, 1.00f);
            Color hover = new Color(0.130f, 0.160f, 0.245f, 1.00f);
            Color edge = new Color(0.170f, 0.210f, 0.310f, 1.00f);
            Color edgeSoft = new Color(0.118f, 0.146f, 0.220f, 1.00f);

            Color cyan = new Color(0.00f, 0.84f, 0.95f, 1.00f);
            Color mint = new Color(0.00f, 0.92f, 0.64f, 1.00f);
            Color green = new Color(0.12f, 0.78f, 0.46f, 1.00f);
            Color blue = new Color(0.20f, 0.48f, 1.00f, 1.00f);
            Color red = new Color(0.92f, 0.27f, 0.34f, 1.00f);
            Color amber = new Color(1.00f, 0.66f, 0.14f, 1.00f);
            Color purple = new Color(0.62f, 0.38f, 0.96f, 1.00f);

            texTopBarBg = MakeRoundedTex(8, panel, edge);
            texCardBg = MakeRoundedTex(8, card, edgeSoft);
            texHeaderBg = MakeRoundedTex(6, raised, edgeSoft);
            texIconBoxBg = MakeRoundedTex(4, new Color(0.03f, 0.04f, 0.07f, 1f), edgeSoft);

            texBtnNormal = MakeRoundedTex(5, raised, edgeSoft);
            texBtnHover = MakeRoundedTex(5, hover, edge);
            texBtnBlue = MakeRoundedTex(5, blue, Lighten(blue, 1.3f));
            texBtnGreen = MakeRoundedTex(5, green, Lighten(green, 1.3f));
            texBtnRed = MakeRoundedTex(5, red, Lighten(red, 1.3f));
            texBtnAmber = MakeRoundedTex(5, amber, Lighten(amber, 1.25f));
            texBtnPurple = MakeRoundedTex(5, purple, Lighten(purple, 1.3f));

            texEspBg = MakeRoundedTex(4, new Color(0.02f, 0.03f, 0.05f, 0.80f), new Color(0.22f, 0.28f, 0.40f, 0.70f));
            texRowSelected = MakeRoundedTex(5, new Color(0.12f, 0.22f, 0.40f, 1f), cyan);
            texHudPanel = MakeRoundedTex(6, new Color(0.020f, 0.028f, 0.048f, 0.78f), new Color(0.16f, 0.22f, 0.33f, 0.85f));

            texRowBg = MakeRoundedTex(4, new Color(0.072f, 0.090f, 0.145f, 1f), new Color(0.100f, 0.126f, 0.190f, 1f));
            texRowHover = MakeRoundedTex(4, new Color(0.108f, 0.138f, 0.218f, 1f), new Color(0.210f, 0.300f, 0.460f, 1f));
            texSwitchOn = MakeRoundedTex(8, mint, Lighten(mint, 1.25f));
            texSwitchOff = MakeRoundedTex(8, new Color(0.150f, 0.180f, 0.260f, 1f), edge);
            texKnob = MakeRoundedTex(6, new Color(0.97f, 0.98f, 1f, 1f), new Color(0.78f, 0.84f, 0.92f, 1f));
            texSliderTrack = MakeRoundedTex(4, new Color(0.110f, 0.135f, 0.205f, 1f), edgeSoft);
            texSliderThumb = MakeRoundedTex(6, cyan, Lighten(cyan, 1.35f));
            texSliderThumbHover = MakeRoundedTex(6, mint, Lighten(mint, 1.35f));
            texScrollTrack = MakeRoundedTex(3, new Color(0.05f, 0.06f, 0.10f, 1f), new Color(0.05f, 0.06f, 0.10f, 1f));
            texScrollThumb = MakeRoundedTex(3, new Color(0.22f, 0.28f, 0.40f, 1f), new Color(0.30f, 0.38f, 0.52f, 1f));
            texScrollThumbHover = MakeRoundedTex(3, cyan, cyan);
            texSearchBg = MakeRoundedTex(5, new Color(0.050f, 0.064f, 0.108f, 1f), edge);
            texSearchFocus = MakeRoundedTex(5, new Color(0.050f, 0.064f, 0.108f, 1f), cyan);
            texWhiteDot = MakeTex(2, 2, Color.white);
            texAccentLine = MakeHGradientTex(cyan, purple);

            // Top Bar
            topBarStyle = new GUIStyle(GUI.skin.box);
            topBarStyle.normal.background = texTopBarBg;
            topBarStyle.border = new RectOffset(8, 8, 8, 8);
            topBarStyle.padding = new RectOffset(6, 6, 6, 6);

            titleBadgeStyle = new GUIStyle(GUI.skin.label);
            titleBadgeStyle.fontSize = 14;
            titleBadgeStyle.fontStyle = FontStyle.Bold;
            titleBadgeStyle.alignment = TextAnchor.MiddleLeft;
            titleBadgeStyle.richText = true;
            titleBadgeStyle.normal.textColor = Color.white;

            versionBadgeStyle = new GUIStyle(GUI.skin.box);
            versionBadgeStyle.normal.background = texBtnPurple;
            versionBadgeStyle.border = new RectOffset(5, 5, 5, 5);
            versionBadgeStyle.normal.textColor = Color.white;
            versionBadgeStyle.fontSize = 10;
            versionBadgeStyle.fontStyle = FontStyle.Bold;
            versionBadgeStyle.alignment = TextAnchor.MiddleCenter;
            versionBadgeStyle.padding = new RectOffset(2, 2, 1, 1);

            // Column Card
            columnCardStyle = new GUIStyle(GUI.skin.box);
            columnCardStyle.normal.background = texCardBg;
            columnCardStyle.border = new RectOffset(8, 8, 8, 8);
            columnCardStyle.padding = new RectOffset(0, 0, 0, 0);
            columnCardStyle.margin = new RectOffset(0, 0, 0, 0);

            columnHeaderStyle = new GUIStyle(GUI.skin.box);
            columnHeaderStyle.normal.background = texHeaderBg;
            columnHeaderStyle.border = new RectOffset(6, 6, 6, 6);
            columnHeaderStyle.normal.textColor = Color.white;
            columnHeaderStyle.fontSize = 11;
            columnHeaderStyle.fontStyle = FontStyle.Bold;
            columnHeaderStyle.alignment = TextAnchor.MiddleCenter;
            columnHeaderStyle.richText = true;
            columnHeaderStyle.padding = new RectOffset(4, 4, 2, 2);
            columnHeaderStyle.margin = new RectOffset(0, 0, 0, 0);

            // Item Labels
            itemLabelStyle = new GUIStyle(GUI.skin.label);
            itemLabelStyle.normal.textColor = new Color(0.80f, 0.85f, 0.93f);
            itemLabelStyle.fontSize = 11;
            itemLabelStyle.alignment = TextAnchor.MiddleLeft;
            itemLabelStyle.richText = true;
            itemLabelStyle.padding = new RectOffset(0, 0, 0, 0);
            itemLabelStyle.margin = new RectOffset(0, 0, 0, 0);

            itemLabelActiveStyle = new GUIStyle(itemLabelStyle);
            itemLabelActiveStyle.normal.textColor = new Color(0.45f, 1.0f, 0.85f);
            itemLabelActiveStyle.fontStyle = FontStyle.Bold;

            subLabelStyle = new GUIStyle(GUI.skin.label);
            subLabelStyle.normal.textColor = new Color(0.56f, 0.63f, 0.75f);
            subLabelStyle.fontSize = 10;
            subLabelStyle.alignment = TextAnchor.MiddleLeft;
            subLabelStyle.richText = true;
            subLabelStyle.padding = new RectOffset(2, 2, 0, 0);
            subLabelStyle.margin = new RectOffset(0, 0, 0, 0);

            // Toggle rows
            rowStyle = new GUIStyle(GUI.skin.box);
            rowStyle.normal.background = texRowBg;
            rowStyle.border = new RectOffset(4, 4, 4, 4);
            rowStyle.padding = new RectOffset(0, 0, 0, 0);
            rowStyle.margin = new RectOffset(0, 0, 0, 0);

            rowHoverStyle = new GUIStyle(rowStyle);
            rowHoverStyle.normal.background = texRowHover;

            // Action Buttons
            actionBtnStyle = MakeButtonStyle(texBtnNormal, texBtnHover, new Color(0.88f, 0.92f, 0.98f));
            actionBtnBlueStyle = MakeButtonStyle(texBtnBlue, texBtnBlue, Color.white);
            actionBtnGreenStyle = MakeButtonStyle(texBtnGreen, texBtnGreen, Color.white);
            actionBtnRedStyle = MakeButtonStyle(texBtnRed, texBtnRed, Color.white);
            actionBtnAmberStyle = MakeButtonStyle(texBtnAmber, texBtnAmber, new Color(0.10f, 0.08f, 0.04f));
            actionBtnPurpleStyle = MakeButtonStyle(texBtnPurple, texBtnPurple, Color.white);

            // Search Bar & Status
            searchBoxStyle = new GUIStyle(GUI.skin.textField);
            searchBoxStyle.normal.background = texSearchBg;
            searchBoxStyle.hover.background = texSearchBg;
            searchBoxStyle.focused.background = texSearchFocus;
            searchBoxStyle.border = new RectOffset(5, 5, 5, 5);
            searchBoxStyle.normal.textColor = Color.white;
            searchBoxStyle.hover.textColor = Color.white;
            searchBoxStyle.focused.textColor = Color.white;
            searchBoxStyle.fontSize = 11;
            searchBoxStyle.alignment = TextAnchor.MiddleLeft;
            searchBoxStyle.padding = new RectOffset(6, 6, 3, 3);
            searchBoxStyle.margin = new RectOffset(0, 0, 0, 0);

            statusBoxStyle = new GUIStyle(GUI.skin.box);
            statusBoxStyle.normal.background = texTopBarBg;
            statusBoxStyle.border = new RectOffset(8, 8, 8, 8);
            statusBoxStyle.padding = new RectOffset(4, 4, 4, 4);

            statusTextStyle = new GUIStyle(GUI.skin.label);
            statusTextStyle.normal.textColor = new Color(0.80f, 0.85f, 0.92f);
            statusTextStyle.fontSize = 11;
            statusTextStyle.fontStyle = FontStyle.Bold;
            statusTextStyle.alignment = TextAnchor.MiddleLeft;
            statusTextStyle.richText = true;

            // ESP Box
            espBoxStyle = new GUIStyle(GUI.skin.box);
            espBoxStyle.normal.background = texEspBg;
            espBoxStyle.border = new RectOffset(4, 4, 4, 4);
            espBoxStyle.normal.textColor = Color.white;
            espBoxStyle.fontSize = 11;
            espBoxStyle.fontStyle = FontStyle.Bold;
            espBoxStyle.alignment = TextAnchor.MiddleCenter;
            espBoxStyle.padding = new RectOffset(4, 4, 2, 2);

            // Item Spawner Row & Icon Box
            itemRowStyle = new GUIStyle(GUI.skin.button);
            itemRowStyle.normal.background = texRowBg;
            itemRowStyle.hover.background = texRowHover;
            itemRowStyle.active.background = texRowHover;
            itemRowStyle.focused.background = texRowBg;
            itemRowStyle.border = new RectOffset(4, 4, 4, 4);
            itemRowStyle.normal.textColor = Color.white;
            itemRowStyle.hover.textColor = Color.white;
            itemRowStyle.active.textColor = Color.white;
            itemRowStyle.fontSize = 10;
            itemRowStyle.alignment = TextAnchor.MiddleLeft;
            itemRowStyle.padding = new RectOffset(4, 4, 2, 2);
            itemRowStyle.margin = new RectOffset(0, 0, 1, 1);

            itemRowSelectedStyle = new GUIStyle(itemRowStyle);
            itemRowSelectedStyle.normal.background = texRowSelected;
            itemRowSelectedStyle.hover.background = texRowSelected;
            itemRowSelectedStyle.active.background = texRowSelected;
            itemRowSelectedStyle.normal.textColor = mint;
            itemRowSelectedStyle.hover.textColor = mint;
            itemRowSelectedStyle.active.textColor = mint;

            iconBoxStyle = new GUIStyle(GUI.skin.box);
            iconBoxStyle.normal.background = texIconBoxBg;
            iconBoxStyle.border = new RectOffset(4, 4, 4, 4);
            iconBoxStyle.padding = new RectOffset(2, 2, 2, 2);

            // HUD panels
            hudPanelStyle = new GUIStyle(GUI.skin.box);
            hudPanelStyle.normal.background = texHudPanel;
            hudPanelStyle.border = new RectOffset(6, 6, 6, 6);
            hudPanelStyle.padding = new RectOffset(6, 6, 4, 4);

            hudTitleStyle = new GUIStyle(GUI.skin.label);
            hudTitleStyle.normal.textColor = new Color(0.45f, 0.85f, 1f);
            hudTitleStyle.fontSize = 10;
            hudTitleStyle.fontStyle = FontStyle.Bold;
            hudTitleStyle.alignment = TextAnchor.MiddleLeft;
            hudTitleStyle.richText = true;

            hudTextStyle = new GUIStyle(GUI.skin.label);
            hudTextStyle.normal.textColor = new Color(0.85f, 0.90f, 0.98f);
            hudTextStyle.fontSize = 10;
            hudTextStyle.alignment = TextAnchor.MiddleLeft;
            hudTextStyle.richText = true;

            // Sliders, scrollbars and the skin the whole menu is drawn with
            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            sliderStyle.normal.background = texSliderTrack;
            sliderStyle.border = new RectOffset(4, 4, 4, 4);
            sliderStyle.fixedHeight = 12f;
            sliderStyle.margin = new RectOffset(2, 2, 4, 4);

            sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            sliderThumbStyle.normal.background = texSliderThumb;
            sliderThumbStyle.hover.background = texSliderThumbHover;
            sliderThumbStyle.active.background = texSliderThumbHover;
            sliderThumbStyle.border = new RectOffset(6, 6, 6, 6);
            sliderThumbStyle.fixedWidth = 12f;
            sliderThumbStyle.fixedHeight = 12f;

            scrollTrackStyle = new GUIStyle(GUI.skin.verticalScrollbar);
            scrollTrackStyle.normal.background = texScrollTrack;
            scrollTrackStyle.fixedWidth = 6f;
            scrollTrackStyle.margin = new RectOffset(1, 1, 0, 0);

            scrollThumbStyle = new GUIStyle(GUI.skin.verticalScrollbarThumb);
            scrollThumbStyle.normal.background = texScrollThumb;
            scrollThumbStyle.hover.background = texScrollThumbHover;
            scrollThumbStyle.active.background = texScrollThumbHover;
            scrollThumbStyle.fixedWidth = 6f;
            scrollThumbStyle.margin = new RectOffset(1, 1, 0, 0);

            hiddenScrollButtonStyle = new GUIStyle();
            hiddenScrollButtonStyle.fixedWidth = 0f;
            hiddenScrollButtonStyle.fixedHeight = 0f;

            trainerSkin = UnityEngine.Object.Instantiate(GUI.skin);
            trainerSkin.horizontalSlider = sliderStyle;
            trainerSkin.horizontalSliderThumb = sliderThumbStyle;
            trainerSkin.verticalScrollbar = scrollTrackStyle;
            trainerSkin.verticalScrollbarThumb = scrollThumbStyle;
            trainerSkin.verticalScrollbarUpButton = hiddenScrollButtonStyle;
            trainerSkin.verticalScrollbarDownButton = hiddenScrollButtonStyle;

            stylesInitialized = true;
        }

        private Texture2D MakeHGradientTex(Color a, Color b)
        {
            int w = 64;
            Texture2D tex = new Texture2D(w, 1, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pix = new Color[w];
            for (int i = 0; i < w; i++) pix[i] = Color.Lerp(a, b, i / (float)(w - 1));
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private GUIStyle MakeButtonStyle(Texture2D normalTex, Texture2D hoverTex, Color textColor)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.background = normalTex;
            style.hover.background = hoverTex;
            style.active.background = hoverTex;
            style.focused.background = normalTex;
            style.normal.textColor = textColor;
            style.hover.textColor = Color.white;
            style.active.textColor = Color.white;
            style.focused.textColor = textColor;
            style.fontSize = 11;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.border = new RectOffset(5, 5, 5, 5);
            style.padding = new RectOffset(4, 4, 2, 2);
            style.margin = new RectOffset(2, 2, 2, 2);
            style.richText = true;
            return style;
        }

        // ==========================================
        // OnGUI: ESP + HUD + МЕНЮ
        // ==========================================
        private void OnGUI()
        {
            InitStyles();

            // Кадр скриншота: весь интерфейс скрыт
            if (uiHiddenForShot)
            {
                if (Event.current.type == EventType.Repaint) uiHiddenForShot = false;
                return;
            }

            if (Event.current.type == EventType.Repaint)
            {
                if (espMaster && !photoMode) DrawEspOverlay();
                if (!photoMode) DrawWorldOverlays();
                if (!photoMode) DrawHudOverlays();
            }

            if (!showMenu) return;

            GUISkin previousSkin = GUI.skin;
            GUI.skin = trainerSkin;
            try
            {
                DrawClickGUI();
            }
            finally
            {
                GUI.skin = previousSkin;
            }
        }

        // ==========================================
        // CLICKGUI С ТАБАМИ
        // ==========================================
        private void DrawClickGUI()
        {
            float menuW = 668f;
            float topBarHeight = 44f;
            float tabStripHeight = 30f;
            float bodyHeight = Mathf.Clamp(Screen.height / uiScale - 158f, 420f, 640f);

            if (menuPos.x < 0f || lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
            {
                if (menuPos.x < 0f)
                {
                    float fit = Mathf.Min(1f, (Screen.width - 24f) / menuW);
                    uiScale = Mathf.Clamp((float)Math.Floor(fit * 20f) / 20f, 0.6f, 1.4f);
                }
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                menuPos = new Vector2(Mathf.Max(10f, (Screen.width - menuW * uiScale) / 2f), 45f);
            }

            // Перетаскивание за верхнюю панель
            if (Event.current.rawType == EventType.MouseDown && Event.current.button == 0)
            {
                Rect screenDragRect = new Rect(menuPos.x, menuPos.y, menuW * uiScale, topBarHeight * uiScale);
                if (screenDragRect.Contains(Event.current.mousePosition))
                {
                    isDragging = true;
                    dragMouseOffset = Event.current.mousePosition - menuPos;
                }
            }
            else if (Event.current.rawType == EventType.MouseUp)
            {
                isDragging = false;
            }

            if (isDragging)
            {
                menuPos = Event.current.mousePosition - dragMouseOffset;
                menuPos.x = Mathf.Clamp(menuPos.x, 10f, Mathf.Max(10f, Screen.width - 200f));
                menuPos.y = Mathf.Clamp(menuPos.y, 10f, Mathf.Max(10f, Screen.height - 80f));
            }

            Matrix4x4 oldMatrix = GUI.matrix;
            if (Mathf.Abs(uiScale - 1.0f) > 0.005f)
            {
                GUIUtility.ScaleAroundPivot(new Vector2(uiScale, uiScale), menuPos);
            }

            float sx = menuPos.x;
            float sy = menuPos.y;

            Color menuBaseColor = GUI.color;
            if (menuOpacity < 0.995f) GUI.color = new Color(1f, 1f, 1f, menuOpacity);

            // Свернутый режим: только узкая шапка
            if (menuCollapsed)
            {
                Rect topBarRect = new Rect(sx, sy, menuW, topBarHeight);
                GUI.Box(topBarRect, GUIContent.none, topBarStyle);
                GUI.DrawTexture(new Rect(sx + 10f, sy + topBarHeight - 3f, menuW - 20f, 2f), texAccentLine);
                GUI.Label(new Rect(sx + 12, sy + 9, 300, 26),
                    "<color=#00E5FF>⚓</color> <b><color=#00E5FF>RAFT</color> <color=#00FFA3>TITAN</color></b> <color=#8FA3BF>v14 — свернуто</color>",
                    titleBadgeStyle);
                if (GUI.Button(new Rect(sx + menuW - 66, sy + 10, 26, 24), "▼", actionBtnStyle)) menuCollapsed = false;
                if (GUI.Button(new Rect(sx + menuW - 36, sy + 10, 26, 24), "✕", actionBtnRedStyle)) showMenu = false;
                GUI.color = menuBaseColor;
                GUI.matrix = oldMatrix;
                return;
            }

            // ВЕРХНЯЯ ПАНЕЛЬ
            Rect topBarRectFull = new Rect(sx, sy, menuW, topBarHeight);
            GUI.Box(topBarRectFull, GUIContent.none, topBarStyle);
            GUI.DrawTexture(new Rect(sx + 10f, sy + topBarHeight - 3f, menuW - 20f, 2f), texAccentLine);

            GUI.Label(new Rect(sx + 12, sy + 9, 240, 26),
                "<color=#00E5FF>⚓</color> <b><color=#00E5FF>RAFT</color> <color=#00FFA3>TITAN</color></b> <color=#8FA3BF>v14</color>",
                titleBadgeStyle);

            // Масштаб UI
            float scaleX = sx + 252;
            GUI.Label(new Rect(scaleX, sy + 12, 26, 20), "UI:", subLabelStyle);
            if (GUI.Button(new Rect(scaleX + 28, sy + 10, 22, 24), "—", actionBtnStyle))
                uiScale = Mathf.Clamp((float)Math.Round(uiScale - 0.1f, 2), 0.6f, 1.4f);
            if (GUI.Button(new Rect(scaleX + 52, sy + 10, 42, 24), string.Format("{0:0}%", uiScale * 100f),
                (Mathf.Abs(uiScale - 1.0f) < 0.01f) ? actionBtnGreenStyle : actionBtnStyle))
                uiScale = 1.0f;
            if (GUI.Button(new Rect(scaleX + 96, sy + 10, 22, 24), "+", actionBtnStyle))
                uiScale = Mathf.Clamp((float)Math.Round(uiScale + 0.1f, 2), 0.6f, 1.4f);

            // Поиск
            float searchX = sx + menuW - 342;
            GUI.Label(new Rect(searchX, sy + 12, 44, 20), "Поиск:", subLabelStyle);
            globalSearch = GUI.TextField(new Rect(searchX + 46, sy + 10, 206, 24), globalSearch, searchBoxStyle);

            if (GUI.Button(new Rect(sx + menuW - 66, sy + 10, 26, 24), "−", actionBtnStyle))
                menuCollapsed = true;

            if (GUI.Button(new Rect(sx + menuW - 36, sy + 10, 26, 24), "✕", actionBtnRedStyle))
                showMenu = false;

            // ПОЛОСА ТАБОВ
            float tabsY = sy + topBarHeight;
            GUI.Box(new Rect(sx, tabsY, menuW, tabStripHeight), GUIContent.none, topBarStyle);
            float tabW = (menuW - 12f) / tabNames.Length;
            for (int i = 0; i < tabNames.Length; i++)
            {
                Rect tr = new Rect(sx + 6f + i * tabW, tabsY + 4f, tabW - 2f, tabStripHeight - 8f);
                if (GUI.Button(tr, tabNames[i], selectedTab == i ? actionBtnBlueStyle : actionBtnStyle))
                {
                    selectedTab = i;
                    if (selectedTab == 9) profileListCache = null;
                }
            }

            // КАРТКА КОНТЕНТА
            float bodyY = tabsY + tabStripHeight + 6f;
            Rect cardRect = new Rect(sx, bodyY, menuW, bodyHeight);
            GUI.Box(cardRect, GUIContent.none, columnCardStyle);
            GUI.DrawTexture(new Rect(sx + 10f, bodyY + 1f, menuW - 20f, 2f), texAccentLine);

            Rect viewArea = new Rect(sx + 8f, bodyY + 8f, menuW - 16f, bodyHeight - 16f);
            GUILayout.BeginArea(viewArea);
            tabScrolls[selectedTab] = GUILayout.BeginScrollView(tabScrolls[selectedTab], false, false);

            if (!string.IsNullOrEmpty(globalSearch)) DrawSearchResults();
            else
            {
                switch (selectedTab)
                {
                    case 0: DrawCombatTab(); break;
                    case 1: DrawMovementTab(); break;
                    case 2: DrawVisualsTab(); break;
                    case 3: DrawPlayerTab(); break;
                    case 4: DrawRaftTab(); break;
                    case 5: DrawFarmingTab(); break;
                    case 6: DrawAutoTab(); break;
                    case 7: DrawSystemTab(); break;
                    case 8: DrawSpawnerTab(); break;
                    case 9: DrawLogTab(); break;
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // НИЖНЯЯ ПАНЕЛЬ СТАТУСА
            float bottomY = bodyY + bodyHeight + 6f;
            Rect bottomRect = new Rect(sx, bottomY, menuW, 30f);
            GUI.Box(bottomRect, GUIContent.none, statusBoxStyle);
            Color prevColor = GUI.color;
            GUI.color = new Color(0f, 1f, 0.64f, 1f);
            GUI.DrawTexture(new Rect(sx + 14f, bottomY + 11f, 8f, 8f), texKnob);
            GUI.color = prevColor;
            GUI.Label(new Rect(sx + 28f, bottomY + 6f, menuW - 40f, 20f),
                "<b>СТАТУС:</b> <color=#00FFA3>" + statusMessage + "</color>", statusTextStyle);

            GUI.color = menuBaseColor;
            GUI.matrix = oldMatrix;
        }

        private void DrawSearchResults()
        {
            EnsureToggleCaches();
            if (profileBoolFields == null) { GUILayout.Label("Инициализация...", subLabelStyle); return; }

            string q = globalSearch.Trim();
            GUILayout.Label("<b>Результаты по всем разделам:</b>", subLabelStyle);
            GUILayout.Space(2);

            int found = 0;
            for (int i = 0; i < profileBoolFields.Length; i++)
            {
                FieldInfo f = profileBoolFields[i];
                string label = GetFeatureLabel(f.Name);
                if (!ContainsIgnoreCase(label, q) && !ContainsIgnoreCase(f.Name, q)) continue;
                DrawRefToggle(f, label);
                found++;
                if (found >= 40) break;
            }
            if (found == 0) GUILayout.Label("<i>Ничего не найдено</i>", subLabelStyle);
        }

        private void DrawRefToggle(FieldInfo field, string label)
        {
            bool val = (bool)field.GetValue(this);
            GUILayout.Space(2f);
            Rect row = GUILayoutUtility.GetRect(0f, 22f, OptToggleRow);
            bool hovered = row.Contains(Event.current.mousePosition);
            GUI.Box(row, GUIContent.none, hovered ? rowHoverStyle : rowStyle);
            if (val) GUI.DrawTexture(new Rect(row.x, row.y + 4f, 2f, row.height - 8f), texSwitchOn);
            GUI.Label(new Rect(row.x + 8f, row.y, row.width - 44f, row.height), label, val ? itemLabelActiveStyle : itemLabelStyle);
            DrawSwitch(new Rect(row.xMax - 34f, row.y + 3f, 28f, 16f), val);
            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                field.SetValue(this, !val);
                LogEvent(label + (!val ? " → ВКЛ" : " → ВЫКЛ"));
            }
        }

        private bool DrawModuleToggle(string title, bool currentVal)
        {
            if (!string.IsNullOrEmpty(globalSearch) && !ContainsIgnoreCase(title, globalSearch))
            {
                return currentVal;
            }

            GUILayout.Space(2f);
            Rect row = GUILayoutUtility.GetRect(0f, 22f, OptToggleRow);
            bool hovered = row.Contains(Event.current.mousePosition);
            GUI.Box(row, GUIContent.none, hovered ? rowHoverStyle : rowStyle);
            if (currentVal) GUI.DrawTexture(new Rect(row.x, row.y + 4f, 2f, row.height - 8f), texSwitchOn);
            GUI.Label(new Rect(row.x + 8f, row.y, row.width - 44f, row.height), title, currentVal ? itemLabelActiveStyle : itemLabelStyle);
            DrawSwitch(new Rect(row.xMax - 34f, row.y + 3f, 28f, 16f), currentVal);
            bool clicked = GUI.Button(row, GUIContent.none, GUIStyle.none);

            if (clicked)
            {
                LogEvent(title + (!currentVal ? " → ВКЛ" : " → ВЫКЛ"));
                return !currentVal;
            }
            return currentVal;
        }

        private void DrawSwitch(Rect r, bool on)
        {
            GUI.DrawTexture(r, on ? texSwitchOn : texSwitchOff);
            float knobX = on ? r.xMax - r.height + 2f : r.x + 2f;
            GUI.DrawTexture(new Rect(knobX, r.y + 2f, r.height - 4f, r.height - 4f), texKnob);
        }

        // ==========================================
        // ТАБ: БОЙ
        // ==========================================
        private void DrawCombatTab()
        {
            oneHitKill = DrawModuleToggle("Ван-Хит Кил (1-Hit)", oneHitKill);
            if (!oneHitKill)
            {
                GUILayout.Label("Множитель урона: " + weaponDamageMultiplier.ToString("F1") + "x", subLabelStyle);
                weaponDamageMultiplier = GUILayout.HorizontalSlider(weaponDamageMultiplier, 1.0f, 25.0f);
            }
            killHostilesAura = DrawModuleToggle("Аура Хищников", killHostilesAura);
            if (killHostilesAura)
            {
                GUILayout.Label("Радиус ауры: " + hostileAuraRadius.ToString("F0") + "м", subLabelStyle);
                hostileAuraRadius = GUILayout.HorizontalSlider(hostileAuraRadius, 10f, 40f);
            }
            pacifyShark = DrawModuleToggle("Мирная Акула", pacifyShark);
            triggerbot = DrawModuleToggle("Триггербот (Auto)", triggerbot);
            multishotBow = DrawModuleToggle("Мультишот Лук (3x)", multishotBow);
            noArrowDrop = DrawModuleToggle("Лазерные Стрелы", noArrowDrop);
            infiniteAmmo = DrawModuleToggle("Вечные Стрелы", infiniteAmmo);
            ghostMode = DrawModuleToggle("Режим Призрака", ghostMode);
            infiniteBait = DrawModuleToggle("Вечная Приманка", infiniteBait);
            sharkRiding = DrawModuleToggle("Оседлать акулу", sharkRiding);
            if (sharkRiding)
            {
                GUILayout.Label("Скорость: " + sharkRideSpeed.ToString("F0"), subLabelStyle);
                sharkRideSpeed = GUILayout.HorizontalSlider(sharkRideSpeed, 6f, 30f);
            }
            perfectBowDraw = DrawModuleToggle("Лук: без задержки", perfectBowDraw);
            infiniteHarpoonNet = DrawModuleToggle("Гарпун: вечные сети", infiniteHarpoonNet);

            GUILayout.Space(4);
            if (GUILayout.Button("💥 Силовой Импульс (20м)", actionBtnAmberStyle, OptHeight22))
            {
                ApplyRepulsorPulse(ComponentManager<Network_Player>.Value);
            }

            GUILayout.Space(4);
            aimbotAnimals = DrawModuleToggle("Aimbot Звери/Акула", aimbotAnimals);
            aimbotItems = DrawModuleToggle("Aimbot Вещи", aimbotItems);

            if (aimbotAnimals || aimbotItems)
            {
                aimbotAlwaysOn = DrawModuleToggle("Аим Всегда", aimbotAlwaysOn);
                if (!aimbotAlwaysOn) GUILayout.Label("Кнопка: <b>X / ПКМ</b>", subLabelStyle);

                GUILayout.Label("Радиус: " + aimbotRange.ToString("F0") + "м", subLabelStyle);
                aimbotRange = GUILayout.HorizontalSlider(aimbotRange, 15f, 80f);

                GUILayout.Label("Угол (FOV): " + aimbotFOV.ToString("F0") + "°", subLabelStyle);
                aimbotFOV = GUILayout.HorizontalSlider(aimbotFOV, 30f, 180f);

                GUILayout.Label("Плавность: " + (aimSmoothness == 0 ? "Моментально" : aimSmoothness.ToString("F1")), subLabelStyle);
                aimSmoothness = GUILayout.HorizontalSlider(aimSmoothness, 0.0f, 10.0f);
            }

            GUILayout.Space(6);
            fastHook = DrawModuleToggle("Турбо-Крюк", fastHook);
            fastChop = DrawModuleToggle("Быстрый Топор", fastChop);

            GUILayout.Space(8);
            if (GUILayout.Button("☠ Уничтожить врагов", actionBtnRedStyle, OptHeight26))
            {
                KillAllEnemies();
            }
        }

        // ==========================================
        // ТАБ: ДВИЖЕНИЕ
        // ==========================================
        private void DrawMovementTab()
        {
            flyMode = DrawModuleToggle("Свободный Полёт", flyMode);
            if (flyMode)
            {
                GUILayout.Label("Скорость: " + flySpeed.ToString("F0"), subLabelStyle);
                flySpeed = GUILayout.HorizontalSlider(flySpeed, 5f, 70f);
                GUILayout.Label("<i>WASD + Space / Ctrl</i>", subLabelStyle);
            }
            noFallDamage = DrawModuleToggle("Без урона падения", noFallDamage);

            freeZiplineAir = DrawModuleToggle("Зиплайн в воздухе", freeZiplineAir);
            if (freeZiplineAir)
            {
                GUILayout.Label("Скорость зиплайна: " + ziplineAirSpeed.ToString("F0"), subLabelStyle);
                ziplineAirSpeed = GUILayout.HorizontalSlider(ziplineAirSpeed, 10f, 45f);
            }

            superDiving = DrawModuleToggle("Супер-Дайвинг", superDiving);
            if (superDiving)
            {
                GUILayout.Label("Скорость воды: " + swimSpeedMultiplier.ToString("F1") + "x", subLabelStyle);
                swimSpeedMultiplier = GUILayout.HorizontalSlider(swimSpeedMultiplier, 1.5f, 6.0f);
            }

            flipperGlider = DrawModuleToggle("Подводный скутер", flipperGlider);
            if (flipperGlider)
            {
                GUILayout.Label("Рывок: " + flipperGlideSpeed.ToString("F0"), subLabelStyle);
                flipperGlideSpeed = GUILayout.HorizontalSlider(flipperGlideSpeed, 6f, 30f);
                GUILayout.Label("<i>Под водой зажмите W</i>", subLabelStyle);
            }
            infiniteOxygen = DrawModuleToggle("Бесконечный кислород", infiniteOxygen);

            zoomEnabled = DrawModuleToggle("Орлиный Взор (Zoom)", zoomEnabled);
            if (zoomEnabled)
            {
                GUILayout.Label("Кратность зума: " + zoomMultiplier.ToString("F1") + "x", subLabelStyle);
                zoomMultiplier = GUILayout.HorizontalSlider(zoomMultiplier, 2f, 10f);
                GUILayout.Label("<i>Или зажмите клавишу Z</i>", subLabelStyle);
            }

            GUILayout.Space(4);
            superSpeed = DrawModuleToggle("Супер-Скорость", superSpeed);
            if (superSpeed)
            {
                GUILayout.Label("Скорость бега: " + speedMultiplier.ToString("F1") + "x", subLabelStyle);
                speedMultiplier = GUILayout.HorizontalSlider(speedMultiplier, 1.5f, 5.0f);
            }

            superJump = DrawModuleToggle("Супер-Прыжок", superJump);
            if (superJump)
            {
                GUILayout.Label("Сила прыжка: " + jumpMultiplier.ToString("F1") + "x", subLabelStyle);
                jumpMultiplier = GUILayout.HorizontalSlider(jumpMultiplier, 1.5f, 6.0f);
            }

            blueprintCamera = DrawModuleToggle("Дрон-камера (чертёж)", blueprintCamera);

            GUILayout.Space(4);
            rescueHook = DrawModuleToggle("🪝 Спасательный крюк (за бортом → на плот)", rescueHook);
            if (rescueHook)
            {
                GUILayout.Label("Задержка возврата: " + rescueDelay.ToString("F0") + " с", subLabelStyle);
                rescueDelay = Mathf.Round(GUILayout.HorizontalSlider(rescueDelay, 1f, 15f));
                GUILayout.Label("<i>Сработает, если дальше 6 м от плота и вы в воде</i>", subLabelStyle);
            }

            GUILayout.Space(8);
            if (GUILayout.Button("🏝 Телепорт на остров", actionBtnGreenStyle, OptHeight24))
            {
                TeleportToNearestIsland();
            }

            if (GUILayout.Button("⚓ Телепорт на плот", actionBtnBlueStyle, OptHeight24))
            {
                TeleportToRaft();
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>БЫСТРЫЕ СЛОТЫ ПОЗИЦИИ (откат ТП):</b>", subLabelStyle);
            for (int i = 0; i < 3; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Слот " + (i + 1) + (posSlotHas[i] ? " ✓" : " (пусто)"), itemLabelStyle, GUILayout.Width(96), GUILayout.Height(20));
                if (GUILayout.Button("Запомнить", actionBtnStyle, GUILayout.Height(20))) SavePosSlot(i);
                if (GUILayout.Button("Вернуться", actionBtnGreenStyle, OptHeight20)) GoPosSlot(i);
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("<i>Возврат по хоткею: F8 — слот 1 (бинды в СИСТЕМА)</i>", subLabelStyle);

            DrawWaypointSection();
        }

        // ==========================================
        // ТАБ: ВИЗУАЛ / ESP
        // ==========================================
        private void DrawVisualsTab()
        {
            espMaster = DrawModuleToggle("ESP Включен", espMaster);
            espShark = DrawModuleToggle("ESP Акула", espShark);
            espPlayers = DrawModuleToggle("ESP Игроки", espPlayers);
            espUnderwaterOres = DrawModuleToggle("ESP Руда под водой", espUnderwaterOres);
            espTreasures = DrawModuleToggle("ESP Клады (Сундуки)", espTreasures);
            espAllItems = DrawModuleToggle("ESP Все вещи", espAllItems);
            espBarrels = DrawModuleToggle("ESP Бочки / Ящики", espBarrels);

            GUILayout.Space(4);
            espTracers = DrawModuleToggle("ESP-линии (трассеры к целям)", espTracers);
            sharkTrailOn = DrawModuleToggle("След акул (лента 30 сек)", sharkTrailOn);
            espCrops = DrawModuleToggle("ESP урожая (% созревания)", espCrops);
            if (GUILayout.Button("🏝 ТП к ближайшему кладу", actionBtnAmberStyle, OptHeight22))
            {
                TeleportToNearestTreasure();
            }

            GUILayout.Space(4);
            GUILayout.Label("Дистанция лута: " + espItemMaxDist.ToString("F0") + "м", subLabelStyle);
            espItemMaxDist = GUILayout.HorizontalSlider(espItemMaxDist, 20f, 150f);

            GUILayout.Label("Дистанция руд: " + espOreMaxDist.ToString("F0") + "м", subLabelStyle);
            espOreMaxDist = GUILayout.HorizontalSlider(espOreMaxDist, 20f, 150f);

            GUILayout.Space(4);
            GUILayout.Label("<b>ФИЛЬТРЫ ЛУТА (для «ESP Все вещи»):</b>", subLabelStyle);
            lootFilterFood = DrawModuleToggle("Показывать только еду", lootFilterFood);
            lootFilterMetal = DrawModuleToggle("Показывать только ресурсы/металл", lootFilterMetal);
            lootFilterRare = DrawModuleToggle("Показывать только редкое", lootFilterRare);
            GUILayout.Label("Чёрный список ID (через запятую):", subLabelStyle);
            lootBlacklist = GUILayout.TextField(lootBlacklist, searchBoxStyle);
            GUILayout.Label("<i>Например: leaf, feather, seaweed</i>", subLabelStyle);

            GUILayout.Space(6);
            clearWater = DrawModuleToggle("Прозрачная вода", clearWater);
            nightVision = DrawModuleToggle("Ночное зрение", nightVision);
            subseaFloodlight = DrawModuleToggle("Глубокий Прожектор", subseaFloodlight);
            alwaysActiveDetector = DrawModuleToggle("Вечный Детектор", alwaysActiveDetector);
            wireframeBuilding = DrawModuleToggle("Подсветка каркаса", wireframeBuilding);
            if (wireframeBuilding) GUILayout.Label("<i>красный = слабо, зелёный = прочно</i>", subLabelStyle);
            soundBooster = DrawModuleToggle("Усилитель звука", soundBooster);
            if (soundBooster)
            {
                GUILayout.Label("Дальность: x" + soundBoostMultiplier.ToString("F1"), subLabelStyle);
                soundBoostMultiplier = GUILayout.HorizontalSlider(soundBoostMultiplier, 1.5f, 6.0f);
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>СЮЖЕТНЫЙ РАДАР:</b>", subLabelStyle);
            if (GUILayout.Button("📡 Радиовышка (#1)", actionBtnStyle, OptHeight18)) SetRadarFrequency(0);
            if (GUILayout.Button("🛳 Васагатан (#2)", actionBtnStyle, OptHeight18)) SetRadarFrequency(1);
            if (GUILayout.Button("🐻 Бальбоа (#3)", actionBtnStyle, OptHeight18)) SetRadarFrequency(2);
            if (GUILayout.Button("🏜 Караван (#4)", actionBtnStyle, OptHeight18)) SetRadarFrequency(3);
            if (GUILayout.Button("🏙 Тангароа (#5)", actionBtnStyle, OptHeight18)) SetRadarFrequency(4);
            if (GUILayout.Button("🏗 Варуна (#6)", actionBtnStyle, OptHeight18)) SetRadarFrequency(5);
            if (GUILayout.Button("❄ Темперанс (#7)", actionBtnStyle, OptHeight18)) SetRadarFrequency(6);
            if (GUILayout.Button("🏰 Утопия (#8)", actionBtnStyle, OptHeight18)) SetRadarFrequency(7);

            GUILayout.Space(6);
            GUILayout.Label("<b>V14 КАРТА И ЗРЕНИЕ:</b>", subLabelStyle);
            worldMapOn = DrawModuleToggle("Глобальная карта мира", worldMapOn);
            if (worldMapOn)
            {
                GUILayout.Label("Зум карты: " + worldMapZoom.ToString("F2") + " (1 px = " + (1f / Mathf.Max(0.05f, worldMapZoom)).ToString("F1") + " м)", subLabelStyle);
                worldMapZoom = GUILayout.HorizontalSlider(worldMapZoom, 0.05f, 1.5f);
                GUILayout.Label("<i>С = север; кольца 250/500/1000 м; блипы у края = далёкие</i>", subLabelStyle);
            }
            thermalVision = DrawModuleToggle("Тепловизор (хищники сквозь стены)", thermalVision);
            autoRadarScan = DrawModuleToggle("Авто-сканер радара (перебор кодов)", autoRadarScan);
            if (autoRadarScan) GUILayout.Label("<i>Смена частоты каждые 5 сек, находки в журнале</i>", subLabelStyle);
        }

        // ==========================================
        // ТАБ: ИГРОК / КРАФТ
        // ==========================================
        private void DrawPlayerTab()
        {
            godMode = DrawModuleToggle("Бессмертие (God)", godMode);
            infiniteNeeds = DrawModuleToggle("Без голода и жажды", infiniteNeeds);
            infiniteDurability = DrawModuleToggle("Вечная прочность", infiniteDurability);
            freeCraft = DrawModuleToggle("Бесплатный Крафт", freeCraft);
            freeBuild = DrawModuleToggle("Бесплатная Стройка", freeBuild);
            noBuildRestrictions = DrawModuleToggle("Стройка без опор", noBuildRestrictions);

            GUILayout.Space(6);
            if (GUILayout.Button("📦 Умная выгрузка (Stash)", actionBtnGreenStyle, OptHeight24))
            {
                SmartChestAutoSort(ComponentManager<Network_Player>.Value);
            }

            if (GUILayout.Button("🔓 Взлом дверей и кодов", actionBtnAmberStyle, OptHeight24))
            {
                PuzzleAutoSolver();
            }

            if (GUILayout.Button("🧰 Открыть ближ. сундук", actionBtnBlueStyle, OptHeight22))
            {
                OpenNearestChest(ComponentManager<Network_Player>.Value);
            }

            GUILayout.Space(4);
            GUILayout.Label("<b>ДУБЛИКАТОР СУНДУКА:</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("x2", actionBtnBlueStyle, OptHeight22)) DuplicateChestItems(2);
            if (GUILayout.Button("x10", actionBtnPurpleStyle, OptHeight22)) DuplicateChestItems(10);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("🔑 Собрать сюжетки в сумку", actionBtnStyle, OptHeight22))
            {
                CollectAllQuestItems(ComponentManager<Network_Player>.Value);
            }

            if (GUILayout.Button("📖 Открыть все заметки (T)", actionBtnGreenStyle, OptHeight22))
            {
                UnlockAllJournalNotes();
            }

            if (GUILayout.Button("👥 Открыть персонажей", actionBtnBlueStyle, OptHeight22))
            {
                UnlockAllGameCharacters();
            }

            if (GUILayout.Button("📚 Изучить все чертежи", actionBtnGreenStyle, OptHeight22))
            {
                UnlockAllResearchRecipes();
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>СНАРЯЖЕНИЕ (снимок инвентаря):</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💾 Запомнить инвентарь", actionBtnBlueStyle, OptHeight22)) SaveInventorySnapshot();
            if (GUILayout.Button("↩ Восстановить", actionBtnGreenStyle, OptHeight22)) RestoreInventorySnapshot();
            GUILayout.EndHorizontal();
            GUILayout.Label(hasInventorySnapshot ? "<i>Снимок готов — восстановит вещи после смерти/потери</i>" : "<i>Снимка нет — запомните инвентарь заранее</i>", subLabelStyle);

            // СУМКА ДРУГА (V14 МП)
            GUILayout.Space(8);
            GUILayout.Label("<b>👥 СУМКА ДРУГА (только просмотр):</b>", subLabelStyle);
            UpdateBagViewer();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", actionBtnStyle, GUILayout.Width(40), GUILayout.Height(20))) { bagViewerIndex--; nextBagTime = 0f; UpdateBagViewer(); }
            GUILayout.Label(bagHeader, itemLabelStyle, GUILayout.Height(20));
            if (GUILayout.Button("▶", actionBtnStyle, GUILayout.Width(40), GUILayout.Height(20))) { bagViewerIndex++; nextBagTime = 0f; UpdateBagViewer(); }
            GUILayout.EndHorizontal();
            bagScroll = GUILayout.BeginScrollView(bagScroll, GUILayout.Height(120));
            for (int i = 0; i < bagLines.Count; i++) GUILayout.Label("• " + bagLines[i], subLabelStyle);
            if (bagLines.Count == 0) GUILayout.Label("<i>Игроков нет либо данные ещё читаются</i>", subLabelStyle);
            GUILayout.EndScrollView();
            GUILayout.Label("<i>В оригинальной игре сумки закрыты — содержимое видно, только если сервер его синхронизирует</i>", subLabelStyle);

            GUILayout.Space(4);
            if (GUILayout.Button("🎒 Отсортировать инвентарь (O)", actionBtnBlueStyle, OptHeight22))
            {
                SortInventoryNow();
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>ПОМОЩНИК КРАФТА (изученные рецепты):</b>", subLabelStyle);
            craftHelperOn = DrawModuleToggle("Показывать списки", craftHelperOn);
            if (craftHelperOn)
            {
                if (GUILayout.Button("⟲ Пересчитать сейчас", actionBtnStyle, OptHeight20))
                {
                    nextCraftScanTime = 0f;
                    ScanCraftHelper();
                }
                GUILayout.Label("<b>✔ Можно скрафтить (" + craftableList.Count + "):</b>", subLabelStyle);
                for (int i = 0; i < craftableList.Count && i < 12; i++) GUILayout.Label("✔ " + craftableList[i], itemLabelStyle);
                if (craftableList.Count > 12) GUILayout.Label("…и ещё " + (craftableList.Count - 12), subLabelStyle);
                GUILayout.Space(2);
                GUILayout.Label("<b>✘ Не хватает ресурсов:</b>", subLabelStyle);
                for (int i = 0; i < missingList.Count && i < 10; i++) GUILayout.Label("✘ " + missingList[i], subLabelStyle);
                if (craftableList.Count == 0 && missingList.Count == 0)
                    GUILayout.Label("<i>Пока пусто — рецепты обновляются раз в 3 сек</i>", subLabelStyle);
            }
        }

        // ==========================================
        // ТАБ: ПЛОТ
        // ==========================================
        private void DrawRaftTab()
        {
            instantDemolish = DrawModuleToggle("Мгн. снос топора", instantDemolish);
            indestructibleRaft = DrawModuleToggle("Неразрушимый плот", indestructibleRaft);
            infinitePipeWater = DrawModuleToggle("Вода в трубах 100%", infinitePipeWater);
            freezeRaft = DrawModuleToggle("Заморозка плота", freezeRaft);
            alwaysTailwind = DrawModuleToggle("Всегда по ветру", alwaysTailwind);
            if (alwaysTailwind)
            {
                GUILayout.Label("Тяга парусов: " + windSpeedMultiplier.ToString("F1") + "x", subLabelStyle);
                windSpeedMultiplier = GUILayout.HorizontalSlider(windSpeedMultiplier, 1.0f, 5.0f);
            }

            turboRaft = DrawModuleToggle("Турбо-Плот (Мотор)", turboRaft);
            if (turboRaft)
            {
                GUILayout.Label("Множитель мотора: " + turboRaftMultiplier.ToString("F1") + "x", subLabelStyle);
                turboRaftMultiplier = GUILayout.HorizontalSlider(turboRaftMultiplier, 1.5f, 6.0f);
            }

            infinitePaint = DrawModuleToggle("Бесконечная краска", infinitePaint);
            raftStabilizer = DrawModuleToggle("Антикрен плота", raftStabilizer);
            if (raftStabilizer)
            {
                GUILayout.Label("Сила: " + stabilizerStrength.ToString("F1") + "x", subLabelStyle);
                stabilizerStrength = GUILayout.HorizontalSlider(stabilizerStrength, 0.5f, 3f);
            }
            calmSea = DrawModuleToggle("Штиль (без волн)", calmSea);
            instantDesalinator = DrawModuleToggle("Мгн. опреснитель", instantDesalinator);

            GUILayout.Space(4);
            antiStormRaft = DrawModuleToggle("Анти-шторм (сам отплывает от рифов)", antiStormRaft);
            autoRefuelEngines = DrawModuleToggle("Авто-дозаправка из сундуков", autoRefuelEngines);
            if (autoRefuelEngines) GUILayout.Label("<i>Двигатели/плавильни/грили берут доски из сундуков рядом</i>", subLabelStyle);

            GUILayout.Space(6);
            if (GUILayout.Button("🔨 Починить плот (100%)", actionBtnGreenStyle, OptHeight22))
            {
                RepairAllRaftBlocks();
            }

            if (GUILayout.Button("🛡 Укрепить фундамент металлом", actionBtnAmberStyle, OptHeight22))
            {
                ReinforceAllRaftFoundations();
            }

            if (GUILayout.Button("🏗 Расширить плот (+3x3)", actionBtnBlueStyle, OptHeight22))
            {
                BatchExpandRaftFoundations(3);
            }

            if (GUILayout.Button("🏗 Расширить плот (+5x5)", actionBtnBlueStyle, OptHeight22))
            {
                BatchExpandRaftFoundations(5);
            }

            GUILayout.Space(2);
            GUILayout.Label("<b> КАЛЬКУЛЯТОР СТРОЙКИ:</b>", subLabelStyle);
            GUILayout.Label("+3 ряда: " + CalcExpandCost(3), subLabelStyle);
            GUILayout.Label("+5 рядов: " + CalcExpandCost(5), subLabelStyle);

            if (GUILayout.Button("⚓ Сбросить/Поднять якорь", actionBtnStyle, OptHeight22))
            {
                ToggleStationaryAnchor();
            }

            if (GUILayout.Button("🎨 Покрасить весь плот", actionBtnGreenStyle, OptHeight22))
            {
                PaintAllRaftBlocks();
            }

            // АВТОПИЛОТ
            GUILayout.Space(8);
            GUILayout.Label("<b>АВТОПИЛОТ ПЛОТА (F9):</b>", subLabelStyle);
            autopilotEnabled = DrawModuleToggle("Автопилот включен", autopilotEnabled);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🏝 Курс на остров", actionBtnGreenStyle, OptHeight20)) SetAutopilotToIsland();
            if (GUILayout.Button("📍 Курс на точку", actionBtnBlueStyle, OptHeight20)) SetAutopilotToWaypoint();
            GUILayout.EndHorizontal();
            GUILayout.Label("Тяга: " + autopilotSpeed.ToString("F1"), subLabelStyle);
            autopilotSpeed = GUILayout.HorizontalSlider(autopilotSpeed, 2f, 14f);
            GUILayout.Label("<i>По прибытии якорь бросится автоматически</i>", subLabelStyle);

            // ЛИНЕЙКА
            GUILayout.Space(8);
            GUILayout.Label("<b>СТРОИТЕЛЬНАЯ ЛИНЕЙКА (ЛКМ в мир):</b>", subLabelStyle);
            rulerOn = DrawModuleToggle("Линейка включена", rulerOn);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Сброс A/B", actionBtnStyle, OptHeight20)) { hasRulerA = false; hasRulerB = false; }
            if (hasRulerA && hasRulerB)
                GUILayout.Label("D = " + Vector3.Distance(rulerA, rulerB).ToString("F2") + " м", itemLabelActiveStyle);
            GUILayout.EndHorizontal();

            // ВЫДЕЛЕНИЕ БЛОКОВ
            GUILayout.Space(8);
            GUILayout.Label("<b>ВЫДЕЛЕНИЕ БЛОКОВ (ЛКМ):</b>", subLabelStyle);
            selectMode = DrawModuleToggle("Режим выделения", selectMode);
            GUILayout.Label("Выделено: " + selectedBlocks.Count + " шт.", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Очистить", actionBtnStyle, OptHeight20)) selectedBlocks.Clear();
            if (GUILayout.Button("Починить", actionBtnGreenStyle, OptHeight20)) RepairSelectedBlocks();
            if (GUILayout.Button("Краска", actionBtnPurpleStyle, OptHeight20)) PaintSelectedBlocks();
            if (GUILayout.Button("Снести", actionBtnRedStyle, OptHeight20)) DeleteSelectedBlocks();
            GUILayout.EndHorizontal();

            // ПЛАН ПЛОТА
            GUILayout.Space(8);
            GUILayout.Label("<b>ПЛАН ПЛОТА (копия фундаментов):</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("📋 Копировать план", actionBtnBlueStyle, OptHeight20)) CopyRaftBlueprint();
            if (GUILayout.Button("📌 Вставить сдвиг", actionBtnGreenStyle, OptHeight20)) PasteRaftBlueprint();
            GUILayout.EndHorizontal();
            GUILayout.Label("Сдвиг вставки: " + blueprintPasteOffset.ToString("F0") + " м", subLabelStyle);
            blueprintPasteOffset = GUILayout.HorizontalSlider(blueprintPasteOffset, 3f, 30f);

            // КОПИР ЭТАЖА (V14)
            GUILayout.Space(8);
            GUILayout.Label("<b>КОПИР ЭТАЖА (выделенные блоки → на этаж выше):</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("📋 Копировать этаж", actionBtnBlueStyle, OptHeight20)) CopyFloor();
            if (GUILayout.Button("📌 Вставить этажом выше", actionBtnGreenStyle, OptHeight20)) PasteFloor();
            GUILayout.EndHorizontal();
            GUILayout.Label("Смещение вверх: " + floorPasteOffset.ToString("F1") + " м", subLabelStyle);
            floorPasteOffset = GUILayout.HorizontalSlider(floorPasteOffset, 1.5f, 6f);
            GUILayout.Label("<i>Включите «Выделение блоков», обведите этаж, копируйте и вставьте копию выше</i>", subLabelStyle);

            DrawPaintPalette();

            GUILayout.Space(4);
            GUILayout.Label("<b>МУСОРНЫЙ ДОЖДЬ: " + junkRainAmount + " шт.</b>", subLabelStyle);
            junkRainAmount = Mathf.RoundToInt(GUILayout.HorizontalSlider(junkRainAmount, 5f, 60f));
            if (GUILayout.Button("🌧 Вызвать дождь", actionBtnAmberStyle, OptHeight22))
            {
                SpawnJunkRain(junkRainAmount);
            }

            GUILayout.Space(4);
            GUILayout.Label("<b>Направление дрейфа:</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⬆ Сев", actionBtnStyle, OptHeight18)) SetRaftDriftDirection(Vector3.forward, "Север");
            if (GUILayout.Button("⬇ Юг", actionBtnStyle, OptHeight18)) SetRaftDriftDirection(Vector3.back, "Юг");
            if (GUILayout.Button("⬅ Зап", actionBtnStyle, OptHeight18)) SetRaftDriftDirection(Vector3.left, "Запад");
            if (GUILayout.Button("➡ Вост", actionBtnStyle, OptHeight18)) SetRaftDriftDirection(Vector3.right, "Восток");
            GUILayout.EndHorizontal();
        }

        // ==========================================
        // ТАБ: ФЕРМА / МИР
        // ==========================================
        private void DrawFarmingTab()
        {
            autoCropHarvest = DrawModuleToggle("Авто-сбор урожая", autoCropHarvest);
            perpetualGrass = DrawModuleToggle("Вечная трава скота", perpetualGrass);
            autoWaterCrops = DrawModuleToggle("Авто-полив грядок", autoWaterCrops);
            instantBiofuel = DrawModuleToggle("Мгн. Биотопливо", instantBiofuel);
            hyperBeehives = DrawModuleToggle("Гипер-Ульи (Мёд x5)", hyperBeehives);
            instantSmeltCook = DrawModuleToggle("Мгн. Плавка / Еда", instantSmeltCook);
            instantFishing = DrawModuleToggle("Мгновенный Клёв", instantFishing);
            autoFishing = DrawModuleToggle("Авто-Рыбалка (Бот)", autoFishing);
            instantDomesticProduction = DrawModuleToggle("Ресурсы зверей x10", instantDomesticProduction);
            autoShearer = DrawModuleToggle("Авто-стрижка/дойка", autoShearer);
            infiniteBattery = DrawModuleToggle("Вечные Батарейки", infiniteBattery);
            infiniteFuel = DrawModuleToggle("Бесконечное Топливо", infiniteFuel);
            noFuelLanterns = DrawModuleToggle("Вечные фонари и костры", noFuelLanterns);
            instantHarvest = DrawModuleToggle("Моментальная добыча", instantHarvest);
            antiSeagull = DrawModuleToggle("Защита от чаек", antiSeagull);
            autoLootPickups = DrawModuleToggle("Авто-сбор мусора", autoLootPickups);
            if (autoLootPickups)
            {
                GUILayout.Label("Радиус сбора: " + autoLootRadius.ToString("F0") + "м", subLabelStyle);
                autoLootRadius = GUILayout.HorizontalSlider(autoLootRadius, 10f, 60f);
            }
            itemMagnet = DrawModuleToggle("Магнит Лута", itemMagnet);
            if (itemMagnet)
            {
                GUILayout.Label("Радиус магнита: " + magnetRadius.ToString("F0") + "м", subLabelStyle);
                magnetRadius = GUILayout.HorizontalSlider(magnetRadius, 10f, 80f);
                GUILayout.Label("<b>Фильтры магнита (если ничего — всё подряд):</b>", subLabelStyle);
                magnetOnlyFood = DrawModuleToggle("Только еда", magnetOnlyFood);
                magnetOnlyMetal = DrawModuleToggle("Только ресурсы/металл", magnetOnlyMetal);
                magnetOnlyRare = DrawModuleToggle("Только редкое", magnetOnlyRare);
            }

            GUILayout.Space(6);
            if (GUILayout.Button("🏆 Откопать все клады", actionBtnAmberStyle, OptHeight22))
            {
                UnearthAllTreasures();
            }

            if (GUILayout.Button("🐑 Собрать всё со скота", actionBtnGreenStyle, OptHeight22))
            {
                CollectAllDomesticYield(ComponentManager<Network_Player>.Value);
            }

            if (GUILayout.Button("💧 Полить все грядки", actionBtnBlueStyle, OptHeight22))
            {
                WaterAllCrops();
            }

            if (GUILayout.Button("🌱 Моментальный рост растений", actionBtnGreenStyle, OptHeight22))
            {
                InstantGrowCrops();
            }

            if (GUILayout.Button("🪓 Срубить все деревья", actionBtnAmberStyle, OptHeight22))
            {
                AutoChopAndHarvestAll(ComponentManager<Network_Player>.Value);
            }

            if (GUILayout.Button("📦 Очистить сетки в сумку", actionBtnBlueStyle, OptHeight22))
            {
                EmptyAllCollectionNets();
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>V14 АВТОМАТИЗАЦИЯ ФЕРМЫ:</b>", subLabelStyle);
            autoCollectNests = DrawModuleToggle("🥚 Авто-сбор гнёзд (яйца/перо)", autoCollectNests);
            autoPlantSeeds = DrawModuleToggle("🌱 Авто-посадка семян в грядки", autoPlantSeeds);
            if (autoPlantSeeds)
            {
                GUILayout.Label("Ключи семян (ID через запятую):", subLabelStyle);
                plantKeywords = GUILayout.TextField(plantKeywords, searchBoxStyle);
                GUILayout.Label("<i>Например: seed — подходит любой семенной предмет</i>", subLabelStyle);
            }
            petsAlwaysFed = DrawModuleToggle("🐾 Питомцы всегда сыты", petsAlwaysFed);
            ratExterminator = DrawModuleToggle("🐀 Крысолов (убивать крыс)", ratExterminator);
            if (ratExterminator) GUILayout.Label("Уничтожено крыс: " + statRats, subLabelStyle);

            GUILayout.Space(4);
            GUILayout.Label("<b>БИОРЕАКТОР:</b>", subLabelStyle);
            GUILayout.Label(CalcBiofuel(), subLabelStyle);
            GUILayout.Label("Сырья на 1 ед. топлива: " + biofuelRawPerUnit.ToString("F2"), subLabelStyle);
            biofuelRawPerUnit = GUILayout.HorizontalSlider(biofuelRawPerUnit, 0.01f, 1f);

            GUILayout.Space(6);
            GUILayout.Label("<b>Время суток:</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("☀ День", actionBtnStyle, OptHeight18)) SetGameTime(12);
            if (GUILayout.Button("🌙 Ночь", actionBtnStyle, OptHeight18)) SetGameTime(0);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("<b>Погода:</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ясно", actionBtnStyle, OptHeight18)) SetWeather("Calm");
            if (GUILayout.Button("Дождь", actionBtnStyle, OptHeight18)) SetWeather("Rain");
            if (GUILayout.Button("Шторм", actionBtnStyle, OptHeight18)) SetWeather("Storm");
            GUILayout.EndHorizontal();

            stormMode = DrawModuleToggle("Режим Шторма (бесконечный)", stormMode);

            GUILayout.Space(4);
            GUILayout.Label("<b>Скорость мира:</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1.0x", actionBtnGreenStyle, OptHeight18)) Time.timeScale = 1.0f;
            if (GUILayout.Button("2.0x", actionBtnStyle, OptHeight18)) Time.timeScale = 2.0f;
            if (GUILayout.Button("5.0x", actionBtnBlueStyle, OptHeight18)) Time.timeScale = 5.0f;
            GUILayout.EndHorizontal();

            fastNight = DrawModuleToggle("Быстрая ночь (ускорять только ночь)", fastNight);
            if (fastNight)
            {
                GUILayout.Label("Ускорение ночью: x" + fastNightScale.ToString("F0"), subLabelStyle);
                fastNightScale = Mathf.Round(GUILayout.HorizontalSlider(fastNightScale, 2f, 15f));
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>ГЕНЕРАТОР ОСТРОВОВ:</b>", subLabelStyle);
            if (GUILayout.Button("🌴 Тропический Большой", actionBtnStyle, OptHeight18)) SpawnIslandBiome(ChunkPointType.Landmark_Big, "Тропики");
            if (GUILayout.Button("✈ Самолет (Пилот)", actionBtnStyle, OptHeight18)) SpawnIslandBiome(ChunkPointType.Landmark_Pilot, "Самолет");
            if (GUILayout.Button("🚢 Кораблекрушение", actionBtnStyle, OptHeight18)) SpawnIslandBiome(ChunkPointType.Landmark_Boat, "Корабль");
            if (GUILayout.Button("🐻 Бальбоа (Сосны)", actionBtnStyle, OptHeight18)) SpawnIslandBiome(ChunkPointType.Landmark_Balboa, "Бальбоа");
            if (GUILayout.Button("🏜 Караван-Таун", actionBtnStyle, OptHeight18)) SpawnIslandBiome(ChunkPointType.Landmark_CaravanIsland, "Караван");
            if (GUILayout.Button("🏙 Тангароа (Город)", actionBtnStyle, OptHeight18)) SpawnIslandBiome(ChunkPointType.Landmark_Tangaroa, "Тангароа");
            if (GUILayout.Button("❄ Темперанс (Снег)", actionBtnStyle, OptHeight18)) SpawnIslandBiome(ChunkPointType.Landmark_Temperance, "Темперанс");
        }

        // ==========================================
        // ТАБ: АВТО / ЛОГИСТИКА
        // ==========================================
        private void DrawAutoTab()
        {
            autoAnchorOnArrival = DrawModuleToggle("Якорь у острова", autoAnchorOnArrival);
            if (autoAnchorOnArrival)
            {
                GUILayout.Label("Срабатывание: " + autoAnchorDistance.ToString("F0") + "м", subLabelStyle);
                autoAnchorDistance = GUILayout.HorizontalSlider(autoAnchorDistance, 10f, 60f);
            }

            autoCooker = DrawModuleToggle("Авто-гриль (шеф)", autoCooker);
            autoSmelter = DrawModuleToggle("Авто-плавильня", autoSmelter);
            if (autoCooker || autoSmelter)
            {
                GUILayout.Label("Радиус сундуков: " + autoLogisticsRadius.ToString("F0") + "м", subLabelStyle);
                autoLogisticsRadius = GUILayout.HorizontalSlider(autoLogisticsRadius, 10f, 60f);
                GUILayout.Label("<i>Сырое мясо, рыба и руда берутся из сундуков рядом</i>", subLabelStyle);
            }

            autoZipline = DrawModuleToggle("Зиплайн + сбор", autoZipline);
            if (autoZipline) GUILayout.Label("<i>Встаньте у начала троса</i>", subLabelStyle);

            autoBatteryCharge = DrawModuleToggle("Авто-заряд батареек", autoBatteryCharge);

            autoTrash = DrawModuleToggle("Авто-выброс мусора", autoTrash);
            if (autoTrash)
            {
                GUILayout.Label("ID-фильтры через запятую:", subLabelStyle);
                trashKeywords = GUILayout.TextField(trashKeywords, searchBoxStyle);
                GUILayout.Label("<i>Например: leaf, feather, seaweed</i>", subLabelStyle);
            }

            autoFishPro = DrawModuleToggle("🎣 Рыбалка 2.0 (наживка + подсечка)", autoFishPro);
            if (autoFishPro)
            {
                GUILayout.Label("Ключ наживки (ID):", subLabelStyle);
                baitKeyword = GUILayout.TextField(baitKeyword, searchBoxStyle);
                GUILayout.Label("<i>Например: bait — наденется сама и будет подсекать улов</i>", subLabelStyle);
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>МАКРО-РУТИНА (клавиша F7):</b>", subLabelStyle);
            if (GUILayout.Button("▶ Запустить макро", actionBtnAmberStyle, OptHeight22))
            {
                StartMacro();
            }
            GUILayout.Label(macroRunning
                ? "<i>Выполняется шаг " + macroStep + "/7 ...</i>"
                : "<i>ТП на плот → ремонт → сортировка → сетки → звери → полив → рост</i>", subLabelStyle);
        }

        // ==========================================
        // ТАБ: СИСТЕМА V14
        // ==========================================
        private void DrawSystemTab()
        {
            if (profileListCache == null) profileListCache = ListProfileFiles();

            GUILayout.Label("<b>ПРОФИЛИ НАСТРОЕК:</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Имя:", subLabelStyle, GUILayout.Width(38));
            profileName = GUILayout.TextField(profileName, searchBoxStyle, GUILayout.Height(22));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💾 Сохранить профиль", actionBtnGreenStyle, OptHeight22)) SaveProfileFile(profileName);
            if (GUILayout.Button("📂 Загрузить", actionBtnBlueStyle, OptHeight22)) LoadProfileFile(profileName);
            if (GUILayout.Button("⟲ Обновить", actionBtnStyle, OptHeight22)) profileListCache = ListProfileFiles();
            GUILayout.EndHorizontal();

            if (profileListCache != null && profileListCache.Length > 0)
            {
                for (int i = 0; i < profileListCache.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("📄 " + profileListCache[i], itemLabelStyle, GUILayout.Width(150), GUILayout.Height(20));
                    if (GUILayout.Button("Загрузить", actionBtnBlueStyle, GUILayout.Height(20))) LoadProfileFile(profileListCache[i]);
                    GUILayout.EndHorizontal();
                }
            }
            else GUILayout.Label("<i>Профилей пока нет — сохраните текущие настройки</i>", subLabelStyle);

            GUILayout.Space(4);
            autoloadProfile = DrawModuleToggle("Автозагрузка профиля при старте игры", autoloadProfile);
            if (autoloadProfile)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Имя:", subLabelStyle, GUILayout.Width(38));
                autoloadProfileName = GUILayout.TextField(autoloadProfileName, searchBoxStyle, GUILayout.Height(22));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>ГОРЯЧИЕ КЛАВИШИ:</b>", subLabelStyle);
            keybindsEnabled = DrawModuleToggle("Клавиши включены", keybindsEnabled);
            EnsureKeybinds();
            for (int i = 0; i < keybindActionNames.Length; i++)
            {
                string action = keybindActionNames[i];
                KeyCode code;
                if (!keybinds.TryGetValue(action, out code)) code = KeyCode.None;
                string btnText = (bindCaptureAction == action) ? "Нажмите..." : (code == KeyCode.None ? "—" : code.ToString());
                GUILayout.BeginHorizontal();
                GUILayout.Label(keybindActionTitles[i], itemLabelStyle, GUILayout.Width(150), GUILayout.Height(20));
                if (GUILayout.Button(btnText, bindCaptureAction == action ? actionBtnAmberStyle : actionBtnStyle, GUILayout.Height(20)))
                    bindCaptureAction = (bindCaptureAction == action) ? null : action;
                if (GUILayout.Button("✕", actionBtnRedStyle, GUILayout.Width(24), GUILayout.Height(20))) keybinds[action] = KeyCode.None;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>ЭКРАННЫЕ ИНДИКАТОРЫ (HUD):</b>", subLabelStyle);
            hudActiveCheats = DrawModuleToggle("HUD активных читов", hudActiveCheats);
            hudSessionStats = DrawModuleToggle("HUD статистики сессии", hudSessionStats);
            hudCompass = DrawModuleToggle("HUD компаса", hudCompass);
            hudWeatherTimer = DrawModuleToggle("HUD погоды / таймера", hudWeatherTimer);
            hudMinimap = DrawModuleToggle("Миникарта", hudMinimap);
            eventLogEnabled = DrawModuleToggle("Журнал событий на экране", eventLogEnabled);
            perfHud = DrawModuleToggle("Тахометр (FPS / сущности / блоки)", perfHud);
            hudMachines = DrawModuleToggle("Дашборд машин плота", hudMachines);
            smartReminders = DrawModuleToggle("Умные напоминания (урожай/топливо/батарейки/погода)", smartReminders);
            hudPlayersTable = DrawModuleToggle("HUD таблица игроков (мультиплеер)", hudPlayersTable);
            hudTeammateStatus = DrawModuleToggle("HUD статусы тиммейтов (HP/еда/вода, локация)", hudTeammateStatus);
            if (hudTeammateStatus) GUILayout.Label("<i>Строки статусов и полоски HP внутри таблицы игроков; «—» = статы не синхронизируются</i>", subLabelStyle);
            GUILayout.Label("Зум миникарты: " + minimapZoom.ToString("F1"), subLabelStyle);
            minimapZoom = GUILayout.HorizontalSlider(minimapZoom, 0.4f, 2.5f);

            GUILayout.Space(6);
            GUILayout.Label("<b>ИНТЕРФЕЙС МЕНЮ:</b>", subLabelStyle);
            GUILayout.Label("Прозрачность меню: " + Mathf.RoundToInt(menuOpacity * 100f) + "%", subLabelStyle);
            menuOpacity = GUILayout.HorizontalSlider(menuOpacity, 0.35f, 1f);
            GUILayout.Label("<i>Кнопка «−» в шапке сворачивает меню в полоску</i>", subLabelStyle);

            GUILayout.Space(6);
            GUILayout.Label("<b>ФОТО-РЕЖИМ:</b>", subLabelStyle);
            photoMode = DrawModuleToggle("Фото-режим (скрыть ESP и HUD)", photoMode);
            if (GUILayout.Button("📸 Скриншот без интерфейса", actionBtnPurpleStyle, OptHeight22))
            {
                TakeScreenshot();
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>СИГНАЛИЗАЦИЯ АКУЛ:</b>", subLabelStyle);
            sharkAlarm = DrawModuleToggle("Тревога при приближении", sharkAlarm);
            GUILayout.Label("Радиус: " + sharkAlarmRadius.ToString("F0") + " м", subLabelStyle);
            sharkAlarmRadius = GUILayout.HorizontalSlider(sharkAlarmRadius, 15f, 80f);

            GUILayout.Space(6);
            GUILayout.Label("<b>ЭКСПОРТ / ИМПОРТ ТОЧЕК:</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⬇ Экспорт точек в файл", actionBtnBlueStyle, OptHeight22)) ExportWaypoints();
            if (GUILayout.Button("⬆ Импорт точек", actionBtnGreenStyle, OptHeight22)) ImportWaypoints();
            GUILayout.EndHorizontal();
        }

        // ==========================================
        // ТАБ: СПАВНЕР
        // ==========================================
        private void DrawSpawnerTab()
        {
            EnsureItemCatalogLoaded();

            GUILayout.BeginHorizontal();
            for (int i = 0; i < categoryNames.Length; i++)
            {
                bool isSel = (selectedCategory == i);
                if (GUILayout.Button(categoryNames[i], isSel ? actionBtnBlueStyle : actionBtnStyle, OptHeight20))
                {
                    selectedCategory = i;
                    UpdateFilteredCatalog();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Фильтр:", subLabelStyle, GUILayout.Width(45));
            string newSearch = GUILayout.TextField(spawnerSearch, searchBoxStyle);
            if (newSearch != spawnerSearch)
            {
                spawnerSearch = newSearch;
                UpdateFilteredCatalog();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            spawnerListScroll = GUILayout.BeginScrollView(spawnerListScroll, GUILayout.Height(230));
            if (filteredCatalogItems != null)
            {
                for (int i = 0; i < filteredCatalogItems.Count; i++)
                {
                    var item = filteredCatalogItems[i];
                    if (item == null) continue;

                    bool isSelected = (selectedItem == item);
                    string title = GetItemTitle(item);

                    Rect rowRect = GUILayoutUtility.GetRect(240f, 28f);

                    if (GUI.Button(rowRect, GUIContent.none, isSelected ? itemRowSelectedStyle : itemRowStyle))
                    {
                        selectedItem = item;
                    }

                    Rect iconBoxRect = new Rect(rowRect.x + 3f, rowRect.y + 2f, 24f, 24f);
                    GUI.Box(iconBoxRect, "", iconBoxStyle);

                    if (item.settings_Inventory != null && item.settings_Inventory.Sprite != null)
                    {
                        DrawSprite(new Rect(iconBoxRect.x + 2f, iconBoxRect.y + 2f, 20f, 20f), item.settings_Inventory.Sprite);
                    }

                    Rect labelRect = new Rect(rowRect.x + 32f, rowRect.y + 4f, rowRect.width - 36f, 20f);
                    GUI.Label(labelRect, title, isSelected ? itemLabelActiveStyle : itemLabelStyle);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4);

            if (selectedItem != null)
            {
                Rect previewRect = GUILayoutUtility.GetRect(240f, 52f);
                GUI.Box(previewRect, "", columnCardStyle);

                Rect bigIconBox = new Rect(previewRect.x + 4f, previewRect.y + 4f, 44f, 44f);
                GUI.Box(bigIconBox, "", iconBoxStyle);

                if (selectedItem.settings_Inventory != null && selectedItem.settings_Inventory.Sprite != null)
                {
                    DrawSprite(new Rect(bigIconBox.x + 3f, bigIconBox.y + 3f, 38f, 38f), selectedItem.settings_Inventory.Sprite);
                }

                float textX = previewRect.x + 54f;
                GUI.Label(new Rect(textX, previewRect.y + 2f, previewRect.width - 58f, 18f), "<b>" + GetItemTitle(selectedItem) + "</b>", itemLabelActiveStyle);
                GUI.Label(new Rect(textX, previewRect.y + 19f, previewRect.width - 58f, 15f), "<color=#7F8FA6>ID: " + selectedItem.UniqueName + "</color>", subLabelStyle);
                int stack = (selectedItem.settings_Inventory != null && selectedItem.settings_Inventory.StackSize > 0) ? selectedItem.settings_Inventory.StackSize : 20;
                GUI.Label(new Rect(textX, previewRect.y + 34f, previewRect.width - 58f, 15f), "<color=#7F8FA6>Макс. стак: " + stack + " шт</color>", subLabelStyle);

                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Кол-во:", subLabelStyle, GUILayout.Width(45));
                customAmountStr = GUILayout.TextField(customAmountStr, searchBoxStyle, GUILayout.Width(45));

                if (GUILayout.Button("+1", actionBtnStyle, GUILayout.Width(30), GUILayout.Height(22)))
                {
                    SpawnItem(selectedItem.UniqueName, 1);
                }
                if (GUILayout.Button("+10", actionBtnStyle, GUILayout.Width(35), GUILayout.Height(22)))
                {
                    SpawnItem(selectedItem.UniqueName, 10);
                }
                if (GUILayout.Button("Стак", actionBtnPurpleStyle, OptHeight22))
                {
                    SpawnItem(selectedItem.UniqueName, stack);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(3);
                if (GUILayout.Button("★ ВЫДАТЬ (" + customAmountStr + " шт)", actionBtnGreenStyle, GUILayout.Height(25)))
                {
                    int cnt = 1;
                    int.TryParse(customAmountStr, out cnt);
                    SpawnItem(selectedItem.UniqueName, cnt > 0 ? cnt : 1);
                }
            }
            else
            {
                GUILayout.Space(6);
                GUILayout.Label("<i>Выберите предмет из списка для выдачи</i>", subLabelStyle);
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>Быстрые наборы:</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🎁 Стройка", actionBtnBlueStyle, OptHeight22))
            {
                SpawnItem("Plank", 100); SpawnItem("Plastic", 100); SpawnItem("Scrap", 50);
            }
            if (GUILayout.Button("⚔ Титан", actionBtnGreenStyle, OptHeight22))
            {
                SpawnItem("Axe_Titanium", 1); SpawnItem("Hook_Titanium", 1); SpawnItem("Machete", 1);
                SpawnItem("Backpack_Big", 1); SpawnItem("OxygenBottle", 1); SpawnItem("Flippers", 1);
            }
            if (GUILayout.Button("🍲 Еда", actionBtnAmberStyle, OptHeight22))
            {
                SpawnItem("Cooked_Meat_Big", 10); SpawnItem("WaterBottle", 2); SpawnItem("Catfish", 10);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("<b>МОИ НАБОРЫ (пресеты):</b>", subLabelStyle);
            for (int i = 0; i < 3; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Набор " + (i + 1) + ": " + (presetItems[i] != null ? GetItemTitle(presetItems[i]) + " x" + presetAmounts[i] : "пусто"),
                    itemLabelStyle, GUILayout.Width(160), GUILayout.Height(20));
                if (GUILayout.Button("Запомнить", actionBtnStyle, GUILayout.Height(20))) SaveSpawnPreset(i);
                if (GUILayout.Button("Выдать", actionBtnGreenStyle, OptHeight20)) GiveSpawnPreset(i);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>КИСТЬ ЖИВОТНЫХ (ЛКМ в прицел):</b>", subLabelStyle);
            animalBrush = DrawModuleToggle("Кисть: спавн зверей", animalBrush);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < brushAnimalNames.Length; i++)
            {
                if (GUILayout.Button(brushAnimalNames[i], brushAnimalIndex == i ? actionBtnBlueStyle : actionBtnStyle, OptHeight20)) brushAnimalIndex = i;
            }
            GUILayout.EndHorizontal();
        }

        // ==========================================
        // ТАБ: ЛОГ / СТАТИСТИКА
        // ==========================================
        private void DrawLogTab()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🗑 Очистить журнал", actionBtnRedStyle, OptHeight22)) eventLog.Clear();
            if (GUILayout.Button("Обнулить статистику", actionBtnAmberStyle, OptHeight22)) ResetSessionStats();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("<b>СТАТИСТИКА СЕССИИ:</b>", subLabelStyle);
            GUILayout.Label("⏱ Время игры: " + FormatSessionTime(), subLabelStyle);
            GUILayout.Label("📦 Собрано предметов: " + sessionItems, subLabelStyle);
            GUILayout.Label("🏃 Пройдено пути: " + FormatDistance(sessionDistance), subLabelStyle);

            GUILayout.Space(4);
            GUILayout.Label("<b>РАСШИРЕННАЯ СТАТИСТИКА:</b>", subLabelStyle);
            GUILayout.Label("🦈 Акул убито: " + statSharkKills, subLabelStyle);
            GUILayout.Label("☠ Смертей: " + statDeaths, subLabelStyle);
            GUILayout.Label("🏝 Островов посещено: " + statIslands, subLabelStyle);
            GUILayout.Label("🧱 Блоков потеряно: " + statBlocksLost, subLabelStyle);
            GUILayout.Label("🐀 Крыс уничтожено: " + statRats, subLabelStyle);

            GUILayout.Space(4);
            GUILayout.Label("<b>ДОБЫТО РЕСУРСОВ (счётчик сессии):</b>", subLabelStyle);
            DrawResourceGains();

            GUILayout.Space(4);
            GUILayout.Label("<b>СОБЫТИЯ (" + eventLog.Count + "):</b>", subLabelStyle);
            logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.Height(300));
            for (int i = eventLog.Count - 1; i >= 0; i--) GUILayout.Label(eventLog[i], subLabelStyle);
            if (eventLog.Count == 0) GUILayout.Label("<i>Пока пусто — все действия тренера попадают сюда</i>", subLabelStyle);
            GUILayout.EndScrollView();
        }

        private void DrawResourceGains()
        {
            if (resourceGains.Count == 0)
            {
                GUILayout.Label("<i>Пока пусто — собирайте ресурсы, они появятся здесь (топ-12)</i>", subLabelStyle);
                return;
            }
            List<string> usedKeys = new List<string>();
            for (int top = 0; top < 12; top++)
            {
                string bestKey = null;
                int bestVal = 0;
                foreach (KeyValuePair<string, int> pair in resourceGains)
                {
                    if (usedKeys.Contains(pair.Key)) continue;
                    if (pair.Value > bestVal) { bestVal = pair.Value; bestKey = pair.Key; }
                }
                if (bestKey == null) break;
                usedKeys.Add(bestKey);
                GUILayout.Label("• " + bestKey + ": +" + bestVal, subLabelStyle);
            }
        }

        // ==========================================
        // СЕКЦИИ (перенесены из v11)
        // ==========================================
        private void DrawWaypointSection()
        {
            GUILayout.Space(6);
            GUILayout.Label("<b>ТОЧКИ ТП (" + waypoints.Count + "/" + MaxWaypoints + ")</b>", subLabelStyle);
            GUILayout.BeginHorizontal();
            newWaypointName = GUILayout.TextField(newWaypointName, searchBoxStyle, GUILayout.Height(20));
            if (GUILayout.Button("+", actionBtnGreenStyle, GUILayout.Width(26), GUILayout.Height(20))) AddWaypointHere();
            GUILayout.EndHorizontal();

            waypointEsp = DrawModuleToggle("Метки точек в мире", waypointEsp);

            int removeIndex = -1;
            for (int i = 0; i < waypoints.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(waypoints[i].name, itemLabelStyle, GUILayout.Width(78), GUILayout.Height(20));
                if (GUILayout.Button("ТП", actionBtnBlueStyle, GUILayout.Width(40), GUILayout.Height(20))) TeleportToWaypoint(i);
                if (GUILayout.Button("✕", actionBtnRedStyle, GUILayout.Width(24), GUILayout.Height(20))) removeIndex = i;
                GUILayout.EndHorizontal();
            }
            if (removeIndex >= 0) waypoints.RemoveAt(removeIndex);
        }

        private void DrawPaintPalette()
        {
            GUILayout.Space(6);
            GUILayout.Label("<b>ПАЛИТРА (любой RGB / HEX):</b>", subLabelStyle);

            Rect swatch = GUILayoutUtility.GetRect(0f, 16f, GUILayout.ExpandWidth(true));
            Color prevColor = GUI.color;
            GUI.color = CurrentPaintColor();
            GUI.DrawTexture(swatch, texWhiteDot);
            GUI.color = prevColor;

            float oldR = paintR;
            float oldG = paintG;
            float oldB = paintB;

            GUILayout.BeginHorizontal();
            GUILayout.Label("R", subLabelStyle, GUILayout.Width(12));
            paintR = GUILayout.HorizontalSlider(paintR, 0f, 255f);
            GUILayout.Label(Mathf.RoundToInt(paintR).ToString(), subLabelStyle, GUILayout.Width(28));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("G", subLabelStyle, GUILayout.Width(12));
            paintG = GUILayout.HorizontalSlider(paintG, 0f, 255f);
            GUILayout.Label(Mathf.RoundToInt(paintG).ToString(), subLabelStyle, GUILayout.Width(28));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("B", subLabelStyle, GUILayout.Width(12));
            paintB = GUILayout.HorizontalSlider(paintB, 0f, 255f);
            GUILayout.Label(Mathf.RoundToInt(paintB).ToString(), subLabelStyle, GUILayout.Width(28));
            GUILayout.EndHorizontal();

            bool sliderChanged = Mathf.RoundToInt(oldR) != Mathf.RoundToInt(paintR)
                || Mathf.RoundToInt(oldG) != Mathf.RoundToInt(paintG)
                || Mathf.RoundToInt(oldB) != Mathf.RoundToInt(paintB);

            GUILayout.BeginHorizontal();
            GUILayout.Label("HEX", subLabelStyle, GUILayout.Width(28));
            string typedHex = GUILayout.TextField(paintHex, searchBoxStyle, GUILayout.Height(20));
            GUILayout.EndHorizontal();

            if (sliderChanged)
            {
                paintHex = ColorToHex(CurrentPaintColor());
            }
            else if (typedHex != paintHex)
            {
                paintHex = typedHex;
                Color parsed;
                if (TryParseHex(typedHex, out parsed))
                {
                    paintR = parsed.r * 255f;
                    paintG = parsed.g * 255f;
                    paintB = parsed.b * 255f;
                }
            }

            GUILayout.Space(3);
            if (GUILayout.Button("🖌 Покрасить блок в прицеле", actionBtnPurpleStyle, OptHeight22)) PaintBlockUnderCrosshair();
            if (GUILayout.Button("🎨 Покрасить весь плот (HEX)", actionBtnBlueStyle, OptHeight22)) PaintWholeRaftCustom();
        }

        // ==========================================
        // HUD-ОВЕРЛЕИ (когда меню закрыто)
        // ==========================================
        private void DrawHudOverlays()
        {
            Camera cam = GetCachedCamera();
            if (cam == null) return;

            if (photoMode)
            {
                GUI.Box(new Rect(Screen.width / 2f - 110f, 8f, 220f, 22f), "📷 ФОТО-РЕЖИМ — F6 скриншот", espBoxStyle);
                return;
            }

            DrawActiveCheatsHud();
            DrawSessionStatsHud();
            DrawCompassHud(cam);
            DrawWeatherHud();
            DrawAlarmHud();
            DrawMinimapHud(cam);
            DrawEventLogHud();
            DrawPerfHud();
            DrawMachinesHud();
            DrawWorldMapHud(cam);
            DrawPlayersTableHud();
        }

        private void DrawActiveCheatsHud()
        {
            if (!hudActiveCheats) return;
            int count = activeCheatNames.Count;
            if (count == 0) return;

            int shown = Mathf.Min(count, 16);
            float h = shown * 15f + 26f + (count > shown ? 12f : 0f);
            Rect panel = new Rect(10f, 10f, 210f, h);
            GUI.Box(panel, GUIContent.none, hudPanelStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, panel.width - 16f, 16f), activeCheatsHeaderStr, hudTitleStyle);
            for (int i = 0; i < shown; i++)
                GUI.Label(new Rect(panel.x + 8f, panel.y + 20f + i * 15f, panel.width - 16f, 14f), activeCheatNames[i], hudTextStyle);
            if (count > shown)
                GUI.Label(new Rect(panel.x + 8f, panel.y + 20f + shown * 15f, panel.width - 16f, 14f), activeCheatsMoreStr, hudTextStyle);
        }

        private void DrawSessionStatsHud()
        {
            if (!hudSessionStats) return;
            Rect panel = new Rect(Screen.width - 224f, 10f, 214f, 64f);
            GUI.Box(panel, GUIContent.none, hudPanelStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, panel.width - 16f, 16f), "<b>📊 СЕССИЯ</b>", hudTitleStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 22f, panel.width - 16f, 14f), cachedSessionTimeStr, hudTextStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 36f, panel.width - 16f, 14f), cachedSessionItemsStr, hudTextStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 50f, panel.width - 16f, 14f), cachedSessionDistStr, hudTextStyle);
        }

        private void DrawCompassHud(Camera cam)
        {
            if (!hudCompass) return;
            try
            {
                if (Time.time > nextCompassUpdateTime || string.IsNullOrEmpty(cachedCompassStr))
                {
                    nextCompassUpdateTime = Time.time + 0.08f;
                    float heading = cam.transform.eulerAngles.y;
                    string text = "🧭 " + CardinalName(heading) + " (" + Mathf.RoundToInt(heading) + "°)";

                    Vector3 targetPos;
                    string label;
                    if (GetCompassTarget(out targetPos, out label))
                    {
                        Vector3 fwd = cam.transform.forward;
                        fwd.y = 0f;
                        if (fwd.sqrMagnitude > 0.01f)
                        {
                            fwd.Normalize();
                            Vector3 to = targetPos - cam.transform.position;
                            to.y = 0f;
                            float dist = to.magnitude;
                            if (to.sqrMagnitude > 0.01f)
                            {
                                float ang = Vector3.SignedAngle(fwd, to.normalized, Vector3.up);
                                string arrow = ang < -15f ? "←" : (ang > 15f ? "→" : "▲");
                                text += "   " + arrow + " " + label + " " + dist.ToString("F0") + "м";

                                if (autopilotEnabled && cachedRaft != null)
                                {
                                    Rigidbody rb = cachedRaftRb;
                                    if (rb != null)
                                    {
                                        float spd = rb.velocity.magnitude;
                                        if (spd > 0.5f) text += " • ETA " + FormatTimeSpan(dist / spd);
                                    }
                                }
                            }
                        }
                    }
                    cachedCompassStr = text;
                }

                GUI.Box(new Rect(Screen.width / 2f - 210f, 8f, 420f, 22f), cachedCompassStr, espBoxStyle);
            }
            catch {}
        }

        private void DrawWeatherHud()
        {
            if (!hudWeatherTimer || cachedWeather == null) return;
            try
            {
                if (Time.time > nextWeatherTxtTime || string.IsNullOrEmpty(cachedWeatherTxt))
                {
                    nextWeatherTxtTime = Time.time + 1f;
                    string wname = cachedWeather.GetCurrentWeatherType().ToString();
                    float timer = -1f;
                    try { timer = cachedWeather.WeatherTimer; } catch {}
                    string txt = "☁ " + TranslateWeather(wname);
                    if (timer >= 0f) txt += " • смена: " + FormatTimeSpan(timer);
                    cachedWeatherTxt = txt;
                }
                GUI.Box(new Rect(Screen.width / 2f - 110f, 34f, 220f, 20f), cachedWeatherTxt, espBoxStyle);
            }
            catch {}
        }

        private void DrawAlarmHud()
        {
            if (!sharkAlarm || Time.time >= sharkAlarmUntil) return;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 8f);
            Color prev = GUI.color;
            Color ac = AlarmBaseColor;
            ac.a = pulse;
            GUI.color = ac;
            GUI.Box(new Rect(Screen.width / 2f - 170f, 60f, 340f, 24f), "⚠ АКУЛА ПРИБЛИЖАЕТСЯ!", espBoxStyle);
            GUI.color = prev;
        }

        private void DrawMinimapHud(Camera cam)
        {
            if (!hudMinimap) return;
            try
            {
                float size = 180f;
                Vector2 center = new Vector2(Screen.width - size / 2f - 16f, Screen.height - size / 2f - 16f);
                Rect panel = new Rect(center.x - size / 2f, center.y - size / 2f, size, size);
                GUI.Box(panel, GUIContent.none, hudPanelStyle);

                float radius = size / 2f - 6f;
                Vector3 pp = cam.transform.position;
                float yaw = cam.transform.eulerAngles.y * Mathf.Deg2Rad;
                float c = Mathf.Cos(yaw);
                float s = Mathf.Sin(yaw);

                Color prev = GUI.color;
                for (int i = 0; i < minimapBlips.Count; i++)
                {
                    MinimapBlip blip = minimapBlips[i];
                    Vector3 rel = blip.Position - pp;
                    float rx = rel.x * c - rel.z * s;
                    float ry = rel.x * s + rel.z * c;
                    float px = rx * minimapZoom;
                    float py = -ry * minimapZoom;
                    if (px * px + py * py > radius * radius) continue;
                    float bs = blip.Size;
                    GUI.color = blip.Col;
                    GUI.DrawTexture(new Rect(center.x + px - bs / 2f, center.y + py - bs / 2f, bs, bs), texWhiteDot);
                }

                // Игрок в центре + направление взгляда
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(center.x - 3f, center.y - 3f, 6f, 6f), texKnob);
                GUI.DrawTexture(new Rect(center.x - 1f, center.y - 12f, 2f, 10f), texWhiteDot);
                GUI.color = MinimapNorthColor;
                GUI.Label(new Rect(center.x - 6f, panel.y + 2f, 20f, 14f), "С", hudTextStyle);
                GUI.color = prev;
            }
            catch {}
        }

        private void DrawEventLogHud()
        {
            if (!eventLogEnabled || eventLog.Count == 0) return;
            int shown = Mathf.Min(5, eventLog.Count);
            Rect panel = new Rect(10f, Screen.height - 10f - shown * 15f - 12f, 320f, shown * 15f + 12f);
            GUI.Box(panel, GUIContent.none, hudPanelStyle);
            for (int i = 0; i < shown; i++)
                GUI.Label(new Rect(panel.x + 6f, panel.y + 4f + i * 15f, panel.width - 12f, 14f),
                    eventLog[eventLog.Count - shown + i], hudTextStyle);
        }

        // ---------- V13 HUD: ТАХОМЕТР ----------
        private void DrawPerfHud()
        {
            if (!perfHud) return;
            float w = 250f;
            float h = 96f;
            Rect panel = new Rect(Screen.width / 2f - w / 2f, Screen.height - h - 12f, w, h);
            GUI.Box(panel, GUIContent.none, hudPanelStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, 180f, 16f), "<b>⏱ ПРОИЗВОДИТЕЛЬНОСТЬ</b>", hudTitleStyle);

            Color prev = GUI.color;
            Color c;
            if (fpsCurrent > 55f) c = FpsHighColor;
            else if (fpsCurrent > 30f) c = FpsMidColor;
            else c = FpsLowColor;
            GUI.color = c;
            GUI.Label(new Rect(panel.x + 8f, panel.y + 20f, 90f, 16f), perfFpsStr, hudTextStyle);
            GUI.color = prev;
            GUI.Label(new Rect(panel.x + 78f, panel.y + 20f, 170f, 16f), perfMsEntityStr, hudTextStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 34f, 230f, 16f), perfBlockCountStr, hudTextStyle);

            float gx = panel.x + 8f;
            float gy = panel.y + 54f;
            float gw = w - 16f;
            float gh = 30f;
            GUI.color = FpsGraphBgColor;
            GUI.DrawTexture(new Rect(gx, gy, gw, gh), texWhiteDot);
            float bw = gw / 60f;
            for (int i = 0; i < fpsSampleCount; i++)
            {
                int idx = (fpsSampleIdx - fpsSampleCount + i + fpsSamples.Length * 2) % fpsSamples.Length;
                float v = Mathf.Clamp01(fpsSamples[idx] / 70f);
                float bh = v * gh;
                if (bh < 1f) bh = 1f;
                GUI.color = v > 0.75f ? FpsGraphHighColor : (v > 0.45f ? FpsGraphMidColor : FpsGraphLowColor);
                GUI.DrawTexture(new Rect(gx + i * bw, gy + gh - bh, bw - 1f, bh), texWhiteDot);
            }
            GUI.color = prev;
        }

        // ---------- V13 HUD: ДАШБОРД МАШИН ----------
        private void DrawMachinesHud()
        {
            if (!hudMachines) return;
            UpdateMachineScan();
            float w = 214f;
            float h = 100f;
            Rect panel = new Rect(Screen.width - w - 10f, 84f, w, h);
            GUI.Box(panel, GUIContent.none, hudPanelStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, panel.width - 16f, 16f), "<b>⚙ МАШИНЫ ПЛОТА</b>", hudTitleStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 22f, panel.width - 16f, 14f), machinesEnginesStr, hudTextStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 36f, panel.width - 16f, 14f), machinesSmeltersStr, hudTextStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 50f, panel.width - 16f, 14f), machinesCookersStr, hudTextStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 64f, panel.width - 16f, 14f), machinesChargersStr, hudTextStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 80f, panel.width - 16f, 14f),
                "<i>обновление каждые 2 сек</i>", subLabelStyle);
        }
    }
}
