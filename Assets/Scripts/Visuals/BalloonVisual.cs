using System.Collections.Generic;
using BalloonToHeaven.Balloons;
using UnityEngine;

namespace BalloonToHeaven.Visuals
{
    /// <summary>
    /// Presentation of the balloon: floating bob, fabric breathing, basket swing driven by lateral
    /// velocity, tilt, hit flash, invulnerability blink, shield bubble and invisibility ghost.
    /// Contains no gameplay logic.
    /// </summary>
    public sealed class BalloonVisual : MonoBehaviour
    {
        public Transform swayRoot, envelope, basket, flame, aura;
        public GameObject shield, ghost;

        readonly List<Material> flashMaterials = new List<Material>();
        readonly List<(Transform t, int side, Quaternion baseRot)> flappers = new List<(Transform, int, Quaternion)>();
        readonly List<(Transform t, Vector3 speed)> spinners = new List<(Transform, Vector3)>();
        readonly List<(Transform t, float amp, float freq, Vector3 basePos)> bobbers = new List<(Transform, float, float, Vector3)>();
        ParticleSystem signatureTrail, cosmeticTrail;

        float lateralVelocity;
        float basketAngle, basketVelocity;
        float flashTimer;
        bool blinking;
        float maxTilt = 14f;
        Vector3 envelopeBaseScale = Vector3.one;
        float seed;
        bool deflating;
        float deflateT;

        public BalloonData Data { get; private set; }

        public void Initialize(BalloonData data)
        {
            Data = data;
            seed = Random.value * 10f;
            if (envelope != null) envelopeBaseScale = envelope.localScale;
        }

        public void RegisterFlashMaterial(Material m) { if (m != null) flashMaterials.Add(m); }
        public void AddFlapper(Transform t, int side) => flappers.Add((t, side, t.localRotation));
        public void AddSpinner(Transform t, Vector3 degreesPerSecond) => spinners.Add((t, degreesPerSecond));
        public void AddBobber(Transform t, float amplitude, float frequency) => bobbers.Add((t, amplitude, frequency, t.localPosition));
        public void SetSignatureTrail(ParticleSystem ps) => signatureTrail = ps;
        public void SetCosmeticTrail(ParticleSystem ps) => cosmeticTrail = ps;

        public void SetMaxTilt(float degrees) => maxTilt = degrees;

        /// <summary>Called by the controller each frame with the current horizontal velocity.</summary>
        public void SetLateralVelocity(float velocity) => lateralVelocity = velocity;

        public void SetShield(bool active)
        {
            if (shield != null && shield.activeSelf != active) shield.SetActive(active);
        }

        public void SetInvisible(bool invisible)
        {
            if (swayRoot != null) swayRoot.gameObject.SetActive(!invisible);
            if (ghost != null) ghost.SetActive(invisible);
        }

        public void SetBlinking(bool value)
        {
            blinking = value;
            if (!value && swayRoot != null && ghost != null && !ghost.activeSelf) swayRoot.gameObject.SetActive(true);
        }

        public void Flash() => flashTimer = 0.25f;

        public void SetTrailsEmitting(bool emitting)
        {
            SetEmitting(signatureTrail, emitting);
            SetEmitting(cosmeticTrail, emitting);
        }

        static void SetEmitting(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            var em = ps.emission;
            em.enabled = on;
        }

        /// <summary>Failure animation: the envelope shrinks and wobbles.</summary>
        public void Deflate() { deflating = true; deflateT = 0f; }

        void Update()
        {
            float t = Time.time + seed;
            float dt = Time.deltaTime;

            // Gentle floating bob and tilt towards movement direction
            if (swayRoot != null)
            {
                float bob = Mathf.Sin(t * 1.6f) * 0.08f;
                swayRoot.localPosition = new Vector3(0, bob, 0);
                float tilt = Mathf.Clamp(-lateralVelocity * 1.6f, -maxTilt, maxTilt) + Mathf.Sin(t * 0.9f) * 2f;
                swayRoot.localRotation = Quaternion.Slerp(swayRoot.localRotation, Quaternion.Euler(0, Mathf.Sin(t * 0.4f) * 10f, tilt), 1f - Mathf.Exp(-8f * dt));
            }

            // Basket swings like a pendulum, pushed by lateral acceleration
            if (basket != null && dt > 0f)
            {
                float target = Mathf.Clamp(lateralVelocity * 2.2f, -22f, 22f);
                basketVelocity += ((target - basketAngle) * 30f - basketVelocity * 4f) * dt;
                basketAngle += basketVelocity * dt;
                basket.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.3f) * 2f, 0, basketAngle);
            }

            // Fabric breathing
            if (envelope != null && !deflating)
            {
                float breathe = 1f + Mathf.Sin(t * 2.3f) * 0.015f;
                envelope.localScale = new Vector3(envelopeBaseScale.x * breathe, envelopeBaseScale.y * (2f - breathe), envelopeBaseScale.z * breathe);
            }
            if (deflating && envelope != null)
            {
                deflateT += dt;
                float k = Mathf.Clamp01(deflateT / 1.2f);
                float wobble = 1f + Mathf.Sin(deflateT * 25f) * 0.08f * (1f - k);
                envelope.localScale = new Vector3(envelopeBaseScale.x * Mathf.Lerp(1f, 0.45f, k) * wobble, envelopeBaseScale.y * Mathf.Lerp(1f, 0.35f, k), envelopeBaseScale.z * Mathf.Lerp(1f, 0.45f, k) * wobble);
            }

            if (flame != null)
            {
                float flicker = 0.85f + Mathf.PerlinNoise(t * 8f, 0.3f) * 0.4f;
                flame.localScale = Vector3.one * 0.42f * flicker;
            }

            for (int i = 0; i < flappers.Count; i++)
            {
                var f = flappers[i];
                if (f.t == null) continue;
                f.t.localRotation = f.baseRot * Quaternion.Euler(0, 0, Mathf.Sin(t * 5f) * 18f * f.side);
            }
            for (int i = 0; i < spinners.Count; i++)
            {
                var s = spinners[i];
                if (s.t != null) s.t.Rotate(s.speed * dt, Space.Self);
            }
            for (int i = 0; i < bobbers.Count; i++)
            {
                var b = bobbers[i];
                if (b.t != null) b.t.localPosition = b.basePos + Vector3.up * Mathf.Sin(t * b.freq) * b.amp;
            }
            if (aura != null) aura.localScale = envelopeBaseScale * (1.18f + Mathf.Sin(t * 3f) * 0.03f);

            // Hit flash
            if (flashTimer > 0f || flashMaterials.Count > 0)
            {
                flashTimer = Mathf.Max(0f, flashTimer - dt);
                float f = flashTimer > 0f ? Mathf.Clamp01(flashTimer / 0.25f) * 0.8f : 0f;
                for (int i = 0; i < flashMaterials.Count; i++) flashMaterials[i].SetFloat(MaterialLibrary.FlashId, f);
            }

            // Invulnerability blink (only toggles visibility of the main model)
            if (blinking && swayRoot != null && (ghost == null || !ghost.activeSelf))
            {
                bool visible = Mathf.Repeat(Time.time * 12f, 1f) > 0.35f;
                if (swayRoot.gameObject.activeSelf != visible) swayRoot.gameObject.SetActive(visible);
            }
        }

        void OnDestroy()
        {
            foreach (var m in flashMaterials) if (m != null) Destroy(m);
        }
    }
}
