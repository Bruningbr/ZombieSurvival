using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;

namespace ZombieSurvival.Player
{
    public class PlayerInventory : NetworkBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private int maxWeaponSlots = 3;
        [SerializeField] private Transform weaponHolder;

        private readonly SyncList<string> weaponIds = new SyncList<string>();
        private readonly List<Weapons.WeaponBase> weapons = new List<Weapons.WeaponBase>();

        [SyncVar(hook = nameof(OnActiveWeaponChanged))]
        private int activeWeaponIndex = -1;

        public Weapons.WeaponBase ActiveWeapon =>
            activeWeaponIndex >= 0 && activeWeaponIndex < weapons.Count
                ? weapons[activeWeaponIndex]
                : null;

        public int ActiveWeaponIndex => activeWeaponIndex;
        public int WeaponCount => weapons.Count;

        public event Action<Weapons.WeaponBase> OnWeaponSwitched;
        public event Action<Weapons.WeaponBase> OnWeaponPickedUp;
        public event Action<Weapons.WeaponBase> OnWeaponDropped;

        private void Update()
        {
            if (!isLocalPlayer) return;

            HandleWeaponSwitching();
        }

        private void HandleWeaponSwitching()
        {
            // Number keys 1-3
            for (int i = 0; i < maxWeaponSlots; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) && i < weapons.Count)
                {
                    CmdSwitchWeapon(i);
                    return;
                }
            }

            // Scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0 && weapons.Count > 0)
            {
                int newIndex = activeWeaponIndex + (scroll > 0 ? -1 : 1);
                if (newIndex < 0) newIndex = weapons.Count - 1;
                if (newIndex >= weapons.Count) newIndex = 0;
                CmdSwitchWeapon(newIndex);
            }
        }

        [Command]
        public void CmdPickupWeapon(string weaponId)
        {
            if (weapons.Count >= maxWeaponSlots) return;

            weaponIds.Add(weaponId);
            RpcOnWeaponPickedUp(weaponId, weapons.Count);
        }

        [Command]
        public void CmdDropWeapon(int index)
        {
            if (index < 0 || index >= weapons.Count) return;

            string droppedId = weaponIds[index];
            weaponIds.RemoveAt(index);
            RpcOnWeaponDropped(droppedId, index);

            if (activeWeaponIndex >= weapons.Count)
            {
                activeWeaponIndex = weapons.Count - 1;
            }
        }

        [Command]
        private void CmdSwitchWeapon(int index)
        {
            if (index < 0 || index >= weapons.Count) return;
            activeWeaponIndex = index;
        }

        [ClientRpc]
        private void RpcOnWeaponPickedUp(string weaponId, int index)
        {
            var prefab = WeaponDatabase.Instance?.GetWeaponPrefab(weaponId);
            if (prefab != null && weaponHolder != null)
            {
                var weaponObj = Instantiate(prefab, weaponHolder);
                var weapon = weaponObj.GetComponent<Weapons.WeaponBase>();
                if (weapon != null)
                {
                    weapons.Add(weapon);
                    weaponObj.SetActive(false);
                    OnWeaponPickedUp?.Invoke(weapon);
                }
            }
        }

        [ClientRpc]
        private void RpcOnWeaponDropped(string weaponId, int index)
        {
            if (index < weapons.Count)
            {
                var weapon = weapons[index];
                OnWeaponDropped?.Invoke(weapon);
                weapons.RemoveAt(index);
                if (weapon != null)
                {
                    Destroy(weapon.gameObject);
                }
            }
        }

        private void OnActiveWeaponChanged(int oldIndex, int newIndex)
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i] != null)
                {
                    weapons[i].gameObject.SetActive(i == newIndex);
                }
            }

            OnWeaponSwitched?.Invoke(ActiveWeapon);
        }
    }

    public class WeaponDatabase : MonoBehaviour
    {
        public static WeaponDatabase Instance { get; private set; }

        [SerializeField] private List<WeaponEntry> weapons = new List<WeaponEntry>();

        [System.Serializable]
        public class WeaponEntry
        {
            public string weaponId;
            public GameObject prefab;
            public Sprite icon;
        }

        private Dictionary<string, WeaponEntry> weaponLookup;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                BuildLookup();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void BuildLookup()
        {
            weaponLookup = new Dictionary<string, WeaponEntry>();
            foreach (var entry in weapons)
            {
                weaponLookup[entry.weaponId] = entry;
            }
        }

        public GameObject GetWeaponPrefab(string weaponId)
        {
            return weaponLookup.TryGetValue(weaponId, out var entry) ? entry.prefab : null;
        }

        public Sprite GetWeaponIcon(string weaponId)
        {
            return weaponLookup.TryGetValue(weaponId, out var entry) ? entry.icon : null;
        }
    }
}
