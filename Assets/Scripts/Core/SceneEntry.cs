using BalloonToHeaven.Levels;
using BalloonToHeaven.UI;
using UnityEngine;

namespace BalloonToHeaven.Core
{
    /// <summary>
    /// The only object stored in each generated scene. It makes sure services exist and then
    /// builds the scene's screen from code, so scenes contain no fragile hand-made references.
    /// </summary>
    public sealed class SceneEntry : MonoBehaviour
    {
        public SceneId scene = SceneId.Boot;

        void Awake()
        {
            GameServices.EnsureCreated();
        }

        // Start (not Awake) so that code which adds this component can set 'scene' first.
        void Start()
        {
            switch (scene)
            {
                case SceneId.Boot: gameObject.AddComponent<BootScreen>(); break;
                case SceneId.MainMenu: gameObject.AddComponent<MainMenuScreen>(); break;
                case SceneId.WorldMap: gameObject.AddComponent<WorldMapScreen>(); break;
                case SceneId.Gameplay: gameObject.AddComponent<GameManager>(); break;
                case SceneId.BalloonShop: gameObject.AddComponent<BalloonShopScreen>(); break;
                case SceneId.Workshop: gameObject.AddComponent<WorkshopScreen>(); break;
                case SceneId.Results: gameObject.AddComponent<ResultsScreen>(); break;
            }
        }

        /// <summary>
        /// Safety net: if a scene without a SceneEntry is started (e.g. an empty default scene),
        /// boot the game anyway.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureEntryInFirstScene()
        {
            if (FindFirstObjectByType<SceneEntry>() != null) return;
            var go = new GameObject("SceneEntry (auto)");
            go.AddComponent<SceneEntry>().scene = SceneId.Boot;
        }
    }
}
