using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of bot heroes.
    /// </summary>
    public class BotHero : Hero
    {
        public Weapon duoCannons;

        private float shootTime;
        private bool canShoot;

        protected override void Update()
        {
            base.Update();

            if (!canShoot) return;

            // Simulates the weapon's fire rate.
            if (Time.time - shootTime >= 1f / duoCannons.WeaponData.FireRate)
            {
                shootTime = Time.time;
                PrimaryFire(true);
            }
        }

        /// <summary>
        /// Sets the 'Duo Cannons' weapon to fire or not.
        /// </summary>
        /// <param name="action">Whether the weapon should fire or not.</param>
        public override void PrimaryFire(bool action)
        {
            canShoot = action;
            duoCannons.weaponCaster.autoShoot = action;
        }
    }
}