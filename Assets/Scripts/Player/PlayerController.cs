using UnityEngine;
using Mirror;

namespace ZombieSurvival.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(PlayerStamina))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 9f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundCheckDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;

        [Header("Camera Settings")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 80f;
        [SerializeField] private float headBobFrequency = 2f;
        [SerializeField] private float headBobAmplitude = 0.05f;

        [Header("Crouch Settings")]
        [SerializeField] private float standingHeight = 2f;
        [SerializeField] private float crouchHeight = 1.2f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        private CharacterController characterController;
        private PlayerHealth playerHealth;
        private PlayerStamina playerStamina;
        private Camera playerCamera;

        private Vector3 velocity;
        private float xRotation;
        private float defaultYPos;
        private float headBobTimer;
        private bool isGrounded;
        private bool isCrouching;
        private bool isSprinting;

        [SyncVar] private string playerName;

        public bool IsSprinting => isSprinting;
        public bool IsCrouching => isCrouching;
        public bool IsGrounded => isGrounded;
        public bool IsMoving => characterController.velocity.magnitude > 0.1f;
        public string PlayerName => playerName;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerHealth = GetComponent<PlayerHealth>();
            playerStamina = GetComponent<PlayerStamina>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            playerCamera = Camera.main;
            if (playerCamera != null && cameraHolder != null)
            {
                playerCamera.transform.SetParent(cameraHolder);
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }

            if (cameraHolder != null)
            {
                defaultYPos = cameraHolder.localPosition.y;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            CmdSetPlayerName($"Player_{netId}");
        }

        [Command]
        private void CmdSetPlayerName(string name)
        {
            playerName = name;
        }

        private void Update()
        {
            if (!isLocalPlayer || playerHealth.IsDead) return;

            HandleGroundCheck();
            HandleMovement();
            HandleMouseLook();
            HandleCrouch();
            HandleHeadBob();
        }

        private void HandleGroundCheck()
        {
            isGrounded = Physics.CheckSphere(
                transform.position + Vector3.down * (characterController.height / 2f),
                groundCheckDistance,
                groundMask
            );

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 direction = transform.right * horizontal + transform.forward * vertical;
            direction = Vector3.ClampMagnitude(direction, 1f);

            // Sprint logic
            isSprinting = Input.GetKey(KeyCode.LeftShift) && vertical > 0 && !isCrouching
                          && playerStamina.CurrentStamina > 0;

            float currentSpeed;
            if (isCrouching)
                currentSpeed = crouchSpeed;
            else if (isSprinting)
                currentSpeed = sprintSpeed;
            else
                currentSpeed = walkSpeed;

            if (isSprinting)
            {
                playerStamina.UseStamina(Time.deltaTime);
            }

            characterController.Move(direction * currentSpeed * Time.deltaTime);

            // Jump
            if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                playerStamina.UseStamina(0.15f);
            }

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }

            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleCrouch()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            {
                isCrouching = !isCrouching;
            }

            float targetHeight = isCrouching ? crouchHeight : standingHeight;
            characterController.height = Mathf.Lerp(
                characterController.height,
                targetHeight,
                crouchTransitionSpeed * Time.deltaTime
            );
        }

        private void HandleHeadBob()
        {
            if (!isGrounded || cameraHolder == null) return;

            if (IsMoving)
            {
                float bobSpeed = isSprinting ? headBobFrequency * 1.5f : headBobFrequency;
                headBobTimer += Time.deltaTime * bobSpeed;
                float bobOffset = Mathf.Sin(headBobTimer * Mathf.PI * 2f) * headBobAmplitude;

                Vector3 pos = cameraHolder.localPosition;
                pos.y = defaultYPos + bobOffset;
                cameraHolder.localPosition = pos;
            }
            else
            {
                headBobTimer = 0;
                Vector3 pos = cameraHolder.localPosition;
                pos.y = Mathf.Lerp(pos.y, defaultYPos, Time.deltaTime * 5f);
                cameraHolder.localPosition = pos;
            }
        }

        public void SetSensitivity(float sensitivity)
        {
            mouseSensitivity = sensitivity;
        }
    }
}
