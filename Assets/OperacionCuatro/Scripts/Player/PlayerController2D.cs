using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OperacionCuatro
{
    /// <summary>
    /// Controlador de personaje jugable: movimiento lateral, salto, agacharse,
    /// daño, invulnerabilidad y muerte. Funciona para Jugador 1 y Jugador 2.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Movimiento")]
        public float moveSpeed = 6.4f;
        public float crouchSpeed = 2.6f;
        public float jumpVelocity = 12.4f;
        public float jumpCutMultiplier = 0.45f;

        [Header("Detección de suelo")]
        [Tooltip("Margen lateral que se suma a la huella real del collider para reconocer el apoyo en bordes y esquinas.")]
        public float groundProbeMargin = 0.06f;

        [Tooltip("Cuánto desciende la sonda por debajo de los pies del collider.")]
        public float groundProbeBelowFeet = 0.05f;

        [Tooltip("Cuánto asciende la sonda por encima de los pies para detectar el contacto exacto.")]
        public float groundProbeAboveFeet = 0.07f;

        [Tooltip("Tolerancia vertical para aceptar una superficie como suelo (descarta paredes y bloques tocados de lado).")]
        public float groundSurfaceTolerance = 0.12f;

        [Tooltip("Velocidad vertical máxima (valor absoluto) para considerar que el personaje está apoyado.")]
        public float groundMaxVerticalSpeed = 6f;

        [Header("Referencias")]
        public Transform visual;
        public SpriteRenderer spriteRenderer;
        public Animator animator;
        public Health health;
        public PlayerShooter shooter;
        public LayerMask groundMask;

        [Header("Input")]
        public InputActionAsset inputAsset;
        public int playerIndex;

        [Header("Estado")]
        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsDead { get; private set; }
        public bool FacingRight { get; private set; } = true;

        private Rigidbody2D body;
        private BoxCollider2D box;
        private PlayerInputReader input;

        private readonly List<Collider2D> groundHits = new List<Collider2D>();
        private ContactFilter2D groundFilter;

        private Vector2 standSize;
        private Vector2 standOffset;
        private Vector2 crouchSize;
        private Vector2 crouchOffset;

        private float coyoteTimer;
        private float jumpBufferTimer;
        private float invulnTimer;
        private float blinkTimer;
        private bool jumpHeldLast;

        private static readonly int ParamSpeed = Animator.StringToHash("Speed");
        private static readonly int ParamYSpeed = Animator.StringToHash("YSpeed");
        private static readonly int ParamGrounded = Animator.StringToHash("Grounded");
        private static readonly int ParamCrouch = Animator.StringToHash("Crouch");
        private static readonly int ParamDead = Animator.StringToHash("Dead");
        private static readonly int ParamHurt = Animator.StringToHash("Hurt");

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            if (box == null) box = gameObject.AddComponent<BoxCollider2D>();

            if (health == null) health = GetComponent<Health>();
            if (shooter == null) shooter = GetComponent<PlayerShooter>();

            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            groundFilter = new ContactFilter2D();
            groundFilter.useTriggers = false;
            groundFilter.useLayerMask = true;
            groundFilter.layerMask = groundMask;

            standSize = new Vector2(0.72f, 1.72f);
            standOffset = new Vector2(0f, 0.86f);
            crouchSize = new Vector2(0.72f, 1.06f);
            crouchOffset = new Vector2(0f, 0.53f);

            box.size = standSize;
            box.offset = standOffset;
        }

        private void Start()
        {
            if (input == null) Setup(playerIndex, inputAsset, true);
        }

        public void Setup(int index, InputActionAsset asset, bool enableInput = true)
        {
            playerIndex = index;
            inputAsset = asset;

            if (input != null) input.Disable();

            if (inputAsset != null)
            {
                input = new PlayerInputReader(inputAsset, "Player" + (index + 1), index);
                if (enableInput) input.Enable();
            }

            if (shooter != null)
            {
                shooter.Bind(this);
                shooter.RefillBombs();
            }

            if (animator != null)
            {
                animator.SetBool(ParamDead, false);
                animator.SetBool(ParamCrouch, false);
            }

            IsDead = false;
            if (health != null)
            {
                health.ResetHealth();
                health.invulnerable = false;
            }
        }

        public void SetInputEnabled(bool value)
        {
            if (input == null) return;
            if (value) input.Enable();
            else input.Disable();
        }

        private void Update()
        {
            if (input == null) return;

            if (IsDead)
            {
                if (animator != null) animator.SetFloat(ParamSpeed, 0f);
                return;
            }

            UpdateTimers();

            // El flanco de pulsacion se captura en Update: si se leyera solo en
            // FixedUpdate, una pulsacion podia caer entre dos pasos de fisica y perderse.
            if (input.JumpPressed) jumpBufferTimer = 0.12f;

            UpdateFacing();
            UpdateAnimatorParams();

            if (shooter != null)
            {
                if (input.FireHeld) shooter.TryFire(new Vector2(FacingRight ? 1f : -1f, 0f));
                if (input.BombPressed) shooter.TryBomb(new Vector2(FacingRight ? 1f : -1f, 0f));
            }

            if (InputPauseLocked) return;
        }

        private static bool InputPauseLocked
        {
            get { return GameManager.Instance != null && GameManager.Instance.IsPaused; }
        }

        private void UpdateTimers()
        {
            coyoteTimer -= Time.deltaTime;
            jumpBufferTimer -= Time.deltaTime;

            if (invulnTimer > 0f)
            {
                invulnTimer -= Time.deltaTime;
                blinkTimer -= Time.deltaTime;

                if (spriteRenderer != null)
                {
                    if (blinkTimer <= 0f)
                    {
                        blinkTimer = 0.08f;
                        spriteRenderer.enabled = !spriteRenderer.enabled;
                    }
                }

                if (invulnTimer <= 0f)
                {
                    invulnTimer = 0f;
                    if (spriteRenderer != null) spriteRenderer.enabled = true;
                    if (health != null) health.invulnerable = false;
                }
            }
        }

        private void UpdateFacing()
        {
            float axis = input.Move.x;
            if (Mathf.Abs(axis) > 0.15f)
            {
                bool right = axis > 0f;
                if (right != FacingRight) SetFacing(right);
            }
        }

        public void SetFacing(bool right)
        {
            FacingRight = right;
            if (visual != null)
            {
                var scale = visual.localScale;
                scale.x = Mathf.Abs(scale.x) * (right ? 1f : -1f);
                visual.localScale = scale;
            }
        }

        private void UpdateAnimatorParams()
        {
            if (animator == null) return;
            animator.SetFloat(ParamSpeed, Mathf.Abs(body.linearVelocity.x));
            animator.SetFloat(ParamYSpeed, body.linearVelocity.y);
            animator.SetBool(ParamGrounded, IsGrounded);
            animator.SetBool(ParamCrouch, IsCrouching);
        }

        private void FixedUpdate()
        {
            if (input == null || IsDead) return;

            if (InputPauseLocked)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }

            CheckGround();

            Vector2 move = input.Move;
            IsCrouching = IsGrounded && move.y < -0.55f;

            ApplyCrouchCollider(IsCrouching);

            float speed = IsCrouching ? crouchSpeed : moveSpeed;
            float targetX = move.x * speed;

            if (IsCrouching) targetX *= 0.8f;

            body.linearVelocity = new Vector2(targetX, body.linearVelocity.y);

            // Salto con margen de tolerancia (el buffer se rellena en Update)
            if (IsGrounded) coyoteTimer = 0.1f;

            if (jumpBufferTimer > 0f && coyoteTimer > 0f && !IsCrouching)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                IsGrounded = false;
                AudioManager.Sfx("sfx_jump", 0.5f);
            }

            // Salto variable: soltar antes corta el impulso
            if (!input.JumpHeld && body.linearVelocity.y > 0f)
                body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * 0.94f);

            jumpHeldLast = input.JumpHeld;

            if (body.linearVelocity.y > 22f)
                body.linearVelocity = new Vector2(body.linearVelocity.x, 22f);
        }

        private void ApplyCrouchCollider(bool crouch)
        {
            if (box == null) return;
            box.size = crouch ? crouchSize : standSize;
            box.offset = crouch ? crouchOffset : standOffset;
        }

        private void CheckGround()
        {
            // Sonda calculada a partir del collider real: ancho de la huella mas un
            // margen pequenio, y una profundidad corta alrededor de los pies. Asi se
            // detecta el apoyo en bordes y esquinas sin alcanzar el suelo estando en el aire.
            float bodyHalfWidth = (box != null ? box.size.x : 0.72f) * 0.5f;
            float feetLocalY = box != null ? box.offset.y - box.size.y * 0.5f : 0f;
            float offsetX = box != null ? box.offset.x : 0f;

            float below = Mathf.Max(0f, groundProbeBelowFeet);
            float above = Mathf.Max(0f, groundProbeAboveFeet);
            float height = Mathf.Max(0.02f, below + above);
            float halfWidth = bodyHalfWidth + Mathf.Max(0f, groundProbeMargin);

            Vector2 center = (Vector2)transform.position +
                new Vector2(offsetX, feetLocalY + above - height * 0.5f);

            groundFilter.layerMask = groundMask;
            groundHits.Clear();
            int count = Physics2D.OverlapBox(center, new Vector2(halfWidth * 2f, height), 0f, groundFilter, groundHits);

            float feetWorldY = transform.position.y + feetLocalY;
            bool supported = false;

            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit == null) continue;
                if (hit.transform.IsChildOf(transform)) continue;

                // Solo vale una superficie horizontal a la altura de los pies: descarta
                // paredes y bloques altos que la caja pudiera estar tocando de lado.
                if (hit.bounds.max.y > feetWorldY + groundSurfaceTolerance) continue;

                supported = true;
                break;
            }

            IsGrounded = supported && Mathf.Abs(body.linearVelocity.y) < groundMaxVerticalSpeed;
        }

        public void ApplySpawnInvulnerability(float seconds)
        {
            invulnTimer = seconds;
            if (health != null) health.invulnerable = true;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }

        public void BindDamage()
        {
            if (health == null) return;
            health.Damaged -= OnDamaged;
            health.Damaged += OnDamaged;
            health.Died -= OnDied;
            health.Died += OnDied;
        }

        private void OnDamaged(DamageInfo info)
        {
            if (IsDead) return;

            invulnTimer = 1.4f;
            blinkTimer = 0f;
            health.invulnerable = true;

            if (animator != null) animator.SetTrigger(ParamHurt);

            if (info.direction != Vector2.zero)
                body.linearVelocity = new Vector2(Mathf.Sign(info.direction.x) * 5.5f, 6f);

            AudioManager.Sfx("sfx_hit", 0.7f);
        }

        private void OnDied(DamageInfo info)
        {
            if (IsDead) return;
            IsDead = true;

            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (box != null) box.enabled = false;
            if (animator != null) animator.SetBool(ParamDead, true);
            if (input != null) input.Disable();

            body.linearVelocity = new Vector2(0f, 5f);

            AudioManager.Sfx("sfx_player_death", 0.8f);

            if (LevelManager.Instance != null) LevelManager.Instance.OnPlayerKilled(this);
        }

        public void Revive(Vector3 position)
        {
            transform.position = position;
            body.linearVelocity = Vector2.zero;

            IsDead = false;
            IsCrouching = false;

            if (box != null)
            {
                box.enabled = true;
                ApplyCrouchCollider(false);
            }

            if (animator != null)
            {
                animator.Rebind();
                animator.SetBool(ParamDead, false);
                animator.SetBool(ParamCrouch, false);
                animator.SetFloat(ParamSpeed, 0f);
            }

            if (spriteRenderer != null) spriteRenderer.enabled = true;

            if (health != null)
            {
                health.ResetHealth();
                health.invulnerable = false;
            }

            if (shooter != null) shooter.RefillBombs();
            if (input != null) input.Enable();

            ApplySpawnInvulnerability(2.2f);
        }

        public void ClearInvulnerability()
        {
            invulnTimer = 0f;
            if (health != null) health.invulnerable = false;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }

        private void OnDisable()
        {
            if (input != null) input.Disable();
        }
    }
}
