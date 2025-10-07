using System.Collections.Generic;

using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Manages the 'quick melee' action.
    /// </summary>
    public class QuickMelee : MonoBehaviour
    {
        [SerializeField]
        private AbilityData abilityData;

        private double currentCooldown,
            currentDuration;

        private bool canExecute = true,
            active;
        public bool CanExecute
        {
            get { return canExecute; }
            set { canExecute = value; }
        }

        private Entity entity;
        private List<Entity> detectedEntities = new List<Entity>();
        private List<Entity> blacklistedEntities = new List<Entity>();

        private void Start()
        {
            entity = transform.root.GetComponent<Entity>();
        }

        private void Update()
        {
            // Cooldown between executions of the quick melee.
            if (currentCooldown > 0)
            {
                currentCooldown -= Time.deltaTime;

                if (currentCooldown <= 0f) canExecute = true;
            }

            // Duration that the quick melee is active (allows for you to hit multiple targets at once within the duration window).
            if (currentDuration > 0)
            {
                currentDuration -= Time.deltaTime;

                if (currentDuration <= 0f)
                {
                    active = false;

                    // Allow the (real) player to use other actions again. 
                    if (entity.playerController.AbilityLock)
                    {
                        entity.playerController.AbilityLock = false;
                        entity.playerController.LockPrimaryFire = false;
                        entity.playerController.LockSecondaryFire = false;
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (!active) return;

            foreach (Entity detectedEntity in detectedEntities)
            {
                // Don't allow the same entity to be affected more than once by a quick melee.
                if (blacklistedEntities.Contains(detectedEntity)) continue;

                // Apply knockback (very small) and damage to the detected entity.
                Rigidbody rb = detectedEntity.GetComponent<Rigidbody>();
                rb.AddExplosionForce(rb.mass * abilityData.KnockbackSpeed.x, transform.root.position, abilityData.Range, 0.5f, ForceMode.Impulse);
                detectedEntity.Hero.UpdateHP(abilityData.Damage.ValueRange.x, true, entity, false, abilityUsed: abilityData, affectArmour: "no");
                entity.SetHitMarker(false);

                blacklistedEntities.Add(detectedEntity);
            }
        }

        /// <summary>
        /// Starts the quick melee action.
        /// </summary>
        /// <param name="animator">The primary weapon's animator.</param>
        public void Execute(Animator animator)
        {
            animator.Play("Quick Melee");

            // Lock the (real) player from doing any other type of action.
            entity.playerController.AbilityLock = true;
            entity.playerController.LockPrimaryFire = true;
            entity.playerController.LockSecondaryFire = true;

            currentCooldown = abilityData.Cooldown;
            currentDuration = abilityData.Duration;

            // Reset the blacklisted entities list.
            blacklistedEntities = new List<Entity>();

            active = true;
        }

        /// <summary>
        /// Called when a new entity is detected.
        /// </summary>
        /// <param name="col">The collider of the entity that was detected.</param>
        public void OnEntityDetect(Collider col)
        {
            Entity detectedEntity = col.transform.root.GetComponent<Entity>();

            // Don't detect entities of the same team, blacklisted entities (entities that have already been processed in the current action) or dead entities.
            if (detectedEntity.Team == entity.Team) return;
            if (blacklistedEntities.Contains(detectedEntity)) return;
            if (entity.IsDead)
            {
                detectedEntities.Remove(detectedEntity);
                return;
            }

            // Add the newly detected entity to the list of detected entities.
            if (!detectedEntities.Contains(detectedEntity)) detectedEntities.Add(detectedEntity);
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
            if (detectedEntities.Contains(detectedEntity)) detectedEntities.Remove(detectedEntity);
        }
    }
}