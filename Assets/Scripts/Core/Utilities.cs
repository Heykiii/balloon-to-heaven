using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BalloonToHeaven.Core
{
    /// <summary>Simple stack based pool for components. Objects are deactivated while pooled.</summary>
    public sealed class ComponentPool<T> where T : Component
    {
        readonly Func<T> factory;
        readonly Stack<T> stack;
        readonly Transform parent;

        public ComponentPool(Func<T> factory, Transform parent, int prewarm = 0)
        {
            this.factory = factory;
            this.parent = parent;
            stack = new Stack<T>(Mathf.Max(4, prewarm));
            for (int i = 0; i < prewarm; i++) Release(Create());
        }

        public int PooledCount => stack.Count;

        T Create()
        {
            var item = factory();
            if (parent != null) item.transform.SetParent(parent, false);
            return item;
        }

        public T Get()
        {
            T item = null;
            while (stack.Count > 0 && item == null) item = stack.Pop();
            if (item == null) item = Create();
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            stack.Push(item);
        }
    }

    /// <summary>Tiny coroutine tween helper for UI and presentation (not for gameplay logic).</summary>
    public sealed class TweenRunner : MonoBehaviour
    {
        static TweenRunner instance;

        static TweenRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[Tweens]");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<TweenRunner>();
                }
                return instance;
            }
        }

        /// <summary>Runs update(t) for t in 0..1 using unscaled time. Stops early when owner is destroyed.</summary>
        public static Coroutine Run(UnityEngine.Object owner, float duration, Action<float> update, Action done = null, float delay = 0f)
        {
            return Instance.StartCoroutine(Routine(owner, duration, update, done, delay));
        }

        public static void Delay(UnityEngine.Object owner, float delay, Action action)
        {
            Instance.StartCoroutine(Routine(owner, 0f, null, action, delay));
        }

        static IEnumerator Routine(UnityEngine.Object owner, float duration, Action<float> update, Action done, float delay)
        {
            bool hasOwner = owner != null;
            if (delay > 0f)
            {
                float d = 0f;
                while (d < delay)
                {
                    d += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            float t = 0f;
            while (t < duration)
            {
                if (hasOwner && owner == null) yield break;
                update?.Invoke(Mathf.Clamp01(t / duration));
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (hasOwner && owner == null) yield break;
            update?.Invoke(1f);
            done?.Invoke();
        }
    }

    public static class Ease
    {
        public static float OutCubic(float t) { t = 1f - t; return 1f - t * t * t; }
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }

    /// <summary>Gameplay area derived from the orthographic camera and the configured width cap.</summary>
    public struct Playfield
    {
        public float HalfWidth;
        public float HalfHeight;
        public float VisibleHalfWidth;

        public static Playfield FromCamera(Camera cam, GameConfig config)
        {
            float halfHeight = cam != null ? cam.orthographicSize : config.cameraOrthoSize;
            float aspect = cam != null ? cam.aspect : 9f / 16f;
            float visible = halfHeight * aspect;
            return new Playfield
            {
                HalfHeight = halfHeight,
                VisibleHalfWidth = visible,
                HalfWidth = Mathf.Min(visible, config.maxPlayfieldHalfWidth)
            };
        }

        public float Top => HalfHeight;
        public float Bottom => -HalfHeight;

        public float ClampX(float x, float radius, float margin)
        {
            float limit = Mathf.Max(0.1f, HalfWidth - radius - margin);
            return Mathf.Clamp(x, -limit, limit);
        }
    }

    /// <summary>Static information carried between scenes.</summary>
    public static class GameSession
    {
        public static int SelectedLevel = 1;
        public static string SelectedBalloonId;
        public static Levels.LevelResult LastResult;
        public static bool ShowCampaignEnding;
    }

    /// <summary>Applies simple quality presets suited to mobile.</summary>
    public static class QualityManager
    {
        public static int CurrentLevel { get; private set; } = 2;

        /// <summary>Multiplier applied to particle counts and decorative density.</summary>
        public static float EffectsDensity => CurrentLevel == 0 ? 0.45f : CurrentLevel == 1 ? 0.75f : 1f;

        public static void Apply(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 0, 2);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = CurrentLevel == 2 ? 2 : 0;
            QualitySettings.shadows = ShadowQuality.Disable;
        }
    }
}
