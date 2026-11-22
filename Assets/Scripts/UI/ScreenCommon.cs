using System;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Core;
using BalloonToHeaven.Upgrades;
using BalloonToHeaven.Visuals;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BalloonToHeaven.UI
{
    /// <summary>Small building blocks shared by the menu screens.</summary>
    public static class ScreenCommon
    {
        /// <summary>Back button + title along the top of a screen.</summary>
        public static Text Header(RectTransform screen, string title, Action onBack)
        {
            if (onBack != null)
            {
                var back = UIFactory.IconButton(screen, SpriteFactory.Arrow, new Color(0f, 0f, 0f, 0.35f), onBack, 120f);
                UIFactory.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(90f, -80f), new Vector2(120f, 120f));
            }
            var t = UIFactory.Label(screen, title, 64, Color.white, TextAnchor.MiddleLeft, true);
            UIFactory.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(180f, -80f), new Vector2(520f, 110f), new Vector2(0f, 0.5f));
            return t;
        }

        /// <summary>A row of up to three stars (filled or empty).</summary>
        public static void Stars(Transform parent, int filled, float size, Vector2 anchor, Vector2 center, float spacing = 1.05f)
        {
            for (int i = 0; i < 3; i++)
            {
                var s = UIFactory.Image("Star" + i, parent, i < filled ? UITheme.Gold : new Color(0f, 0f, 0f, 0.35f), SpriteFactory.Star);
                float x = (i - 1) * size * spacing;
                float y = i == 1 ? size * 0.15f : 0f;
                UIFactory.Place(s.rectTransform, anchor, center + new Vector2(x, y), new Vector2(size, size));
            }
        }

        public static Image GradientBackground(Transform parent, Color top, Color bottom)
        {
            var bg = UIFactory.Image("Background", parent, Color.white, SpriteFactory.VerticalGradient(top, bottom));
            UIFactory.Stretch(bg.rectTransform, -200, -200, -200, -200);
            return bg;
        }

        public static void Deny(string message)
        {
            if (GameServices.IsReady) GameServices.Instance.Audio.PlaySfx(SfxId.Deny);
            GameEvents.RaiseToast(message);
        }

        public static Sprite BalloonSprite(BalloonData data) => SpriteFactory.FromTexture(ProceduralTextures.BalloonPattern(data), "pattern_" + data.id);
    }

    /// <summary>
    /// Renders a rotating 3D balloon into a RawImage. The preview rig lives far away from the
    /// gameplay/menu world so no layers are needed.
    /// </summary>
    public sealed class BalloonPreview : MonoBehaviour, IDragHandler
    {
        static int slotCounter;

        Camera previewCamera;
        RenderTexture texture;
        Transform rig, pivot;
        BalloonVisual current;
        float yaw = -20f, spinVelocity = 25f;

        public static BalloonPreview Create(Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var img = UIFactory.Rect("BalloonPreview", parent);
            UIFactory.Place(img, anchor, position, size);
            var raw = img.gameObject.AddComponent<RawImage>();
            var preview = img.gameObject.AddComponent<BalloonPreview>();

            int res = QualityManager.CurrentLevel == 0 ? 384 : 640;
            preview.texture = new RenderTexture(res, res, 16, RenderTextureFormat.ARGB32) { name = "BalloonPreviewRT", antiAliasing = QualityManager.CurrentLevel == 2 ? 4 : 1 };
            raw.texture = preview.texture;

            slotCounter++;
            preview.rig = new GameObject("PreviewRig").transform;
            preview.rig.position = new Vector3(1000f + slotCounter * 50f, 0f, 0f);
            preview.pivot = new GameObject("Pivot").transform;
            preview.pivot.SetParent(preview.rig, false);

            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(preview.rig, false);
            camGo.transform.localPosition = new Vector3(0f, 0.2f, -20f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 2.7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 40f;
            cam.targetTexture = preview.texture;
            cam.depth = -10;
            preview.previewCamera = cam;
            return preview;
        }

        public void Show(BalloonData data, int stage, TrailCosmetic trail = null)
        {
            if (current != null) Destroy(current.gameObject);
            current = BalloonModelBuilder.Build(data, stage, pivot, trail);
            current.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            current.SetTrailsEmitting(false);
            if (previewCamera != null) previewCamera.orthographicSize = data.ornaments != 0 ? 2.9f : 2.7f;
        }

        public void OnDrag(PointerEventData e)
        {
            spinVelocity = e.delta.x * 6f;
            yaw += e.delta.x * 0.4f;
        }

        void Update()
        {
            spinVelocity = Mathf.Lerp(spinVelocity, 25f, Time.unscaledDeltaTime * 1.5f);
            yaw += spinVelocity * Time.unscaledDeltaTime;
            if (pivot != null) pivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void OnDestroy()
        {
            if (rig != null) Destroy(rig.gameObject);
            if (texture != null) { texture.Release(); Destroy(texture); }
        }
    }
}
