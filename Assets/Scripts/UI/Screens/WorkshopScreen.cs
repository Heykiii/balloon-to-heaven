using System.Collections.Generic;
using BalloonToHeaven.Audio;
using BalloonToHeaven.Core;
using BalloonToHeaven.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonToHeaven.UI
{
    /// <summary>
    /// Workshop: four permanent upgrade categories (5 levels each, paid with coins) that apply to
    /// every balloon, plus gem-priced cosmetic trails.
    /// </summary>
    public sealed class WorkshopScreen : MonoBehaviour
    {
        RectTransform screen;
        readonly List<System.Action> refreshers = new List<System.Action>();
        BalloonPreview preview;

        void Start()
        {
            var services = GameServices.Instance;
            services.Audio.PlayMusic(MusicCue.Menu, null);
            screen = UIFactory.CreateScreen("Workshop", 0);
            ScreenCommon.GradientBackground(screen, new Color(0.22f, 0.14f, 0.3f), new Color(0.75f, 0.45f, 0.35f));
            ScreenCommon.Header(screen, "Workshop", () => services.LoadScene(SceneId.MainMenu));
            CurrencyBar.Create(screen, -80f);

            var scroll = UIFactory.ScrollView(screen, out var content);
            UIFactory.Stretch((RectTransform)scroll.transform, 30f, 30f, 170f, 30f);

            float y = -20f;
            var intro = UIFactory.Label(content, "Permanent upgrades for every balloon", 38, UITheme.TextDim);
            UIFactory.Place(intro.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 30f), new Vector2(980f, 60f));
            y -= 90f;

            for (int c = 0; c < GameConstants.WorkshopCategoryCount; c++)
            {
                BuildCategory(content, (WorkshopCategory)c, y);
                y -= 280f;
            }

            y -= 30f;
            var trailsTitle = UIFactory.Label(content, "Trails  <size=34><color=#9FDcFF>(Sky Gems)</color></size>", 56, Color.white, TextAnchor.MiddleLeft, true);
            UIFactory.Place(trailsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 40f), new Vector2(960f, 80f));
            y -= 110f;

            preview = BalloonPreview.Create(content, new Vector2(0.5f, 1f), new Vector2(0f, y - 200f), new Vector2(400f, 400f));
            ShowPreview();
            y -= 420f;

            BuildTrailRow(content, null, y);
            y -= 170f;
            foreach (var trail in services.Database.trails)
            {
                BuildTrailRow(content, trail, y);
                y -= 170f;
            }
            content.sizeDelta = new Vector2(0f, -y + 40f);

            ToastView.Create(screen, -300f);
            RefreshAll();
            GameEvents.CurrencyChanged += OnCurrency;
        }

        void OnDestroy() { GameEvents.CurrencyChanged -= OnCurrency; }
        void OnCurrency(int c, int g) => RefreshAll();
        void RefreshAll() { foreach (var r in refreshers) r(); }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) GameServices.Instance.LoadScene(SceneId.MainMenu);
        }

        void ShowPreview()
        {
            var services = GameServices.Instance;
            var b = services.Shop.Equipped;
            preview.Show(b, services.Shop.GetStage(b.id), services.Database.GetTrail(services.Upgrades.EquippedTrail));
        }

        static Sprite CategoryIcon(WorkshopCategory c)
        {
            switch (c)
            {
                case WorkshopCategory.Durability: return SpriteFactory.Heart;
                case WorkshopCategory.CoinMagnet: return SpriteFactory.Coin;
                case WorkshopCategory.Movement: return SpriteFactory.Arrow;
                default: return SpriteFactory.Shield;
            }
        }

        static Color CategoryColor(WorkshopCategory c)
        {
            switch (c)
            {
                case WorkshopCategory.Durability: return new Color(1f, 0.4f, 0.45f);
                case WorkshopCategory.CoinMagnet: return UITheme.Gold;
                case WorkshopCategory.Movement: return new Color(0.4f, 0.9f, 0.6f);
                default: return new Color(0.45f, 0.75f, 1f);
            }
        }

        void BuildCategory(RectTransform content, WorkshopCategory category, float y)
        {
            var services = GameServices.Instance;
            var upgrades = services.Upgrades;
            var card = UIFactory.Panel(content);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(980f, 260f), new Vector2(0.5f, 1f));
            var ct = card.transform;

            var iconBg = UIFactory.Image("IconBg", ct, new Color(0f, 0f, 0f, 0.3f), SpriteFactory.CircleSprite);
            UIFactory.Place(iconBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(160f, 160f));
            var icon = UIFactory.Image("Icon", iconBg.transform, CategoryColor(category), CategoryIcon(category));
            UIFactory.Stretch(icon.rectTransform, 30f, 30f, 30f, 30f);
            if (category == WorkshopCategory.Movement) icon.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);

            var name = UIFactory.Label(ct, WorkshopConfig.CategoryName(category), 46, Color.white, TextAnchor.UpperLeft, true);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(220f, -30f), new Vector2(420f, 60f), new Vector2(0f, 1f));
            var desc = UIFactory.Label(ct, "", 30, UITheme.TextDim, TextAnchor.UpperLeft);
            UIFactory.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(220f, -95f), new Vector2(420f, 90f), new Vector2(0f, 1f));

            var pips = new List<Image>();
            for (int i = 0; i < GameConstants.MaxWorkshopLevel; i++)
            {
                var pip = UIFactory.Image("Pip", ct, Color.white, SpriteFactory.Pill);
                UIFactory.Place(pip.rectTransform, new Vector2(0f, 0f), new Vector2(250f + i * 76f, 45f), new Vector2(64f, 24f));
                pips.Add(pip);
            }

            var button = UIFactory.Button(ct, "", UITheme.Primary, () =>
            {
                var r = upgrades.TryUpgrade(category);
                if (r == UpgradeResult.Success)
                {
                    services.Audio.PlaySfx(SfxId.Upgrade);
                    GameEvents.RaiseToast(WorkshopConfig.CategoryName(category) + " level " + upgrades.GetLevel(category) + "!");
                    services.Achievements.Evaluate();
                }
                else ScreenCommon.Deny(r == UpgradeResult.MaxLevel ? "Already at maximum level" : "Not enough coins");
                RefreshAll();
            }, 38);
            UIFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-170f, 0f), new Vector2(290f, 150f));

            refreshers.Add(() =>
            {
                int level = upgrades.GetLevel(category);
                bool max = level >= GameConstants.MaxWorkshopLevel;
                desc.text = (level > 0 ? "Now: " + upgrades.Config.DescribeLevel(category, level) : "Not upgraded yet")
                    + (max ? "\n<color=#7CFF9E>Fully upgraded</color>" : "\nNext: " + upgrades.Config.DescribeLevel(category, level + 1));
                for (int i = 0; i < pips.Count; i++) pips[i].color = i < level ? CategoryColor(category) : new Color(1f, 1f, 1f, 0.2f);
                var label = UIFactory.ButtonLabel(button);
                if (max)
                {
                    label.text = "MAX";
                    UIFactory.SetButtonEnabled(button, false, UITheme.Primary);
                }
                else
                {
                    int cost = upgrades.GetNextCost(category);
                    label.text = "Upgrade\n● " + UIFactory.FormatNumber(cost);
                    UIFactory.SetButtonEnabled(button, services.Economy.CanAffordCoins(cost), UITheme.Primary);
                    button.interactable = true;
                }
            });
        }

        void BuildTrailRow(RectTransform content, TrailCosmetic trail, float y)
        {
            var services = GameServices.Instance;
            var upgrades = services.Upgrades;
            var row = UIFactory.Image("Trail", content, UITheme.PanelLight, SpriteFactory.RoundedRect);
            UIFactory.Place(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(980f, 150f), new Vector2(0.5f, 1f));
            var rt = row.transform;

            var swatch = UIFactory.Image("Swatch", rt, Color.white,
                trail != null ? SpriteFactory.VerticalGradient(trail.colorA, trail.colorB) : SpriteFactory.VerticalGradient(Color.gray, Color.black));
            UIFactory.Place(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(90f, 0f), new Vector2(90f, 110f));
            var name = UIFactory.Label(rt, trail != null ? trail.displayName : "No trail", 42, Color.white, TextAnchor.MiddleLeft, true);
            UIFactory.Stretch(name.rectTransform, 170f, 360f, 0f, 0f);

            var button = UIFactory.Button(rt, "", UITheme.Secondary, () =>
            {
                string id = trail != null ? trail.id : "";
                if (trail != null && !upgrades.OwnsTrail(trail.id))
                {
                    var r = upgrades.TryBuyTrail(trail);
                    if (r == UpgradeResult.Success) { services.Audio.PlaySfx(SfxId.Purchase); GameEvents.RaiseToast(trail.displayName + " unlocked!"); }
                    else { ScreenCommon.Deny("Not enough Sky Gems"); return; }
                }
                else
                {
                    upgrades.TryEquipTrail(id);
                    services.Audio.PlaySfx(SfxId.Click);
                }
                ShowPreview();
                RefreshAll();
            }, 36);
            UIFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-170f, 0f), new Vector2(290f, 110f));

            refreshers.Add(() =>
            {
                string id = trail != null ? trail.id : "";
                bool equipped = (upgrades.EquippedTrail ?? "") == id;
                bool owned = trail == null || upgrades.OwnsTrail(trail.id);
                var label = UIFactory.ButtonLabel(button);
                if (equipped) { label.text = "Equipped"; UIFactory.SetButtonEnabled(button, false, UITheme.Success); }
                else if (owned) { label.text = "Equip"; UIFactory.SetButtonEnabled(button, true, UITheme.Success); }
                else
                {
                    label.text = "◆ " + trail.gemPrice;
                    UIFactory.SetButtonEnabled(button, services.Economy.CanAffordGems(trail.gemPrice), UITheme.Secondary);
                    button.interactable = true;
                }
            });
        }
    }
}
