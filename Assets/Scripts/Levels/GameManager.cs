using BalloonToHeaven.Audio;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Bosses;
using BalloonToHeaven.Content;
using BalloonToHeaven.Core;
using BalloonToHeaven.Economy;
using BalloonToHeaven.Obstacles;
using BalloonToHeaven.Player;
using BalloonToHeaven.PowerUps;
using BalloonToHeaven.UI;
using BalloonToHeaven.Visuals;
using BalloonToHeaven.Worlds;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonToHeaven.Levels
{
    /// <summary>
    /// Gameplay scene controller. Builds the level from data, owns the frame loop of every gameplay
    /// system (in a fixed, deterministic order) and turns the run into a LevelResult.
    /// Flow: Intro -> Playing -> (Boss) -> Finishing | Failed -> Results scene.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        enum State { Intro, Playing, Boss, Finishing, Failed, Done }

        const float IntroDuration = 1.8f;
        const float FinishDuration = 2.4f;
        const float FailDuration = 2.2f;

        GameServices services;
        LevelContext ctx;
        EnvironmentController environment;
        HUDView hud;
        BossManager boss;
        AbilityContext abilityContext;
        State state;
        float stateTimer;
        bool paused;
        bool bossDefeated;
        GameObject pauseMenu;

        void Start()
        {
            Time.timeScale = 1f;
            services = GameServices.Instance;
            UIFactory.EnsureEventSystem();
            BuildLevel();
        }

        // ------------------------------------------------------------------ setup

        void BuildLevel()
        {
            var db = services.Database;
            var cfg = db.Config;
            var save = services.Save.Data;

            var level = db.GetLevel(Mathf.Clamp(GameSession.SelectedLevel, 1, GameConstants.TotalLevels)) ?? db.GetLevel(1);
            var world = db.GetWorld(level.worldNumber);

            // Balloon: the one chosen in the level popup if owned, otherwise the equipped one.
            BalloonData balloonData = null;
            if (!string.IsNullOrEmpty(GameSession.SelectedBalloonId) && services.Shop.IsOwned(GameSession.SelectedBalloonId))
                balloonData = db.GetBalloon(GameSession.SelectedBalloonId);
            if (balloonData == null) balloonData = services.Shop.Equipped;

            CameraShake.Enabled = save.settings.screenShake;

            var root = new GameObject("Level " + level.levelNumber).transform;
            var cam = CameraRig.Create(cfg.cameraOrthoSize, world != null ? world.skyBottom : Color.cyan, root);
            CameraRig.FitPlayfield(cam, cfg.cameraOrthoSize, cfg.maxPlayfieldHalfWidth);
            EnvironmentLighting.Apply(world);

            int seed = level.levelNumber * 7919 + System.Environment.TickCount % 1000;
            environment = EnvironmentBuilder.Build(world, cam, root, level.levelNumber * 31);
            var field = Playfield.FromCamera(cam, cfg);
            if (world != null) EnvironmentBuilder.AddWeather(world.weather, cam.transform, field.VisibleHalfWidth, field.HalfHeight);

            var worldRoot = new GameObject("Gameplay").transform;
            worldRoot.SetParent(root, false);

            ctx = new LevelContext
            {
                Database = db,
                Config = cfg,
                Level = level,
                World = world,
                BalloonData = balloonData,
                Field = field,
                Rng = new System.Random(seed),
                Camera = cam,
                Shake = cam.GetComponent<CameraShake>(),
                WorldRoot = worldRoot,
                Audio = services.Audio,
                ScrollSpeed = level.ascentSpeed,
            };

            // Balloon (order matters: abilities need stats, health needs abilities, abilities.Begin needs health)
            int stage = services.Shop.GetStage(balloonData.id);
            var trail = db.GetTrail(services.Upgrades.EquippedTrail);
            var visual = BalloonModelBuilder.Build(balloonData, stage, worldRoot, trail);
            var go = visual.gameObject;
            var controller = go.AddComponent<BalloonController>();
            var health = go.AddComponent<BalloonHealth>();
            var abilities = go.AddComponent<BalloonAbilities>();
            ctx.Balloon = controller;
            ctx.Health = health;
            ctx.Abilities = abilities;

            var stats = services.Shop.GetStats(balloonData);
            abilityContext = new AbilityContext { Data = balloonData, Controller = controller, Health = health, Level = ctx };
            abilities.Initialize(balloonData, stats, abilityContext);
            controller.Initialize(ctx, visual, stats.speedMultiplier);
            controller.InputEnabled = false;
            controller.SetVerticalPosition(field.Bottom - 2f);

            // Systems
            ctx.Lanes = new LanePlanner(cfg.laneCount, field.HalfWidth, ctx.Rng);
            ctx.PowerUps = new PowerUpManager(ctx);
            ctx.Obstacles = new ObstacleSpawner(ctx, worldRoot);
            ctx.Coins = new CoinManager(ctx, worldRoot);
            ctx.PowerUpSpawner = new PowerUpSpawner(ctx, worldRoot);
            ctx.Effects = new EnvironmentEffects(ctx, worldRoot);

            health.Initialize(stats.maxHealth, stats.armor, cfg, visual, abilities, ctx.Rng);
            abilities.Begin(abilityContext);
            health.Died += OnBalloonDied;

            // HUD
            hud = HUDView.Create(level, world != null ? world.worldName : "", abilities.Active);
            hud.PauseRequested = TogglePause;
            hud.SetAbilityVisible(false);
            GameEvents.RaiseLevelCoinsChanged(0);
            GameEvents.RaiseLevelProgressChanged(0f);
            GameEvents.RaiseBossHealthChanged(-1f);
            GameEvents.RaiseHealthChanged(health.Current, health.Max);

            services.Audio.PlayMusic(MusicCue.World, world != null ? world.music : null);

            string subtitle = level.isBossLevel && level.boss != null ? "Boss ahead: " + level.boss.bossName : level.hint;
            hud.ShowBanner(level.levelName, subtitle, 2.4f);
            if (level.challenge != StarChallengeType.CoinTarget || level.coinsForSecondStar > 0)
                TweenRunner.Delay(this, 2.6f, () => GameEvents.RaiseToast("★ " + level.ChallengeDescription()));

            state = State.Intro;
            stateTimer = 0f;
        }

        // ------------------------------------------------------------------ frame loop

        void Update()
        {
            if (ctx == null) return;
            if (Input.GetKeyDown(KeyCode.Escape) && (state == State.Playing || state == State.Boss || state == State.Intro)) TogglePause();
            if (paused) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            stateTimer += dt;

            switch (state)
            {
                case State.Intro: TickIntro(dt); break;
                case State.Playing: TickPlaying(dt); break;
                case State.Boss: TickBoss(dt); break;
                case State.Finishing: TickFinishing(dt); break;
                case State.Failed: TickFailed(dt); break;
            }
        }

        void TickIntro(float dt)
        {
            float t = Mathf.Clamp01(stateTimer / IntroDuration);
            float startY = ctx.Field.Bottom - 2f;
            ctx.Balloon.SetVerticalPosition(Mathf.Lerp(startY, ctx.BalloonY, Ease.OutBack(t)));
            ctx.ScrollSpeed = ctx.Level.ascentSpeed * t;
            environment.Scroll(ctx.ScrollSpeed * dt);
            if (stateTimer >= IntroDuration)
            {
                ctx.Balloon.SetVerticalPosition(ctx.BalloonY);
                ctx.Balloon.InputEnabled = true;
                hud.SetAbilityVisible(ctx.Abilities.Active != null);
                state = State.Playing;
                stateTimer = 0f;
            }
        }

        void TickSystems(float dt, bool advanceDistance, float scrollFactor)
        {
            ctx.ElapsedTime += dt;
            ctx.ScrollSpeed = ctx.Level.ascentSpeed * ctx.PowerUps.ScrollMultiplier * scrollFactor;
            if (advanceDistance) ctx.Distance += ctx.ScrollSpeed * dt;
            environment.Scroll(ctx.ScrollSpeed * dt);

            ctx.Abilities.Tick(dt);
            ctx.PowerUps.Tick(dt);
            ctx.Effects.Tick(dt);
            ctx.Obstacles.Tick(dt);
            ctx.Coins.Tick(dt);
            ctx.PowerUpSpawner.Tick(dt);
            boss?.Tick(dt);
        }

        void TickPlaying(float dt)
        {
            TickSystems(dt, true, 1f);
            GameEvents.RaiseLevelProgressChanged(ctx.Progress);
            if (state != State.Playing) return; // died during tick

            if (ctx.Distance >= ctx.Level.LevelLength)
            {
                if (ctx.Level.isBossLevel && ctx.Level.boss != null) StartBoss();
                else BeginFinish();
            }
        }

        void StartBoss()
        {
            state = State.Boss;
            stateTimer = 0f;
            ctx.NormalSpawning = false;
            ctx.BossActive = true;
            ctx.Coins.PatternsEnabled = false;
            ctx.Obstacles.ClearAll(true);
            var data = ctx.Level.boss;
            boss = new BossManager(ctx, data, ctx.WorldRoot);
            hud.SetBossName(data.bossName);
            hud.ShowBanner(data.bossName, data.title, 2.2f);
            services.Audio.PlayMusic(MusicCue.Boss, ctx.World != null ? ctx.World.music : null);
            TweenRunner.Delay(this, 2.8f, () => { if (state == State.Boss) GameEvents.RaiseToast("Collect the glowing crystals to strike!"); });
        }

        void TickBoss(float dt)
        {
            // Background keeps drifting slowly while the boss holds the balloon in place.
            TickSystems(dt, false, 0.55f);
            if (state != State.Boss) return;
            if (boss.IsDone)
            {
                bossDefeated = true;
                BeginFinish();
            }
        }

        void BeginFinish()
        {
            state = State.Finishing;
            stateTimer = 0f;
            ctx.Balloon.InputEnabled = false;
            ctx.Health.SetInvulnerable(FinishDuration + 5f);
            ctx.NormalSpawning = false;
            ctx.Coins.PatternsEnabled = false;
            ctx.PowerUpSpawner.Enabled = false;
            ctx.Obstacles.ClearAll(true);
            GameEvents.RaiseLevelProgressChanged(1f);
            GameEvents.RaiseBossHealthChanged(-1f);
            hud.SetAbilityVisible(false);
            hud.ShowBanner(bossDefeated ? "Victory!" : "Level complete!", ctx.Level.levelName, FinishDuration);
            services.Audio.PlaySfx(SfxId.LevelComplete);
            services.Audio.PlayMusic(MusicCue.Victory, null);
            ParticleFactory.Burst(ctx.BalloonCenter + Vector2.up * 1.5f, new Color(1f, 0.9f, 0.4f), 50, 6f, 0.35f, 1.2f);
        }

        void TickFinishing(float dt)
        {
            // Keep coins already on screen collectable and let the balloon soar out of view.
            ctx.ScrollSpeed = Mathf.Lerp(ctx.ScrollSpeed, ctx.Level.ascentSpeed * 2.2f, dt * 2f);
            environment.Scroll(ctx.ScrollSpeed * dt);
            ctx.Coins.Tick(dt);
            ctx.PowerUps.Tick(dt);
            ctx.Obstacles.Tick(dt);
            boss?.Tick(dt);
            float rise = Mathf.Max(0f, stateTimer - 0.8f);
            ctx.Balloon.SetVerticalPosition(ctx.BalloonY + rise * rise * 4f);
            if (stateTimer >= FinishDuration) Complete(true);
        }

        void OnBalloonDied()
        {
            if (state == State.Finishing || state == State.Failed || state == State.Done) return;
            state = State.Failed;
            stateTimer = 0f;
            ctx.Balloon.InputEnabled = false;
            ctx.Balloon.Visual.Deflate();
            ctx.Balloon.Visual.SetTrailsEmitting(false);
            hud.SetAbilityVisible(false);
            ctx.Shake?.Shake(0.7f);
            services.Audio.PlaySfx(SfxId.Fail);
            services.Audio.PlayMusic(MusicCue.Fail, null);
            ParticleFactory.Burst(ctx.BalloonCenter, ctx.BalloonData.primaryColor, 40, 5f, 0.3f, 0.9f, false);
            hud.ShowBanner("Balloon down!", Mathf.RoundToInt(ctx.Progress * 100f) + "% of the way up", FailDuration);
        }

        void TickFailed(float dt)
        {
            ctx.ScrollSpeed = Mathf.MoveTowards(ctx.ScrollSpeed, 0f, dt * 4f);
            environment.Scroll(ctx.ScrollSpeed * dt);
            ctx.Obstacles.Tick(dt);
            ctx.Coins.Tick(0f);
            boss?.Tick(dt);
            ctx.Balloon.SetVerticalPosition(ctx.BalloonY - stateTimer * stateTimer * 3f);
            if (stateTimer >= FailDuration) Complete(false);
        }

        // ------------------------------------------------------------------ results

        void Complete(bool victory)
        {
            if (state == State.Done) return;
            state = State.Done;

            var result = new LevelResult
            {
                levelNumber = ctx.Level.levelNumber,
                victory = victory,
                coinsCollected = ctx.CoinsCollected,
                specialCoinsCollected = ctx.SpecialCoinsCollected,
                specialCoinsTotal = ctx.SpecialCoinsTotal,
                tookDamage = ctx.Health.TookDamage,
                powerUpsCollected = ctx.PowerUpsCollected,
                progress = victory ? 1f : ctx.Progress,
                timeSeconds = ctx.ElapsedTime,
                bossDefeated = bossDefeated,
                balloonId = ctx.BalloonData.id,
                rewardMultiplier = ctx.Abilities.RewardMultiplier,
            };

            for (int i = 0; i < ctx.PowerUpsCollected; i++) services.Progression.RecordPowerUpCollected();
            services.Progression.RecordResult(result);
            services.Achievements.Evaluate();
            services.Save.SaveIfDirty();

            GameSession.LastResult = result;
            GameSession.ShowCampaignEnding = result.rewards.campaignJustCompleted;
            services.LoadScene(SceneId.Results);
        }

        // ------------------------------------------------------------------ pause

        void TogglePause()
        {
            if (state == State.Done || state == State.Finishing || state == State.Failed) return;
            if (paused) Resume();
            else Pause();
        }

        void Pause()
        {
            if (paused) return;
            paused = true;
            Time.timeScale = 0f;
            services.Audio.SetMusicDucked(true);
            BuildPauseMenu();
        }

        void Resume()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = 1f;
            services.Audio.SetMusicDucked(false);
            if (pauseMenu != null) Destroy(pauseMenu);
        }

        void BuildPauseMenu()
        {
            if (pauseMenu != null) Destroy(pauseMenu);
            var screen = hud.Screen;
            var rootRt = UIFactory.Rect("PauseMenu", screen);
            UIFactory.Stretch(rootRt);
            pauseMenu = rootRt.gameObject;
            UIFactory.Dimmer(rootRt, 0.65f);

            var panel = UIFactory.Panel(rootRt);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 1060f));
            UIFactory.PopIn(panel.rectTransform);

            var title = UIFactory.Label(panel.transform, "Paused", 72, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(700f, 100f));

            var info = UIFactory.Label(panel.transform,
                "World " + ctx.Level.worldNumber + " · Level " + ctx.Level.indexInWorld + "\n<size=34>" + ctx.Level.ChallengeDescription() + "</size>",
                38, UITheme.TextDim);
            UIFactory.Place(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(680f, 110f));

            float y = -330f;
            void Add(string label, Color color, System.Action action)
            {
                var b = UIFactory.Button(panel.transform, label, color, action, 46);
                UIFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(560f, 130f));
                y -= 160f;
            }

            Add("Resume", UITheme.Success, Resume);
            Add("Restart", UITheme.Secondary, () => { Resume(); services.LoadScene(SceneId.Gameplay); });
            Add("Settings", UITheme.PanelLight, () => SettingsPanel.Show(screen, () => CameraShake.Enabled = services.Save.Data.settings.screenShake));
            Add("World Map", UITheme.Primary, () => { Resume(); services.LoadScene(SceneId.WorldMap); });
            Add("Main Menu", UITheme.Danger, () => { Resume(); services.LoadScene(SceneId.MainMenu); });
        }

        void OnApplicationPause(bool pausedByOs)
        {
            if (pausedByOs && (state == State.Playing || state == State.Boss)) Pause();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (ctx != null && ctx.Health != null) ctx.Health.Died -= OnBalloonDied;
            boss?.Dispose();
            ParticleFactory.ClearBursts();
            GameEvents.RaiseBossHealthChanged(-1f);
        }
    }
}
