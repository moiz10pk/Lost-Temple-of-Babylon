using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Babylon
{
    /// <summary>Code-built uGUI helpers in the Babylon style: dark panels, gold borders, parchment text.</summary>
    public static class UIFactory
    {
        public static readonly Color PanelColor = new Color32(0x1A, 0x12, 0x0B, 217);
        public static readonly Color Gold = new Color32(0xC8, 0xA0, 0x4A, 255);
        public static readonly Color TitleGold = new Color32(0xF2, 0xC1, 0x4E, 255);
        public static readonly Color TextColor = new Color32(0xF3, 0xE3, 0xC3, 255);
        public static readonly Color ButtonColor = new Color32(0x2A, 0x1C, 0x10, 240);

        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Canvas CreateCanvas(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent) go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var m = go.AddComponent<InputSystemUIInputModule>();
            m.AssignDefaultActions();
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static Image Image(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, bool border = true)
        {
            var rt = Rect(name, parent, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = PanelColor;
            if (border) AddBorder(rt, Gold, 3f);
            return img;
        }

        /// <summary>Four thin gold strips framing a rect (crisper than an Outline effect).</summary>
        public static void AddBorder(RectTransform rt, Color c, float w)
        {
            void Strip(string n, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
            {
                var img = Image(rt, n, c); img.raycastTarget = false;
                var r = img.rectTransform; r.anchorMin = min; r.anchorMax = max; r.offsetMin = offMin; r.offsetMax = offMax;
            }
            Strip("BorderTop", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -w), Vector2.zero);
            Strip("BorderBottom", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, w));
            Strip("BorderLeft", new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(w, 0));
            Strip("BorderRight", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-w, 0), Vector2.zero);
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static Text Label(RectTransform parent, string text, int size, Color color, TextAnchor align, FontStyle style, Vector2 pos, Vector2 sizeDelta)
        {
            var t = Label(parent, text, size, color, align, style);
            t.rectTransform.anchoredPosition = pos; t.rectTransform.sizeDelta = sizeDelta;
            return t;
        }

        public static void AddShadow(Text t)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.85f); s.effectDistance = new Vector2(2, -2);
        }

        public static Button Button(Transform parent, string label, Vector2 pos, Vector2 size, int fontSize = 30)
        {
            var rt = Rect("Button_" + label, parent, new Vector2(0.5f, 0.5f), pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var b = rt.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = ButtonColor; cb.highlightedColor = new Color32(0x6B, 0x4A, 0x1E, 255);
            cb.pressedColor = new Color32(0x9C, 0x74, 0x2C, 255); cb.selectedColor = cb.highlightedColor;
            cb.colorMultiplier = 1f;
            b.colors = cb;
            AddBorder(rt, Gold, 2f);
            var t = Label(rt, label, fontSize, TextColor);
            Stretch(t.rectTransform);
            return b;
        }

        public static void SetButtonLabel(Button b, string label)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t) t.text = label;
        }

        public static Slider Slider(Transform parent, string label, Vector2 pos, float value)
        {
            var row = Rect("Slider_" + label, parent, new Vector2(0.5f, 0.5f), pos, new Vector2(640, 50));
            var lt = Label(row, label, 26, TextColor, TextAnchor.MiddleLeft);
            lt.rectTransform.anchorMin = new Vector2(0, 0); lt.rectTransform.anchorMax = new Vector2(0.42f, 1);
            lt.rectTransform.offsetMin = lt.rectTransform.offsetMax = Vector2.zero;

            var area = Rect("Track", row, new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(360, 14));
            var bg = area.gameObject.AddComponent<Image>(); bg.color = new Color(0, 0, 0, 0.6f);
            var fillArea = Rect("Fill Area", area, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(fillArea);
            var fill = Image(fillArea, "Fill", Gold); Stretch(fill.rectTransform);
            var handleArea = Rect("Handle Area", area, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(handleArea);
            var handle = Image(handleArea, "Handle", TitleGold);
            handle.rectTransform.sizeDelta = new Vector2(22, 34);

            var s = row.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = 0; s.maxValue = 1; s.value = value;
            return s;
        }

        public static Toggle Toggle(Transform parent, string label, Vector2 pos, bool value)
        {
            var row = Rect("Toggle_" + label, parent, new Vector2(0.5f, 0.5f), pos, new Vector2(640, 50));
            var lt = Label(row, label, 26, TextColor, TextAnchor.MiddleLeft);
            lt.rectTransform.anchorMin = new Vector2(0, 0); lt.rectTransform.anchorMax = new Vector2(0.6f, 1);
            lt.rectTransform.offsetMin = lt.rectTransform.offsetMax = Vector2.zero;
            var box = Rect("Box", row, new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(40, 40));
            var bg = box.gameObject.AddComponent<Image>(); bg.color = new Color(0, 0, 0, 0.6f);
            AddBorder(box, Gold, 2f);
            var check = Image(box, "Check", TitleGold);
            check.rectTransform.anchorMin = new Vector2(0.2f, 0.2f); check.rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
            check.rectTransform.offsetMin = check.rectTransform.offsetMax = Vector2.zero;
            var tg = row.gameObject.AddComponent<Toggle>();
            tg.targetGraphic = bg; tg.graphic = check; tg.isOn = value;
            return tg;
        }

        /// <summary>Horizontal bar: returns the fill rect whose anchorMax.x is the value.</summary>
        public static RectTransform Bar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor)
        {
            var rt = Rect(name, parent, anchor, pos, size);
            var bg = rt.gameObject.AddComponent<Image>(); bg.color = new Color(0, 0, 0, 0.65f);
            AddBorder(rt, Gold, 2f);
            var fill = Image(rt, "Fill", fillColor);
            var f = fill.rectTransform;
            f.anchorMin = Vector2.zero; f.anchorMax = Vector2.one; f.offsetMin = new Vector2(3, 3); f.offsetMax = new Vector2(-3, -3);
            f.SetAsFirstSibling();
            return f;
        }

        public static void SetBar(RectTransform fill, float v)
        {
            if (!fill) return;
            var a = fill.anchorMax; a.x = Mathf.Clamp01(v); fill.anchorMax = a;
        }

        public static void OnClick(Button b, UnityAction a)
        {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => { if (AudioManager.I) AudioManager.I.Play2D(SoundId.UIClick, 0.6f); a(); });
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Object.Destroy(t.GetChild(i).gameObject);
                else Object.DestroyImmediate(t.GetChild(i).gameObject);
            }
        }
    }
}
