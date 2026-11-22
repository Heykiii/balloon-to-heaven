using BalloonToHeaven.Audio;
using BalloonToHeaven.Core;
using BalloonToHeaven.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonToHeaven.UI
{
    /// <summary>Main menu: animated 3D backdrop with the equipped balloon, Play and the hub buttons.</summary>
    public sealed class MainMenuScreen : MonoBehaviour
    {
        RectTransform screen;

        void Start()
        {
            var services = GameServices.Instance;
            var db = services.Database;
            int world = services.Progression.HighestUnlockedWorld;
            var worldData = db.GetWorld(world);
            var (cam, env) = MenuBackdrop.Create(worldData, 0.9f);
            if (worldData != null) EnvironmentBuilder.AddWeather(worldData.weather, cam.transform, cam.orthographicSize * cam.aspect, cam.orthographicSize);

            // Hero balloon
            var balloonData = services.Shop.Equipped;
            var trail = db.GetTrail(services.Upgrades.EquippedTrail);
            var visual = BalloonModelBuilder.Build(balloonData, services.Shop.GetStage(balloonData.id), null, trail);
            visual.transform.position = new Vector3(0f, 0.6f, 0f);
            visual.transform.localScale = Vector3.one * 1.45f;
            visual.SetTrailsEmitting(false);
            visual.gameObject.AddComponent<MenuBalloonDrift>();

            services.Audio.PlayMusic(MusicCue.Menu, null);

            screen = UIFactory.CreateScreen("MainMenu", 0);
            var title = UIFactory.Label(screen, "Balloon\nto Heaven", 120, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(1000f, 320f));
            title.lineSpacing = 0.85f;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.2f, 0.45f, 0.6f);
            outline.effectDistance = new Vector2(4f, -4f);
            UIFactory.PopIn(title.rectTransform, 0.5f);

            CurrencyBar.Create(screen, -70f);
            var settings = UIFactory.IconButton(screen, SpriteFactory.Gear, new Color(0f, 0f, 0f, 0.35f), () => SettingsPanel.Show(screen), 110f);
            UIFactory.Place((RectTransform)settings.transform, new Vector2(0f, 1f), new Vector2(85f, -70f), new Vector2(110f, 110f));

            // Progress summary
            int stars = services.Save.Data.TotalStars();
            string progress = services.Save.Data.campaignCompleted
                ? "Heaven reached!  ★ " + stars + "/180"
                : "World " + world + (worldData != null ? " · " + worldData.worldName : "") + "   ★ " + stars + "/180";
            var info = UIFactory.Label(screen, progress, 40, Color.white);
            UIFactory.Place(info.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 880f), new Vector2(1000f, 70f));

            var play = UIFactory.Button(screen, "PLAY", UITheme.Primary, () => services.LoadScene(SceneId.WorldMap), 72, SpriteFactory.Play);
            UIFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0f, 740f), new Vector2(640f, 180f));
            TweenRunner.Run(play, 0.6f, t => play.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(t)), null, 0.2f);

            int current = services.Progression.CurrentLevel;
            var cont = UIFactory.Label(screen, "Next: Level " + GameConstants.IndexInWorld(current) + " of World " + GameConstants.WorldOfLevel(current), 34, UITheme.TextDim);
            UIFactory.Place(cont.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 620f), new Vector2(900f, 60f));

            var shop = UIFactory.Button(screen, "Balloons", UITheme.Secondary, () => services.LoadScene(SceneId.BalloonShop), 44, SpriteFactory.BalloonIcon);
            UIFactory.Place((RectTransform)shop.transform, new Vector2(0.5f, 0f), new Vector2(-235f, 470f), new Vector2(440f, 140f));
            var workshop = UIFactory.Button(screen, "Workshop", new Color(0.55f, 0.4f, 0.9f), () => services.LoadScene(SceneId.Workshop), 44, SpriteFactory.Wrench);
            UIFactory.Place((RectTransform)workshop.transform, new Vector2(0.5f, 0f), new Vector2(235f, 470f), new Vector2(440f, 140f));
            var achievements = UIFactory.Button(screen,
                "Achievements  " + services.Achievements.UnlockedCount + "/" + db.achievements.Count,
                new Color(0.2f, 0.65f, 0.55f), () => AchievementsPanel.Show(screen), 40, SpriteFactory.Trophy);
            UIFactory.Place((RectTransform)achievements.transform, new Vector2(0.5f, 0f), new Vector2(0f, 310f), new Vector2(910f, 130f));

            if (services.Save.Data.campaignCompleted)
            {
                var ending = UIFactory.Button(screen, "Watch the ending again", new Color(1f, 0.8f, 0.35f), () =>
                {
                    GameSession.LastResult = null;
                    GameSession.ShowCampaignEnding = true;
                    services.LoadScene(SceneId.Results);
                }, 36);
                UIFactory.Place((RectTransform)ending.transform, new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(600f, 100f));
            }

            ToastView.Create(screen, -420f);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        }
    }

    /// <summary>Gentle sideways drift for the main menu balloon.</summary>
    public sealed class MenuBalloonDrift : MonoBehaviour
    {
        Vector3 basePos;
        BalloonVisual visual;

        void Start()
        {
            basePos = transform.position;
            visual = GetComponent<BalloonVisual>();
        }

        void Update()
        {
            float t = Time.time;
            float x = Mathf.Sin(t * 0.45f) * 0.9f;
            float vx = Mathf.Cos(t * 0.45f) * 0.9f * 0.45f;
            transform.position = basePos + new Vector3(x, Mathf.Sin(t * 0.8f) * 0.25f, 0f);
            if (visual != null) visual.SetLateralVelocity(vx * 3f);
        }
    }
}
