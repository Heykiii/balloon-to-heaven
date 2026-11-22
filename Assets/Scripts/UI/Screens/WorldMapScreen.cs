using System.Collections;
using System.Collections.Generic;
using BalloonToHeaven.Audio;
using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Worlds;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonToHeaven.UI
{
    /// <summary>
    /// Vertical, winding map of all 10 worlds (World 1 at the bottom, Heaven's Gate at the top).
    /// Each level node shows its stars or a lock; tapping an unlocked node opens the level popup
    /// where the player picks one of their owned balloons and starts the level.
    /// </summary>
    public sealed class WorldMapScreen : MonoBehaviour
    {
        const float NodeSpacing = 230f;
        const float WorldHeaderHeight = 300f;
        const float WorldGap = 80f;

        RectTransform screen;
        ScrollRect scroll;
        RectTransform content;
        float scrollTargetY;
        GameObject popup;

        float WorldBlockHeight => WorldHeaderHeight + GameConstants.LevelsPerWorld * NodeSpacing + WorldGap;

        void Start()
        {
            var services = GameServices.Instance;
            services.Audio.PlayMusic(MusicCue.Menu, null);
            screen = UIFactory.CreateScreen("WorldMap", 0);
            ScreenCommon.GradientBackground(screen, new Color(0.12f, 0.15f, 0.38f), new Color(0.35f, 0.65f, 0.95f));

            scroll = UIFactory.ScrollView(screen, out content);
            UIFactory.Stretch((RectTransform)scroll.transform, 0f, 0f, 170f, 190f);
            BuildMap();

            // Top bar drawn after the map so it stays above it.
            var topBar = UIFactory.Image("TopBar", screen, new Color(0.05f, 0.07f, 0.18f, 0.75f));
            UIFactory.Place(topBar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(2400f, 170f));
            ScreenCommon.Header(screen, "World Map", () => services.LoadScene(SceneId.MainMenu));
            CurrencyBar.Create(screen, -80f);

            var bottomBar = UIFactory.Image("BottomBar", screen, new Color(0.05f, 0.07f, 0.18f, 0.75f));
            UIFactory.Place(bottomBar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 95f), new Vector2(2400f, 190f));
            var shop = UIFactory.Button(screen, "Balloons", UITheme.Secondary, () => services.LoadScene(SceneId.BalloonShop), 42, SpriteFactory.BalloonIcon);
            UIFactory.Place((RectTransform)shop.transform, new Vector2(0.5f, 0f), new Vector2(-240f, 95f), new Vector2(440f, 130f));
            var workshop = UIFactory.Button(screen, "Workshop", new Color(0.55f, 0.4f, 0.9f), () => services.LoadScene(SceneId.Workshop), 42, SpriteFactory.Wrench);
            UIFactory.Place((RectTransform)workshop.transform, new Vector2(0.5f, 0f), new Vector2(240f, 95f), new Vector2(440f, 130f));

            ToastView.Create(screen, -300f);
            StartCoroutine(ScrollToCurrent());
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (popup != null) Destroy(popup);
                else GameServices.Instance.LoadScene(SceneId.MainMenu);
            }
        }

        // ------------------------------------------------------------------ map

        /// <summary>Y position (from content top, negative down) of a level node.</summary>
        float NodeY(int level)
        {
            int world = GameConstants.WorldOfLevel(level);
            int index = GameConstants.IndexInWorld(level);
            float total = GameConstants.WorldCount * WorldBlockHeight;
            // World 1 at the bottom: block top for world w = total - w * blockHeight
            float blockTop = total - world * WorldBlockHeight;
            // Inside a block, level 1 is at the bottom, header at the top.
            return -(blockTop + WorldHeaderHeight + (GameConstants.LevelsPerWorld - index + 0.5f) * NodeSpacing);
        }

        static float NodeX(int level) => Mathf.Sin(level * 1.15f) * 270f;

        void BuildMap()
        {
            var services = GameServices.Instance;
            var db = services.Database;
            var prog = services.Progression;
            float total = GameConstants.WorldCount * WorldBlockHeight + 120f;
            content.sizeDelta = new Vector2(0f, total);

            for (int w = 1; w <= GameConstants.WorldCount; w++)
            {
                var world = db.GetWorld(w);
                bool unlocked = prog.IsWorldUnlocked(w);
                float blockTop = GameConstants.WorldCount * WorldBlockHeight - w * WorldBlockHeight;

                var block = UIFactory.Image("World" + w, content, Color.white,
                    SpriteFactory.VerticalGradient(world != null ? world.skyTop : Color.blue, world != null ? world.skyBottom : Color.cyan));
                var brt = block.rectTransform;
                brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0.5f, 1f);
                brt.sizeDelta = new Vector2(-40f, WorldBlockHeight - WorldGap * 0.5f);
                brt.anchoredPosition = new Vector2(0f, -blockTop);
                if (!unlocked) block.color = new Color(0.45f, 0.45f, 0.5f);
                var mask = UIFactory.Image("Rounded", block.transform, new Color(1f, 1f, 1f, 0.08f), SpriteFactory.RoundedOutline);
                UIFactory.Stretch(mask.rectTransform);

                // Header
                var header = UIFactory.Image("Header", block.transform, new Color(0f, 0f, 0f, 0.3f), SpriteFactory.RoundedRect);
                UIFactory.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(940f, 230f));
                var name = UIFactory.Label(header.transform, "World " + w + "\n<size=60>" + (world != null ? world.worldName : "") + "</size>", 38, Color.white, TextAnchor.MiddleLeft, true);
                UIFactory.Stretch(name.rectTransform, 40f, 300f, 10f, 10f);
                if (unlocked)
                {
                    int stars = prog.WorldStars(w);
                    var st = UIFactory.Label(header.transform, "★ " + stars + "/18" + (prog.IsWorldCompleted(w) ? "\n<size=30>Completed</size>" : ""), 46, UITheme.Gold, TextAnchor.MiddleRight, true);
                    UIFactory.Stretch(st.rectTransform, 600f, 40f, 10f, 10f);
                }
                else
                {
                    var lockIcon = UIFactory.Image("Lock", header.transform, Color.white, SpriteFactory.Lock);
                    UIFactory.Place(lockIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(-110f, 0f), new Vector2(110f, 110f));
                }
                if (world != null && unlocked)
                {
                    var desc = UIFactory.Label(block.transform, world.description, 30, new Color(1f, 1f, 1f, 0.85f));
                    UIFactory.Place(desc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -285f), new Vector2(900f, 50f));
                }
            }

            // Path dots between consecutive nodes
            for (int level = 1; level < GameConstants.TotalLevels; level++)
            {
                Vector2 a = new Vector2(NodeX(level), NodeY(level));
                Vector2 b = new Vector2(NodeX(level + 1), NodeY(level + 1));
                bool reached = prog.IsLevelUnlocked(level + 1);
                int dots = GameConstants.IndexInWorld(level) == GameConstants.LevelsPerWorld ? 9 : 4;
                for (int d = 1; d <= dots; d++)
                {
                    var p = Vector2.Lerp(a, b, d / (float)(dots + 1));
                    var dot = UIFactory.Image("Dot", content, reached ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.3f), SpriteFactory.CircleSprite);
                    UIFactory.Place(dot.rectTransform, new Vector2(0.5f, 1f), p, new Vector2(22f, 22f));
                }
            }

            // Nodes
            for (int level = 1; level <= GameConstants.TotalLevels; level++) CreateNode(level);
        }

        void CreateNode(int level)
        {
            var services = GameServices.Instance;
            var prog = services.Progression;
            var data = services.Database.GetLevel(level);
            bool unlocked = prog.IsLevelUnlocked(level);
            bool completed = prog.IsLevelCompleted(level);
            bool boss = data != null && data.isBossLevel;
            bool current = level == prog.CurrentLevel && !completed;
            float size = boss ? 190f : 150f;
            var pos = new Vector2(NodeX(level), NodeY(level));

            if (current)
            {
                var ring = UIFactory.Image("Pulse", content, new Color(1f, 0.9f, 0.4f, 0.8f), SpriteFactory.Ring);
                UIFactory.Place(ring.rectTransform, new Vector2(0.5f, 1f), pos, new Vector2(size * 1.5f, size * 1.5f));
                ring.gameObject.AddComponent<UIPulse>();
                scrollTargetY = -pos.y;
            }

            Color color = !unlocked ? UITheme.Disabled : boss ? UITheme.Danger : completed ? UITheme.Success : UITheme.Primary;
            var node = UIFactory.Button(content, unlocked && !boss ? GameConstants.IndexInWorld(level).ToString() : "", color, () => OnNodeTapped(level), boss ? 64 : 58);
            var rt = (RectTransform)node.transform;
            ((Image)node.targetGraphic).sprite = SpriteFactory.CircleSprite;
            ((Image)node.targetGraphic).type = Image.Type.Simple;
            UIFactory.Place(rt, new Vector2(0.5f, 1f), pos, new Vector2(size, size));

            if (!unlocked)
            {
                var l = UIFactory.Image("Lock", rt, Color.white, SpriteFactory.Lock);
                UIFactory.Place(l.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.5f, size * 0.5f));
            }
            else if (boss)
            {
                var icon = UIFactory.Image("Boss", rt, Color.white, SpriteFactory.Shield);
                UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.55f, size * 0.55f));
            }
            if (completed) ScreenCommon.Stars(rt, prog.GetStars(level), 52f, new Vector2(0.5f, 0f), new Vector2(0f, -22f));
        }

        IEnumerator ScrollToCurrent()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var viewport = (RectTransform)scroll.transform;
            float viewH = viewport.rect.height;
            float contentH = content.rect.height;
            if (contentH <= viewH) yield break;
            float y = Mathf.Clamp(scrollTargetY - viewH * 0.55f, 0f, contentH - viewH);
            content.anchoredPosition = new Vector2(0f, y);
        }

        // ------------------------------------------------------------------ popup

        void OnNodeTapped(int level)
        {
            var services = GameServices.Instance;
            if (!services.Progression.IsLevelUnlocked(level))
            {
                ScreenCommon.Deny(GameConstants.IndexInWorld(level) == 1 ? "Defeat the previous world's boss to unlock this world" : "Complete the previous level first");
                return;
            }
            ShowLevelPopup(level);
        }

        void ShowLevelPopup(int level)
        {
            if (popup != null) Destroy(popup);
            var services = GameServices.Instance;
            var db = services.Database;
            var data = db.GetLevel(level);
            if (data == null) return;
            var world = db.GetWorld(data.worldNumber);

            var root = UIFactory.Rect("LevelPopup", screen);
            UIFactory.Stretch(root);
            popup = root.gameObject;
            var dim = UIFactory.Dimmer(root, 0.65f);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => Destroy(popup));

            var panel = UIFactory.Panel(root);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(960f, 1480f));
            UIFactory.PopIn(panel.rectTransform);
            var pt = panel.transform;

            var head = UIFactory.Label(pt, "World " + data.worldNumber + (world != null ? " · " + world.worldName : "") + " · Level " + data.indexInWorld, 36, UITheme.TextDim);
            UIFactory.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 60f));
            var title = UIFactory.Label(pt, data.levelName, 66, data.isBossLevel ? new Color(1f, 0.55f, 0.5f) : UITheme.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(900f, 100f));

            string details = data.hint;
            if (data.isBossLevel && data.boss != null) details = "<b>BOSS: " + data.boss.bossName + "</b>, " + data.boss.title + "\n" + data.hint;
            var hint = UIFactory.Label(pt, details, 34, Color.white);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(860f, 130f));

            var diff = UIFactory.Label(pt, "Difficulty " + Difficulty(data.difficulty), 34, UITheme.TextDim);
            UIFactory.Place(diff.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(860f, 50f));

            int best = services.Progression.GetStars(level);
            ScreenCommon.Stars(pt, best, 96f, new Vector2(0.5f, 1f), new Vector2(0f, -460f));

            // Star objectives
            string[] goals =
            {
                "Reach the top",
                "Collect " + data.coinsForSecondStar + " coins",
                data.ChallengeDescription(),
            };
            float y = -580f;
            for (int i = 0; i < 3; i++)
            {
                var row = UIFactory.Image("Goal", pt, UITheme.PanelLight, SpriteFactory.RoundedRect);
                UIFactory.Place(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(860f, 80f));
                var s = UIFactory.Image("Star", row.transform, i < best ? UITheme.Gold : new Color(1f, 1f, 1f, 0.35f), SpriteFactory.Star);
                UIFactory.Place(s.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(56f, 56f));
                var l = UIFactory.Label(row.transform, goals[i], 34, Color.white, TextAnchor.MiddleLeft);
                UIFactory.Stretch(l.rectTransform, 100f, 20f, 0f, 0f);
                y -= 92f;
            }
            int bestCoins = services.Save.Data.bestCoins[level - 1];
            if (bestCoins > 0)
            {
                var bc = UIFactory.Label(pt, "Best: " + bestCoins + " coins", 32, UITheme.TextDim);
                UIFactory.Place(bc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y + 20f), new Vector2(860f, 50f));
            }

            // Balloon selection (owned balloons only)
            var pick = UIFactory.Label(pt, "Choose your balloon", 38, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Place(pick.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 560f), new Vector2(860f, 60f));
            var selectedName = UIFactory.Label(pt, "", 32, UITheme.TextDim);
            UIFactory.Place(selectedName.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 290f), new Vector2(860f, 50f));
            var balloonScroll = UIFactory.ScrollView(pt, out var row2, false);
            UIFactory.Place((RectTransform)balloonScroll.transform, new Vector2(0.5f, 0f), new Vector2(0f, 420f), new Vector2(880f, 210f));
            row2.anchorMin = new Vector2(0f, 0f); row2.anchorMax = new Vector2(0f, 1f); row2.pivot = new Vector2(0f, 0.5f);

            var owned = new List<Balloons.BalloonData>();
            foreach (var b in db.balloons) if (services.Shop.IsOwned(b.id)) owned.Add(b);
            row2.sizeDelta = new Vector2(owned.Count * 190f + 20f, 0f);
            var frames = new List<Image>();
            string selected = services.Shop.Equipped.id;

            void Select(Balloons.BalloonData b)
            {
                selected = b.id;
                services.Shop.TryEquip(b.id);
                GameSession.SelectedBalloonId = b.id;
                var stats = services.Shop.GetStats(b);
                selectedName.text = b.displayName + "  ·  ♥ " + stats.maxHealth + "  ·  " + b.abilityName;
                for (int i = 0; i < owned.Count; i++) frames[i].color = owned[i].id == selected ? UITheme.Gold : new Color(1f, 1f, 1f, 0.15f);
            }

            for (int i = 0; i < owned.Count; i++)
            {
                var b = owned[i];
                var frame = UIFactory.Image("Frame", row2, Color.white, SpriteFactory.RoundedRect, true);
                UIFactory.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f + i * 190f, 0f), new Vector2(170f, 190f), new Vector2(0f, 0.5f));
                var btn = frame.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => { services.Audio.PlaySfx(SfxId.Click); Select(b); });
                var icon = UIFactory.Image("Balloon", frame.transform, Color.white, ScreenCommon.BalloonSprite(b));
                icon.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(120f, 120f));
                icon.sprite = SpriteFactory.CircleSprite;
                var pattern = UIFactory.Image("Pattern", icon.transform, Color.white, ScreenCommon.BalloonSprite(b));
                UIFactory.Stretch(pattern.rectTransform);
                var stage = UIFactory.Label(frame.transform, "Stage " + services.Shop.GetStage(b.id), 24, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Place(stage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(160f, 36f));
                frames.Add(frame);
            }
            var initial = services.Shop.Equipped;
            if (owned.Count > 0) Select(owned.Contains(initial) ? initial : owned[0]);

            var playBtn = UIFactory.Button(pt, "PLAY", UITheme.Primary, () =>
            {
                GameSession.SelectedLevel = level;
                GameSession.SelectedBalloonId = selected;
                services.LoadScene(SceneId.Gameplay);
            }, 64, SpriteFactory.Play);
            UIFactory.Place((RectTransform)playBtn.transform, new Vector2(0.5f, 0f), new Vector2(110f, 140f), new Vector2(560f, 150f));
            var close = UIFactory.Button(pt, "Back", UITheme.PanelLight, () => Destroy(popup), 44);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(-300f, 140f), new Vector2(260f, 150f));
        }

        static string Difficulty(float d)
        {
            int n = Mathf.Clamp(Mathf.CeilToInt(d * 5f), 1, 5);
            return new string('●', n) + new string('○', 5 - n);
        }
    }

    /// <summary>Looping scale/alpha pulse for highlighted UI elements.</summary>
    public sealed class UIPulse : MonoBehaviour
    {
        Graphic graphic;
        void Awake() { graphic = GetComponent<Graphic>(); }
        void Update()
        {
            float t = Mathf.Repeat(Time.unscaledTime * 0.8f, 1f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.15f, t);
            if (graphic != null) { var c = graphic.color; c.a = 1f - t; graphic.color = c; }
        }
    }
}
