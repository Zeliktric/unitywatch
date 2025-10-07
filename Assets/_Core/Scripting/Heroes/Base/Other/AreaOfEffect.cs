using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using RaycastPro.Detectors;

namespace Unitywatch
{
    /// <summary>
    /// Manages creation and state of an 'area of effect'.
    /// </summary>
    public class AreaOfEffect : MonoBehaviour
    {
        private Entity owner;
        public Entity Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        private AbilityData abilityData;
        public AbilityData AbilityData
        {
            get { return abilityData; }
            set { abilityData = value; }
        }

        private bool grounded;
        public bool Grounded
        {
            get { return grounded; }
            set { grounded = value; }
        }

        private bool isUltimate;
        public bool IsUltimate
        {
            get { return isUltimate; }
            set { isUltimate = value; }
        }

        private GameObject deleteObject;
        public GameObject DeleteObject
        {
            get { return deleteObject; }
            set { deleteObject = value; }
        }

        private GameObject collisionObject;
        public GameObject CollisionObject
        {
            get { return collisionObject; }
            set { collisionObject = value; }
        }

        private LOSDetector losDetector;
        public LOSDetector LOSDetector
        {
            get { return losDetector; }
            set { losDetector = value; }
        }
        
        private List<Entity> affectedEntities = new List<Entity>();
        private List<Collider> affectedColliders = new List<Collider>();
        private bool affectOnce;

        private SphereCollider areaTrigger;
        private Entity recipient;

        private bool AffectedSelf
        {
            // Returns true if the area of effect can affect the owner and the recipient is the owner.
            get { return abilityData.AffectSelf && recipient.EntityID == owner.EntityID; }
        }

        /// <summary>
        /// Called when the area of effect should be spawned in.
        /// </summary>
        public void StartEffect()
        {
            areaTrigger = gameObject.AddComponent<SphereCollider>();
            areaTrigger.isTrigger = true;
            areaTrigger.includeLayers = LayerMask.GetMask("Entity");
            areaTrigger.radius = abilityData.AreaOfEffectRadius.y;

            if (abilityData.LOSRequired)
            {
                // Setup the sight detector to use as an LOS indicator for the area of effect.
                losDetector = transform.Find("LOS Detector").GetComponent<LOSDetector>();
                SightDetector detector = losDetector.GetComponent<SightDetector>();
                detector.radius = areaTrigger.radius;

                if (abilityData.BlockedByBarrier) detector.blockLayer = LayerMask.GetMask("Wall", "Barrier");
            }

            affectOnce = abilityData.Duration == 0f;

            StartCoroutine(DestroySelf());
        }

        private void OnTriggerStay(Collider col)
        {
            recipient = col.transform.root.GetComponent<Entity>();

            // Area of effect should only affect entities that are alive.
            if (recipient == null || recipient.IsDead) return;

            // Grounded area of effects should only affect entities that are grounded.
            if (grounded && !recipient.Grounded) return;

            // Damage area of effects shouldn't affect the recipient if they are on the same team as the owner.
            if (!AffectedSelf && abilityData.AreaOfEffect.IsDamage && owner.Team == recipient.Team) return;

            // Healing area of effects can only affect the recipient if they are on the same team as the owner.
            if (!abilityData.AreaOfEffect.IsDamage && owner.Team != recipient.Team) return;

            // Only one collider per recipient is processed (avoids multiple processes for one recipient due to multiple colliders).
            if (affectedEntities.Contains(recipient))
            {
                if (!affectedColliders.Contains(col) || affectOnce) return;
            }
            else
            {
                affectedEntities.Add(recipient);
                affectedColliders.Add(col);
            }
            
            double hitValue;
            bool valuePerSecond = false;

            if (abilityData.AreaOfEffect.ValueRange != Vector2.zero)
            {
                // Calculate the falloff value if required.
                Vector3 colPosition = new Vector3(col.transform.position.x, transform.position.y, col.transform.position.z);
                float distance = Vector3.Distance(transform.position, colPosition);

                hitValue = Constants.CalculateFalloffValue(distance, abilityData.AreaOfEffect, abilityData.AreaOfEffectRadius);
            }
            else
            {
                hitValue = abilityData.AreaOfEffect.ValuePerSecond;
                valuePerSecond = true;
            }

            // Check whether the bullet has collided with a barrier, given that it can collide with barriers.
            // if (abilityData.BlockedByBarrier && collisionObject.layer == LayerMask.NameToLayer("Barrier"))
            // {
            //     if (!collisionObject.GetComponent<Barrier>().Active)
            //     {
            //         collisionObject.GetComponent<Barrier>().OnHit(hitValue);
            //         return;
            //     }
            // }

            // Check whether the area of effect has LOS to the recipient, given that the area of effect has an LOS detector.
            if (losDetector && !losDetector.DetectedEntities.Contains(recipient))
            {
                // If the bullet collided with a barrier, then update the health of the barrier.
                if (collisionObject != null && collisionObject.layer == LayerMask.NameToLayer("Barrier")) collisionObject.GetComponent<Barrier>().OnHit(hitValue);
                return;
            }

            bool knockback = false;

            // This area of effect applies knockback to entities within its range.
            if (abilityData.KnockbackSpeed != Vector2.zero) knockback = true;

            if (AffectedSelf)
            {
                // Area of effect can and is affecting the owner.
                if (abilityData.KnockbackSpeed == Vector2.zero) knockback = false;
                if (abilityData.AreaOfEffect.IsDamage) hitValue /= 2f;
            }

            if (knockback)
            {
                // Apply the knockback to the recipient entity.
                Rigidbody rb = recipient.GetComponent<Rigidbody>();
                rb.AddExplosionForce(rb.mass * abilityData.KnockbackSpeed.x, transform.position, abilityData.AreaOfEffectRadius.x, 0.5f, ForceMode.Impulse);
            }

            if (valuePerSecond) hitValue *= Time.deltaTime;
            if (hitValue != 0) recipient.Hero.UpdateHP(hitValue, abilityData.AreaOfEffect.IsDamage, owner, isUltimate, abilityUsed: abilityData, affectArmour: valuePerSecond ? "dot" : "");

            owner.Hero.OnAreaOfEffectHit();
        }

        /// <summary>
        /// Method that destroys itself after it expires, as well as any objects as part of the area of effect.
        /// </summary>
        private IEnumerator DestroySelf()
        {
            yield return new WaitForSeconds(Mathf.Max(Time.deltaTime, abilityData.Duration));

            if (deleteObject) Destroy(deleteObject);

            Destroy(gameObject);
        }

        /// <summary>
        /// Calculates the falloff value based on the distance of the recipient from the area of effect.
        /// </summary>
        /// <param name="distance">The distance between the recipient and the area of effect.</param>
        /// <returns>The calculated falloff value.</returns>
        // private float CalculateFalloffValue(float distance)
        // {
        //     float falloffStart = abilityData.AreaOfEffectRadius.x;
        //     float falloffEnd = abilityData.AreaOfEffectRadius.y;

        //     float maxValue = abilityData.AreaOfEffect.valueRange.x;
        //     float minValue = abilityData.AreaOfEffect.valueRange.y;

        //     if (distance <= falloffStart) return maxValue;
        //     if (distance >= falloffEnd) return minValue;

        //     // Linearly interpolate between falloffStart and falloffEnd
        //     float t = (distance - falloffStart) / (falloffEnd - falloffStart);
        //     return Mathf.Lerp(maxValue, minValue, t);
        // }
    }
}