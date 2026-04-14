using UnityEngine;

namespace ZombieSurvival.Weapons
{
    public class Pistol : WeaponBase
    {
        protected override void Awake()
        {
            base.Awake();
            weaponName = "Pistol";
            weaponId = "pistol_m9";
            weaponType = WeaponType.Pistol;
            baseDamage = 25f;
            headshotMultiplier = 2.5f;
            range = 50f;
            fireRate = 0.3f;
            isAutomatic = false;
            magazineSize = 15;
            maxReserveAmmo = 90;
            reloadTime = 1.5f;
            hipSpread = 1.5f;
            aimSpread = 0.3f;
            recoilX = 0.05f;
            recoilY = 0.2f;
        }
    }

    public class AssaultRifle : WeaponBase
    {
        protected override void Awake()
        {
            base.Awake();
            weaponName = "Assault Rifle";
            weaponId = "rifle_ar15";
            weaponType = WeaponType.Rifle;
            baseDamage = 30f;
            headshotMultiplier = 2f;
            range = 100f;
            fireRate = 0.1f;
            isAutomatic = true;
            magazineSize = 30;
            maxReserveAmmo = 180;
            reloadTime = 2.5f;
            hipSpread = 2.5f;
            aimSpread = 0.5f;
            recoilX = 0.1f;
            recoilY = 0.25f;
        }
    }

    public class Shotgun : WeaponBase
    {
        [Header("Shotgun Settings")]
        [SerializeField] private int pelletsPerShot = 8;
        [SerializeField] private float pelletSpread = 5f;

        protected override void Awake()
        {
            base.Awake();
            weaponName = "Shotgun";
            weaponId = "shotgun_pump";
            weaponType = WeaponType.Shotgun;
            baseDamage = 15f; // per pellet
            headshotMultiplier = 1.5f;
            range = 25f;
            fireRate = 0.8f;
            isAutomatic = false;
            magazineSize = 8;
            maxReserveAmmo = 48;
            reloadTime = 3f;
            hipSpread = 4f;
            aimSpread = 2f;
            recoilX = 0.2f;
            recoilY = 0.6f;
        }

        protected override void CmdShoot(Vector3 direction)
        {
            if (currentAmmo <= 0) return;

            currentAmmo--;

            if (playerCamera != null)
            {
                for (int i = 0; i < pelletsPerShot; i++)
                {
                    Vector3 pelletDir = direction + new Vector3(
                        Random.Range(-pelletSpread, pelletSpread) * 0.01f,
                        Random.Range(-pelletSpread, pelletSpread) * 0.01f,
                        0
                    );

                    Ray ray = new Ray(playerCamera.transform.position, pelletDir.normalized);
                    if (Physics.Raycast(ray, out RaycastHit hit, range))
                    {
                        HandleHit(hit);
                    }
                }
            }

            RpcShootEffects(direction);
        }
    }

    public class SMG : WeaponBase
    {
        protected override void Awake()
        {
            base.Awake();
            weaponName = "SMG";
            weaponId = "smg_mp5";
            weaponType = WeaponType.SMG;
            baseDamage = 18f;
            headshotMultiplier = 2f;
            range = 50f;
            fireRate = 0.07f;
            isAutomatic = true;
            magazineSize = 40;
            maxReserveAmmo = 200;
            reloadTime = 2f;
            hipSpread = 3f;
            aimSpread = 1f;
            recoilX = 0.08f;
            recoilY = 0.15f;
        }
    }

    public class SniperRifle : WeaponBase
    {
        [Header("Sniper Settings")]
        [SerializeField] private float scopeZoom = 4f;

        protected override void Awake()
        {
            base.Awake();
            weaponName = "Sniper Rifle";
            weaponId = "sniper_awp";
            weaponType = WeaponType.Sniper;
            baseDamage = 100f;
            headshotMultiplier = 3f;
            range = 200f;
            fireRate = 1.5f;
            isAutomatic = false;
            magazineSize = 5;
            maxReserveAmmo = 30;
            reloadTime = 3.5f;
            hipSpread = 5f;
            aimSpread = 0.1f;
            recoilX = 0.3f;
            recoilY = 1f;
            adsFOV = 15f;
        }

        protected override void HandleAiming()
        {
            base.HandleAiming();

            // Scope overlay when aiming
            if (isAiming)
            {
                UI.GameHUD.Instance?.ShowScopeOverlay(true);
            }
            else
            {
                UI.GameHUD.Instance?.ShowScopeOverlay(false);
            }
        }
    }

    public class MeleeWeapon : WeaponBase
    {
        [Header("Melee Settings")]
        [SerializeField] private float meleeRange = 2.5f;
        [SerializeField] private float meleeAngle = 60f;
        [SerializeField] private float knockbackForce = 5f;

        protected override void Awake()
        {
            base.Awake();
            weaponName = "Knife";
            weaponId = "melee_knife";
            weaponType = WeaponType.Melee;
            baseDamage = 50f;
            headshotMultiplier = 2f;
            range = meleeRange;
            fireRate = 0.6f;
            isAutomatic = false;
            magazineSize = 1;
            maxReserveAmmo = 0;
            currentAmmo = 1;
        }

        protected override void TryShoot()
        {
            if (Time.time - lastFireTime < fireRate) return;
            lastFireTime = Time.time;
            CmdMeleeAttack();
        }

        [Command]
        private void CmdMeleeAttack()
        {
            Collider[] hits = Physics.OverlapSphere(
                playerCamera != null ? playerCamera.transform.position : transform.position,
                meleeRange
            );

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToTarget);

                if (angle > meleeAngle / 2f) continue;

                var zombie = hit.GetComponentInParent<Zombie.ZombieAI>();
                if (zombie != null)
                {
                    zombie.TakeDamage(baseDamage, connectionToClient?.identity?.gameObject);

                    var rb = hit.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(dirToTarget * knockbackForce, ForceMode.Impulse);
                    }
                }

                var playerHealth = hit.GetComponentInParent<Player.PlayerHealth>();
                if (playerHealth != null && playerHealth.gameObject != connectionToClient?.identity?.gameObject)
                {
                    playerHealth.TakeDamage(baseDamage, connectionToClient?.identity?.gameObject);
                }
            }

            RpcMeleeEffects();
        }

        [ClientRpc]
        private void RpcMeleeEffects()
        {
            var animController = GetComponentInParent<Player.PlayerAnimationController>();
            animController?.PlayMeleeAnimation();
            AudioManager.AudioManager.Instance?.PlaySFX("melee_swing");
        }

        protected override void TryReload() { } // No reload for melee
    }
}
