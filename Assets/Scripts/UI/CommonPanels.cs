using System;
using System.Collections.Generic;
using BalloonToHeaven.Core;
using BalloonToHeaven.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonToHeaven.UI
{
    /// <summary>Settings popup: music / SFX volume, quality, screen shake, progress reset.</summary>
    public static class SettingsPanel
    {
        public static void Show(Transform screen, Action onClosed = null)
        {
            var services = GameServices.Instance;
            var settings = services.Save.Data.settings;
            var root = UIFactory.Rect("SettingsPopup", screen);
            UIFactory.Stretch(root);
            UIFactory.Dimmer(root);
            var panel = UIFactory.Panel(root);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1120f));
            var title = UIFactory.Label(panel.transform, "Settings", 72, UITheme.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(800f, 100f));

            float y = -220f;
            void SliderRow(string label, float value, Action<float> changed)
            {
                var l = UIFactory.Label(panel.transform, label, 44, Color.white, TextAnchor.MiddleLeft);
                UIFactory.Place(l.rectTransform, new Vector2(0.5f, 1f), new Vector2(-180f, y), new Vector2(420f, 80f));
                var s = UIFactory.Slider(panel.transform, value, changed);
                UIFactory.Place((RectTransform)s.transform, new Vector2(0.5f, 1f), new Vector2(170f, y), new Vector2(420f, 80f));
                y -= 130f;
            }
            SliderRow("Music", settings.musicVolume, v => { settings.musicVolume = v; services.Audio.SetMusicVolume(v); services.Save.MarkDirty(); });
            SliderRow("Sound effects", settings.sfxVolume, v => { settings.sfxVolume = v; services.Audio.SetSfxVolume(v); services.Save.MarkDirty(); });

            var ql = UIFactory.Label(panel.transform, "Graphics quality", 44, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Place(ql.rectTransform, new Vector2(0.5f, 1f), new Vector2(-180f, y), new Vector2(420f, 80f));
            var qualityButtons = new List<Button>();
            string[] names = { "Low", "Mid", "High" };
            for (int i = 0; i < 3; i++)
            {
                int q = i;
                var b = UIFactory.Button(panel.transform, names[i], UITheme.Secondary, null, 38);
                UIFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(60f + i * 130f, y), new Vector2(120f, 80f));
                qualityButtons.Add(b);
                b.onClick.AddListener(() =>
                {
                    settings.quality = q;
                    QualityManager.Apply(q);
                    services.Save.MarkDirty();
                    for (int k = 0; k < qualityButtons.Count; k++) ((Image)qualityButtons[k].targetGraphic).color = k == q ? UITheme.Primary : UITheme.Secondary;
                });
                ((Image)b.targetGraphic).color = i == settings.quality ? UITheme.Primary : UITheme.Secondary;
            }
            y -= 130f;

            Button shake = null;
            shake = UIFactory.Button(panel.transform, ShakeLabel(settings.screenShake), UITheme.Secondary, () =>
            {
                settings.screenShake = !settings.screenShake;
                CameraShake.Enabled = settings.screenShake;
                services.Save.MarkDirty();
                UIFactory.ButtonLabel(shake).text = ShakeLabel(settings.screenShake);
            }, 40);
            UIFactory.Place((RectTransform)shake.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(620f, 100f));
            y -= 140f;

            int resetTaps = 0;
            Button reset = null;
            reset = UIFactory.Button(panel.transform, "Reset progress", UITheme.Danger, () =>
            {
                resetTaps++;
                if (resetTaps == 1) { UIFactory.ButtonLabel(reset).text = "Tap again to erase everything"; return; }
                services.Save.ResetProgress();
                GameEvents.RaiseToast("Progress reset");
                UnityEngine.Object.Destroy(root.gameObject);
                services.LoadScene(SceneId.MainMenu);
            }, 38);
            UIFactory.Place((RectTransform)reset.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(620f, 100f));

            var close = UIFactory.Button(panel.transform, "Close", UITheme.Success, () =>
            {
                services.Save.SaveIfDirty();
                UnityEngine.Object.Destroy(root.gameObject);
                onClosed?.Invoke();
            }, 46);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(420f, 120f));
            UIFactory.PopIn(panel.rectTransform);
        }

        static string ShakeLabel(bool on) => "Screen shake: " + (on ? "On" : "Off");
    }

    /// <summary>Achievements popup listing all achievements with progress.</summary>
    public static class AchievementsPanel
    {
        public static void Show(Transform screen)
        {
            var services = GameServices.Instance;
            var root = UIFactory.Rect("AchievementsPopup", screen);
            UIFactory.Stretch(root);
            UIFactory.Dimmer(root);
            var panel = UIFactory.Panel(root);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1600f));
            var title = UIFactory.Label(panel.transform, "Achievements", 70, UITheme.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(860f, 100f));
            var scroll = UIFactory.ScrollView(panel.transform, out var content);
            UIFactory.Stretch((RectTransform)scroll.transform, 30f, 30f, 160f, 200f);
            var list = services.Database.achievements;
            float rowH = 150f;
            content.sizeDelta = new Vector2(0f, list.Count * (rowH + 16f) + 20f);
            for (int i = 0; i < list.Count; i++)
            {
                var def = list[i];
                bool unlocked = services.Achievements.IsUnlocked(def.id);
                long value = services.Achievements.GetValue(def.stat);
                var row = UIFactory.Image("Row", content, unlocked ? new Color(0.3f, 0.75f, 0.45f, 0.35f) : UITheme.PanelLight, SpriteFactory.RoundedRect);
                var rt = row.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(-20f, rowH);
                rt.anchoredPosition = new Vector2(0f, -10f - i * (rowH + 16f));
                var icon = UIFactory.Image("Icon", row.transform, unlocked ? UITheme.Gold : UITheme.Disabled, SpriteFactory.Trophy);
                UIFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(100f, 100f));
                var name = UIFactory.Label(row.transform, def.title, 44, Color.white, TextAnchor.UpperLeft, true);
                UIFactory.Stretch(name.rectTransform, 150f, 200f, 18f, 60f);
                var desc = UIFactory.Label(row.transform, def.description, 32, UITheme.TextDim, TextAnchor.LowerLeft);
                UIFactory.Stretch(desc.rectTransform, 150f, 200f, 70f, 18f);
                string status = unlocked ? "Done" : UIFactory.FormatNumber(Math.Min(value, def.target)) + "/" + UIFactory.FormatNumber(def.target);
                var st = UIFactory.Label(row.transform, status + "\n+" + def.gemReward + " gems", 32, unlocked ? UITheme.Success : UITheme.GemColor, TextAnchor.MiddleRight, true);
                UIFactory.Stretch(st.rectTransform, 600f, 24f, 10f, 10f);
            }
            var close = UIFactory.Button(panel.transform, "Close", UITheme.Success, () => UnityEngine.Object.Destroy(root.gameObject), 46);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(420f, 120f));
            UIFactory.PopIn(panel.rectTransform);
        }
    }

    /// <summary>Short notifications (power-ups, achievements) shown near the top of the screen.</summary>
    public sealed class ToastView : MonoBehaviour
    {
        Text text;
        CanvasGroup group;
        float timer;
        readonly Queue<string> queue = new Queue<string>();

        public static ToastView Create(Transform parent, float y = -420f)
        {
            var bg = UIFactory.Image("Toast", parent, new Color(0.05f, 0.07f, 0.18f, 0.8f), SpriteFactory.Pill);
            UIFactory.Place(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(760f, 100f));
            var view = bg.gameObject.AddComponent<ToastView>();
            view.text = UIFactory.Label(bg.transform, "", 42, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(view.text.rectTransform, 20, 20, 0, 0);
            view.group = bg.gameObject.AddComponent<CanvasGroup>();
            view.group.alpha = 0f;
            view.group.blocksRaycasts = false;
            return view;
        }

        void OnEnable()
        {
            GameEvents.Toast += Enqueue;
            GameEvents.AchievementUnlocked += OnAchievement;
        }

        void OnDisable()
        {
            GameEvents.Toast -= Enqueue;
            GameEvents.AchievementUnlocked -= OnAchievement;
        }

        void OnAchievement(string title)
        {
            Enqueue("Achievement: " + title);
            if (GameServices.IsReady) GameServices.Instance.Audio.PlaySfx(SfxId.Unlock);
        }

        public void Enqueue(string message)
        {
            if (queue.Count < 4) queue.Enqueue(message);
        }

        void Update()
        {
            if (timer > 0f)
            {
                timer -= Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(Mathf.Min(timer * 3f, (1.6f - timer) * 6f + 0.2f));
                return;
            }
            group.alpha = 0f;
            if (queue.Count > 0)
            {
                text.text = queue.Dequeue();
                timer = 1.6f;
            }
        }
    }

    /// <summary>Top bar with coin and gem balances that update through events.</summary>
    public sealed class CurrencyBar : MonoBehaviour
    {
        Text coins, gems;

        public static CurrencyBar Create(Transform parent, float y = -70f)
        {
            var root = UIFactory.Rect("CurrencyBar", parent);
            UIFactory.Place(root, new Vector2(1f, 1f), new Vector2(-30f, y), new Vector2(640f, 100f), new Vector2(1f, 0.5f));
            var bar = root.gameObject.AddComponent<CurrencyBar>();
            bar.coins = UIFactory.CurrencyBadge(root, false, out var coinRect);
            UIFactory.Place(coinRect, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(330f, 90f), new Vector2(0f, 0.5f));
            bar.gems = UIFactory.CurrencyBadge(root, true, out var gemRect);
            UIFactory.Place(gemRect, new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(270f, 90f), new Vector2(1f, 0.5f));
            bar.Refresh(GameServices.Instance.Economy.Coins, GameServices.Instance.Economy.Gems);
            return bar;
        }

        void OnEnable() { GameEvents.CurrencyChanged += Refresh; }
        void OnDisable() { GameEvents.CurrencyChanged -= Refresh; }

        void Refresh(int c, int g)
        {
            if (coins != null) coins.text = UIFactory.FormatNumber(c);
            if (gems != null) gems.text = UIFactory.FormatNumber(g);
        }
    }

    /// <summary>Shared helper to build a 3D backdrop camera + environment for menu scenes.</summary>
    public static class MenuBackdrop
    {
        public static (Camera camera, EnvironmentController env) Create(Worlds.WorldData world, float scrollSpeed = 0.8f)
        {
            var cam = CameraRig.Create(10f, world != null ? world.skyBottom : new Color(0.6f, 0.8f, 1f));
            Visuals.EnvironmentLighting.Apply(world);
            var env = EnvironmentBuilder.Build(world, cam, null, 7);
            env.AutoScrollSpeed = scrollSpeed;
            return (cam, env);
        }
    }
}
