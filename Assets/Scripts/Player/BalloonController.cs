using BalloonToHeaven.Balloons;
using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Visuals;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BalloonToHeaven.Player
{
    /// <summary>
    /// Touch-and-drag horizontal control. The finger position is converted into a smoothed target;
    /// the balloon accelerates towards it with a capped velocity, keeps slight inertia when released
    /// and never teleports. Touches that start on UI buttons are ignored.
    /// </summary>
    public sealed class BalloonController : MonoBehaviour
    {
        LevelContext ctx;
        BalloonVisual visual;
        GameConfig cfg;
        float x;
        float velocity;
        float targetX;
        float smoothedTarget;
        bool dragging;
        int fingerId = -1;
        float dragOffset;
        float speedMultiplier = 1f;

        public bool InputEnabled { get; set; } = true;
        /// <summary>Additional velocity from wind and gravity effects (units/second), set every frame.</summary>
        public float ExternalVelocity { get; set; }
        /// <summary>0..1, reduces grip (ice).</summary>
        public float Slipperiness { get; set; }

        public float X => x;
        public float Velocity => velocity;
        public bool IsDragging => dragging;
        public BalloonVisual Visual => visual;

        public void Initialize(LevelContext context, BalloonVisual balloonVisual, float statsSpeedMultiplier)
        {
            ctx = context;
            cfg = context.Config;
            visual = balloonVisual;
            speedMultiplier = Mathf.Max(0.5f, statsSpeedMultiplier);
            x = 0f;
            targetX = smoothedTarget = 0f;
            if (visual != null) visual.SetMaxTilt(cfg.maxTiltDegrees);
        }

        public void SetVerticalPosition(float y)
        {
            transform.position = new Vector3(x, y, 0f);
        }

        float ScreenToWorldX(Vector2 screen)
        {
            var cam = ctx.Camera;
            if (cam == null) return x;
            return cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 20f)).x;
        }

        static bool OverUI(int pointerId)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            return pointerId < 0 ? es.IsPointerOverGameObject() : es.IsPointerOverGameObject(pointerId);
        }

        void ReadInput()
        {
            if (!InputEnabled)
            {
                dragging = false;
                fingerId = -1;
                return;
            }

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (fingerId < 0 && touch.phase == TouchPhase.Began && !OverUI(touch.fingerId))
                    {
                        fingerId = touch.fingerId;
                        dragging = true;
                        dragOffset = (x - ScreenToWorldX(touch.position)) * 0.35f; // mostly direct, but no jump
                        targetX = ScreenToWorldX(touch.position) + dragOffset;
                        smoothedTarget = x;
                    }
                    else if (touch.fingerId == fingerId)
                    {
                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            dragging = false;
                            fingerId = -1;
                        }
                        else
                        {
                            targetX = ScreenToWorldX(touch.position) * cfg.dragSensitivity + dragOffset;
                        }
                    }
                }
                return;
            }

            // Mouse (editor, WebGL on desktop)
            if (Input.GetMouseButtonDown(0) && !OverUI(-1))
            {
                dragging = true;
                dragOffset = (x - ScreenToWorldX(Input.mousePosition)) * 0.35f;
                targetX = ScreenToWorldX(Input.mousePosition) + dragOffset;
                smoothedTarget = x;
            }
            else if (dragging && Input.GetMouseButton(0))
            {
                targetX = ScreenToWorldX(Input.mousePosition) * cfg.dragSensitivity + dragOffset;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }

            // Keyboard support for desktop testing
            float axis = Input.GetAxisRaw("Horizontal");
            if (!dragging && Mathf.Abs(axis) > 0.1f)
            {
                targetX = x + axis * 2.5f;
                smoothedTarget = targetX;
                keyboardSteering = true;
            }
            else keyboardSteering = false;
        }

        bool keyboardSteering;

        void Update()
        {
            if (ctx == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            ReadInput();

            var mods = new MovementModifiers { acceleration = 1f, maxSpeed = 1f };
            ctx.Abilities?.ModifyMovement(ref mods);
            float grip = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(Slipperiness));
            float maxSpeed = cfg.maxHorizontalVelocity * speedMultiplier * mods.maxSpeed;
            float accel = cfg.horizontalAcceleration * speedMultiplier * mods.acceleration * grip;

            float limit = ctx.Field.HalfWidth - cfg.balloonCollisionRadius - cfg.screenEdgeMargin;
            targetX = Mathf.Clamp(targetX, -limit, limit);

            if (dragging || keyboardSteering)
            {
                float smoothing = Mathf.Max(0.01f, cfg.movementSmoothing);
                smoothedTarget = Mathf.Lerp(smoothedTarget, targetX, 1f - Mathf.Exp(-dt / smoothing));
                float desired = Mathf.Clamp((smoothedTarget - x) * cfg.horizontalSpeed * speedMultiplier * mods.maxSpeed, -maxSpeed, maxSpeed);
                velocity = Mathf.MoveTowards(velocity, desired, accel * dt);
            }
            else
            {
                // Inertia: glide to a stop. Ice makes the glide longer.
                velocity *= Mathf.Exp(-cfg.releaseDamping * grip * dt);
                smoothedTarget = x;
            }

            float newX = x + (velocity + ExternalVelocity) * dt;
            float clamped = ctx.Field.ClampX(newX, cfg.balloonCollisionRadius, cfg.screenEdgeMargin);
            if (!Mathf.Approximately(clamped, newX)) velocity = 0f;
            x = clamped;

            var p = transform.position;
            p.x = x;
            transform.position = p;
            if (visual != null) visual.SetLateralVelocity(velocity + ExternalVelocity * 0.5f);
        }

        /// <summary>Instant short-range blink used by the Quantum balloon.</summary>
        public void Teleport(float deltaX)
        {
            float from = x;
            x = ctx.Field.ClampX(x + deltaX, cfg.balloonCollisionRadius, cfg.screenEdgeMargin);
            targetX = smoothedTarget = x;
            if (dragging) dragOffset += x - from;
            var p = transform.position;
            p.x = x;
            transform.position = p;
            ParticleFactory.Burst(new Vector3(from, p.y, -1f), new Color(0.4f, 1f, 1f), 18, 4f, 0.3f);
            ParticleFactory.Burst(new Vector3(x, p.y, -1f), Color.white, 18, 3f, 0.3f);
        }
    }
}
