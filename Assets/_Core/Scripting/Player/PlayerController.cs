using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

using NaughtyAttributes;
using TMPro;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of the (real) player.
    /// </summary>
    public class PlayerController : Player
    {
        #region Camera

        [SerializeField, Foldout("Camera")]
        private float camSensX = 0.1f,
            camSensY = 0.1f,
            maxCamY = 89f,
            minCamY = -89f,
            rotationSmooth = 0f;

        [SerializeField, Foldout("HUD - Player Info")]
        private TMP_Text perkLevelText,
            playerNameText,
            hpStatusText;

        [SerializeField, Foldout("HUD - Player Info")]
        private Image perkProgress;

        [SerializeField, Foldout("HUD - Player Info")]
        private GameObject hpBarPrefab,
            hpBackgroundBarPrefab;

        [SerializeField, Foldout("HUD - Player Info")]
        private Transform hpBarsContainer,
            hpBarsBackground,
            hpStatus;

        [SerializeField, Foldout("HUD - Ultimate")]
        private GameObject ultimateChargeObject,
            ultimateReadyObject;

        [SerializeField, Foldout("HUD - Ultimate")]
        private TMP_Text ultimateChargeText;

        [SerializeField, Foldout("HUD - Ultimate")]
        private Image ultimateCharge;

        [SerializeField, Foldout("HUD - Abilities & Gun")]
        private TMP_Text currentAmmo,
            ammoCapacity;

        [SerializeField, Foldout("Debug")]
        private bool isCrouching;

        [HideInInspector]
        public float cinemachineTargetYaw,
            cinemachineTargetPitch;

        [HideInInspector]
        public bool PrimaryFireActive,
            SecondaryFireActive,
            AbilityLock;

        private float meshScale,
            camRootHeight,
            moveSpeed;

        private bool init,
            perkSelectActive;

        private Transform camRoot;
        private GameObject previousHPStatus,
            fullHealthStatus,
            healingStatus,
            notFullHealthStatus;
        private Vector2 currentCamRotation,
            rotationVelocity;

        private Vector3 moveInput,
            lookInput;
        public Vector3 MoveInput => moveInput;
        public Vector3 LookInput => lookInput;

        private List<Image> hpBars = new List<Image>();

        private Entity pingedEntity;

        #endregion
        #region Unity Functions

        protected override void Start()
        {
            base.Start();

            camRoot = transform.GetChild(0).GetChild(0);

            meshScale = mesh.localScale.y;
            camRootHeight = camRoot.localPosition.y;

            fullHealthStatus = hpStatus.GetChild(0).gameObject;
            healingStatus = hpStatus.GetChild(1).gameObject;
            notFullHealthStatus = hpStatus.GetChild(2).gameObject;

            previousHPStatus = fullHealthStatus;

            InitialiseHUD();

            Cursor.lockState = CursorLockMode.Locked;
        }

        protected override void Update()
        {
            base.Update();
            ManageHUD();
        }

        private void FixedUpdate()
        {
            if (IsDead) return;

            Movement();
        }

        private void LateUpdate()
        {
            if (IsDead) return;

            CameraRotation();
        }

        #endregion
        #region Movement

        /// <summary>
        /// Called when the (real) player initiates the move keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnPlayerMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// Called when the (real) player initiates the crouch keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnPlayerCrouch(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                // Start crouching.
                isCrouching = true;
                mesh.localScale = new Vector3(mesh.localScale.x, mesh.localScale.y * hero.HeroData.CrouchHeight, mesh.localScale.z);
                camRoot.localPosition = new Vector3(camRoot.localPosition.x, camRoot.localPosition.y * hero.HeroData.CrouchHeight, camRoot.localPosition.z);
            }
            else if (context.canceled)
            {
                // End crouching.
                isCrouching = false;
                mesh.localScale = new Vector3(mesh.localScale.x, meshScale, mesh.localScale.z);
                camRoot.localPosition = new Vector3(camRoot.localPosition.x, camRootHeight, camRoot.localPosition.z);
            }
        }

        /// <summary>
        /// Called when the (real) player initiates the jump keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnPlayerJump(InputAction.CallbackContext context)
        {
            if (context.started && grounded)
            {
                // If the (real) player can jump and is trying to jump, set its y velocity to simulate a jump.
                Vector3 currentVelocity = rb.linearVelocity;
                currentVelocity.y = Mathf.Sqrt(hero.HeroData.JumpHeight * -2f * Physics.gravity.y);
                rb.linearVelocity = currentVelocity;
            }
        }

        /// <summary>
        /// Manages the movement of the (real) player.
        /// </summary>
        protected override void Movement()
        {
            base.Movement();

            Vector3 moveDirection = CalculateMovement();

            if (isCrouching)
            {
                moveSpeed = hero.HeroData.BaseCrouchSpeed;
            }
            else
            {
                moveSpeed = moveInput.y > 0f ? hero.HeroData.BaseMoveSpeed : hero.HeroData.BaseMoveSpeed * hero.HeroData.BackwardsMoveReduction;
            }

            moveSpeed *= hero.MovementModification;

            // Allows the (real) player to keep momentum during a jump (Generated by AI).
            if (moveDirection.magnitude > 0.01f)
            {
                // If we have input, set horizontal velocity directly.
                Vector3 newVelocity = moveDirection * moveSpeed;

                // Preserve jump/gravity.
                newVelocity.y = rb.linearVelocity.y; 
                rb.linearVelocity = newVelocity;
            }
            else
            {
                // No input: keep horizontal momentum.
                Vector3 newVelocity = !grounded ? rb.linearVelocity : Vector3.zero;

                // Gravity still applies.
                newVelocity.y = rb.linearVelocity.y; 
                rb.linearVelocity = newVelocity;
            }
        }

        /// <summary>
        /// Calculates the move direction of the (real) player.
        /// </summary>
        /// <returns>The calculated move direction based on the (real) player's inputs.</returns>
        private Vector3 CalculateMovement()
        {
            Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);

            Vector3 cameraForward = camRoot.forward;
            Vector3 cameraRight = camRoot.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            return (cameraForward * movement.z + cameraRight * movement.x).normalized;
        }

        #endregion
        #region Camera

        /// <summary>
        /// Called when the (real) player initiates the look keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnPlayerLook(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// Sets the rotation of the camera based on the (real) player's inputs.
        /// </summary>
        private void CameraRotation()
        {
            // Directly apply sensitivity to the input before smoothing.
            float targetYaw = cinemachineTargetYaw + (lookInput.x * camSensX);
            float targetPitch = cinemachineTargetPitch + (-lookInput.y * camSensY);

            targetPitch = ClampAngle(targetPitch, minCamY, maxCamY);

            // Smooth rotation toward target using SmoothDampAngle.
            currentCamRotation.x = Mathf.SmoothDampAngle(currentCamRotation.x, targetPitch, ref rotationVelocity.x, rotationSmooth);
            currentCamRotation.y = Mathf.SmoothDampAngle(currentCamRotation.y, targetYaw, ref rotationVelocity.y, rotationSmooth);

            // Apply the smooth rotation
            camRoot.rotation = Quaternion.Euler(currentCamRotation.x, currentCamRotation.y, 0f);
            mesh.rotation = Quaternion.Euler(0f, currentCamRotation.y, 0f);

            // Update the target values for the next frame.
            cinemachineTargetYaw = targetYaw;
            cinemachineTargetPitch = targetPitch;
        }

        /// <summary>
        /// Normalises and clamps an angle between 'min' and 'max'
        /// </summary>
        /// <param name="angle">The angle to clamp.</param>
        /// <param name="min">The minimum the angle can be.</param>
        /// <param name="max">The maximum the angle can be.</param>
        /// <returns>The clamped angle.</returns>
        private float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;

            return Mathf.Clamp(angle, min, max);
        }

        #endregion
        #region Actions

        /// <summary>
        /// Called when the (real) player dies.
        /// </summary>
        protected override void OnDeath()
        {
            base.OnDeath();

            // If the (real) player was doing an action, then stop that action. 
            hero.PrimaryFire(false);
            hero.SecondaryFire(false);
            hero.Ability1Active = false;
            hero.Ability2Active = false;
            hero.UltimateAbilityActive = false;
            hero.QuickMeleeActive = false;
        }

        /// <summary>
        /// Called when the (real) player initiates the primary fire keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnPrimaryFire(InputAction.CallbackContext context)
        {
            if (IsDead) return;

            if (context.started)
            {
                if (perkSelectActive)
                {
                    // Use the primary fire input to select the first perk of level 2/3.
                    if (hero.PerkSystem.CurrentLevel == 2)
                    {
                        hero.Level2Perk1();
                    }
                    else if (hero.PerkSystem.CurrentLevel == 3)
                    {
                        hero.Level3Perk1();
                    }

                    perkSelectActive = false;
                    return;
                }

                PrimaryFireActive = true;

                if (LockPrimaryFire)
                {
                    // Unable to fire, so stop firing.
                    hero.PrimaryFire(false);
                    return;
                }

                hero.PrimaryFire(true);
            }
            else if (context.canceled)
            {
                // Stop firing as the (real) player is no longer firing.
                hero.PrimaryFire(false);
                PrimaryFireActive = false;
            }
        }

        /// <summary>
        /// Prevents the (real) player from using the primary fire action (basically on cooldown).
        /// </summary>
        /// <param name="recoveryDuration">The length of time to lock the primary fire action for.</param>
        public IEnumerator PrimaryFireLock(float recoveryDuration)
        {
            LockPrimaryFire = true;
            hero.PrimaryFire(false);

            yield return new WaitForSeconds(recoveryDuration);

            LockPrimaryFire = false;
            if (PrimaryFireActive) hero.PrimaryFire(true);
        }

        /// <summary>
        /// Called when the (real) player initiates the secondary fire keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnSecondaryFire(InputAction.CallbackContext context)
        {
            if (IsDead) return;

            if (context.started)
            {
                if (perkSelectActive)
                {
                    // Use the secondary fire input to select the second perk of level 2/3.
                    if (hero.PerkSystem.CurrentLevel == 2)
                    {
                        hero.Level2Perk2();
                    }
                    else if (hero.PerkSystem.CurrentLevel == 3)
                    {
                        hero.Level3Perk2();
                    }

                    perkSelectActive = false;
                    return;
                }

                SecondaryFireActive = true;

                // Unable to fire, so don't fire.
                if (LockSecondaryFire) return;

                hero.SecondaryFire(true);
            }
            else if (context.canceled)
            {
                // Stop firing as the (real) player is no longer firing.
                hero.SecondaryFire(false);
                SecondaryFireActive = false;
            }
        }

        /// <summary>
        /// Prevents the (real) player from using the secondary fire action (basically on cooldown).
        /// </summary>
        /// <param name="recoveryDuration">The length of time to lock the secondary fire action for.</param>
        public IEnumerator SecondaryFireLock(float recoveryDuration)
        {
            LockSecondaryFire = true;

            yield return new WaitForSeconds(recoveryDuration);

            LockSecondaryFire = false;
            if (SecondaryFireActive) hero.SecondaryFire(true);
        }

        /// <summary>
        /// Called when the (real) player initiates the ability 1 keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnAbility1(InputAction.CallbackContext context)
        {
            if (IsDead || AbilityLock)
            {
                hero.Ability1Active = false;
                return;
            }

            // TODO: This works for "toggle" based actions, whereas 'OnAbility2' works for "holding down" based actions.
            // Add a way for either to work based on which one is selected (dependent on hero).
            if (context.started) hero.Ability1Active = !hero.Ability1Active;
        }

        /// <summary>
        /// Called when the (real) player initiates the ability 2 keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnAbility2(InputAction.CallbackContext context)
        {
            if (IsDead || AbilityLock)
            {
                hero.Ability2Active = false;
                return;
            }

            if (context.started)
            {
                hero.Ability2Active = true;
            }
            else if (context.canceled)
            {
                hero.Ability2Active = false;
            }
        }

        /// <summary>
        /// Called when the (real) player initiates the ultimate ability keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnUltimateAbility(InputAction.CallbackContext context)
        {
            if (IsDead || AbilityLock)
            {
                hero.UltimateAbilityActive = false;
                return;
            }

            if (context.started)
            {
                hero.UltimateAbilityActive = true;
            }
            else if (context.canceled)
            {
                hero.UltimateAbilityActive = false;
            }
        }

        /// <summary>
        /// Called when the (real) player initiates the quick melee keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnQuickMelee(InputAction.CallbackContext context)
        {
            if (IsDead || AbilityLock)
            {
                hero.QuickMeleeActive = false;
                return;
            }

            if (context.started)
            {
                hero.QuickMeleeActive = true;
            }
            else if (context.canceled)
            {
                hero.QuickMeleeActive = false;
            }
        }

        /// <summary>
        /// Called when the (real) player initiates the reload keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnReload(InputAction.CallbackContext context)
        {
            if (IsDead || AbilityLock) return;

            if (context.started) hero.Reload();
        }

        /// <summary>
        /// Called when the (real) player initiates the perk select keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnPerkSelect(InputAction.CallbackContext context)
        {
            if (isDead) return;

            if (context.started && hero.PerkAvailable)
            {
                perkSelectActive = !perkSelectActive;
                hero.SelectPerk();
            }
        }

        /// <summary>
        /// Called when the (real) player initiates the ping keybind.
        /// </summary>
        /// <param name="context">The data of the player's actions.</param>
        public void OnPing(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                // Check to see if the (real) player has tried to ping another entity
                if (Physics.Raycast(ray, out RaycastHit hit, 200, LayerMask.GetMask("Entity")))
                {
                    // Initiate the ping attributes of the entity.
                    pingedEntity = hit.collider.transform.root.GetComponent<Entity>();
                    pingedEntity.PingCooldown = 6;
                    pingedEntity.IsPinged = true;

                    pingedEntity.transform.GetChild(0).Find("UI/UI Full Rotation/Ping").gameObject.SetActive(true); // Bot Hero/UI/UI Full Rotation/Ping
                }
            }
        }

        #endregion
        #region HUD

        /// <summary>
        /// Initialises the HUD by calling specific functions at the start of the game.
        /// </summary>
        private void InitialiseHUD()
        {
            playerNameText.text = EntityName;

            UpdatePerkUI();
            UpdateWeaponUI();

            init = true;
        }

        /// <summary>
        /// Update the (real) player's UI every frame.
        /// </summary>
        private void ManageHUD()
        {
            if (!init) return;

            UpdatePerkUI();
            UpdateUltimateUI();
            UpdateWeaponUI();
            UpdateHPUI();
        }

        /// <summary>
        /// Updates the perk UI to the (real) player's current perk progress.
        /// </summary>
        public void UpdatePerkUI()
        {
            perkLevelText.text = $"{hero.PerkSystem.CurrentLevel}";

            switch (hero.PerkSystem.CurrentLevel)
            {
                case 1:
                    perkProgress.fillAmount = (float)(hero.PerkSystem.Current / hero.PerkSystem.Levels[0]);
                    break;
                case 2:
                    perkProgress.fillAmount = (float)((hero.PerkSystem.Current - hero.PerkSystem.Levels[0]) / (hero.PerkSystem.Levels[1] - hero.PerkSystem.Levels[0]));
                    break;
                default:
                    perkProgress.fillAmount = 0f;
                    break;
            }
        }

        /// <summary>
        /// Updates the ultimate UI to the (real) player's current ultimate charge.
        /// </summary>
        public void UpdateUltimateUI()
        {
            float value = Mathf.Min((float)(hero.UltimateSystem.Current / hero.UltimateSystem.Cost), 1f);
            if (value == 1f)
            {
                ultimateChargeObject.SetActive(false);
                ultimateReadyObject.SetActive(true);
                return;
            }
            else if (ultimateReadyObject.activeInHierarchy)
            {
                ultimateChargeObject.SetActive(true);
                ultimateReadyObject.SetActive(false);
            }

            // Don't display "100" as a number (instead the ultimate icon should appear).
            ultimateChargeText.text = $"{Mathf.Min(Mathf.Round(value * 100f), 99f)}";
            ultimateCharge.fillAmount = value;
        }
        
        /// <summary>
        /// Updates the weapon UI to the (real) player's current weapon ammo information.
        /// </summary>
        public void UpdateWeaponUI()
        {
            currentAmmo.text = $"{weapon.weaponCaster.ammo.magazineAmount}";
            ammoCapacity.text = $"<color=#42a2f1>|</color><alpha=#C8> {weapon.weaponCaster.ammo.magazineCapacity}";
        }

        /// <summary>
        /// Updates the HP UI to the (real) player's current HP status.
        /// </summary>
        /// <param name="init">Whether to initialise the UI or not.</param>
        public void UpdateHPUI(bool init = false)
        {
            double totalHP = hero.CurrentHP.Health + hero.CurrentHP.Armour + hero.CurrentHP.Shields;
            hpStatusText.text = $"{Mathf.Max(0f, Mathf.Floor((float)totalHP))} <color=#42a2f1>|</color><alpha=#C8> <size=20>{hero.HeroData.TotalHP}</size>";

            if (init)
            {
                double healthRemaining = hero.HeroData.HP.Health,
                    armourRemaining = hero.HeroData.HP.Armour,
                    shieldsRemaining = hero.HeroData.HP.Shields,
                    totalHealthRemaining = hero.HeroData.TotalHP;
                Color colour = Color.white;

                // Create each of the bars based on the type of HP and the total amount of HP of the hero.
                for (int i = 0; i < Mathf.Ceil((float)hero.HeroData.TotalHP / Hero.HPBAR.Size); i++)
                {
                    if (totalHealthRemaining < 0f) break;

                    GameObject newHPBar = Instantiate(hpBarPrefab);
                    GameObject newHPBarBg = Instantiate(hpBackgroundBarPrefab);

                    newHPBar.transform.SetParent(hpBarsContainer, false);
                    newHPBar.transform.localPosition = Vector3.zero;
                    newHPBar.transform.localScale = Vector3.one;

                    newHPBarBg.transform.SetParent(hpBarsBackground, false);
                    newHPBarBg.transform.localPosition = Vector3.zero;
                    newHPBarBg.transform.localScale = Vector3.one;

                    if (healthRemaining > 0f)
                    {
                        colour = Hero.HPBAR.White;
                        healthRemaining -= Hero.HPBAR.Size;

                        if (totalHealthRemaining - Hero.HPBAR.Size < 0f) newHPBar.GetComponent<Image>().fillAmount = (float)totalHealthRemaining / Hero.HPBAR.Size;
                    }
                    else if (armourRemaining > 0f)
                    {
                        colour = Hero.HPBAR.Armour;
                        armourRemaining -= Hero.HPBAR.Size;

                        if (totalHealthRemaining - Hero.HPBAR.Size < 0f) newHPBar.GetComponent<Image>().fillAmount = (float)totalHealthRemaining / Hero.HPBAR.Size;
                    }
                    else if (shieldsRemaining > 0f)
                    {
                        float size = (float)shieldsRemaining / Hero.HPBAR.Size;

                        newHPBar.GetComponent<Image>().fillAmount = 1f - size;
                        newHPBar.transform.GetChild(0).GetComponent<Image>().fillAmount = size;

                        shieldsRemaining -= Hero.HPBAR.Size;
                    }

                    totalHealthRemaining -= Hero.HPBAR.Size;

                    newHPBar.GetComponent<Image>().color = colour;

                    newHPBar.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = Vector2.zero;

                    hpBars.Add(newHPBar.GetComponent<Image>());
                }
            }
            else
            {
                double healthRemaining = hero.CurrentHP.Health,
                armourRemaining = hero.CurrentHP.Armour,
                shieldsRemaining = hero.CurrentHP.Shields,
                totalHealth = hero.CurrentHP.Health + hero.CurrentHP.Armour + hero.CurrentHP.Shields;

                if (IsHealing)
                {
                    if (!healingStatus.activeInHierarchy)
                    {
                        // Set the HP status to 'healing' since the (real) player is being healed.
                        previousHPStatus.SetActive(false);
                        healingStatus.SetActive(true);
                        previousHPStatus = healingStatus;
                    }
                }
                else
                {
                    if (hero.TotalHP() != hero.HeroData.TotalHP && !notFullHealthStatus.activeInHierarchy)
                    {
                        // Set the HP status to 'not full health' since the (real) player is not full health and is not being healed.
                        previousHPStatus.SetActive(false);
                        notFullHealthStatus.SetActive(true);
                        previousHPStatus = notFullHealthStatus;
                    }
                    else if (hero.TotalHP() == hero.HeroData.TotalHP && !fullHealthStatus.activeInHierarchy)
                    {
                        // Set the HP status to 'full health' since the (real) player is full health.
                        previousHPStatus.SetActive(false);
                        fullHealthStatus.SetActive(true);
                        previousHPStatus = fullHealthStatus;
                    }
                }

                if (totalHealth < 0f)
                {
                    // Set each of the bars' fill amount to 0 if the hero is dead.
                    foreach (Image hpBar in hpBars)
                    {
                        hpBar.fillAmount = 0f;
                    }
                    return;
                }

                if (totalHealth <= hero.HeroData.TotalHP)
                {
                    double totalHealthRemaining = totalHealth;

                    foreach (Image hpBar in hpBars)
                    {
                        hpBar.fillAmount = 0f;
                        hpBar.transform.GetChild(0).GetComponent<Image>().fillAmount = 0f;

                        if (healthRemaining > 0f)
                        {
                            float size = (float)healthRemaining / Hero.HPBAR.Size;
                            hpBar.fillAmount = size;
                            healthRemaining -= Hero.HPBAR.Size;

                            // Since you can have shields while not being full health, dynamically calculate the shields to be in front of health.
                            if (shieldsRemaining > 0f && size < 1f)
                            {
                                if (shieldsRemaining < Hero.HPBAR.Size)
                                {
                                    hpBar.transform.GetChild(0).GetComponent<Image>().fillOrigin = (int)Image.OriginHorizontal.Left;
                                    hpBar.transform.GetChild(0).GetComponent<Image>().fillAmount = size + ((float)shieldsRemaining / Hero.HPBAR.Size);
                                }
                                else
                                {
                                    hpBar.transform.GetChild(0).GetComponent<Image>().fillOrigin = (int)Image.OriginHorizontal.Right;
                                    hpBar.transform.GetChild(0).GetComponent<Image>().fillAmount = 1f - size;
                                }

                                shieldsRemaining -= (1f - size) * Hero.HPBAR.Size;
                            }
                            else
                            {
                                hpBar.transform.GetChild(0).GetComponent<Image>().fillAmount = 0f;
                            }
                        }
                        else if (armourRemaining > 0f)
                        {
                            hpBar.fillAmount = (float)armourRemaining / Hero.HPBAR.Size;
                            armourRemaining -= Hero.HPBAR.Size;
                        }
                        else if (shieldsRemaining > 0f)
                        {
                            float size = (float)shieldsRemaining / Hero.HPBAR.Size;

                            hpBar.transform.GetChild(0).GetComponent<Image>().fillOrigin = (int)Image.OriginHorizontal.Left;
                            hpBar.transform.GetChild(0).GetComponent<Image>().fillAmount = size;

                            shieldsRemaining -= Hero.HPBAR.Size;
                        }

                        totalHealthRemaining -= Hero.HPBAR.Size;
                    }
                }
            }
        }

        #endregion
    }
}