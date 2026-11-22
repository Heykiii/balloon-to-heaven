using System.Collections.Generic;
using BalloonToHeaven.Audio;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Core;
using BalloonToHeaven.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonToHeaven.UI
{
    /// <summary>
    /// Balloon shop: grid of all 20 balloons, rotating 3D preview of the selection (any of the
    /// 5 visual stages can be previewed), stats, ability, and validated Buy / Equip / Upgrade.
    /// </summary>
    public sealed class BalloonShopScreen : MonoBehaviour
    {
        RectTransform screen;
        BalloonPreview preview;
        BalloonData selected;
        int previewStage;

        Text nameText, rarityText, descText, abilityText, stageText;
        readonly List<(Image fill, Text value)> statBars = new List<(Image, Text)>();
        readonly List<Image> stageDots = new List<Image>();
        Button primaryButton, secondaryButton;
        readonly Dictionary<string, (Image frame, Text status, Image lockIcon)> cards = new Dictionary<string, (Image, Text, Image)>();

        void Start()
        {
            var services = GameServices.Instance;
            services.Audio.PlayMusic(MusicCue.Menu, null);
            screen = UIFactory.CreateScreen("BalloonShop", 0);
            ScreenCommon.GradientBackground(screen, new Color(0.16f, 0.12f, 0.36f), new Color(0.45f, 0.35f, 0.75f));
            ScreenCommon.Header(screen, "Balloons", () => services.LoadScene(SceneId.MainMenu));
            CurrencyBar.Create(screen, -80f);

            BuildDetail();
            BuildGrid();
            ToastView.Create(screen, -300f);
            Select(services.Shop.Equipped);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) GameServices.Instance.LoadScene(SceneId.MainMenu);
        }

        // ------------------------------------------------------------------ layout

        void BuildDetail()
        {
            var panel = UIFactory.Panel(screen);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1020f, 900f), new Vector2(0.5f, 1f));
            var pt = panel.transform;

            var glow = UIFactory.Image("Glow", pt, new Color(1f, 1f, 1f, 0.18f), SpriteFactory.SoftGlow);
            UIFactory.Place(glow.rectTransform, new Vector2(0f, 1f), new Vector2(240f, -300f), new Vector2(520f, 520f));
            preview = BalloonPreview.Create(pt, new Vector2(0f, 1f), new Vector2(240f, -300f), new Vector2(480f, 480f));

            nameText = UIFactory.Label(pt, "", 56, Color.white, TextAnchor.UpperLeft, true);
            UIFactory.Place(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(490f, -40f), new Vector2(510f, 80f), new Vector2(0f, 1f));
            rarityText = UIFactory.Label(pt, "", 34, Color.white, TextAnchor.UpperLeft, true);
            UIFactory.Place(rarityText.rectTransform, new Vector2(0f, 1f), new Vector2(490f, -115f), new Vector2(510f, 50f), new Vector2(0f, 1f));

            string[] labels = { "Durability", "Speed", "Armor", "Magnet" };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = -200f - i * 72f;
                var l = UIFactory.Label(pt, labels[i], 30, UITheme.TextDim, TextAnchor.MiddleLeft);
                UIFactory.Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(490f, y), new Vector2(170f, 50f), new Vector2(0f, 0.5f));
                var bar = UIFactory.Bar(pt, new Color(0f, 0f, 0f, 0.35f), UITheme.Secondary, out var fill);
                UIFactory.Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(660f, y), new Vector2(240f, 34f), new Vector2(0f, 0.5f));
                var v = UIFactory.Label(pt, "", 30, Color.white, TextAnchor.MiddleLeft, true);
                UIFactory.Place(v.rectTransform, new Vector2(0f, 1f), new Vector2(910f, y), new Vector2(100f, 50f), new Vector2(0f, 0.5f));
                statBars.Add((fill, v));
            }

            stageText = UIFactory.Label(pt, "", 32, Color.white, TextAnchor.MiddleLeft, true);
            UIFactory.Place(stageText.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -575f), new Vector2(420f, 50f), new Vector2(0f, 0.5f));
            for (int s = 1; s <= GameConstants.MaxBalloonStage; s++)
            {
                int stage = s;
                var dot = UIFactory.Image("Stage" + s, pt, Color.white, SpriteFactory.CircleSprite, true);
                UIFactory.Place(dot.rectTransform, new Vector2(0f, 1f), new Vector2(60f + (s - 1) * 84f, -640f), new Vector2(68f, 68f));
                var btn = dot.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => { GameServices.Instance.Audio.PlaySfx(SfxId.Click); PreviewStage(stage); });
                var num = UIFactory.Label(dot.transform, s.ToString(), 32, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(num.rectTransform);
                stageDots.Add(dot);
            }

            abilityText = UIFactory.Label(pt, "", 30, Color.white, TextAnchor.UpperLeft);
            UIFactory.Place(abilityText.rectTransform, new Vector2(0f, 1f), new Vector2(490f, -500f), new Vector2(510f, 200f), new Vector2(0f, 1f));
            descText = UIFactory.Label(pt, "", 28, UITheme.TextDim, TextAnchor.UpperLeft);
            UIFactory.Place(descText.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -700f), new Vector2(940f, 70f), new Vector2(0f, 1f));

            primaryButton = UIFactory.Button(pt, "", UITheme.Primary, OnPrimary, 42);
            UIFactory.Place((RectTransform)primaryButton.transform, new Vector2(0.5f, 0f), new Vector2(-235f, 80f), new Vector2(450f, 120f));
            secondaryButton = UIFactory.Button(pt, "", UITheme.Secondary, OnSecondary, 42);
            UIFactory.Place((RectTransform)secondaryButton.transform, new Vector2(0.5f, 0f), new Vector2(235f, 80f), new Vector2(450f, 120f));
        }

        void BuildGrid()
        {
            var services = GameServices.Instance;
            var scroll = UIFactory.ScrollView(screen, out var content);
            UIFactory.Stretch((RectTransform)scroll.transform, 30f, 30f, 1090f, 30f);
            var list = new List<BalloonData>(services.Database.balloons);
            list.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            const int columns = 4;
            const float cell = 250f, cellH = 300f;
            int rows = Mathf.CeilToInt(list.Count / (float)columns);
            content.sizeDelta = new Vector2(0f, rows * cellH + 20f);

            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                int col = i % columns, row = i / columns;
                var frame = UIFactory.Image("Card_" + b.id, content, UITheme.PanelLight, SpriteFactory.RoundedRect, true);
                UIFactory.Place(frame.rectTransform, new Vector2(0.5f, 1f), new Vector2((col - (columns - 1) * 0.5f) * cell, -20f - row * cellH), new Vector2(cell - 16f, cellH - 16f), new Vector2(0.5f, 1f));
                frame.gameObject.AddComponent<Button>().onClick.AddListener(() => { services.Audio.PlaySfx(SfxId.Click); Select(b); });

                var rarity = UIFactory.Image("Rarity", frame.transform, BalloonData.RarityColor(b.rarity), SpriteFactory.Pill);
                UIFactory.Place(rarity.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(150f, 14f));

                var circle = UIFactory.Image("Circle", frame.transform, Color.white, SpriteFactory.CircleSprite);
                circle.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                UIFactory.Place(circle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(140f, 140f));
                var pattern = UIFactory.Image("Pattern", circle.transform, Color.white, ScreenCommon.BalloonSprite(b));
                UIFactory.Stretch(pattern.rectTransform);

                var name = UIFactory.Label(frame.transform, b.displayName, 28, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(cell - 30f, 44f));
                name.resizeTextForBestFit = true; name.resizeTextMinSize = 18; name.resizeTextMaxSize = 28;
                var status = UIFactory.Label(frame.transform, "", 26, UITheme.Gold, TextAnchor.MiddleCenter, true);
                UIFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(cell - 30f, 40f));
                var lockIcon = UIFactory.Image("Lock", frame.transform, Color.white, SpriteFactory.Lock);
                UIFactory.Place(lockIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(80f, 80f));
                cards[b.id] = (frame, status, lockIcon);
            }
            RefreshCards();
        }

        // ------------------------------------------------------------------ state

        void RefreshCards()
        {
            var shop = GameServices.Instance.Shop;
            foreach (var b in GameServices.Instance.Database.balloons)
            {
                if (!cards.TryGetValue(b.id, out var c)) continue;
                bool owned = shop.IsOwned(b.id);
                bool unlocked = shop.IsUnlockedByProgress(b);
                c.lockIcon.gameObject.SetActive(!owned && !unlocked);
                if (shop.IsEquipped(b.id)) { c.status.text = "Equipped"; c.status.color = UITheme.Success; }
                else if (owned) { c.status.text = "Stage " + shop.GetStage(b.id); c.status.color = Color.white; }
                else if (!unlocked) { c.status.text = "World " + b.unlockWorld; c.status.color = UITheme.TextDim; }
                else { c.status.text = b.price == 0 ? "Free" : "● " + UIFactory.FormatNumber(b.price); c.status.color = UITheme.Gold; }
                c.frame.color = selected == b ? new Color(1f, 0.85f, 0.35f, 0.5f) : UITheme.PanelLight;
            }
        }

        void Select(BalloonData data)
        {
            if (data == null) return;
            selected = data;
            previewStage = GameServices.Instance.Shop.GetStage(data.id);
            Refresh();
        }

        void PreviewStage(int stage)
        {
            previewStage = stage;
            Refresh();
        }

        void Refresh()
        {
            var services = GameServices.Instance;
            var shop = services.Shop;
            var b = selected;
            bool owned = shop.IsOwned(b.id);
            int ownedStage = shop.GetStage(b.id);

            preview.Show(b, previewStage, null);
            nameText.text = b.displayName;
            rarityText.text = b.rarity.ToString().ToUpperInvariant();
            rarityText.color = BalloonData.RarityColor(b.rarity);
            descText.text = b.description;
            abilityText.text = "<b><color=#FFD86B>" + b.abilityName + "</color></b>\n" + b.abilityDescription;

            var stats = shop.GetStats(b, previewStage);
            SetStat(0, stats.maxHealth / 10f, stats.maxHealth + " HP");
            SetStat(1, (stats.speedMultiplier - 0.7f) / 0.9f, "x" + stats.speedMultiplier.ToString("0.00"));
            SetStat(2, stats.armor / 8f, stats.armor.ToString());
            SetStat(3, stats.magnetRadius / 4f, stats.magnetRadius.ToString("0.0"));

            stageText.text = previewStage == ownedStage || !owned ? "Visual stage " + previewStage : "Previewing stage " + previewStage + " (yours: " + ownedStage + ")";
            for (int i = 0; i < stageDots.Count; i++)
            {
                int s = i + 1;
                Color c = s <= ownedStage && owned ? UITheme.Success : new Color(1f, 1f, 1f, 0.25f);
                if (s == previewStage) c = UITheme.Gold;
                stageDots[i].color = c;
                stageDots[i].transform.localScale = Vector3.one * (s == previewStage ? 1.15f : 1f);
            }

            // Buttons
            var primaryLabel = UIFactory.ButtonLabel(primaryButton);
            var secondaryLabel = UIFactory.ButtonLabel(secondaryButton);
            if (!owned)
            {
                bool unlocked = shop.IsUnlockedByProgress(b);
                if (!unlocked)
                {
                    primaryLabel.text = "Unlocks in World " + b.unlockWorld;
                    UIFactory.SetButtonEnabled(primaryButton, false, UITheme.Primary);
                }
                else
                {
                    primaryLabel.text = "Buy  ● " + UIFactory.FormatNumber(b.price);
                    UIFactory.SetButtonEnabled(primaryButton, services.Economy.CanAffordCoins(b.price), UITheme.Primary);
                    primaryButton.interactable = true; // tapping when short explains why
                }
                secondaryButton.gameObject.SetActive(false);
            }
            else
            {
                bool equipped = shop.IsEquipped(b.id);
                primaryLabel.text = equipped ? "Equipped" : "Equip";
                UIFactory.SetButtonEnabled(primaryButton, !equipped, UITheme.Success);
                secondaryButton.gameObject.SetActive(true);
                if (ownedStage >= GameConstants.MaxBalloonStage)
                {
                    secondaryLabel.text = "Max stage";
                    UIFactory.SetButtonEnabled(secondaryButton, false, UITheme.Secondary);
                }
                else
                {
                    int cost = shop.GetUpgradeCost(b.id);
                    secondaryLabel.text = "Stage " + (ownedStage + 1) + "  ● " + UIFactory.FormatNumber(cost);
                    UIFactory.SetButtonEnabled(secondaryButton, services.Economy.CanAffordCoins(cost), UITheme.Secondary);
                    secondaryButton.interactable = true;
                }
            }
            RefreshCards();
        }

        void SetStat(int index, float normalized, string value)
        {
            statBars[index].fill.fillAmount = Mathf.Clamp(normalized, 0.04f, 1f);
            statBars[index].value.text = value;
        }

        // ------------------------------------------------------------------ actions

        void OnPrimary()
        {
            var services = GameServices.Instance;
            var shop = services.Shop;
            if (!shop.IsOwned(selected.id))
            {
                var r = shop.TryPurchase(selected.id);
                if (r == ShopResult.Success)
                {
                    services.Audio.PlaySfx(SfxId.Purchase);
                    shop.TryEquip(selected.id);
                    GameEvents.RaiseToast(selected.displayName + " is yours!");
                    services.Achievements.Evaluate();
                    Celebrate();
                }
                else ScreenCommon.Deny(BalloonShopManager.Describe(r));
            }
            else if (!shop.IsEquipped(selected.id))
            {
                if (shop.TryEquip(selected.id) == ShopResult.Success)
                {
                    services.Audio.PlaySfx(SfxId.Upgrade, 0.6f);
                    GameEvents.RaiseToast(selected.displayName + " equipped");
                }
            }
            previewStage = shop.GetStage(selected.id);
            Refresh();
        }

        void OnSecondary()
        {
            var services = GameServices.Instance;
            var r = services.Shop.TryUpgrade(selected.id);
            if (r == ShopResult.Success)
            {
                services.Audio.PlaySfx(SfxId.Upgrade);
                int stage = services.Shop.GetStage(selected.id);
                GameEvents.RaiseToast(selected.displayName + " reached stage " + stage + "!");
                services.Achievements.Evaluate();
                previewStage = stage;
                Celebrate();
            }
            else ScreenCommon.Deny(BalloonShopManager.Describe(r));
            Refresh();
        }

        void Celebrate()
        {
            for (int i = 0; i < 14; i++)
            {
                var s = UIFactory.Image("Confetti", screen, Color.HSVToRGB(Random.value, 0.6f, 1f), i % 2 == 0 ? SpriteFactory.Star : SpriteFactory.CircleSprite);
                var rt = s.rectTransform;
                UIFactory.Place(rt, new Vector2(0.5f, 1f), new Vector2(-270f, -470f), new Vector2(50f, 50f));
                Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(200f, 420f);
                Vector2 origin = rt.anchoredPosition;
                TweenRunner.Run(rt, 0.9f, t =>
                {
                    rt.anchoredPosition = origin + dir * Ease.OutCubic(t) + Vector2.down * (t * t * 200f);
                    s.color = new Color(s.color.r, s.color.g, s.color.b, 1f - t);
                }, () => Destroy(s.gameObject));
            }
        }
    }
}
