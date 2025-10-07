using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of an entity's barrier.
    /// </summary>
    public class Barrier : MonoBehaviour
    {
        [SerializeField]
        private double totalHealth,
            currentHealth;

        [SerializeField]
        private bool active = true;
        public bool Active => active;

        private void Start()
        {
            currentHealth = totalHealth;
        }

        private void Update()
        {
            if (!active) return;

            if (currentHealth < 0)
            {
                transform.GetChild(0).gameObject.SetActive(false);
                active = false;
            }
        }

        /// <summary>
        /// Update the barrier's health when hit by a bullet.
        /// </summary>
        /// <param name="hitValue">The hit value that the entity would've taken.</param>
        public void OnHit(double hitValue)
        {
            currentHealth -= hitValue;
        }

        /// <summary>
        /// Called when the entity respawns.
        /// TODO: Only applicable for training "Tank Bot".
        /// </summary>
        public void Respawn()
        {
            currentHealth = totalHealth;
            transform.GetChild(0).gameObject.SetActive(true);
            active = true;
        }
    }
}