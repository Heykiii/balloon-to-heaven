using BalloonToHeaven.Balloons;
using BalloonToHeaven.Visuals;
using UnityEngine;

namespace BalloonToHeaven.Levels
{
    /// <summary>
    /// World-specific environmental mechanics: global wind gusts / unpredictable wind, slippery ice,
    /// gravity distortion wells, darkness (vignette around the balloon) and light/shadow zones.
    /// Balloon abilities reduce these through <see cref="IEnvironmentModifier"/>.
    /// </summary>
    public sealed class EnvironmentEffects
    {
        readonly LevelContext ctx;
        float zoneWind;
        float gustTarget, gust;
        float gustTimer;
        float gravityTimer;
        float gravityTarget;
        float gravityCenter;
        Transform darknessQuad;
        Material darknessMaterial;
        Transform gravityWell;
        float shadowPhase;

        public EnvironmentEffects(LevelContext context, Transform parent)
        {
            ctx = context;
            var level = ctx.Level;
            if (level.darkness > 0f || level.lightShadowZones)
            {
                // Large vignette quad in front of the gameplay plane that follows the balloon.
                darknessMaterial = MaterialLibrary.UnlitInstance(new Color(0.02f, 0.02f, 0.06f, 0f), ProceduralTextures.Vignette);
                var r = ModelKit.Part("Darkness", parent, MeshLibrary.Quad, darknessMaterial, new Vector3(0, ctx.BalloonY, -4f), new Vector3(60f, 60f, 1f));
                darknessQuad = r.transform;
            }
            if (level.gravityDistortion > 0f)
            {
                var well = ModelKit.Glow("GravityWell", parent, new Vector3(0, 0, 2f), 4f, new Color(0.6f, 0.4f, 1f, 0.35f), true, ProceduralTextures.Ring);
                gravityWell = well.transform;
                gravityWell.gameObject.SetActive(false);
            }
        }

        /// <summary>Push from overlapping wind-current zones, reported by the obstacle system.</summary>
        public void SetZoneWind(float value) => zoneWind = value;

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            var level = ctx.Level;
            var mods = ctx.Abilities != null ? ctx.Abilities.EnvironmentMultipliers() : new EnvironmentModifiers { wind = 1, slippery = 1, gravity = 1, darkness = 1 };

            // Global wind
            gustTimer -= dt;
            if (gustTimer <= 0f)
            {
                switch (level.windMode)
                {
                    case WindMode.Gusts:
                        gustTarget = gustTarget == 0f ? (ctx.Rng.NextDouble() < 0.5 ? -1f : 1f) * level.windStrength : 0f;
                        gustTimer = gustTarget == 0f ? 2.5f : 2f;
                        break;
                    case WindMode.Unpredictable:
                        gustTarget = ((float)ctx.Rng.NextDouble() * 2f - 1f) * level.windStrength;
                        gustTimer = 1.2f + (float)ctx.Rng.NextDouble() * 1.5f;
                        break;
                    default:
                        gustTarget = 0f;
                        gustTimer = 10f;
                        break;
                }
            }
            gust = Mathf.MoveTowards(gust, gustTarget, dt * 2.5f);

            // Gravity distortion: a well periodically appears and pulls the balloon sideways.
            float gravityPull = 0f;
            if (level.gravityDistortion > 0f)
            {
                gravityTimer -= dt;
                if (gravityTimer <= 0f)
                {
                    bool on = gravityTarget == 0f;
                    gravityTarget = on ? level.gravityDistortion : 0f;
                    gravityCenter = ((float)ctx.Rng.NextDouble() * 2f - 1f) * (ctx.Field.HalfWidth - 1.5f);
                    gravityTimer = on ? 3.5f : 3f;
                    if (gravityWell != null)
                    {
                        gravityWell.gameObject.SetActive(on && mods.gravity > 0f);
                        gravityWell.localPosition = new Vector3(gravityCenter, ctx.BalloonY + 1f, 2f);
                    }
                }
                if (gravityTarget > 0f)
                {
                    float dx = gravityCenter - ctx.Balloon.X;
                    gravityPull = Mathf.Clamp(dx, -1f, 1f) * gravityTarget * 3.2f * mods.gravity;
                    if (gravityWell != null) gravityWell.localScale = Vector3.one * (4f + Mathf.Sin(Time.time * 4f) * 0.4f);
                }
            }

            ctx.Balloon.ExternalVelocity = (zoneWind + gust) * mods.wind + gravityPull;
            ctx.Balloon.Slipperiness = level.slipperiness * mods.slippery;

            if (darknessQuad != null)
            {
                float darkness = level.darkness;
                if (level.lightShadowZones)
                {
                    shadowPhase += dt;
                    float zone = Mathf.Sin(shadowPhase * 0.7f) * 0.5f + 0.5f; // moving into and out of shadow
                    darkness = Mathf.Max(darkness, Mathf.SmoothStep(0f, 0.75f, zone));
                }
                darkness *= mods.darkness;
                darknessQuad.localPosition = new Vector3(ctx.Balloon.X, ctx.BalloonY + 1.5f, -4f);
                // The quad stays huge; texture tiling shrinks the clear area. Clamped edges stay dark.
                float clearSize = Mathf.Lerp(40f, 15f, darkness);
                float k = 60f / clearSize;
                darknessMaterial.mainTextureScale = new Vector2(k, k);
                darknessMaterial.mainTextureOffset = new Vector2((1f - k) * 0.5f, (1f - k) * 0.5f);
                darknessMaterial.SetColor(MaterialLibrary.ColorId, new Color(0.02f, 0.02f, 0.06f, Mathf.Clamp01(darkness * 1.2f)));
            }
        }
    }
}
