using System;
using UnityEngine;
using UnityEngine.UI;

namespace MissionForge
{
    /// <summary>Tiny helper to build uGUI from code (no prefabs needed during the hackathon).</summary>
    public static class UIKit
    {
        static Font _font;
        public static Font Font => _font ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static readonly Color Bg = new Color(0.04f, 0.06f, 0.11f, 0.97f);
        public static readonly Color Card = new Color(0.10f, 0.13f, 0.21f, 1f);
        public static readonly Color CardSel = new Color(0.16f, 0.32f, 0.55f, 1f);
        public static readonly Color Accent = new Color(0.98f, 0.45f, 0.20f, 1f);
        public static readonly Color Good = new Color(0.30f, 0.85f, 0.45f, 1f);
        public static readonly Color Bad = new Color(0.95f, 0.30f, 0.30f, 1f);
        public static readonly Color Warn = new Color(1f, 0.78f, 0.25f, 1f);
        public static readonly Color TextCol = new Color(0.92f, 0.94f, 1f, 1f);
        public static readonly Color Muted = new Color(0.62f, 0.68f, 0.80f, 1f);

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchor a rect to a fraction of its parent (0..1).</summary>
        public static RectTransform Place(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Panel(Transform parent, Color c, string name = "Panel")
        {
            var img = Node(name, parent).gameObject.AddComponent<Image>();
            img.color = c;
            return img;
        }

        public static LayoutElement Size(Component c, float height = -1, float width = -1, float flexW = -1)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            if (height >= 0) le.preferredHeight = le.minHeight = height;
            if (width >= 0) le.preferredWidth = le.minWidth = width;
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        public static Text Label(Transform parent, string text, int size = 20, TextAnchor align = TextAnchor.MiddleLeft, Color? color = null, FontStyle style = FontStyle.Normal)
        {
            var t = Node("Label", parent).gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.alignment = align; t.fontStyle = style;
            t.color = color ?? TextCol; t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(Transform parent, string text, Action onClick, Color? bg = null, int size = 20, float height = 52, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var img = Panel(parent, bg ?? Card, "Button");
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f); colors.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = colors;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            var label = Label(img.transform, text, size, align);
            Place(label.rectTransform, 0, 0, 1, 1);
            label.rectTransform.offsetMin = new Vector2(14, 6); label.rectTransform.offsetMax = new Vector2(-14, -6);
            if (height > 0) Size(img, height);
            return b;
        }

        public static RectTransform VBox(Transform parent, float spacing = 8, int pad = 0, string name = "VBox")
        {
            var rt = Node(name, parent);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlHeight = v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            return rt;
        }

        public static RectTransform HBox(Transform parent, float spacing = 8, float height = 52)
        {
            var rt = Node("HBox", parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlHeight = h.childControlWidth = true;
            h.childForceExpandHeight = true; h.childForceExpandWidth = true;
            Size(rt, height).flexibleHeight = 0;
            return rt;
        }

        /// <summary>Scrollable vertical list. Returns the content transform to add children to.</summary>
        public static RectTransform ScrollList(RectTransform area, float spacing = 8, int pad = 12)
        {
            var sr = area.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.scrollSensitivity = 30; sr.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Place(Node("Viewport", area), 0, 0, 1, 1);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f); // catches scroll events
            var content = VBox(viewport, spacing, pad, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = viewport; sr.content = content;
            return content;
        }

        /// <summary>Resource bar: label on top, filled bar, red when over limit.</summary>
        public static void Bar(Transform parent, string label, float value, float max, string valueText, bool ok)
        {
            var box = VBox(parent, 4);
            var row = Label(box, $"{label}  <color={Hex(ok ? Good : Bad)}>{valueText}</color>", 18);
            Size(row, 24);
            var bg = Panel(box, new Color(1, 1, 1, 0.08f), "BarBg");
            Size(bg, 12);
            var fill = Panel(bg.transform, ok ? Good : Bad, "Fill");
            float f = max > 0 ? Mathf.Clamp01(value / max) : (value > 0 ? 1 : 0);
            Place(fill.rectTransform, 0, 0, f, 1);
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
