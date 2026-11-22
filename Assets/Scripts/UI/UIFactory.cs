using System;
using BalloonToHeaven.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BalloonToHeaven.UI
{
    public static class UITheme
    {
        public static readonly Color Panel = new Color(0.08f, 0.1f, 0.22f, 0.88f);
        public static readonly Color PanelLight = new Color(1f, 1f, 1f, 0.12f);
        public static readonly Color Primary = new Color(1f, 0.62f, 0.18f);
        public static readonly Color Secondary = new Color(0.25f, 0.62f, 1f);
        public static readonly Color Success = new Color(0.3f, 0.82f, 0.45f);
        public static readonly Color Danger = new Color(0.95f, 0.35f, 0.35f);
        public static readonly Color Disabled = new Color(0.45f, 0.48f, 0.55f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        public static readonly Color GemColor = new Color(0.55f, 0.85f, 1f);
        public static readonly Color Text = Color.white;
        public static readonly Color TextDim = new Color(0.8f, 0.85f, 0.95f, 0.85f);

        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return font;
            }
        }
    }

    /// <summary>Press feedback for buttons: scale punch and click sound.</summary>
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 baseScale = Vector3.one;
        void Awake() { baseScale = transform.localScale; }
        public void OnPointerDown(PointerEventData e) { transform.localScale = baseScale * 0.94f; }
        public void OnPointerUp(PointerEventData e) { transform.localScale = baseScale; }
        public void OnPointerExit(PointerEventData e) { transform.localScale = baseScale; }
    }

    /// <summary>Keeps the UI inside the device safe area (notches, rounded corners).</summary>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        Rect last;
        void Update()
        {
            var safe = Screen.safeArea;
            if (safe == last || Screen.width <= 0 || Screen.height <= 0) return;
            last = safe;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Code-first helpers for building uGUI screens with a consistent style.</summary>
    public static class UIFactory
    {
        public const float RefWidth = 1080f;
        public const float RefHeight = 1920f;

        public static RectTransform CreateScreen(string name, int sortingOrder = 0)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(go.transform, false);
            var rt = (RectTransform)safe.transform;
            Stretch(rt);
            safe.AddComponent<SafeAreaFitter>();
            return rt;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Places a rect using a single anchor point (0..1), pivot and size in reference pixels.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static Image Panel(Transform parent, Color? color = null, string name = "Panel")
        {
            var img = Image(name, parent, color ?? UITheme.Panel, SpriteFactory.RoundedRect, true);
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color? color = null, TextAnchor anchor = TextAnchor.MiddleCenter, bool bold = false, bool shadow = true)
        {
            var rt = Rect("Label", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UITheme.Font;
            t.text = text;
            t.fontSize = size;
            t.color = color ?? UITheme.Text;
            t.alignment = anchor;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.45f);
                s.effectDistance = new Vector2(2f, -3f);
            }
            return t;
        }

        public static Button Button(Transform parent, string label, Color color, Action onClick, int fontSize = 46, Sprite icon = null)
        {
            var img = Image("Button", parent, color, SpriteFactory.RoundedRect, true);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                if (GameServices.IsReady) GameServices.Instance.Audio.PlaySfx(SfxId.Click);
                onClick?.Invoke();
            });
            img.gameObject.AddComponent<ButtonFeedback>();
            var shadow = img.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            shadow.effectDistance = new Vector2(0f, -6f);

            if (icon != null)
            {
                var ic = Image("Icon", img.transform, Color.white, icon);
                ic.preserveAspect = true;
                var irt = ic.rectTransform;
                irt.anchorMin = new Vector2(0f, 0.5f);
                irt.anchorMax = new Vector2(0f, 0.5f);
                irt.sizeDelta = new Vector2(fontSize * 1.3f, fontSize * 1.3f);
                irt.anchoredPosition = new Vector2(fontSize * 1.1f, 0f);
            }
            if (!string.IsNullOrEmpty(label))
            {
                var t = Label(img.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, true);
                Stretch(t.rectTransform, icon != null ? fontSize * 1.8f : 10f, 10f, 0f, 0f);
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 18;
                t.resizeTextMaxSize = fontSize;
            }
            return button;
        }

        public static Button IconButton(Transform parent, Sprite icon, Color color, Action onClick, float size = 130f)
        {
            var b = Button(parent, null, color, onClick);
            var ic = Image("Icon", b.transform, Color.white, icon);
            ic.preserveAspect = true;
            Stretch(ic.rectTransform, size * 0.2f, size * 0.2f, size * 0.2f, size * 0.2f);
            ((RectTransform)b.transform).sizeDelta = new Vector2(size, size);
            return b;
        }

        public static Text ButtonLabel(Button b) => b.GetComponentInChildren<Text>();

        public static void SetButtonEnabled(Button b, bool enabled, Color enabledColor)
        {
            b.interactable = enabled;
            var img = b.targetGraphic as Image;
            if (img != null) img.color = enabled ? enabledColor : UITheme.Disabled;
        }

        /// <summary>Coin or gem amount display with icon.</summary>
        public static Text CurrencyBadge(Transform parent, bool gems, out RectTransform rect)
        {
            var bg = Image(gems ? "GemBadge" : "CoinBadge", parent, new Color(0f, 0f, 0f, 0.35f), SpriteFactory.Pill);
            rect = bg.rectTransform;
            var icon = Image("Icon", bg.transform, gems ? UITheme.GemColor : UITheme.Gold, gems ? SpriteFactory.Gem : SpriteFactory.Coin);
            Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(64f, 64f));
            if (!gems)
            {
                var inner = Image("Inner", icon.transform, new Color(1f, 0.65f, 0.1f), SpriteFactory.CoinInner);
                Stretch(inner.rectTransform);
            }
            var t = Label(bg.transform, "0", 42, Color.white, TextAnchor.MiddleLeft, true);
            Stretch(t.rectTransform, 86f, 16f, 0f, 0f);
            return t;
        }

        public static ScrollRect ScrollView(Transform parent, out RectTransform content, bool vertical = true)
        {
            var viewport = Image("ScrollView", parent, new Color(1f, 1f, 1f, 0.001f), null, true);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            content = Rect("Content", viewport.transform);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 100f);
            scroll.content = content;
            scroll.viewport = viewport.rectTransform;
            scroll.horizontal = !vertical;
            scroll.vertical = vertical;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 30f;
            return scroll;
        }

        public static Slider Slider(Transform parent, float value, Action<float> onChanged)
        {
            var root = Rect("Slider", parent);
            var bg = Image("Background", root, new Color(1f, 1f, 1f, 0.2f), SpriteFactory.Pill);
            Stretch(bg.rectTransform, 0, 0, 22, 22);
            var fillArea = Rect("FillArea", root);
            Stretch(fillArea, 0, 0, 22, 22);
            var fill = Image("Fill", fillArea, UITheme.Primary, SpriteFactory.Pill);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var handleArea = Rect("HandleArea", root);
            Stretch(handleArea, 30, 30, 0, 0);
            var handle = Image("Handle", handleArea, Color.white, SpriteFactory.CircleSprite, true);
            handle.rectTransform.sizeDelta = new Vector2(64f, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            return slider;
        }

        /// <summary>A horizontal fill bar (progress, boss health, stats).</summary>
        public static Image Bar(Transform parent, Color background, Color fillColor, out Image fill)
        {
            var bg = Image("Bar", parent, background, SpriteFactory.Pill);
            fill = Image("Fill", bg.transform, fillColor, SpriteFactory.Pill);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            Stretch(fill.rectTransform, 6, 6, 6, 6);
            return bg;
        }

        /// <summary>Fade + scale-in animation for popups.</summary>
        public static void PopIn(RectTransform rt, float duration = 0.28f)
        {
            var group = rt.GetComponent<CanvasGroup>();
            if (group == null) group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            rt.localScale = Vector3.one * 0.85f;
            TweenRunner.Run(rt, duration, t =>
            {
                group.alpha = t;
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, Ease.OutBack(t));
            });
        }

        /// <summary>Full screen dimmer that blocks input behind popups.</summary>
        public static Image Dimmer(Transform parent, float alpha = 0.6f)
        {
            var img = Image("Dimmer", parent, new Color(0.02f, 0.03f, 0.08f, alpha), null, true);
            Stretch(img.rectTransform, -200, -200, -200, -200);
            return img;
        }

        public static string FormatNumber(long value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
