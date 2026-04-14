using UnityEngine;
using Mirror;

namespace ZombieSurvival.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : NetworkBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string isGroundedParam = "IsGrounded";
        [SerializeField] private string isSprintingParam = "IsSprinting";
        [SerializeField] private string isCrouchingParam = "IsCrouching";
        [SerializeField] private string isAimingParam = "IsAiming";
        [SerializeField] private string shootTrigger = "Shoot";
        [SerializeField] private string reloadTrigger = "Reload";
        [SerializeField] private string meleeTrigger = "Melee";
        [SerializeField] private string deathTrigger = "Death";
        [SerializeField] private string reviveTrigger = "Revive";
        [SerializeField] private string hitTrigger = "Hit";
        [SerializeField] private string jumpTrigger = "Jump";

        [Header("Smoothing")]
        [SerializeField] private float animationSmoothTime = 0.1f;

        private Animator animator;
        private PlayerController playerController;
        private PlayerHealth playerHealth;
        private CharacterController characterController;

        private float currentSpeed;
        private float speedVelocity;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
            playerHealth = GetComponent<PlayerHealth>();
            characterController = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath += PlayDeathAnimation;
                playerHealth.OnRevived += PlayReviveAnimation;
                playerHealth.OnDamageTaken += PlayHitAnimation;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath -= PlayDeathAnimation;
                playerHealth.OnRevived -= PlayReviveAnimation;
                playerHealth.OnDamageTaken -= PlayHitAnimation;
            }
        }

        private void Update()
        {
            if (animator == null || playerController == null) return;

            float targetSpeed = characterController.velocity.magnitude;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, animationSmoothTime);

            animator.SetFloat(speedParam, currentSpeed);
            animator.SetBool(isGroundedParam, playerController.IsGrounded);
            animator.SetBool(isSprintingParam, playerController.IsSprinting);
            animator.SetBool(isCrouchingParam, playerController.IsCrouching);
        }

        public void PlayShootAnimation()
        {
            animator.SetTrigger(shootTrigger);
        }

        public void PlayReloadAnimation()
        {
            animator.SetTrigger(reloadTrigger);
        }

        public void PlayMeleeAnimation()
        {
            animator.SetTrigger(meleeTrigger);
        }

        public void PlayJumpAnimation()
        {
            animator.SetTrigger(jumpTrigger);
        }

        public void SetAiming(bool isAiming)
        {
            animator.SetBool(isAimingParam, isAiming);
        }

        private void PlayDeathAnimation()
        {
            animator.SetTrigger(deathTrigger);
        }

        private void PlayReviveAnimation()
        {
            animator.SetTrigger(reviveTrigger);
        }

        private void PlayHitAnimation(float damage)
        {
            animator.SetTrigger(hitTrigger);
        }
    }
}
