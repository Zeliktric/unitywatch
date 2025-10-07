using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using NaughtyAttributes;
using RaycastPro;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of the damage hero: Soldier: 76.
    /// </summary>
    public class Soldier76 : DamageHero
    {
        #region Attributes Init

        [SerializeField]
        private Transform cameraRoot;

        [Foldout("Weapons")]
        public Weapon heavyPulseRifle;
        [Foldout("Weapons")]
        public AbilityWeapon helixRockets;

        private Sprint sprint;
        private BioticField bioticField;
        private TacticalVisor tacticalVisor;
        private StimPack stimPack;

        [SerializeField, Foldout("VFX")]
        private Transform ammoVFX;

        [SerializeField, Foldout("VFX")]
        private Material helixRocketsVFX;

        [SerializeField, Foldout("Vertical Recoil")]
        private float facingValueChange = 0.098f,
            facingValueMultipler = 10f,
            returnFacingValueMultipler = 5f,
            endShootWait = 0.1f;

        private float initialFacingValue,
            inRateEnd;
        private int bulletCount = 0;
        private bool shooting,
            helixRocketsFired,
            lowAmmo,
            lvl2Perk1Executed;

        private Color32 blue = new Color32(0, 240, 255, 255),
            orange = new Color32(255, 94, 0, 255);

        #endregion
        #region Unity Functions

        protected override void Start()
        {
            base.Start();

            sprint = GetComponent<Sprint>();
            bioticField = GetComponent<BioticField>();
            tacticalVisor = GetComponent<TacticalVisor>();
            stimPack = GetComponent<StimPack>();
            stimPack.enabled = false;

            heavyPulseRifle.weaponCaster.OnBulletCastEvent += new OnBulletCastHandler(OnBulletCast);
            heavyPulseRifle.weaponCaster.onReloadFinish.AddListener(ReloadVFX);
            helixRockets.weaponCaster.onRate.AddListener(HelixRocketsVFX);
            helixRockets.onCooldownEnd.AddListener(HelixRocketsVFX);

            animator = heavyPulseRifle.GetComponent<Animator>();

            helixRocketsVFX.color = blue;
        }

        protected override void Update()
        {
            base.Update();

            // You cannot use helix rockets while sprinting.
            if (sprint.IsSprinting && !helixRockets.OnCooldown && !helixRockets.Disabled) helixRockets.DisableAbility(true);
        }

        private void LateUpdate()
        {

            if (!heavyPulseRifle.weaponCaster.ammo.IsInRate && shooting)
            {
                inRateEnd = Time.time;
                shooting = false;
            }

            if (!shooting && Time.time - inRateEnd > endShootWait)
            {
                // Return facing value back to original
                StartCoroutine(ResetFacingValue());
                inRateEnd = Mathf.Infinity;
                return;
            }

            if (heavyPulseRifle.weaponCaster.ammo.IsInRate) shooting = true;
        }

        /// <summary>
        /// Attempts to return the camera (and gun) to the initial facing position after the (real) player has stopped shooting.
        /// </summary>
        private IEnumerator ResetFacingValue()
        {
            while (NormaliseAngle(cameraRoot.localEulerAngles.x) < initialFacingValue)
            {
                if (shooting) yield break;

                player.playerController.cinemachineTargetPitch += facingValueChange * returnFacingValueMultipler;
                yield return new WaitForSeconds(0.01f);
            }
        }

        #endregion
        #region Actions

        /// <summary>
        /// Returns Soldier: 76's weapon.
        /// </summary>
        /// <returns>Returns the 'Heavy Pulse Rifle' weapon.</returns>
        public override Weapon GetWeapon()
        {
            return heavyPulseRifle;
        }

        /// <summary>
        /// Returns Soldier: 76's ultimate ability.
        /// </summary>
        /// <returns>Returns the 'Ultimate Visor' ultimate ability.</returns>
        public override Ability GetUltimate()
        {
            return tacticalVisor;
        }

        /// <summary>
        /// Sets the 'Heavy Pulse Rifle' weapon to fire or not.
        /// If the (real) player attempts to fire, it will cancel the sprint if the hero is sprinting.
        /// </summary>
        /// <param name="action">Whether the weapon should attempt to fire or not.</param>
        public override void PrimaryFire(bool action)
        {
            if (action && sprint.IsSprinting)
            {
                CancelSprint();
                return;
            }

            heavyPulseRifle.Shoot(action);
            shooting = action;
            if (action) initialFacingValue = NormaliseAngle(cameraRoot.localEulerAngles.x);
        }

        /// <summary>
        /// Sets the 'Helix Rockets' ability weapon to fire or not.
        /// If the (real) player attempts to fire, it will cancel the sprint if the hero is sprinting.
        /// </summary>
        /// <param name="action">Whether the ability weapon should attempt to fire or not.</param>
        public override void SecondaryFire(bool action)
        {
            if (action && sprint.IsSprinting)
            {
                CancelSprint();
                return;
            }

            bool fired = helixRockets.Shoot(action);

            if (fired)
            {
                // Temporarily lock the hero from using their primary weapon straight after firing their ability weapon.
                float recoveryDuration = helixRockets.AbilityData.RecoveryDuration;
                if (recoveryDuration > 0f) StartCoroutine(player.playerController.PrimaryFireLock(recoveryDuration));
            }
        }
        
        /// <summary>
        /// Initiates the execution of ability 1, once its flag has been set to true.
        /// This allows for the (real) player to hold down the keybind for the ability to be instantly executed once off cooldown.
        /// </summary>
        public override IEnumerator Ability1()
        {
            while (true)
            {
                if (ability1Active)
                {
                    ability1Active = false;
                    StopWeapons(sprint.AbilityData.CastTime);
                    sprint.Execute();
                }

                yield return new WaitForSeconds(Time.deltaTime);
            }

        }

        /// <summary>
        /// Initiates the execution of ability 2, once its flag has been set to true.
        /// This allows for the (real) player to hold down the keybind for the ability to be instantly executed once off cooldown.
        /// </summary>
        public override IEnumerator Ability2()
        {
            while (true)
            {
                if (ability2Active)
                {
                    // Check between the two abilities that share the same keybind.
                    if (level3Perk2 && stimPack.CanExecute)
                    {
                        stimPack.CanExecute = false;
                        StopWeapons(bioticField.AbilityData.CastTime);
                        stimPack.Execute();
                    }
                    else if (bioticField.CanExecute)
                    {
                        bioticField.CanExecute = false;
                        bioticField.Execute();
                    }
                }

                yield return new WaitForSeconds(Time.deltaTime);
            }
        }

        /// <summary>
        /// Initiates the execution of the ultimate ability, once its flag has been set to true.
        /// This allows for the (real) player to hold down the keybind for the ultimate ability to be instantly executed once ready and not active.
        /// </summary>
        public override IEnumerator UltimateAbility()
        {
            while (true)
            {
                if (ultimateAbilityActive && ultimateSystem.Ready && !ultimateSystem.Active)
                {
                    StopWeapons(tacticalVisor.AbilityData.CastTime);
                    if (sprint.IsSprinting) CancelSprint();

                    ultimateSystem.Ready = false;
                    ultimateSystem.Active = true;
                    ultimateSystem.Current = 0;
                    
                    // Update the (real) player's ultimate UI, if this hero is being controlled by the (real) player.
                    player.playerController?.UpdateUltimateUI();
                    tacticalVisor.Execute();
                }

                yield return new WaitForSeconds(Time.deltaTime);
            }
        }

        /// <summary>
        /// Reloads the weapon.
        /// This action will cancel the sprint if the hero is sprinting.
        /// </summary>
        public override void Reload()
        {
            if (sprint.IsSprinting && !level3Perk1) CancelSprint();

            heavyPulseRifle.Reload();
        }

        /// <summary>
        /// Called when the (real) player chooses the second level 3 perk (stim pack).
        /// </summary>
        public override void Level3Perk2()
        {
            base.Level3Perk2();

            // Enable stim pack, disable biotic field.
            GameObject.Find($"Canvas/HUD/Bottom/Abilities & Gun/Abilities/Biotic Field").SetActive(false);
            GameObject.Find($"Canvas/HUD/Bottom/Abilities & Gun/Abilities/Stim Pack").SetActive(true);

            bioticField.enabled = false;
            stimPack.enabled = true;
        }

        #endregion
        #region Other

        /// <summary>
        /// Called when an entity is hit by an area of effect that was created by this hero.
        /// Used to grant ammo back to the hero if the first level 2 perk has been selected.
        /// </summary>
        public override void OnAreaOfEffectHit()
        {
            if (level2Perk1)
            {
                if (lvl2Perk1Executed) return;
                lvl2Perk1Executed = true;

                heavyPulseRifle.StopReloading();

                int capacity = heavyPulseRifle.weaponCaster.ammo.magazineCapacity;
                int ammoRefunded = Mathf.Min(15, capacity - heavyPulseRifle.weaponCaster.ammo.magazineAmount);
                heavyPulseRifle.weaponCaster.ammo.magazineAmount += ammoRefunded;
                bulletCount = capacity - heavyPulseRifle.weaponCaster.ammo.magazineAmount;
                for (int i = 0; i < heavyPulseRifle.weaponCaster.ammo.magazineAmount; i++)
                {
                    ammoVFX.GetChild(capacity - 1 - i).gameObject.SetActive(true);
                    ammoVFX.GetChild(capacity - 1 - i).GetComponent<Image>().color = blue;
                }
            }
        }
        
        /// <summary>
        /// Cancels the sprint and adds a short recovery period for both primary and secondary fire.
        /// </summary>
        public void CancelSprint()
        {
            // You can use helix rockets now that you are not sprinting.
            helixRockets.DisableAbility(false);
            sprint.StopSprint();

            float recoveryDuration = sprint.AbilityData.RecoveryDuration;
            if (recoveryDuration > 0f)
            {
                StopWeapons(recoveryDuration);
            }
        }

        /// <summary>
        /// Locks both the primary and secondary fire for a period of time.
        /// </summary>
        /// <param name="recoveryDuration">The length of time to lock both primary and secondary fire for.</param>
        private void StopWeapons(float recoveryDuration)
        {
            StartCoroutine(player.playerController.PrimaryFireLock(recoveryDuration));
            StartCoroutine(player.playerController.SecondaryFireLock(recoveryDuration));
        }
        
        
        /// <summary>
        /// Called when a bullet is shot from the weapon.
        /// This function is used primarily for VFX purposes.
        /// </summary>
        /// <param name="bullet">The bullet that was shot.</param>
        private void OnBulletCast(BaseBullet bullet)
        {
            player.playerController.cinemachineTargetPitch -= facingValueChange * facingValueMultipler;
            ammoVFX.GetChild(Mathf.Min(bulletCount++, heavyPulseRifle.WeaponData.Ammo - 1)).gameObject.SetActive(false);

            if (heavyPulseRifle.weaponCaster.ammo.magazineAmount / (float)heavyPulseRifle.weaponCaster.ammo.magazineCapacity < 0.5f && !lowAmmo)
            {
                lowAmmo = true;
                foreach (Transform ammoBar in ammoVFX) ammoBar.GetComponent<Image>().color = orange;
            }
            else if (heavyPulseRifle.weaponCaster.ammo.magazineAmount / (float)heavyPulseRifle.weaponCaster.ammo.magazineCapacity >= 0.5f && lowAmmo)
            {
                lowAmmo = false;
                foreach (Transform ammoBar in ammoVFX) ammoBar.GetComponent<Image>().color = blue;
            }

            player.playerController?.UpdateWeaponUI();
        }

        #endregion
        #region VFX

        /// <summary>
        /// Updates the ammo VFX for the 'Heavy Pulse Rifle'.
        /// </summary>
        public void ReloadVFX()
        {
            bulletCount = 0;
            foreach (Transform ammoBar in ammoVFX)
            {
                ammoBar.gameObject.SetActive(true);
                ammoBar.GetComponent<Image>().color = blue;
            }

            player.playerController?.UpdateWeaponUI();
        }

        /// <summary>
        /// Called when helix rockets are fired and when the cooldown for helix rockets ends.
        /// This function is used primarily for VFX purposes.
        /// </summary>
        private void HelixRocketsVFX()
        {
            if (helixRocketsFired)
            {
                helixRocketsVFX.color = blue;
            }
            else
            {
                helixRocketsVFX.color = orange;
            }

            helixRocketsFired = !helixRocketsFired;

            // The first level 2 perk grants ammo on helix rockets damage. This ensures that this grant only potentially occurs once per helix rockets shot.
            if (level2Perk1) lvl2Perk1Executed = false;
        }

        /// <summary>
        /// Set the colour of the helix rockets to blue when the game stops playing (since changes to materials are permanent).
        /// </summary>
        private void OnApplicationQuit()
        {
            helixRocketsVFX.color = blue;
        }

        #endregion

        /// <summary>
        /// Normalises an angle.
        /// </summary>
        /// <param name="angle">The angle to normalise.</param>
        /// <returns>The normalised angle.</returns>
        private float NormaliseAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            return angle;
        }
    }
}