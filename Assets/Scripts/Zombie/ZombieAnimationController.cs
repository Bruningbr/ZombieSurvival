using UnityEngine;

namespace ZombieSurvival.Zombie
{
    [RequireComponent(typeof(Animator))]
    public class ZombieAnimationController : MonoBehaviour
    {
        [Header("Ragdoll")]
        [SerializeField] private bool enableRagdollOnDeath = true;

        private Animator animator;
        private ZombieAI zombieAI;
        private Rigidbody[] ragdollBodies;
        private Collider[] ragdollColliders;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeathHash = Animator.StringToHash("Death");
        private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");
        private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            zombieAI = GetComponent<ZombieAI>();

            ragdollBodies = GetComponentsInChildren<Rigidbody>();
            ragdollColliders = GetComponentsInChildren<Collider>();

            DisableRagdoll();
        }

        private void DisableRagdoll()
        {
            foreach (var rb in ragdollBodies)
            {
                if (rb.gameObject != gameObject)
                {
                    rb.isKinematic = true;
                }
            }
            foreach (var col in ragdollColliders)
            {
                if (col.gameObject != gameObject)
                {
                    col.enabled = false;
                }
            }
        }

        public void EnableRagdoll()
        {
            if (!enableRagdollOnDeath) return;

            animator.enabled = false;

            foreach (var rb in ragdollBodies)
            {
                if (rb.gameObject != gameObject)
                {
                    rb.isKinematic = false;
                }
            }
            foreach (var col in ragdollColliders)
            {
                if (col.gameObject != gameObject)
                {
                    col.enabled = true;
                }
            }
        }

        public void PlayAttackAnimation(int attackType = 0)
        {
            animator.SetInteger(AttackTypeHash, attackType);
            animator.SetTrigger(AttackHash);
        }

        public void PlayHitAnimation()
        {
            animator.SetTrigger(HitHash);
        }

        public void PlayDeathAnimation()
        {
            if (enableRagdollOnDeath)
            {
                EnableRagdoll();
            }
            else
            {
                animator.SetTrigger(DeathHash);
            }
        }

        public void UpdateMovementAnimation(float speed)
        {
            animator.SetFloat(SpeedHash, speed);
        }

        public void SetAlertState(bool isAlert)
        {
            animator.SetBool(IsAlertHash, isAlert);
        }
    }
}
