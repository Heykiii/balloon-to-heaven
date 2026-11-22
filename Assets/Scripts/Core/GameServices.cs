using System.Collections;
using BalloonToHeaven.Audio;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Content;
using BalloonToHeaven.Economy;
using BalloonToHeaven.Progression;
using BalloonToHeaven.Saving;
using BalloonToHeaven.Upgrades;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonToHeaven.Core
{
    /// <summary>
    /// Composition root. Created once (from the Boot scene or lazily from any scene) and kept
    /// across scene loads. Services are plain C# objects that receive their dependencies through
    /// constructors, so only this one object is globally reachable.
    /// </summary>
    public sealed class GameServices : MonoBehaviour
    {
        static GameServices instance;

        public static GameServices Instance
        {
            get
            {
                EnsureCreated();
                return instance;
            }
        }

        public static bool IsReady => instance != null;

        public GameDatabase Database { get; private set; }
        public SaveManager Save { get; private set; }
        public EconomyManager Economy { get; private set; }
        public ProgressionManager Progression { get; private set; }
        public BalloonShopManager Shop { get; private set; }
        public UpgradeManager Upgrades { get; private set; }
        public AchievementManager Achievements { get; private set; }
        public AudioManager Audio { get; private set; }

        CanvasGroup fader;
        bool loading;

        public static void EnsureCreated()
        {
            if (instance != null) return;
            var go = new GameObject("[GameServices]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameServices>();
            instance.Initialize();
        }

        void Initialize()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;

            Database = GameDatabase.LoadOrCreate();
            Save = new SaveManager(SaveStorageFactory.Create(), Database);
            Save.Load();

            Economy = new EconomyManager(Save, Database.Config.rewards);
            Progression = new ProgressionManager(Save, Economy, Database);
            Shop = new BalloonShopManager(Save, Economy, Database);
            Upgrades = new UpgradeManager(Save, Economy, Database.Workshop);
            Achievements = new AchievementManager(Save, Economy, Database);

            Audio = gameObject.AddComponent<AudioManager>();
            Audio.Initialize(Save.Data.settings);

            QualityManager.Apply(Save.Data.settings.quality);
            Visuals.EnvironmentLighting.ApplyDefault();
            CreateFader();
            Achievements.Evaluate();
        }

        void CreateFader()
        {
            var canvasGo = new GameObject("[Fader]");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var imageGo = new GameObject("Black");
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rt = imageGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var image = imageGo.AddComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.12f, 1f);
            fader = imageGo.AddComponent<CanvasGroup>();
            fader.alpha = 0f;
            fader.blocksRaycasts = false;
        }

        /// <summary>Loads a scene with a short fade. Falls back to a generated scene if it is not in the build.</summary>
        public void LoadScene(SceneId scene)
        {
            if (loading) return;
            StartCoroutine(LoadRoutine(scene));
        }

        IEnumerator LoadRoutine(SceneId scene)
        {
            loading = true;
            fader.blocksRaycasts = true;
            for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                fader.alpha = t / 0.22f;
                yield return null;
            }
            fader.alpha = 1f;
            Time.timeScale = 1f;
            Save.SaveIfDirty();

            string sceneName = scene.ToString();
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                yield return SceneManager.LoadSceneAsync(sceneName);
            }
            else
            {
                // Scene not in build settings (e.g. project opened before the generator ran):
                // create an empty scene at runtime and let SceneEntry build its contents.
                var previous = SceneManager.GetActiveScene();
                var created = SceneManager.CreateScene(sceneName + "_Runtime_" + Time.frameCount);
                SceneManager.SetActiveScene(created);
                var entry = new GameObject("SceneEntry").AddComponent<SceneEntry>();
                entry.scene = scene;
                yield return SceneManager.UnloadSceneAsync(previous);
            }

            yield return null;
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                fader.alpha = 1f - t / 0.25f;
                yield return null;
            }
            fader.alpha = 0f;
            fader.blocksRaycasts = false;
            loading = false;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save?.SaveNow();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus) Save?.SaveIfDirty();
        }

        void OnApplicationQuit()
        {
            Save?.SaveNow();
        }
    }
}
