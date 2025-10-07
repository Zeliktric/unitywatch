using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using NaughtyAttributes;
using RaycastPro;
using RaycastPro.Casters;
using RaycastPro.Bullets;
using RaycastPro.RaySensors;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of all ability weapons.
    /// </summary>
    public class AbilityWeapon : Ability
    {
        [SerializeField]
        private Transform shootPoint;

        [SerializeField]
        private Bullet[] bullets;

        [SerializeField, Foldout("VFX")]
        private GameObject flashEffect,
            hitEffect;

        [SerializeField, Foldout("Audio")]
        private AudioSource shootClip;

        [SerializeField, ReadOnly, ResizableTextArea, Foldout("Debug")]
        private string debugHit;

        [HideInInspector]
        public BasicCaster weaponCaster;
        private PipeRay weaponRay;

        [HideInInspector]
        public float cooldown;

        [HideInInspector]
        public UnityEvent onCooldownEnd;

        private bool isShooting;

        /// <summary>
        /// Sets up all the required components with this ability weapon's data.
        /// </summary>
        [Button("Apply AbilityWeapon Data")]
        private void ApplyAbilityWeaponData()
        {
            // Create the required components if they do not exist already.
            weaponCaster = GetComponent<BasicCaster>();
            if (weaponCaster == null)
            {
                weaponCaster = gameObject.AddComponent<BasicCaster>();
                print("Added 'BasicCaster' component");
            }

            if (transform.parent.Find("Ability Gun Ray") == null)
            {
                GameObject weaponRayGameObject = new GameObject("Ability Gun Ray");
                weaponRayGameObject.transform.SetParent(transform.parent, false);
                print("Created 'Ability Gun Ray' gameObject");

                weaponRayGameObject.transform.position = shootPoint.transform.position;
                weaponRayGameObject.transform.rotation = shootPoint.transform.rotation;
                weaponRay = weaponRayGameObject.AddComponent<PipeRay>();
                print("Added 'PipeRay' component");
            }
            else
            {
                weaponRay = transform.parent.Find("Ability Gun Ray").GetComponent<PipeRay>();
            }

            GameObject poolManager = GameObject.FindWithTag("Pool Manager");
            if (poolManager == null)
            {
                poolManager = new GameObject("Pool Manager");
                poolManager.tag = "Pool Manager";
                print("Created 'Pool Manager' gameObject");
            }

            // Setup the weapon ray with required settings and data.
            weaponRay.direction = new Vector3(0f, 0f, Mathf.Min(150f, abilityData.Range));
            weaponRay.Radius = abilityData.ProjectileRadius;
            weaponRay.detectLayer = ~LayerMask.GetMask("Ignore Raycast", "Weapon", "Pick Up");

            // Setup the weapon caster with the required settings and data.
            weaponCaster.raySource = weaponRay;

            weaponCaster.autoShoot = false;
            weaponCaster.ArrayCasting = false;
            weaponCaster.cloneBullets = new Bullet[abilityData.Charges];
            weaponCaster.bullets = bullets;
            foreach (Bullet b in weaponCaster.bullets)
            {
                b.ownerReference = gameObject;
            }

            weaponCaster.usingAmmo = true;
            weaponCaster.ammo = new BaseCaster.Ammo();
            weaponCaster.ammo.infiniteAmmo = true;
            weaponCaster.ammo.amount = abilityData.Charges;
            weaponCaster.ammo.magazineCapacity = abilityData.Charges;
            weaponCaster.ammo.magazineAmount = abilityData.Charges;
            weaponCaster.ammo.reloadTime = 0f;
            weaponCaster.ammo.rateTime = 0f;

            weaponCaster.autoReload = true;
            weaponCaster.triggerInteraction = QueryTriggerInteraction.Ignore;
            weaponCaster.poolManager = poolManager.transform;

            print($"Applied {abilityData.Name}'s ability weapon data!");
        }

        protected override void Start()
        {
            base.Start();
            if (weaponCaster == null) weaponCaster = GetComponent<BasicCaster>();

            weaponCaster.OnBulletCastEvent += new OnBulletCastHandler(OnBulletCast);
            cooldown = abilityData.Cooldown;
        }

        /// <summary>
        /// Called when the cooldown for the ability weapon has expired.
        /// </summary>
        protected override void OnEnd()
        {
            base.OnEnd();

            onCooldownEnd.Invoke();
            if (isShooting) Shoot(true);
        }

        /// <summary>
        /// Sets the 'shoot' action (to shoot or not).
        /// </summary>
        /// <param name="action">Whether the ability weapon should shoot or not.</param>
        /// <returns>Whether the ability weapon fired.</returns>
        public bool Shoot(bool action)
        {
            isShooting = action;

            if (onCooldown) return false;

            weaponCaster.autoShoot = action;
            if (action && abilityData.Cooldown != 0f) SetCooldown(cooldown);
            return action;
        }

        /// <summary>
        /// Called when the ability weapon's bullet hits an entity
        /// </summary>
        /// <param name="targetPos">The position of the entity that was hit.</param>
        /// <param name="damage">Whether this ability weapon damages or heals.</param>
        /// <param name="headshot">Whether the hit was a headshot or not.</param>
        /// <returns>The calculated hit value of the bullet.</returns>
        public float OnHit(Vector3 targetPos, bool damage, bool headshot)
        {
            float hitValue;
            float distance = Vector3.Distance(transform.root.GetComponent<Rigidbody>().position, targetPos);

            HealthDelta healthDelta = damage ? abilityData.Damage : abilityData.Healing;
            string healthDeltaString = damage ? "Damage" : "Healing";

            if (abilityData.FalloffRange != Vector2.zero)
            {
                // Calculate hit value based on distance from entity if falloff exists.
                hitValue = Constants.CalculateFalloffValue(distance, healthDelta, abilityData.FalloffRange);
            }
            else
            {
                hitValue = healthDelta.ValueRange.x;
            }

            if (headshot) hitValue *= abilityData.HeadshotMultiplier;

            debugHit = $"Hit target at {distance}m\n{healthDeltaString} value: {hitValue} (headshot: {headshot})\n";

            return hitValue;
        }
        
        /// <summary>
        /// Called when a bullet is shot from the ability weapon.
        /// </summary>
        /// <param name="bullet">The bullet that was shot.</param>
        private void OnBulletCast(BaseBullet bullet)
        {
            // Create the flash VFX at the shoot point position.
            Instantiate(flashEffect, shootPoint.position, shootPoint.rotation, shootPoint.transform);

            shootClip.Play();
            bullet.OnBulletHitEvent += new OnBulletHitHandler(OnBulletHit);

            // Ability weapons only shoot once and use cooldowns rather than having a traditional reload time.
            weaponCaster.autoShoot = false;
            weaponCaster.Reload();
        }

        /// <summary>
        /// Called when a bullet hits any collider.
        /// </summary>
        /// <param name="col">The collider that the bullet hit.</param>
        /// <param name="bullet">The bullet that hit the collider.</param>
        private void OnBulletHit(Collision col, Bullet bullet)
        {
            Vector3 hitPoint;
            
            // Check whether the bullet is partway through a collider.
            // If so, set its collision point to the actual hit point using rays (Generated by AI).
            Vector3 dir = bullet.GetComponent<Rigidbody>().linearVelocity.normalized;
            if (Physics.Raycast(col.GetContact(0).point - dir * 0.5f, dir, out RaycastHit hit, 1f))
            {
                hitPoint = hit.point;
            }
            else
            {
                hitPoint = col.GetContact(0).point;
            }

            // Create the hit VFX at the bullet hit position.
            Instantiate(hitEffect, hitPoint, Quaternion.identity);

            if (abilityData.AreaOfEffectRadius != Vector2.zero)
            {
                // Create an area of effect if required.
                GameObject areaOfEffectObject = Instantiate(entity.Hero.areaOfEffectPrefab, hitPoint, Quaternion.identity);
                AreaOfEffect areaOfEffect = areaOfEffectObject.GetComponent<AreaOfEffect>();

                areaOfEffect.Owner = transform.root.GetComponent<Player>();
                areaOfEffect.AbilityData = abilityData;
                areaOfEffect.CollisionObject = col.collider.transform.parent.gameObject;

                areaOfEffect.StartEffect();
            }
        }
    }
}