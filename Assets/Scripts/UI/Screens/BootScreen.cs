using System.Collections;
using BalloonToHeaven.Audio;
using BalloonToHeaven.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonToHeaven.UI
{
    /// <summary>Logo + loading bar. Pre-renders the menu music while the bar fills, then opens the main menu.</summary>
    public sealed class BootScreen : MonoBehaviour
    {
        Image fill;
        Text status;

        void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            var screen = UIFactory.CreateScreen("Boot", 0);
            ScreenCommon.GradientBackground(screen, new Color(0.25f, 0.5f, 0.95f), new Color(0.98f, 0.8f, 0.6f));

            var glow = UIFactory.Image("Glow", screen, new Color(1f, 0.95f, 0.7f, 0.6f), SpriteFactory.SoftGlow);
            UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(900f, 900f));

            var icon = UIFactory.Image("Logo", screen, new Color(1f, 0.45f, 0.35f), SpriteFactory.BalloonIcon);
            UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0f, 80f), new Vector2(360f, 360f));
            TweenRunner.Run(icon, 1.6f, t => icon.rectTransform.anchoredPosition = new Vector2(0f, 80f + Mathf.Sin(t * Mathf.PI * 2f) * 18f));

            var title = UIFactory.Label(screen, "Balloon to Heaven", 104, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0f, -200f), new Vector2(1000f, 160f));
            var sub = UIFactory.Label(screen, "Rise above the clouds", 46, new Color(1f, 1f, 1f, 0.85f));
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0f, -310f), new Vector2(900f, 80f));

            var bar = UIFactory.Bar(screen, new Color(0f, 0f, 0f, 0.3f), UITheme.Gold, out fill);
            UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(640f, 44f));
            status = UIFactory.Label(screen, "Loading...", 36, new Color(1f, 1f, 1f, 0.8f));
            UIFactory.Place(status.rectTransform, new Vector2(0.5f, 0.18f), new Vector2(0f, -70f), new Vector2(800f, 60f));

            UIFactory.PopIn(title.rectTransform, 0.5f);
            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            var services = GameServices.Instance;
            fill.fillAmount = 0.1f;
            yield return null;

            status.text = "Checking save data...";
            if (!services.Database.IsValid(out var error))
            {
                Debug.LogWarning("[Balloon to Heaven] Game data problem: " + error);
                status.text = "Game data problem: " + error;
            }
            fill.fillAmount = 0.35f;
            yield return null;

            status.text = "Tuning the music...";
            yield return null;
            services.Audio.PlayMusic(MusicCue.Menu, null);
            fill.fillAmount = 0.75f;
            yield return null;

            for (float t = 0; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                fill.fillAmount = Mathf.Lerp(0.75f, 1f, t / 0.6f);
                yield return null;
            }
            fill.fillAmount = 1f;
            services.LoadScene(SceneId.MainMenu);
        }
    }
}
