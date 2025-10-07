using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using RaycastPro.RaySensors;
using RaycastPro.Detectors;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of Soldier: 76's tactical visor.
    /// </summary>
    public class TacticalVisor : Ability
    {
        [SerializeField]
        private float reloadTime = 0.65f;

        [SerializeField]
        private SightDetector ultimateDetector;

        [SerializeField]
        private PipeRay ultimateGunRay;

        [SerializeField]
        private GameObject targetedPlayerIcon,
            ultimateDuration;

        [SerializeField]
        private AudioSource ultimateVoiceline;

        [SerializeField]
        private Image ultimateDurationProgress;
        
        private GameObject currentTarget,
            previousTarget;
        private List<Transform> detectedEntities = new List<Transform>();

        private Soldier76 hero;

        protected override void Start()
        {
            base.Start();

            if (hero == null) hero = (Soldier76)entity.Hero;
        }

        private void FixedUpdate()
        {
            if (hero.UltimateSystem.Active)
            {
                ultimateDurationProgress.fillAmount = (float)(currentCooldown / AbilityData.Duration);

                // Find the closest detected entity to the hero, if there are any.
                Transform target = GetClosestToHero();
                if (target != null)
                {
                    // Set the ultimate gun ray to look at the target.
                    // This ray is different from the normal gun ray and is used to direct bullets towards the centre of the target.
                    ultimateGunRay.transform.LookAt(target.position);

                    // Add the marker to the target (so you know which entity you are shooting at).
                    Transform uiParent = target.GetChild(0).Find("UI");
                    Transform icon = uiParent.GetChild(1).Find("Tactical Visor Marker");
                    if (icon == null)
                    {
                        currentTarget = Instantiate(targetedPlayerIcon);
                        currentTarget.name = "Tactical Visor Marker";
                        currentTarget.transform.SetParent(uiParent.GetChild(1), false);
                        currentTarget.transform.localPosition = new Vector3(0f, -1f, -0.75f);
                        currentTarget.transform.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        icon.gameObject.SetActive(true);
                        currentTarget = icon.gameObject;
                    }

                    // If the current target has changed, hide the marker of the previous target.
                    if (previousTarget != null && !currentTarget.Equals(previousTarget)) previousTarget.SetActive(false);

                    // If the (real) player is pointing at an entity, then use the normal weapon ray. Otherwise, use the ultimate gun ray.
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, hero.heavyPulseRifle.WeaponData.Range, LayerMask.GetMask("Entity")))
                    {
                        hero.heavyPulseRifle.weaponCaster.raySource = hero.heavyPulseRifle.weaponRay;
                    }
                    else
                    {
                        hero.heavyPulseRifle.weaponCaster.raySource = ultimateGunRay;
                    }

                    previousTarget = currentTarget;
                }
                else
                {
                    // No target has been detected, so reset to use the normal weapon ray.
                    hero.heavyPulseRifle.weaponCaster.raySource = hero.heavyPulseRifle.weaponRay;

                    // Hide the marker of any marker that exists.
                    currentTarget?.SetActive(false);
                    previousTarget?.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Called when a new entity is detected.
        /// </summary>
        /// <param name="col">The collider of the entity that was detected.</param>
        public void OnEntityDetect(Collider col)
        {
            Entity detectedEntity = col.transform.root.GetComponent<Entity>();

            // Don't detect entities of the same team or dead entities.
            if (detectedEntity.Team == entity.Team) return;
            if (detectedEntity.IsDead)
            {
                detectedEntities.Remove(detectedEntity.transform);
                return;
            }

            // Add the newly detected entity to the list of detected entities.
            if (!detectedEntities.Contains(detectedEntity.transform)) detectedEntities.Add(detectedEntity.transform);
        }
        
        /// <summary>
        /// Called when a detected entity is lost.
        /// </summary>
        /// <param name="col">The collider of the detected entity that was lost.</param>
        public void OnEntityLost(Collider col)
        {
            Entity detectedEntity = col.transform.root.GetComponent<Entity>();

            if (detectedEntity.Team == entity.Team) return;

            // Remove the lost entity from the list of detected entities.
            if (detectedEntities.Contains(detectedEntity.transform)) detectedEntities.Remove(detectedEntity.transform);
        }

        /// <summary>
        /// Calculates the closest entitiy to the hero that has been detected.
        /// </summary>
        /// <returns>The closest entity if one has been found.</returns>
        private Transform GetClosestToHero()
        {
            // Entities whose angle to the hero are 46 deg or more, should not be detected.
            float closestAngle = 46f;
            Transform closestPlayer = null;

            foreach (Transform detectedEntity in detectedEntities)
            {
                float angle = GetAngleToHero(detectedEntity);

                if (Mathf.Abs(angle) < closestAngle)
                {
                    closestAngle = angle;
                    closestPlayer = detectedEntity;
                }
            }

            return closestPlayer;
        }

        /// <summary>
        /// Calcualtes the angle between the detected entity and the hero.
        /// </summary>
        /// <param name="detectedEntity">The entity that has been detected.</param>
        /// <returns>The angle between the detected entity and the hero.</returns>
        private float GetAngleToHero(Transform detectedEntity)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            Vector3 target = detectedEntity.position - Camera.main.transform.position;
            target.y = 0f;
            target.Normalize();

            return Vector3.SignedAngle(cameraForward, target, Vector3.up);
        }
        
        /// <summary>
        /// Starts the tactical visor action.
        /// </summary>
        public override void Execute()
        {
            ultimateVoiceline.Play();
            StartCoroutine(StartAction());
        }

        /// <summary>
        /// Casts and starts the tactical visor action.
        /// </summary>
        private IEnumerator StartAction()
        {
            entity.playerController.AbilityLock = true;

            detectedEntities = new List<Transform>();
            ultimateDurationProgress.fillAmount = 0f;
            ultimateDuration.SetActive(true);

            yield return new WaitForSeconds(AbilityData.CastTime);
            entity.playerController.AbilityLock = false;

            // Reload the weapon automatically.
            hero.heavyPulseRifle.weaponCaster.ammo.magazineAmount = hero.heavyPulseRifle.weaponCaster.ammo.magazineCapacity;
            hero.ReloadVFX();
            hero.heavyPulseRifle.ReloadTime = reloadTime;
            
            if (hero.level2Perk2) hero.helixRockets.cooldown *= 0.2f;

            ultimateDetector.Radius = hero.heavyPulseRifle.WeaponData.Range;
            ultimateDurationProgress.fillAmount = 1f;

            SetCooldown(AbilityData.Duration);
        }

        /// <summary>
        /// Called when the ultimate ability has ended.
        /// </summary>
        protected override void OnEnd()
        {
            base.OnEnd();

            hero.UltimateSystem.Active = false;
            hero.heavyPulseRifle.ReloadTime = hero.heavyPulseRifle.WeaponData.ReloadTime;
            hero.heavyPulseRifle.weaponCaster.raySource = hero.heavyPulseRifle.weaponRay;

            ultimateDetector.Radius = 0f;
            ultimateDuration.SetActive(false);

            // Hide the marker of any marker that exists.
            currentTarget?.SetActive(false);
            previousTarget?.SetActive(false);

            if (hero.level2Perk2) hero.helixRockets.cooldown = hero.helixRockets.AbilityData.Cooldown;
        }
    }
}