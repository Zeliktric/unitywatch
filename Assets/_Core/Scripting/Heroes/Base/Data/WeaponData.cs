using System;

using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Class as a structure for health delta.
    /// Helps identify whether the value is healing / damage and how the value should be applied.
    /// </summary>
    [Serializable]
    public class HealthDelta
    {
        [SerializeField]
        private Vector2 valueRange;
        public Vector2 ValueRange => valueRange;

        [SerializeField]
        private float valuePerSecond;
        public float ValuePerSecond => valuePerSecond;

        [SerializeField]
        private bool isDamage;
        public bool IsDamage => isDamage;
    }

    /// <summary>
    /// Scriptable object for storing information on weapons.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon Data", menuName = "Unitywatch/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [SerializeField]
        private string weaponName;
        public string Name => weaponName;

        [SerializeField]
        private Sprite weaponIcon;
        public Sprite WeaponIcon => weaponIcon;

        [SerializeField]
        private bool isProjectile;
        public bool IsProjectile => isProjectile;

        [SerializeField]
        private HealthDelta healing;
        public HealthDelta Healing => healing;

        [SerializeField]
        private HealthDelta damage;
        public HealthDelta Damage => damage;

        [SerializeField]
        private bool selfAffect;
        public bool SelfAffect => selfAffect;

        [SerializeField]
        private Vector2 falloffRange;
        public Vector2 FalloffRange => falloffRange;

        [SerializeField]
        private float headshotMultiplier;
        public float HeadshotMultiplier => headshotMultiplier;

        [SerializeField]
        private float spread;
        public float Spread => spread;

        [SerializeField]
        private float projectileSpeed;
        public float ProjectileSpeed => projectileSpeed;

        [SerializeField]
        private float projectileRadius;
        public float ProjectileRadius => projectileRadius;

        [SerializeField]
        private float range = Mathf.Infinity;
        public float Range => range;

        [SerializeField]
        private float radius;
        public float Radius => radius;

        [SerializeField]
        private float fireRate;
        public float FireRate => fireRate;

        [SerializeField]
        private int ammo;
        public int Ammo => ammo;

        [SerializeField]
        private float reloadTime;
        public float ReloadTime => reloadTime;

        [SerializeField]
        private bool canReload = true;
        public bool CanReload => canReload;
    }
}