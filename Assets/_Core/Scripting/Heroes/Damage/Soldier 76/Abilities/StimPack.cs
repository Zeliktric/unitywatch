using System.Collections;

using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of Soldier: 76's stim pack.
    /// </summary>
    public class StimPack : Ability
    {
        private bool active;
        private double currentDuration;

        private Soldier76 hero;

        protected override void Start()
        {
            base.Start();

            if (hero == null) hero = (Soldier76)entity.Hero;
        }

        protected override void Update()
        {
            base.Update();

            if (currentDuration > 0)
            {
                currentDuration -= Time.deltaTime;

                if (currentDuration <= 0)
                {
                    SetInactive();
                    SetCooldown(AbilityData.Cooldown - AbilityData.Duration);
                    active = false;

                    // Reset the weapon's fire rate.
                    hero.heavyPulseRifle.weaponCaster.ammo.rateTime = hero.heavyPulseRifle.WeaponData.FireRate;
                }
            }

            AutoHealing();
        }

        /// <summary>
        /// Heals the hero while stim pack is active.
        /// </summary>
        private void AutoHealing()
        {
            if (!active) return;

            float hitValue = AbilityData.Healing.ValuePerSecond;
            hitValue *= Time.deltaTime;

            entity.Hero.UpdateHP(hitValue, false, entity, false);
        }

        /// <summary>
        /// Starts the stim pack action.
        /// </summary>
        public override void Execute()
        {
            StartCoroutine(StartAction());
        }

        /// <summary>
        /// Casts and starts the stim pack action.
        /// </summary>
        private IEnumerator StartAction()
        {
            entity.playerController.AbilityLock = true;
            SetActive();

            yield return new WaitForSeconds(AbilityData.CastTime);
            entity.playerController.AbilityLock = false;

            active = true;

            currentDuration = AbilityData.Duration;

            // Weapon fires at a faster rate during stim pack.
            hero.heavyPulseRifle.weaponCaster.ammo.rateTime *= 1.2f;
        }
    }
}