using System;
using System.Collections.Generic;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonToHeaven.UI
{
    /// <summary>
    /// Gameplay HUD: world/level, progress bar, coins, durability hearts, shield and power-up timers,
    /// boss bar, ability button and pause. Purely reactive to GameEvents (no gameplay logic).
    /// </summary>
    public sealed class HUDView : MonoBehaviour
    {
        RectTransform screen;
        Image progressFill;
        RectTransform progressMarker;
        Text coinText, bannerText, bannerSub, bossName, bossPhase, shieldText, abilityLabel;
        CanvasGroup bannerGroup;
        RectTransform heartsRoot;
        readonly List<Image> hearts = new List<Image>();
        Image bossFill;
        GameObject bossRoot, shieldRoot, abilityRoot;
        Image abilityFill;
        Image damageFlash;
        readonly Dictionary<PowerUpType, (GameObject root, Image fill)> powerUpIcons = new Dictionary<PowerUpType, (GameObject, Image)>();
        RectTransform powerUpRow;
        float bannerTimer;
        float flashTimer;
        int displayedCoins = -1;
        IActiveAbility ability;

        public Action PauseRequested;
        public RectTransform Screen => screen;

        public static HUDView Create(LevelData level, string worldName, IActiveAbility activeAbility)
        {
            var screen = UIFactory.CreateScreen("HUD", 10);
            var hud = screen.gameObject.AddComponent<HUDView>();
            hud.screen = screen;
            hud.ability = activeAbility;
            hud.Build(level, worldName);
            return hud;
        }

        void Build(LevelData level, string worldName)
        {
            // Damage flash (full screen, behind everything else in the HUD)
            var vignetteSprite = SpriteFactory.FromTexture(ProceduralTextures.Vignette, "vignette");
            damageFlash = UIFactory.Image("DamageFlash", screen, new Color(1f, 0.15f, 0.1f, 0f), vignetteSprite);
            UIFactory.Stretch(damageFlash.rectTransform, -100, -100, -100, -100);

            // Top bar
            var pause = UIFactory.IconButton(screen, SpriteFactory.Pause, new Color(0f, 0f, 0f, 0.35f), () => PauseRequested?.Invoke(), 120f);
            UIFactory.Place((RectTransform)pause.transform, new Vector2(0f, 1f), new Vector2(90f, -90f), new Vector2(120f, 120f));

            var title = UIFactory.Label(screen, "World " + level.worldNumber + " · Level " + level.indexInWorld + "  <color=#FFE08A>" + level.levelName + "</color>", 36, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(620f, 60f));
            var bar = UIFactory.Bar(screen, new Color(0f, 0f, 0f, 0.4f), new Color(0.45f, 0.9f, 1f), out progressFill);
            UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -115f), new Vector2(560f, 40f));
            var flag = UIFactory.Image("Goal", bar.transform, UITheme.Gold, SpriteFactory.Star);
            UIFactory.Place(flag.rectTransform, new Vector2(1f, 0.5f), new Vector2(10f, 0f), new Vector2(60f, 60f));
            progressMarker = UIFactory.Image("Marker", bar.transform, Color.white, SpriteFactory.BalloonIcon).rectTransform;
            UIFactory.Place(progressMarker, new Vector2(0f, 0.5f), new Vector2(0f, 6f), new Vector2(56f, 56f));

            var coinBadge = UIFactory.CurrencyBadge(screen, false, out var coinRect);
            UIFactory.Place(coinRect, new Vector2(1f, 1f), new Vector2(-30f, -90f), new Vector2(250f, 90f), new Vector2(1f, 0.5f));
            coinText = coinBadge;
            coinText.text = "0";

            // Hearts
            heartsRoot = UIFactory.Rect("Hearts", screen);
            UIFactory.Place(heartsRoot, new Vector2(0f, 1f), new Vector2(40f, -190f), new Vector2(600f, 70f), new Vector2(0f, 0.5f));

            // Shield indicator
            var shieldBg = UIFactory.Image("Shield", screen, new Color(0f, 0f, 0f, 0.35f), SpriteFactory.Pill);
            UIFactory.Place(shieldBg.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -265f), new Vector2(220f, 70f), new Vector2(0f, 0.5f));
            var shieldIcon = UIFactory.Image("Icon", shieldBg.transform, new Color(0.35f, 0.95f, 1f), SpriteFactory.Shield);
            UIFactory.Place(shieldIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(56f, 56f));
            shieldText = UIFactory.Label(shieldBg.transform, "", 34, Color.white, TextAnchor.MiddleLeft, true);
            UIFactory.Stretch(shieldText.rectTransform, 80f, 10f, 0, 0);
            shieldRoot = shieldBg.gameObject;
            shieldRoot.SetActive(false);

            // Power-up timers
            powerUpRow = UIFactory.Rect("PowerUps", screen);
            UIFactory.Place(powerUpRow, new Vector2(1f, 1f), new Vector2(-30f, -190f), new Vector2(500f, 90f), new Vector2(1f, 0.5f));

            // Boss bar
            var bossBg = UIFactory.Rect("Boss", screen);
            UIFactory.Place(bossBg, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(820f, 120f));
            bossName = UIFactory.Label(bossBg, "", 40, new Color(1f, 0.85f, 0.85f), TextAnchor.UpperCenter, true);
            UIFactory.Stretch(bossName.rectTransform, 0, 0, 0, 60);
            var bossBar = UIFactory.Bar(bossBg, new Color(0f, 0f, 0f, 0.5f), new Color(1f, 0.35f, 0.35f), out bossFill);
            UIFactory.Place(bossBar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(800f, 44f));
            bossPhase = UIFactory.Label(bossBg, "", 30, UITheme.TextDim, TextAnchor.LowerCenter);
            UIFactory.Place(bossPhase.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -10f), new Vector2(800f, 40f));
            bossRoot = bossBg.gameObject;
            bossRoot.SetActive(false);

            // Ability button
            if (ability != null)
            {
                var ab = UIFactory.Button(screen, "", new Color(0.45f, 0.35f, 0.95f), () => ability.Activate(), 40);
                var abRt = (RectTransform)ab.transform;
                UIFactory.Place(abRt, new Vector2(1f, 0f), new Vector2(-130f, 170f), new Vector2(200f, 200f));
                ((Image)ab.targetGraphic).sprite = SpriteFactory.CircleSprite;
                ((Image)ab.targetGraphic).type = Image.Type.Simple;
                abilityFill = UIFactory.Image("Cooldown", ab.transform, new Color(0f, 0f, 0f, 0.5f), SpriteFactory.CircleSprite);
                abilityFill.type = Image.Type.Filled;
                abilityFill.fillMethod = Image.FillMethod.Radial360;
                UIFactory.Stretch(abilityFill.rectTransform);
                abilityLabel = UIFactory.Label(ab.transform, ability.ButtonLabel, 40, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(abilityLabel.rectTransform);
                abilityRoot = ab.gameObject;
            }

            // Banner
            var bannerRoot = UIFactory.Rect("Banner", screen);
            UIFactory.Place(bannerRoot, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1000f, 320f));
            bannerGroup = bannerRoot.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            bannerText = UIFactory.Label(bannerRoot, "", 84, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(bannerText.rectTransform, 0, 0, 0, 120);
            var outline = bannerText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.15f, 0.35f, 0.8f);
            outline.effectDistance = new Vector2(3f, -3f);
            bannerSub = UIFactory.Label(bannerRoot, "", 40, new Color(1f, 0.95f, 0.8f), TextAnchor.UpperCenter);
            UIFactory.Stretch(bannerSub.rectTransform, 0, 0, 200, 0);
            bannerGroup.alpha = 0f;

            ToastView.Create(screen, -440f);
        }

        void OnEnable()
        {
            GameEvents.HealthChanged += OnHealth;
            GameEvents.LevelCoinsChanged += OnCoins;
            GameEvents.LevelProgressChanged += OnProgress;
            GameEvents.PowerUpUpdated += OnPowerUp;
            GameEvents.ShieldChanged += OnShield;
            GameEvents.BossHealthChanged += OnBossHealth;
            GameEvents.BossPhaseStarted += OnBossPhase;
            GameEvents.BalloonDamaged += OnDamaged;
            GameEvents.AbilityCooldownChanged += OnAbility;
        }

        void OnDisable()
        {
            GameEvents.HealthChanged -= OnHealth;
            GameEvents.LevelCoinsChanged -= OnCoins;
            GameEvents.LevelProgressChanged -= OnProgress;
            GameEvents.PowerUpUpdated -= OnPowerUp;
            GameEvents.ShieldChanged -= OnShield;
            GameEvents.BossHealthChanged -= OnBossHealth;
            GameEvents.BossPhaseStarted -= OnBossPhase;
            GameEvents.BalloonDamaged -= OnDamaged;
            GameEvents.AbilityCooldownChanged -= OnAbility;
        }

        void OnHealth(int current, int max)
        {
            while (hearts.Count < max)
            {
                var h = UIFactory.Image("Heart", heartsRoot, Color.white, SpriteFactory.Heart);
                UIFactory.Place(h.rectTransform, new Vector2(0f, 0.5f), new Vector2(35f + hearts.Count * 66f, 0f), new Vector2(60f, 60f));
                hearts.Add(h);
            }
            for (int i = 0; i < hearts.Count; i++)
            {
                hearts[i].gameObject.SetActive(i < max);
                bool full = i < current;
                hearts[i].color = full ? new Color(1f, 0.3f, 0.4f) : new Color(0.2f, 0.2f, 0.25f, 0.6f);
                if (full && hearts[i].rectTransform.localScale.x < 0.99f) hearts[i].rectTransform.localScale = Vector3.one;
            }
            if (current < hearts.Count && current >= 0)
            {
                var lost = hearts[Mathf.Clamp(current, 0, hearts.Count - 1)].rectTransform;
                TweenRunner.Run(lost, 0.3f, t => lost.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.4f));
            }
        }

        void OnCoins(int coins)
        {
            if (coins == displayedCoins) return;
            displayedCoins = coins;
            coinText.text = UIFactory.FormatNumber(coins);
        }

        void OnProgress(float p)
        {
            progressFill.fillAmount = p;
            var parent = (RectTransform)progressMarker.parent;
            progressMarker.anchoredPosition = new Vector2(parent.rect.width * p, 6f);
        }

        void OnPowerUp(PowerUpType type, float remaining, float duration)
        {
            if (type == PowerUpType.EnergyShield) return; // shown by the shield indicator
            if (!powerUpIcons.TryGetValue(type, out var entry))
            {
                var bg = UIFactory.Image(type.ToString(), powerUpRow, new Color(0f, 0f, 0f, 0.4f), SpriteFactory.CircleSprite);
                var color = GameServices.Instance.Database.GetPowerUp(type)?.color ?? Color.white;
                var fill = UIFactory.Image("Fill", bg.transform, color, SpriteFactory.Ring);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                UIFactory.Stretch(fill.rectTransform);
                Sprite icon = type == PowerUpType.CoinMagnet ? SpriteFactory.Coin : type == PowerUpType.SkyRocket ? SpriteFactory.Arrow : type == PowerUpType.DoubleCoins ? SpriteFactory.Coin : SpriteFactory.Heart;
                var ic = UIFactory.Image("Icon", bg.transform, color, icon);
                UIFactory.Stretch(ic.rectTransform, 22, 22, 22, 22);
                if (type == PowerUpType.SkyRocket) ic.rectTransform.localEulerAngles = new Vector3(0, 0, -90);
                if (type == PowerUpType.DoubleCoins)
                {
                    var x2 = UIFactory.Label(bg.transform, "x2", 30, Color.white, TextAnchor.MiddleCenter, true);
                    UIFactory.Stretch(x2.rectTransform);
                }
                entry = (bg.gameObject, fill);
                powerUpIcons[type] = entry;
            }
            bool active = remaining > 0f;
            if (entry.root.activeSelf != active) entry.root.SetActive(active);
            entry.fill.fillAmount = duration > 0 ? remaining / duration : 0f;
            // Layout active icons right-to-left
            int index = 0;
            foreach (var kv in powerUpIcons)
            {
                if (!kv.Value.root.activeSelf) continue;
                UIFactory.Place((RectTransform)kv.Value.root.transform, new Vector2(1f, 0.5f), new Vector2(-45f - index * 100f, 0f), new Vector2(90f, 90f));
                index++;
            }
        }

        void OnShield(float remaining)
        {
            bool active = remaining != 0f;
            if (shieldRoot.activeSelf != active) shieldRoot.SetActive(active);
            if (active) shieldText.text = remaining > 0f ? remaining.ToString("0.0") + "s" : "Ready";
        }

        void OnBossHealth(float normalized)
        {
            bool show = normalized >= 0f;
            if (bossRoot.activeSelf != show) bossRoot.SetActive(show);
            if (show) bossFill.fillAmount = normalized;
        }

        void OnBossPhase(string phase) { bossPhase.text = phase; }

        public void SetBossName(string text) { bossName.text = text; }

        void OnDamaged(DamageInfo info) { flashTimer = 0.35f; }

        void OnAbility(float readiness)
        {
            if (abilityFill == null) return;
            abilityFill.fillAmount = 1f - readiness;
            abilityLabel.color = readiness >= 1f ? Color.white : new Color(1f, 1f, 1f, 0.6f);
        }

        public void ShowBanner(string title, string subtitle, float duration = 2.2f)
        {
            bannerText.text = title;
            bannerSub.text = subtitle;
            bannerTimer = duration;
        }

        public void SetAbilityVisible(bool visible) { if (abilityRoot != null) abilityRoot.SetActive(visible); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (bannerTimer > 0f)
            {
                bannerTimer -= dt;
                bannerGroup.alpha = Mathf.Clamp01(Mathf.Min(bannerTimer * 2.5f, 1f));
            }
            else bannerGroup.alpha = 0f;

            if (flashTimer > 0f) flashTimer -= dt;
            var c = damageFlash.color;
            c.a = Mathf.Clamp01(flashTimer / 0.35f) * 0.65f;
            damageFlash.color = c;
        }
    }
}
