using System;
using System.Collections.Generic;

using UnityEngine;

using NaughtyAttributes;
using RaycastPro;
using RaycastPro.Casters;
using RaycastPro.Bullets;
using RaycastPro.RaySensors;

namespace Unitywatch
{
    /// <summary>
    /// Structure for audio source clips that allows to play them consecutively.
    /// </summary>
    [Serializable]
    public class SoundSource
    {
        public AudioSource clip;
        public float playAt;
    }

    /// <summary>
    /// Manages the state of all weapons.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [SerializeField, ReadOnly, ResizableTextArea, Foldout("Debug")]
        private string debugHit;

        [SerializeField]
        private WeaponData weaponData;
        public WeaponData WeaponData { get { return weaponData; } }

        [SerializeField]
        private Bullet[] bullets;

        [SerializeField, Foldout("VFX")]
        private GameObject flashVFX,
            hitVFX;

        [SerializeField]
        private bool multipleShootPoints;

        [SerializeField, HideIf("multipleShootPoints")]
        private Transform shootPoint;

        [SerializeField, ShowIf("multipleShootPoints")]
        private List<Transform> shootPoints;

        [SerializeField, Foldout("Audio"), HideIf("multipleShootPoints")]
        private AudioSource shootClip;

        [SerializeField, Foldout("Audio"), ShowIf("multipleShootPoints")]
        private List<AudioSource> shootClips;

        [SerializeField, Foldout("Audio")]
        private List<SoundSource> reloadClips;

        [HideInInspector]
        public BasicCaster weaponCaster;

        [HideInInspector]
        public PipeRay weaponRay;

        [SerializeField, ReadOnly, Foldout("Debug")]
        private double reloadTime;
        public double ReloadTime
        {
            get { return reloadTime; }
            set { reloadTime = value; }
        }

        [SerializeField, ReadOnly, Foldout("Debug")]
        private bool reloading,
            isShooting;

        private int shootPointIndex = 0;
        private double currentReload;
        private float shootTime;

        /// <summary>
        /// Sets up all the required components with this weapon's data.
        /// </summary>
        [Button("Apply Weapon Data")]
        private void ApplyWeaponData()
        {
            // Create the required components if they do not exist already.
            weaponCaster = GetComponent<BasicCaster>();
            if (weaponCaster == null)
            {
                weaponCaster = gameObject.AddComponent<BasicCaster>();
                print("Added 'BasicCaster' component");
            }

            if (!multipleShootPoints)
            {
                if (transform.parent.Find("Gun Ray") == null)
                {
                    GameObject weaponRayGameObject = new GameObject("Gun Ray");
                    weaponRayGameObject.transform.SetParent(transform.parent, false);
                    print("Created 'Gun Ray' gameObject");

                    weaponRayGameObject.transform.localPosition = Vector3.zero;
                    weaponRay = weaponRayGameObject.AddComponent<PipeRay>();
                    print("Added 'PipeRay' component");
                }
                else
                {
                    weaponRay = transform.parent.Find("Gun Ray").GetComponent<PipeRay>();
                }
            }

            GameObject poolManager = GameObject.FindWithTag("Pool Manager");
            if (poolManager == null)
            {
                poolManager = new GameObject("Pool Manager");
                poolManager.tag = "Pool Manager";
                print("Created 'Pool Manager' gameObject");
            }

            // Setup the weapon ray with required settings and data.
            weaponRay.direction = new Vector3(0f, 0f, weaponData.Range);
            weaponRay.Radius = weaponData.ProjectileRadius;
            weaponRay.detectLayer = ~LayerMask.GetMask("Ignore Raycast", "Weapon", "Pick Up");

            // Setup the weapon caster with the required settings and data.
            weaponCaster.raySource = !multipleShootPoints ? weaponRay : shootPoints[0].GetComponent<PipeRay>();

            weaponCaster.autoShoot = false;
            weaponCaster.ArrayCasting = true;
            weaponCaster.ArrayCapacity = weaponData.Ammo;
            weaponCaster.cloneBullets = new Bullet[weaponData.Ammo];
            weaponCaster.bullets = bullets;
            foreach (Bullet b in weaponCaster.bullets)
            {
                b.ownerReference = gameObject;
            }

            weaponCaster.usingAmmo = true;
            weaponCaster.ammo = new BaseCaster.Ammo();
            weaponCaster.ammo.infiniteAmmo = true;
            weaponCaster.ammo.amount = weaponData.Ammo;
            weaponCaster.ammo.magazineCapacity = weaponData.Ammo;
            weaponCaster.ammo.magazineAmount = weaponData.Ammo;
            weaponCaster.ammo.reloadTime = 0f;
            weaponCaster.ammo.rateTime = weaponData.FireRate;

            weaponCaster.autoReload = true;
            weaponCaster.triggerInteraction = QueryTriggerInteraction.Ignore;
            weaponCaster.poolManager = poolManager.transform;

            print($"Applied {weaponData.Name}'s weapon data!");
        }

        protected void Start()
        {
            if (weaponCaster == null) weaponCaster = GetComponent<BasicCaster>();
            if (weaponRay == null && !multipleShootPoints) weaponRay = transform.parent.Find("Gun Ray").GetComponent<PipeRay>();

            weaponCaster.OnBulletCastEvent += new OnBulletCastHandler(OnBulletCast);

            reloadTime = weaponData.ReloadTime;
        }

        private void Update()
        {
            // Reloading is done manually instead of using the third-party asset.
            if (currentReload > 0)
            {
                currentReload -= Time.deltaTime;

                if (currentReload <= 0) OnReloadEnd();
            }
        }

        /// <summary>
        /// Sets the 'shoot' action (to shoot or not).
        /// </summary>
        /// <param name="action">Whether the weapon should shoot or not.</param>
        public void Shoot(bool action)
        {
            isShooting = action;

            if (Time.time - shootTime < 1f / weaponData.FireRate)
            {
                weaponCaster.autoShoot = false;
                return;
            }

            if (reloading)
            {
                // Can't shoot if there is no ammo.
                if (weaponCaster.ammo.magazineAmount == 0) return;

                // You can interrupt the reload sequence by shooting.
                StopReloading();
            }

            weaponCaster.autoShoot = action;

            shootTime = Time.time;
        }
        
        /// <summary>
        /// Initiates the reload sequence if valid.
        /// </summary>
        public void Reload()
        {
            if (weaponData.CanReload && weaponCaster.ammo.magazineAmount != weaponCaster.ammo.magazineCapacity)
            {
                weaponCaster.autoShoot = false;
                reloading = true;

                StartReload();
            }
        }

        /// <summary>
        /// Starts the reload sequence.
        /// </summary>
        private void StartReload()
        {
            reloading = true;
            currentReload = reloadTime;

            ReloadSound();
        }

        /// <summary>
        /// Stops the reload sequence.
        /// </summary>
        public void StopReloading()
        {
            reloading = false;
            currentReload = 0f;
            StopReloadSound();
        }

        /// <summary>
        /// Called when the weapon has finished reloading.
        /// </summary>
        private void OnReloadEnd()
        {
            if (!reloading) return;

            reloading = false;
            weaponCaster.Reload();

            if (isShooting) Shoot(true);
        }

        /// <summary>
        /// Plays the reload sound.
        /// </summary>
        private void ReloadSound()
        {
            if (reloadTime != weaponData.ReloadTime)
            {
                // If the current reload time is different to the data's reload time, then only play the first sound.
                // TODO: This is actually specific to Soldier: 76's reload during his ultimate.
                reloadClips[0].clip.Play();
            }
            else
            {
                foreach (SoundSource sound in reloadClips)
                {
                    sound.clip.PlayDelayed(sound.playAt);
                }
            }
        }

        /// <summary>
        /// Stops each reload sound.
        /// </summary>
        private void StopReloadSound()
        {
            foreach (SoundSource sound in reloadClips)
            {
                sound.clip.Stop();
            }
        }

        /// <summary>
        /// Called when a bullet is shot from the weapon.
        /// </summary>
        /// <param name="bullet">The bullet that was shot.</param>
        private void OnBulletCast(BaseBullet bullet)
        {
            if (multipleShootPoints)
            {
                // Create the flash VFX at the current shoot point position.
                Instantiate(flashVFX, shootPoints[shootPointIndex].position, shootPoints[shootPointIndex].rotation, shootPoints[shootPointIndex].transform);

                shootClips[shootPointIndex].Play();
                // Increment to the next shoot position.
                shootPointIndex += 1;
                if (shootPointIndex > shootPoints.Count - 1) shootPointIndex = 0;

                weaponCaster.raySource = shootPoints[shootPointIndex].GetComponent<PipeRay>();
            }
            else
            {
                // Create the flash VFX at the shoot point position.
                Instantiate(flashVFX, shootPoint.position, shootPoint.rotation, shootPoint.transform);
                shootClip.Play();
            }

            // Subscribe to when the bullet hits a collider if this weapon is a projectile weapon.
            if (weaponData.IsProjectile) bullet.OnBulletHitEvent += new OnBulletHitHandler(OnBulletHit);

            // Automatically reload when ammo has been depleted.
            if (weaponCaster.ammo.magazineAmount == 0) Reload();
        }

        /// <summary>
        /// Called when the weapon's bullet hits an entity
        /// </summary>
        /// <param name="targetPos">The position of the entity that was hit.</param>
        /// <param name="damage">Whether this weapon damages or heals.</param>
        /// <param name="headshot">Whether the hit was a headshot or not.</param>
        /// <returns>The calculated hit value of the bullet.</returns>
        public float OnHit(Vector3 targetPos, bool damage, bool headshot)
        {
            float hitValue;
            float distance = Vector3.Distance(transform.root.GetComponent<Rigidbody>().position, targetPos);

            HealthDelta healthDelta = damage ? weaponData.Damage : weaponData.Healing;
            string healthDeltaString = damage ? "Damage" : "Healing";

            if (weaponData.FalloffRange != Vector2.zero)
            {
                // Calculate hit value based on distance from entity if falloff exists.
                hitValue = Constants.CalculateFalloffValue(distance, healthDelta, weaponData.FalloffRange);
            }
            else
            {
                hitValue = healthDelta.ValueRange.x;
            }

            if (headshot) hitValue *= weaponData.HeadshotMultiplier;

            debugHit = $"Hit target at {distance}m\nOriginal {healthDeltaString} value: {hitValue} (headshot: {headshot})\n";

            return hitValue;
        }
        
        /// <summary>
        /// Called when a (projectile) bullet is shot from the weapon.
        /// </summary>
        /// <param name="bullet">The (projectile) bullet that was shot.</param>
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
            Instantiate(hitVFX, hitPoint, Quaternion.identity);
        }

        // private float CalculateFalloffValue(float distance, HealthDelta healthDelta)
        // {
        //     float falloffStart = weaponData.FalloffRange.x;
        //     float falloffEnd = weaponData.FalloffRange.y;

        //     float maxValue = healthDelta.valueRange.x;
        //     float minValue = healthDelta.valueRange.y;

        //     if (distance <= falloffStart) return maxValue;
        //     if (distance >= falloffEnd) return minValue;

        //     // Linearly interpolate between maxValue and minValue
        //     float t = (distance - falloffStart) / (falloffEnd - falloffStart);
        //     return Mathf.Lerp(maxValue, minValue, t);
        // }
    }
}