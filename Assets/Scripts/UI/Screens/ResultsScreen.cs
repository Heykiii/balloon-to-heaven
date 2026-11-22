using System.Collections;
using BalloonToHeaven.Audio;
using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonToHeaven.UI
{
    /// <summary>
    /// Victory / failure screen (animated stars, reward breakdown, unlocks) and, after the final
    /// level, the Heaven's Gate campaign ending.
    /// </summary>
    public sealed class ResultsScreen : MonoBehaviour
    {
        RectTransform screen;

        void Start()
        {
            var services = GameServices.Instance;
            var result = GameSession.LastResult;

            if (GameSession.ShowCampaignEnding)
            {
                GameSession.ShowCampaignEnding = false;
                var ending = gameObject.AddComponent<HeavensGateEnding>();
                ending.Finished = () =>
                {
                    if (result != null) ShowResults(result);
                    else services.LoadScene(SceneId.MainMenu);
                };
                return;
            }

            if (result == null)
            {
                services.LoadScene(SceneId.MainMenu);
                return;
            }
            ShowResults(result);
        }

        void ShowResults(LevelResult result)
        {
            var services = GameServices.Instance;
            var db = services.Database;
            var level = db.GetLevel(result.levelNumber);
            var world = level != null ? db.GetWorld(level.worldNumber) : null;

            if (Camera.main == null) MenuBackdrop.Create(world, result.victory ? 1.2f : 0.3f);
            services.Audio.PlayMusic(MusicCue.Menu, null);

            screen = UIFactory.CreateScreen("Results", 0);
            UIFactory.Dimmer(screen, 0.35f).raycastTarget = false;

            var panel = UIFactory.Panel(screen);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(980f, 1540f));
            UIFactory.PopIn(panel.rectTransform, 0.35f);
            var pt = panel.transform;

            string titleText = result.victory ? (result.bossDefeated ? "Boss Defeated!" : "Level Complete!") : "Balloon Down";
            var title = UIFactory.Label(pt, titleText, 80, result.victory ? UITheme.Gold : new Color(1f, 0.55f, 0.5f), TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(920f, 120f));
            string sub = level != null ? "World " + level.worldNumber + " · Level " + level.indexInWorld + " · " + level.levelName : "";
            var subtitle = UIFactory.Label(pt, sub, 34, UITheme.TextDim);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(920f, 50f));

            float y = -300f;
            if (result.victory)
            {
                // Stars (animated in sequence)
                for (int i = 0; i < 3; i++)
                {
                    var empty = UIFactory.Image("StarSlot" + i, pt, new Color(0f, 0f, 0f, 0.35f), SpriteFactory.Star);
                    float x = (i - 1) * 190f;
                    float sy = i == 1 ? y - 60f : y - 90f;
                    float size = i == 1 ? 200f : 160f;
                    UIFactory.Place(empty.rectTransform, new Vector2(0.5f, 1f), new Vector2(x, sy), new Vector2(size, size));
                    if (!result.starFlags[i]) continue;
                    var star = UIFactory.Image("Star" + i, empty.transform, UITheme.Gold, SpriteFactory.Star);
                    UIFactory.Stretch(star.rectTransform);
                    star.rectTransform.localScale = Vector3.zero;
                    int index = i;
                    TweenRunner.Run(star, 0.35f, t => star.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, Ease.OutBack(t)),
                        null, 0.4f + index * 0.35f);
                    TweenRunner.Delay(star, 0.4f + index * 0.35f, () => services.Audio.PlaySfx(SfxId.Star));
                }
                y -= 250f;

                string[] goals = { "Reached the top", "Collect " + (level != null ? level.coinsForSecondStar : 0) + " coins", level != null ? level.ChallengeDescription() : "" };
                for (int i = 0; i < 3; i++)
                {
                    var row = UIFactory.Label(pt, (result.starFlags[i] ? "<color=#7CFF9E>✓</color>  " : "<color=#FF8A80>✗</color>  ") + goals[i], 32, Color.white, TextAnchor.MiddleLeft);
                    UIFactory.Place(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(800f, 50f));
                    y -= 52f;
                }
                if (result.rewards.newBestStars)
                {
                    var best = UIFactory.Label(pt, "New best!", 36, UITheme.Gold, TextAnchor.MiddleCenter, true);
                    UIFactory.Place(best.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(800f, 50f));
                    y -= 52f;
                }
            }
            else
            {
                var pct = Mathf.RoundToInt(result.progress * 100f);
                var progressLabel = UIFactory.Label(pt, "You made it " + pct + "% of the way", 44, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Place(progressLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 20f), new Vector2(900f, 70f));
                var bar = UIFactory.Bar(pt, new Color(0f, 0f, 0f, 0.35f), UITheme.Primary, out var fill);
                UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 110f), new Vector2(760f, 46f));
                TweenRunner.Run(fill, 0.8f, t => fill.fillAmount = result.progress * Ease.OutCubic(t), null, 0.3f);
                var tip = UIFactory.Label(pt, Tip(result), 32, UITheme.TextDim);
                UIFactory.Place(tip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 220f), new Vector2(860f, 120f));
                y -= 320f;
            }

            // Reward breakdown
            var rewardsBox = UIFactory.Image("Rewards", pt, UITheme.PanelLight, SpriteFactory.RoundedRect);
            int lines = Mathf.Max(1, result.rewards.lines.Count);
            float boxH = 70f + lines * 54f;
            UIFactory.Place(rewardsBox.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 10f), new Vector2(880f, boxH), new Vector2(0.5f, 1f));
            float ly = -35f;
            if (result.rewards.lines.Count == 0)
            {
                var none = UIFactory.Label(rewardsBox.transform, "No coins this time", 32, UITheme.TextDim);
                UIFactory.Place(none.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, ly - 20f), new Vector2(800f, 50f));
            }
            for (int i = 0; i < result.rewards.lines.Count; i++)
            {
                var line = result.rewards.lines[i];
                var l = UIFactory.Label(rewardsBox.transform, line.label, 32, Color.white, TextAnchor.MiddleLeft);
                UIFactory.Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(40f, ly), new Vector2(520f, 50f), new Vector2(0f, 1f));
                string amount = (line.coins > 0 ? "<color=#FFD86B>+" + UIFactory.FormatNumber(line.coins) + " ●</color>" : "")
                    + (line.gems > 0 ? "  <color=#9FDCFF>+" + line.gems + " ◆</color>" : "");
                var a = UIFactory.Label(rewardsBox.transform, amount, 32, Color.white, TextAnchor.MiddleRight, true);
                UIFactory.Place(a.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, ly), new Vector2(320f, 50f), new Vector2(1f, 1f));
                var group = l.gameObject.AddComponent<CanvasGroup>();
                var groupA = a.gameObject.AddComponent<CanvasGroup>();
                group.alpha = groupA.alpha = 0f;
                TweenRunner.Run(l, 0.25f, t => { group.alpha = t; groupA.alpha = t; }, () => services.Audio.PlaySfx(SfxId.Coin, 0.4f), 0.9f + i * 0.18f);
                ly -= 54f;
            }
            y -= boxH + 30f;

            var total = UIFactory.Label(pt, "Total  <color=#FFD86B>+" + UIFactory.FormatNumber(result.rewards.TotalCoins) + " ●</color>"
                + (result.rewards.TotalGems > 0 ? "   <color=#9FDCFF>+" + result.rewards.TotalGems + " ◆</color>" : ""), 46, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Place(total.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 20f), new Vector2(900f, 70f));
            y -= 90f;

            foreach (var unlock in result.rewards.unlocks)
            {
                var u = UIFactory.Label(pt, "★ " + unlock, 32, new Color(0.6f, 1f, 0.75f), TextAnchor.MiddleCenter, true);
                UIFactory.Place(u.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(900f, 50f));
                y -= 50f;
            }

            // Buttons
            int next = result.levelNumber + 1;
            bool canNext = result.victory && next <= GameConstants.TotalLevels && services.Progression.IsLevelUnlocked(next);
            if (canNext)
            {
                var nextBtn = UIFactory.Button(pt, "Next Level", UITheme.Primary, () =>
                {
                    GameSession.SelectedLevel = next;
                    services.LoadScene(SceneId.Gameplay);
                }, 50, SpriteFactory.Play);
                UIFactory.Place((RectTransform)nextBtn.transform, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(620f, 140f));
            }
            else if (!result.victory)
            {
                var shopBtn = UIFactory.Button(pt, "Upgrade balloon", UITheme.Secondary, () => services.LoadScene(SceneId.BalloonShop), 42, SpriteFactory.BalloonIcon);
                UIFactory.Place((RectTransform)shopBtn.transform, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(620f, 130f));
            }
            var retry = UIFactory.Button(pt, "Retry", result.victory ? UITheme.Secondary : UITheme.Primary, () =>
            {
                GameSession.SelectedLevel = result.levelNumber;
                services.LoadScene(SceneId.Gameplay);
            }, 46);
            UIFactory.Place((RectTransform)retry.transform, new Vector2(0.5f, 0f), new Vector2(-220f, 90f), new Vector2(400f, 130f));
            var map = UIFactory.Button(pt, "World Map", UITheme.Success, () => services.LoadScene(SceneId.WorldMap), 46);
            UIFactory.Place((RectTransform)map.transform, new Vector2(0.5f, 0f), new Vector2(220f, 90f), new Vector2(400f, 130f));

            CurrencyBar.Create(screen, -70f);
            ToastView.Create(screen, -200f);

            if (result.victory) StartCoroutine(Confetti());
        }

        static string Tip(LevelResult r)
        {
            string[] tips =
            {
                "Tip: Watch for the warning flicker before hazards activate.",
                "Tip: Workshop upgrades make every balloon tougher.",
                "Tip: There is always a safe path. Look for the gap!",
                "Tip: Shields block one hit. Grab them before tough sections.",
                "Tip: Higher balloon stages add durability and armor.",
            };
            return tips[(r.levelNumber + Mathf.RoundToInt(r.progress * 10f)) % tips.Length];
        }

        IEnumerator Confetti()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            for (int i = 0; i < 40; i++)
            {
                var c = UIFactory.Image("Confetti", screen, Color.HSVToRGB(Random.value, 0.55f, 1f), i % 3 == 0 ? SpriteFactory.Star : SpriteFactory.RoundedRect);
                var rt = c.rectTransform;
                UIFactory.Place(rt, new Vector2(Random.value, 1f), new Vector2(0f, 40f), new Vector2(28f, 40f));
                float speed = Random.Range(500f, 900f), sway = Random.Range(-120f, 120f), spin = Random.Range(-360f, 360f);
                Vector2 origin = rt.anchoredPosition;
                TweenRunner.Run(rt, 3f, t =>
                {
                    rt.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 8f) * sway * 0.3f + sway * t, -speed * t * 3f);
                    rt.localEulerAngles = new Vector3(0f, 0f, spin * t * 3f);
                }, () => Destroy(c.gameObject));
                yield return new WaitForSecondsRealtime(0.04f);
            }
        }
    }

    /// <summary>
    /// The Heaven's Gate ending: golden gates open, the player's balloon rises through them into
    /// light, followed by a short closing message.
    /// </summary>
    public sealed class HeavensGateEnding : MonoBehaviour
    {
        public System.Action Finished;
        Transform leftDoor, rightDoor, balloon, lightBurst;
        Text message;
        CanvasGroup whiteout;
        bool skip;

        IEnumerator Start()
        {
            var services = GameServices.Instance;
            var db = services.Database;
            var world = db.GetWorld(GameConstants.WorldCount);
            var (cam, env) = MenuBackdrop.Create(world, 0.4f);
            services.Audio.PlayMusic(MusicCue.Ending, null);
            BuildGate();

            var data = services.Shop.Equipped;
            var visual = BalloonModelBuilder.Build(data, services.Shop.GetStage(data.id), null, db.GetTrail(services.Upgrades.EquippedTrail));
            balloon = visual.transform;
            balloon.position = new Vector3(0f, -13f, 0f);
            balloon.localScale = Vector3.one * 1.2f;

            var screen = UIFactory.CreateScreen("Ending", 20);
            message = UIFactory.Label(screen, "", 56, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Place(message.rectTransform, new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(1000f, 400f));
            var msgGroup = message.gameObject.AddComponent<CanvasGroup>();
            var white = UIFactory.Image("Whiteout", screen, new Color(1f, 0.98f, 0.9f, 1f));
            UIFactory.Stretch(white.rectTransform, -200, -200, -200, -200);
            whiteout = white.gameObject.AddComponent<CanvasGroup>();
            whiteout.alpha = 0f;
            whiteout.blocksRaycasts = false;
            var skipBtn = UIFactory.Button(screen, "Skip", new Color(0f, 0f, 0f, 0.35f), () => skip = true, 36);
            UIFactory.Place((RectTransform)skipBtn.transform, new Vector2(1f, 1f), new Vector2(-120f, -80f), new Vector2(200f, 90f));

            string[] lines =
            {
                "Through valleys, storms and stars...",
                "past fire and ice, and every guardian of the sky...",
                "your balloon has reached Heaven's Gate.",
            };

            // 1) Balloon rises toward the closed gate.
            yield return Phase(4.5f, t =>
            {
                balloon.position = new Vector3(Mathf.Sin(t * 3f) * 0.6f, Mathf.Lerp(-13f, -3.5f, Ease.InOutSine(t)), 0f);
                msgGroup.alpha = Mathf.Clamp01(t * 3f) * Mathf.Clamp01((1f - t) * 4f);
            }, () => message.text = lines[0]);

            // 2) Gates open.
            services.Audio.PlaySfx(SfxId.Unlock);
            yield return Phase(3.5f, t =>
            {
                float a = Ease.InOutSine(t) * 100f;
                leftDoor.localRotation = Quaternion.Euler(0f, -a, 0f);
                rightDoor.localRotation = Quaternion.Euler(0f, a, 0f);
                lightBurst.localScale = Vector3.one * Mathf.Lerp(1f, 9f, t);
                msgGroup.alpha = Mathf.Clamp01(t * 3f) * Mathf.Clamp01((1f - t) * 4f);
            }, () => message.text = lines[1]);

            // 3) Balloon passes through the gate into the light.
            services.Audio.PlaySfx(SfxId.LevelComplete);
            yield return Phase(4.5f, t =>
            {
                balloon.position = new Vector3(0f, Mathf.Lerp(-3.5f, 4.5f, Ease.InOutSine(t)), Mathf.Lerp(0f, 6f, t));
                balloon.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.4f, t);
                whiteout.alpha = Mathf.Clamp01((t - 0.6f) / 0.4f);
                msgGroup.alpha = Mathf.Clamp01(t * 3f);
                if (Random.value < 0.25f) ParticleFactory.Burst(new Vector3(Random.Range(-4f, 4f), Random.Range(-2f, 6f), -2f), new Color(1f, 0.9f, 0.5f), 6, 2f, 0.3f, 1f);
            }, () => message.text = lines[2]);

            // 4) Final card on white.
            message.color = new Color(0.35f, 0.28f, 0.12f);
            message.text = "<size=84>The End</size>\n\nThank you for playing\nBalloon to Heaven";
            int stars = services.Save.Data.TotalStars();
            var stats = UIFactory.Label(screen, "★ " + stars + "/180 stars   ·   " + services.Save.Data.ownedBalloons.Count + "/20 balloons", 38, new Color(0.45f, 0.38f, 0.2f));
            UIFactory.Place(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1000f, 80f));
            UIFactory.Place(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1000f, 500f));
            msgGroup.alpha = 1f;
            skip = false;
            skipBtn.gameObject.SetActive(false);
            var cont = UIFactory.Button(screen, "Continue", UITheme.Primary, () => skip = true, 50);
            UIFactory.Place((RectTransform)cont.transform, new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(520f, 140f));
            UIFactory.PopIn((RectTransform)cont.transform);
            while (!skip) yield return null;

            Destroy(screen.root.gameObject);
            Destroy(balloon.gameObject);
            Destroy(leftDoor.parent.gameObject);
            Finished?.Invoke();
        }

        IEnumerator Phase(float duration, System.Action<float> update, System.Action begin)
        {
            begin?.Invoke();
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (skip) { update(1f); yield break; }
                update(t / duration);
                yield return null;
            }
            update(1f);
        }

        void BuildGate()
        {
            var root = new GameObject("HeavensGate").transform;
            root.position = new Vector3(0f, 1.5f, 3f);
            var gold = MaterialLibrary.Toon(new Color(1f, 0.82f, 0.35f), 0.25f, 0.9f);
            var marble = MaterialLibrary.Toon(new Color(0.97f, 0.95f, 0.9f), 0.05f, 0.4f);

            lightBurst = ModelKit.Glow("Light", root, new Vector3(0f, 0f, 1f), 3f, new Color(1f, 0.95f, 0.75f), true).transform;
            ModelKit.Part("PillarL", root, MeshLibrary.Cylinder, marble, new Vector3(-3.1f, -1f, 0f), new Vector3(0.9f, 7f, 0.9f));
            ModelKit.Part("PillarR", root, MeshLibrary.Cylinder, marble, new Vector3(3.1f, -1f, 0f), new Vector3(0.9f, 7f, 0.9f));
            ModelKit.Part("CapL", root, MeshLibrary.Cube, gold, new Vector3(-3.1f, 2.6f, 0f), new Vector3(1.3f, 0.3f, 1.3f));
            ModelKit.Part("CapR", root, MeshLibrary.Cube, gold, new Vector3(3.1f, 2.6f, 0f), new Vector3(1.3f, 0.3f, 1.3f));
            ModelKit.Part("Arch", root, MeshLibrary.Arc, gold, new Vector3(0f, 2.6f, 0f), new Vector3(6.2f, 6.2f, 4f));
            ModelKit.Part("Orb", root, MeshLibrary.Gem, gold, new Vector3(0f, 6f, 0f), new Vector3(0.8f, 1f, 0.8f));
            ModelKit.Glow("OrbGlow", root, new Vector3(0f, 6f, -0.3f), 2.4f, new Color(1f, 0.9f, 0.5f), true);

            leftDoor = ModelKit.Node("LeftDoor", root, new Vector3(-2.6f, -1f, 0f)).transform;
            rightDoor = ModelKit.Node("RightDoor", root, new Vector3(2.6f, -1f, 0f)).transform;
            for (int side = 0; side < 2; side++)
            {
                var door = side == 0 ? leftDoor : rightDoor;
                float dir = side == 0 ? 1f : -1f;
                for (int b = 0; b < 5; b++)
                    ModelKit.Part("Bar" + b, door, MeshLibrary.Cylinder, gold, new Vector3(dir * (0.3f + b * 0.5f), 0f, 0f), new Vector3(0.12f, 6.4f, 0.12f));
                ModelKit.Part("Rail", door, MeshLibrary.Cube, gold, new Vector3(dir * 1.3f, 1.8f, 0f), new Vector3(2.6f, 0.15f, 0.15f));
                ModelKit.Part("Rail2", door, MeshLibrary.Cube, gold, new Vector3(dir * 1.3f, -1.8f, 0f), new Vector3(2.6f, 0.15f, 0.15f));
                ModelKit.Part("Ring", door, MeshLibrary.Torus, gold, new Vector3(dir * 1.3f, 0f, 0f), new Vector3(1.4f, 1.4f, 1.4f), new Vector3(90f, 0f, 0f));
            }
            var cloudMat = MaterialLibrary.Unlit(new Color(1f, 1f, 1f, 0.9f), ProceduralTextures.CloudPuff);
            for (int i = 0; i < 7; i++)
                ModelKit.Part("Cloud" + i, root, MeshLibrary.Quad, cloudMat, new Vector3(-6f + i * 2f, -4.6f + (i % 2) * 0.4f, -1f), new Vector3(4f, 2.2f, 1f));
        }
    }
}
