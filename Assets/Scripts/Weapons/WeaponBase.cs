using UnityEngine;
using Mirror;
using System;

namespace ZombieSurvival.Weapons
{
    public enum WeaponType
    {
        Pistol,
        Rifle,
        Shotgun,
        SMG,
        Sniper,
        Melee
    }

    public abstract class WeaponBase : NetworkBehaviour
    {
        [Header("Weapon Info")]
        [SerializeField] protected string weaponName = "Weapon";
        [SerializeField] protected string weaponId = "weapon_default";
        [SerializeField] protected WeaponType weaponType = WeaponType.Pistol;
        [SerializeField] protected Sprite weaponIcon;

        [Header("Damage")]
        [SerializeField] protected float baseDamage = 25f;
        [SerializeField] protected float headshotMultiplier = 2.5f;
        [SerializeField] protected float range = 100f;
        [SerializeField] protected float fireRate = 0.15f;
        [SerializeField] protected bool isAutomatic = false;

        [Header("Ammo")]
        [SerializeField] protected int magazineSize = 30;
        [SerializeField] protected int maxReserveAmmo = 120;
        [SerializeField] protected float reloadTime = 2f;

        [Header("Recoil")]
        [SerializeField] protected float recoilX = 0.1f;
        [SerializeField] protected float recoilY = 0.3f;
        [SerializeField] protected float aimRecoilMultiplier = 0.5f;
        [SerializeField] protected float recoilRecoverySpeed = 5f;

        [Header("Spread")]
        [SerializeField] protected float hipSpread = 2f;
        [SerializeField] protected float aimSpread = 0.5f;
        [SerializeField] protected float movingSpreadMultiplier = 1.5f;
        [SerializeField] protected float crouchSpreadMultiplier = 0.7f;

        [Header("Aim Down Sights")]
        [SerializeField] protected Vector3 adsPosition;
        [SerializeField] protected float adsSpeed = 8f;
        [SerializeField] protected float adsFOV = 40f;

        [Header("Effects")]
        [SerializeField] protected Transform muzzlePoint;
        [SerializeField] protected GameObject muzzleFlashPrefab;
        [SerializeField] protected GameObject bulletImpactPrefab;
        [SerializeField] protected GameObject bulletTracerPrefab;
        [SerializeField] protected Transform casingEjectPoint;
        [SerializeField] protected GameObject casingPrefab;

        [Header("Audio")]
        [SerializeField] protected AudioClip shootSound;
        [SerializeField] protected AudioClip reloadSound;
        [SerializeField] protected AudioClip emptySound;
        [SerializeField] protected AudioClip equipSound;

        [SyncVar] protected int currentAmmo;
        [SyncVar] protected int reserveAmmo;

        protected bool isReloading;
        protected bool isAiming;
        protected float lastFireTime;
        protected float currentRecoilX;
        protected float currentRecoilY;
        protected Vector3 defaultPosition;
        protected AudioSource audioSource;
        protected Camera playerCamera;

        public string WeaponName => weaponName;
        public string WeaponId => weaponId;
        public WeaponType Type => weaponType;
        public Sprite Icon => weaponIcon;
        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public int MagazineSize => magazineSize;
        public bool IsReloading => isReloading;
        public bool IsAiming => isAiming;
        public float FireRate => fireRate;

        public event Action OnFired;
        public event Action OnReloaded;
        public event Action<int, int> OnAmmoChanged; // current, reserve

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentAmmo = magazineSize;
            reserveAmmo = maxReserveAmmo;
        }

        protected virtual void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            defaultPosition = transform.localPosition;
        }

        protected virtual void Start()
        {
            playerCamera = Camera.main;
        }

        protected virtual void Update()
        {
            if (!isLocalPlayer) return;

            HandleInput();
            HandleAiming();
            HandleRecoilRecovery();
        }

        protected virtual void HandleInput()
        {
            // Shoot
            if (isAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0))
            {
                TryShoot();
            }

            // Aim
            isAiming = Input.GetMouseButton(1);

            // Reload
            if (Input.GetKeyDown(KeyCode.R))
            {
                TryReload();
            }
        }

        protected virtual void TryShoot()
        {
            if (isReloading) return;
            if (Time.time - lastFireTime < fireRate) return;

            if (currentAmmo <= 0)
            {
                PlayEmptySound();
                if (reserveAmmo > 0) TryReload();
                return;
            }

            lastFireTime = Time.time;
            CmdShoot(GetShootDirection());
        }

        [Command]
        protected virtual void CmdShoot(Vector3 direction)
        {
            if (currentAmmo <= 0) return;

            currentAmmo--;

            // Raycast for hit detection
            if (playerCamera != null)
            {
                Ray ray = new Ray(playerCamera.transform.position, direction);
                if (Physics.Raycast(ray, out RaycastHit hit, range))
                {
                    HandleHit(hit);
                }
            }

            RpcShootEffects(direction);
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        }

        [ClientRpc]
        protected virtual void RpcShootEffects(Vector3 direction)
        {
            PlayShootSound();
            SpawnMuzzleFlash();
            ApplyRecoil();
            OnFired?.Invoke();

            var animController = GetComponentInParent<Player.PlayerAnimationController>();
            animController?.PlayShootAnimation();
        }

        protected virtual void HandleHit(RaycastHit hit)
        {
            float damage = baseDamage;

            // Headshot detection
            if (hit.collider.CompareTag("Head"))
            {
                damage *= headshotMultiplier;
                RpcShowHitMarker(true);
            }
            else if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Zombie"))
            {
                RpcShowHitMarker(false);
            }

            // Damage zombie
            var zombie = hit.collider.GetComponentInParent<Zombie.ZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(damage, connectionToClient?.identity?.gameObject);
            }

            // Damage player (PvP if enabled)
            var playerHealth = hit.collider.GetComponentInParent<Player.PlayerHealth>();
            if (playerHealth != null && playerHealth.gameObject != connectionToClient?.identity?.gameObject)
            {
                playerHealth.TakeDamage(damage, connectionToClient?.identity?.gameObject);
            }

            RpcSpawnImpact(hit.point, hit.normal);
        }

        [ClientRpc]
        protected void RpcShowHitMarker(bool isHeadshot)
        {
            UI.GameHUD.Instance?.ShowHitMarker(isHeadshot);
        }

        [ClientRpc]
        protected void RpcSpawnImpact(Vector3 position, Vector3 normal)
        {
            if (bulletImpactPrefab != null)
            {
                var impact = Instantiate(bulletImpactPrefab, position, Quaternion.LookRotation(normal));
                Destroy(impact, 3f);
            }
        }

        protected Vector3 GetShootDirection()
        {
            if (playerCamera == null) return transform.forward;

            Vector3 direction = playerCamera.transform.forward;

            float spread = isAiming ? aimSpread : hipSpread;

            var playerController = GetComponentInParent<Player.PlayerController>();
            if (playerController != null)
            {
                if (playerController.IsMoving) spread *= movingSpreadMultiplier;
                if (playerController.IsCrouching) spread *= crouchSpreadMultiplier;
            }

            direction += new Vector3(
                UnityEngine.Random.Range(-spread, spread) * 0.01f,
                UnityEngine.Random.Range(-spread, spread) * 0.01f,
                0
            );

            return direction.normalized;
        }

        protected virtual void TryReload()
        {
            if (isReloading || currentAmmo >= magazineSize || reserveAmmo <= 0) return;
            CmdReload();
        }

        [Command]
        protected virtual void CmdReload()
        {
            if (isReloading || currentAmmo >= magazineSize || reserveAmmo <= 0) return;
            isReloading = true;
            RpcStartReload();
            Invoke(nameof(FinishReload), reloadTime);
        }

        [ClientRpc]
        protected void RpcStartReload()
        {
            isReloading = true;
            PlayReloadSound();

            var animController = GetComponentInParent<Player.PlayerAnimationController>();
            animController?.PlayReloadAnimation();
        }

        [Server]
        protected void FinishReload()
        {
            int ammoNeeded = magazineSize - currentAmmo;
            int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

            currentAmmo += ammoToReload;
            reserveAmmo -= ammoToReload;
            isReloading = false;

            RpcFinishReload();
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        }

        [ClientRpc]
        protected void RpcFinishReload()
        {
            isReloading = false;
            OnReloaded?.Invoke();
        }

        protected virtual void HandleAiming()
        {
            Vector3 targetPos = isAiming ? adsPosition : defaultPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, adsSpeed * Time.deltaTime);
        }

        protected virtual void ApplyRecoil()
        {
            float multiplier = isAiming ? aimRecoilMultiplier : 1f;
            currentRecoilX += UnityEngine.Random.Range(-recoilX, recoilX) * multiplier;
            currentRecoilY += recoilY * multiplier;
        }

        protected virtual void HandleRecoilRecovery()
        {
            currentRecoilX = Mathf.Lerp(currentRecoilX, 0, recoilRecoverySpeed * Time.deltaTime);
            currentRecoilY = Mathf.Lerp(currentRecoilY, 0, recoilRecoverySpeed * Time.deltaTime);
        }

        protected void SpawnMuzzleFlash()
        {
            if (muzzleFlashPrefab != null && muzzlePoint != null)
            {
                var flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
                Destroy(flash, 0.1f);
            }

            if (casingPrefab != null && casingEjectPoint != null)
            {
                var casing = Instantiate(casingPrefab, casingEjectPoint.position, casingEjectPoint.rotation);
                var rb = casing.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(casingEjectPoint.right * 2f + Vector3.up * 1.5f, ForceMode.Impulse);
                    rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.Impulse);
                }
                Destroy(casing, 3f);
            }
        }

        protected void PlayShootSound()
        {
            if (shootSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(shootSound);
            }
        }

        protected void PlayReloadSound()
        {
            if (reloadSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
        }

        protected void PlayEmptySound()
        {
            if (emptySound != null && audioSource != null)
            {
                audioSource.PlayOneShot(emptySound);
            }
        }

        public void PlayEquipSound()
        {
            if (equipSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(equipSound);
            }
        }

        [Server]
        public void AddAmmo(int amount)
        {
            reserveAmmo = Mathf.Min(reserveAmmo + amount, maxReserveAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        }
    }
}
