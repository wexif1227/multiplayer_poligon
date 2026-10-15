using UnityEngine;
using Poligon.Weapons;

namespace Poligon.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Move")]
        public float walkSpeed = 5.2f;
        public float runSpeed = 8.2f;
        public float jumpSpeed = 7.2f;
        public float gravity = 24f;
        public float mouseSensitivity = 2.0f;

        [Header("Feel")]
        public float headBobSpeed = 10f;
        public float headBobAmount = 0.045f;
        public float landBob = 0.12f;

        public Camera PlayerCamera;
        public WeaponController weapons;
        public Transform groundCheck;
        public LayerMask groundMask = ~0;

        public bool GodMode { get; set; }
        public WeaponId CurrentWeaponId => weapons != null ? weapons.CurrentId : WeaponId.ak47;
        public int Health { get; private set; } = 100;
        public float MoveSpeed01 { get; private set; }

        CharacterController _cc;
        float _yaw, _pitch;
        Vector3 _vel;
        bool _grounded, _wasGrounded;
        float _coyote;
        float _bobTimer;
        float _landOffset;
        Vector3 _camBaseLocal;
        Vector3 _spawnPos;
        const float CoyoteTime = 0.1f;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (PlayerCamera == null) PlayerCamera = GetComponentInChildren<Camera>();
            if (PlayerCamera != null) _camBaseLocal = PlayerCamera.transform.localPosition;
            _spawnPos = transform.position;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
            if (Cursor.lockState == CursorLockMode.Locked) Look();
            Move();
            AnimateCamera();
            if (weapons != null) weapons.Tick(this);
        }

        void Look()
        {
            _yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, -89f, 89f);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        void Move()
        {
            bool ccG = _cc.isGrounded;
            bool rayG = false;
            if (Physics.Raycast(transform.position + Vector3.up * 0.15f, Vector3.down, out var hit, 0.4f, groundMask, QueryTriggerInteraction.Ignore))
                if (hit.collider && !hit.collider.transform.IsChildOf(transform))
                    rayG = true;
            _grounded = ccG || rayG;

            if (_grounded) _coyote = CoyoteTime;
            else _coyote -= Time.deltaTime;

            if (!_wasGrounded && _grounded)
                _landOffset = -landBob;
            _wasGrounded = _grounded;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            var input = new Vector3(h, 0f, v);
            if (input.sqrMagnitude > 1f) input.Normalize();

            bool run = Input.GetKey(KeyCode.LeftShift) && v > 0.1f;
            float speed = run ? runSpeed : walkSpeed;
            MoveSpeed01 = input.magnitude * (run ? 1f : 0.55f) * (_grounded ? 1f : 0.2f);

            var wish = transform.TransformDirection(input) * speed;
            if (_grounded && _vel.y < 0f) _vel.y = -2f;

            if (_coyote > 0f && Input.GetButtonDown("Jump") && _vel.y <= 0.15f)
            {
                _vel.y = jumpSpeed;
                _coyote = 0f;
                _grounded = false;
            }

            _vel.y -= gravity * Time.deltaTime;
            var motion = wish * Time.deltaTime;
            motion.y = _vel.y * Time.deltaTime;
            _cc.Move(motion);
        }

        void AnimateCamera()
        {
            if (PlayerCamera == null) return;
            if (MoveSpeed01 > 0.05f && _grounded)
                _bobTimer += Time.deltaTime * headBobSpeed * (0.6f + MoveSpeed01);
            else
                _bobTimer = Mathf.Lerp(_bobTimer, 0f, Time.deltaTime * 5f);

            float bobY = Mathf.Sin(_bobTimer) * headBobAmount * MoveSpeed01;
            float bobX = Mathf.Cos(_bobTimer * 0.5f) * headBobAmount * 0.5f * MoveSpeed01;
            _landOffset = Mathf.Lerp(_landOffset, 0f, Time.deltaTime * 6f);

            PlayerCamera.transform.localPosition = _camBaseLocal + new Vector3(bobX, bobY + _landOffset, 0f);
            PlayerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, bobX * 8f);
        }

        public void ApplyDamage(int hp)
        {
            if (GodMode) { Health = 100; return; }
            Health = Mathf.Clamp(hp, 0, 100);
        }

        public void Respawn()
        {
            Health = 100;
            _cc.enabled = false;
            transform.position = _spawnPos;
            _cc.enabled = true;
            _vel = Vector3.zero;
        }

        public bool IsMoving => MoveSpeed01 > 0.08f;
        public bool IsGrounded => _grounded;
        public float Pitch => _pitch;
        public float Yaw => _yaw;
    }
}
