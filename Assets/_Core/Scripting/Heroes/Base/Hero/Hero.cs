using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using NaughtyAttributes;
using TMPro;

namespace Unitywatch
{
    /// <summary>
    /// Class as a structure for HP bars (both HUD and world UI). 
    /// </summary>
    public class HPBar
    {
        public Color32 White = Color.white,
            EnemyWhite = Color.red,
            Armour = new Color32(255, 92, 0, 255),
            Shields = new Color32(4, 217, 255, 255),
            Overhealth = new Color32(255, 20, 255, 255);

        // Alpha used for the background of each bar & each bar is equivalent to 25 HP.
        public float Alpha = 150f,
            Size = 25f;
    }

    /// <summary>
    /// Base class for all heroes.
    /// </summary>
    public class Hero : MonoBehaviour
    {
        #region Init Attributes
        [SerializeField]
        private bool enableHealthAutoRegen = true,
            enableShieldAutoRegen = true;

        [SerializeField]
        protected HeroData heroData;
        public HeroData HeroData { get { return heroData; } }

        [SerializeField]
        protected HitPoints currentHP;
        public HitPoints CurrentHP
        {
            get { return currentHP; }
            set { currentHP = value; }
        }

        /// <summary>
        /// Returns the current total HP of this hero as a number.
        /// </summary>
        /// <returns>The current total HP.</returns>
        public double TotalHP()
        {
            return CurrentHP.Health + CurrentHP.Armour + CurrentHP.Shields;
        }

        [SerializeField]
        protected UltimateSystem ultimateSystem;
        public UltimateSystem UltimateSystem
        {
            get { return ultimateSystem; }
            set { ultimateSystem = value; }
        }

        [SerializeField]
        protected PerkSystem perkSystem;
        public PerkSystem PerkSystem
        {
            get { return perkSystem; }
            set { perkSystem = value; }
        }

        [SerializeField, Foldout("HUD - Player Info")]
        private Image heroImg,
            heroImgTop;

        [SerializeField, Foldout("HUD - Ultimate")]
        private AbilityData ultimateAbilityData;

        [SerializeField, Foldout("HUD - Ultimate")]
        private Image ultimateAbilityImg,
            ultimateAbilityDisabledImg;

        [SerializeField, Foldout("HUD - Abilities & Gun")]
        private WeaponData weaponData;

        [SerializeField, Foldout("HUD - Abilities & Gun")]
        private Image weaponImg;

        [SerializeField, Foldout("HUD - Abilities & Gun")]
        private List<AbilityData> abilityDatas;

        [SerializeField, Foldout("HUD - Abilities & Gun")]
        private Transform abilitiesParent;

        [SerializeField, Foldout("HUD - Abilities & Gun")]
        private GameObject abilityKBPrefab,
            abilityMousePrefab;

        [SerializeField, Foldout("HUD - Abilities & Gun")]
        private SerializedDictionary<string, Sprite> mouseKeybindSprites;

        [SerializeField, Foldout("HUD - Perks")]
        private GameObject perkSelectNotification,
            perkSelect,
            selectedPerk,
            selectedLvl2Perk,
            selectedLvl3Perk;

        [SerializeField, Foldout("HUD - Perks")]
        private Image perk1Icon,
            perk2Icon,
            selectedPerkIcon,
            selectedLvl2PerkIcon,
            selectedLvl3PerkIcon;

        [SerializeField, Foldout("HUD - Perks")]
        private TMP_Text perk1Name,
            perk1Desc,
            perk2Name,
            perk2Desc;

        [SerializeField, Foldout("Perks")]
        private PerkData level2Perk1Data,
            level2Perk2Data,
            level3Perk1Data,
            level3Perk2Data;

        [Foldout("Perks")]
        public bool level2Perk1,
            level2Perk2,
            level3Perk1,
            level3Perk2;

        [SerializeField, Foldout("UI")]
        private Transform ui,
            hpBarsContainer,
            hpBarsBackground,
            uiFullRotation,
            uiYRotation;

        [SerializeField, Foldout("UI")]
        private CanvasGroup hpUI;

        [SerializeField, Foldout("Prefabs")]
        private GameObject HPBarPrefab,
            HPBackgroundBarPrefab;

        [Foldout("Prefabs")]
        public GameObject areaOfEffectPrefab;

        [SerializeField, Foldout("Debug")]
        private float healingModification = 1f;
        public float HealingModification
        {
            get { return healingModification; }
            set { healingModification = value; }
        }

        [SerializeField, Foldout("Debug")]
        private float damageModification = 1f;
        public float DamageModification
        {
            get { return damageModification; }
            set { damageModification = value; }
        }

        [SerializeField, Foldout("Debug")]
        private float movementModification = 1f;
        public float MovementModification
        {
            get { return movementModification; }
            set { movementModification = value; }
        }

        [SerializeField, ReadOnly, Foldout("Debug")]
        private float damagePassiveDuration = 3f,
            damagePassiveTime;

        [HideInInspector]
        public Animator animator;
        public static HPBar HPBAR = new HPBar();

        private bool perkAvailable;
        public bool PerkAvailable
        {
            get { return perkAvailable; }
            set { perkAvailable = value; }
        }

        private bool damagePassiveApplied;
        public bool DamagePassiveApplied
        {
            get { return damagePassiveApplied; }
            set { damagePassiveApplied = value; }
        }

        protected bool ability1Active,
            ability2Active,
            ultimateAbilityActive,
            quickMeleeActive;

        public bool Ability1Active
        {
            get { return ability1Active; }
            set { ability1Active = value; }
        }
        public bool Ability2Active
        {
            get { return ability2Active; }
            set { ability2Active = value; }
        }
        public bool UltimateAbilityActive
        {
            get { return ultimateAbilityActive; }
            set { ultimateAbilityActive = value; }
        }
        public bool QuickMeleeActive
        {
            get { return quickMeleeActive; }
            set { quickMeleeActive = value; }
        }

        private float timeSinceDamageTaken,
            timeSinceHealingReceived,
            healingReceivedTimeout = 0.5f;
        private bool updateHPBUI;

        private List<Image> hpBars = new List<Image>();

        protected Entity entity;
        protected Player player;
        private QuickMelee quickMelee;

        #endregion
        #region Unity Functions

        private void Awake()
        {
            player = transform.root.GetComponent<Player>();

            if (player != null) InitialisePlayerHUD();            
        }

        protected virtual void Start()
        {
            entity = transform.root.GetComponent<Entity>();

            quickMelee = GetComponent<QuickMelee>();

            currentHP = new HitPoints();
            currentHP.Health = HeroData.HP.Health;
            currentHP.Armour = HeroData.HP.Armour;
            currentHP.Shields = HeroData.HP.Shields;

            ultimateSystem = new UltimateSystem();
            ultimateSystem.Cost = HeroData.UltimateSystem.Cost;

            perkSystem = new PerkSystem();
            perkSystem.Levels[0] = HeroData.PerkSystem.Levels[0];
            perkSystem.Levels[1] = HeroData.PerkSystem.Levels[1];

            if (player != null)
            {
                // For the (real) player entity only.

                player.playerController.UpdateHPUI(true);
                player.playerController.UpdateUltimateUI();
            }
            else
            {
                player = GameObject.FindWithTag("Player").GetComponent<Player>();
            }

            // For entities other than the (real) player if they have a HP UI.
            if (ui != null)
            {
                updateHPBUI = true;
                UpdateUIHP(true);
            }

            StartCoroutine(PassiveUltimateCharge());

            StartCoroutine(Ability1());
            StartCoroutine(Ability2());
            StartCoroutine(UltimateAbility());
            StartCoroutine(QuickMelee());
        }

        protected virtual void Update()
        {
            if (updateHPBUI)
            {
                UpdateUIHP();

                uiFullRotation.localRotation = Camera.main.transform.localRotation;

                uiYRotation.localRotation = Camera.main.transform.localRotation;
                uiYRotation.RotateAround(transform.position, Vector3.up, Vector3.Angle(uiYRotation.localEulerAngles, Camera.main.transform.localEulerAngles));
                uiYRotation.transform.localEulerAngles = new Vector3(0f, uiYRotation.transform.localEulerAngles.y, 0f);
            }

            if (entity.IsDead) return;

            if (damagePassiveApplied) DamagePassive();

            AutoRegenHP();
            if (HeroData.HP.Shields > 0f) AutoRegenShields();

            // Checks whether this hero hasn't received healing in a specified window.
            if (entity.IsHealing && Time.time - timeSinceHealingReceived > healingReceivedTimeout) entity.IsHealing = false;
        }

        /// <summary>
        /// Set the static UI elements of this hero on the HUD.
        /// </summary>
        private void InitialisePlayerHUD()
        {
            heroImg.sprite = heroData.HeroIcon;
            heroImgTop.sprite = heroData.HeroIcon;
            weaponImg.sprite = weaponData.WeaponIcon;
            ultimateAbilityImg.sprite = ultimateAbilityData.AbilityIcon;
            ultimateAbilityDisabledImg.sprite = ultimateAbilityData.AbilityIcon;

            foreach (AbilityData abilityData in abilityDatas)
            {
                GameObject ability;

                if (mouseKeybindSprites.ContainsKey(abilityData.Keybind))
                {
                    // The ability's keybind is a mouse input.
                    ability = Instantiate(abilityMousePrefab);
                    ability.transform.GetChild(1).GetComponent<Image>().sprite = mouseKeybindSprites[abilityData.Keybind];
                }
                else
                {
                    // The ability's keybind is a keyboard input.
                    ability = Instantiate(abilityKBPrefab);

                    string keybindName = abilityData.Keybind;
                    if (keybindName == "Left Shift") keybindName = "LSHIFT";
                    if (keybindName == "Left Alt") keybindName = "LALT";

                    ability.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = keybindName;

                    // Makes sure that the outer box for keyboard keybinds renders correctly.
                    StartCoroutine(RebuildNextFrame(ability.transform.GetChild(1).GetComponent<RectTransform>()));
                }

                ability.name = abilityData.Name;
                ability.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Image>().sprite = abilityData.AbilityIcon;

                ability.transform.SetParent(abilitiesParent);
                ability.transform.localScale = Vector3.one;

                if (abilityData.StartDisabled) ability.SetActive(false);
            }
        }

        private IEnumerator RebuildNextFrame(RectTransform rectTransform)
        {
            yield return null; // wait one frame

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        #endregion
        #region Passives

        /// <summary>
        /// Manages the state of the damage passive that has been applied.
        /// </summary>
        private void DamagePassive()
        {
            if (damagePassiveTime > 0f)
            {
                damagePassiveTime -= Time.deltaTime;
            }
            else
            {
                damagePassiveApplied = false;
                HealingModification /= 1f - (HeroData.Role == "Tank" ? 0.15f : 0.3f);
            }
        }

        /// <summary>
        /// Adds 5 ultimate points per second while this hero is not dead.
        /// </summary>
        private IEnumerator PassiveUltimateCharge()
        {
            while (true)
            {
                if (entity.IsDead) yield return null;

                yield return new WaitForSeconds(1f);
                CalculateUltCharge(5, entity);
            }
        }

        /// <summary>
        /// Regenerates this hero's HP after they have not taken damage for a certain period of time.
        /// Supports will regenerate HP after 3 seconds of no damage, 6 for tanks and dps.
        /// </summary>
        private void AutoRegenHP()
        {
            if (!enableHealthAutoRegen) return;

            if (TotalHP() != HeroData.TotalHP && Time.time - timeSinceDamageTaken >= (HeroData.Role == "Support" ? 3f : 6f))
            {
                entity.Hero.UpdateHP((Constants.AUTO_REGEN_CONSTANT + Constants.AUTO_REGEN_PERCENTAGE * HeroData.TotalHP) * Time.deltaTime, false, entity, false, autoRegen: true);
            }
        }

        /// <summary>
        /// Regenerates this hero's shields (if they have shields) after they have not taken damage for 3 seconds.
        /// </summary>
        private void AutoRegenShields()
        {
            if (!enableShieldAutoRegen) return;

            if (CurrentHP.Shields != HeroData.HP.Shields && Time.time - timeSinceDamageTaken >= 3f)
            {
                double tempValue = Constants.SHIELD_REGEN_CONSTANT * HealingModification * Time.deltaTime;

                if (CurrentHP.SelfDamage > 0f)
                {
                    // Heal any self damage.
                    if (CurrentHP.SelfDamage - tempValue > 0f)
                    {
                        CurrentHP.SelfDamage -= tempValue;
                    }
                    else
                    {
                        CurrentHP.SelfDamage = 0f;
                    }
                }

                if (TotalHP() != HeroData.TotalHP)
                {
                    // This hero is being healed.
                    entity.IsHealing = true;
                    timeSinceHealingReceived = Time.time;
                }

                if (tempValue > 0f)
                {
                    // Regenerate the shields.
                    if (CurrentHP.Shields + tempValue >= HeroData.HP.Shields)
                    {
                        tempValue = CurrentHP.Shields + tempValue - HeroData.HP.Shields;
                        CurrentHP.Shields = HeroData.HP.Shields;
                    }
                    else
                    {
                        CurrentHP.Shields += tempValue;
                        tempValue = 0f;
                    }
                }

                if (TotalHP() == HeroData.TotalHP) entity.IsHealing = false;

                entity.Hero.UpdateHP(0f, false, entity, false, autoRegen: true);
            }
        }

        #endregion
        #region Update HP

        /// <summary>
        /// Updates the HP of this hero.
        /// This method also handles:
        ///     - applying damage or healing modification.
        ///     - final blow detection.
        ///     - whether ultimate and perk points should be awarded.
        /// </summary>
        /// <param name="value">The base value of the received damage/healing.</param>
        /// <param name="damage">Whether 'value' is damage or not.</param>
        /// <param name="from">The entity that 'value' is coming from.</param>
        /// <param name="isUltimate">Whether the 'value' came from an ultimate being used.</param>
        /// <param name="abilityUsed">Whether the 'value' came from an ability being used.</param>
        /// <param name="headshot">Whether the 'value' is a headshot or not.</param>
        /// <param name="autoRegen">Whether the 'value' came from auto-regen.</param>
        /// <param name="affectArmour">How the 'value' should affect any armour of this hero.</param>
        public void UpdateHP(double value, bool damage, Entity from, bool isUltimate, AbilityData abilityUsed = null, bool headshot = false, bool autoRegen = false, string affectArmour = "yes")
        {
            // Apply the healing or damage modification.
            value *= damage && from ? from.Hero.DamageModification : HealingModification;

            double tempValue = value;
            double hitValue = 0;
            bool selfDamage = damage && from?.EntityID == entity.EntityID;

            if (damage)
            {
                timeSinceDamageTaken = Time.time;

                // Keep track of self damage, as you cannot gain ultimate charge & perk progress from dealing or healing it.
                if (selfDamage) CurrentHP.SelfDamage += tempValue;

                if (from?.Hero.HeroData.Role == "Damage" && !selfDamage)
                {
                    // If the damage received was from a hero from the 'Damage' role, then apply the damage passive.
                    damagePassiveTime = damagePassiveDuration;
                    if (!damagePassiveApplied) HealingModification *= 1f - (HeroData.Role == "Tank" ? Constants.DAMAGE_PASSIVE_TANKS : Constants.DAMAGE_PASSIVE);

                    damagePassiveApplied = true;
                }

                if (CurrentHP.Shields > 0f)
                {
                    // Remove any shields if this hero currently has any.
                    if (tempValue > CurrentHP.Shields)
                    {
                        tempValue -= CurrentHP.Shields;
                        CurrentHP.Shields = 0f;
                    }
                    else
                    {
                        CurrentHP.Shields -= tempValue;
                        tempValue = 0f;
                    }
                }

                if (tempValue > 0f && CurrentHP.Armour > 0f)
                {
                    // Remove any armour based on type of damage received, if this hero currently has any.
                    if (affectArmour == "beam")
                    {
                        tempValue *= 1f - Constants.BEAM_ON_ARMOUR;
                    }
                    else if (affectArmour == "dot")
                    {
                        tempValue *= 1f - Constants.DOT_ON_ARMOUR;
                    }
                    else if (affectArmour == "yes")
                    {
                        if (value <= 14)
                        {
                            tempValue /= 2f;
                        }
                        else
                        {
                            tempValue -= 7f;
                        }
                    }
                    value = tempValue;

                    if (tempValue > CurrentHP.Armour)
                    {
                        tempValue -= CurrentHP.Armour;
                        CurrentHP.Armour = 0f;
                    }
                    else
                    {
                        CurrentHP.Armour -= tempValue;
                        tempValue = 0f;
                    }
                }

                if (tempValue > 0f && CurrentHP.Health > 0f)
                {
                    // Remove any health if this hero currently has any.
                    CurrentHP.Health -= tempValue;
                }

                // Store the true value of the damage received.
                hitValue = CurrentHP.Health < 0f ? value + CurrentHP.Health : value;
            }
            else
            {
                if (CurrentHP.SelfDamage > 0f)
                {
                    // Heal the self damage, but this won't be eligble to gain ultimate charge & perk progress.
                    selfDamage = true;

                    if (CurrentHP.SelfDamage - tempValue > 0f)
                    {
                        CurrentHP.SelfDamage -= tempValue;
                    }
                    else
                    {
                        // TODO: if healing received > self damage, then the difference should be treated as normal healing.
                        CurrentHP.SelfDamage = 0f;
                    }
                }

                if (TotalHP() != HeroData.TotalHP)
                {
                    // This hero is being healed.
                    entity.IsHealing = true;
                    timeSinceHealingReceived = Time.time;
                }

                // Regenerate the hero's health.
                if (CurrentHP.Health + tempValue >= HeroData.HP.Health)
                {
                    tempValue = CurrentHP.Health + tempValue - HeroData.HP.Health;
                    hitValue += HeroData.HP.Health - CurrentHP.Health;
                    CurrentHP.Health = HeroData.HP.Health;
                }
                else
                {
                    CurrentHP.Health += tempValue;
                    hitValue += tempValue;
                    tempValue = 0f;
                }

                if (tempValue > 0f)
                {
                    // Regenerate the hero's armour if there is no more health to heal.
                    if (CurrentHP.Armour + tempValue >= HeroData.HP.Armour)
                    {
                        tempValue = CurrentHP.Armour + tempValue - HeroData.HP.Armour;
                        hitValue += HeroData.HP.Armour - CurrentHP.Armour;
                        CurrentHP.Armour = HeroData.HP.Armour;
                    }
                    else
                    {
                        CurrentHP.Armour += tempValue;
                        hitValue += tempValue;
                        tempValue = 0f;
                    }
                }

                if (tempValue > 0f)
                {
                    // Regenerate the hero's shields if there is no more health nor armour to heal.
                    if (CurrentHP.Shields + tempValue >= HeroData.HP.Shields)
                    {
                        tempValue = CurrentHP.Shields + tempValue - HeroData.HP.Shields;
                        hitValue += HeroData.HP.Shields - CurrentHP.Shields;
                        CurrentHP.Shields = HeroData.HP.Shields;
                    }
                    else
                    {
                        CurrentHP.Shields += tempValue;
                        hitValue += tempValue;
                        tempValue = 0f;
                    }
                }

                // Hero is full health => not being healed.
                if (TotalHP() == HeroData.TotalHP) entity.IsHealing = false;
            }

            double totalHealth = CurrentHP.Health + CurrentHP.Armour + CurrentHP.Shields;
            if (totalHealth <= 0f)
            {
                // Set this entity to "dead".
                entity.OnDead.Invoke();

                if (from && !selfDamage)
                {
                    // Award the final blow to the 'from' entity if they exist (and they are not the entity that died).
                    from.FinalBlow(entity, abilityUsed, headshot, isUltimate);
                }
                else
                {
                    // This entity either killed themselves or died to the void by themself.
                    entity.FinalBlow(entity, null, false, false);
                }
                
            }

            if (!selfDamage && !isUltimate && !autoRegen && from?.gameObject.tag == "Player")
            {
                // Award ultimate charge and perk progress for the 'from' entity if they exist.
                if (from) CalculateUltCharge(hitValue, from);
                if (from) CalculatePerkProgress(hitValue, from);
            }

            if (damage && from) ShowUIHealth(from);

            // Spawn a world-UI number to show the damage / healing done.
            if (entity.hitValuePrefab != null && hitValue != 0) entity.hitValuePrefab.Spawn(entity.hitValueParent.transform.position, (float)hitValue, entity.hitValueParent);
        }

        #endregion
        #region HP UI

        /// <summary>
        /// Updates the UI HP of this hero.
        /// This does not apply to the (real) player.
        /// </summary>
        /// <param name="init">Whether to initialise the UI or not.</param>
        public void UpdateUIHP(bool init = false)
        {
            if (init)
            {
                double healthRemaining = HeroData.HP.Health,
                armourRemaining = HeroData.HP.Armour,
                shieldsRemaining = HeroData.HP.Shields,
                totalHealthRemaining = HeroData.TotalHP;
                Color colour = Color.white;

                // Create each of the bars based on the type of HP and the total amount of HP of the hero.
                for (int i = 0; i < Mathf.Ceil((float)HeroData.TotalHP / HPBAR.Size); i++)
                {
                    if (totalHealthRemaining < 0f) break;

                    GameObject newHPBar = Instantiate(HPBarPrefab);
                    GameObject newHPBarBg = Instantiate(HPBackgroundBarPrefab);

                    newHPBar.transform.SetParent(hpBarsContainer, false);
                    newHPBar.transform.localPosition = Vector3.zero;

                    newHPBarBg.transform.SetParent(hpBarsBackground, false);
                    newHPBarBg.transform.localPosition = Vector3.zero;

                    if (healthRemaining > 0f)
                    {
                        colour = player.Team == entity.Team ? HPBAR.White : HPBAR.EnemyWhite;
                        healthRemaining -= HPBAR.Size;

                        if (totalHealthRemaining - HPBAR.Size < 0f) newHPBar.GetComponent<Image>().fillAmount = (float)totalHealthRemaining / HPBAR.Size;
                    }
                    else if (armourRemaining > 0f)
                    {
                        colour = HPBAR.Armour;
                        armourRemaining -= HPBAR.Size;

                        if (totalHealthRemaining - HPBAR.Size < 0f) newHPBar.GetComponent<Image>().fillAmount = (float)totalHealthRemaining / HPBAR.Size;
                    }
                    else if (shieldsRemaining > 0f)
                    {
                        float size = (float)shieldsRemaining / HPBAR.Size;

                        newHPBar.GetComponent<Image>().fillAmount = 1f - size;
                        newHPBar.transform.GetChild(0).GetComponent<Image>().fillAmount = size;

                        shieldsRemaining -= HPBAR.Size;
                    }

                    totalHealthRemaining -= HPBAR.Size;

                    newHPBar.GetComponent<Image>().color = colour;
                    newHPBar.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = Vector2.zero;

                    colour = player.Team == entity.Team ? HPBAR.White : HPBAR.EnemyWhite;
                    colour.a = HPBAR.Alpha;
                    newHPBarBg.GetComponent<Image>().color = colour;

                    hpBars.Add(newHPBar.GetComponent<Image>());
                }
            }
            else
            {
                double healthRemaining = CurrentHP.Health,
                armourRemaining = CurrentHP.Armour,
                shieldsRemaining = CurrentHP.Shields,
                totalHealth = CurrentHP.Health + CurrentHP.Armour + CurrentHP.Shields;

                if (totalHealth < 0f)
                {
                    // Set each of the bars' fill amount to 0 if the hero is dead.
                    foreach (Image hpBar in hpBars)
                    {
                        hpBar.fillAmount = 0f;
                    }
                    return;
                }

                if (totalHealth <= HeroData.TotalHP)
                {
                    double totalHealthRemaining = totalHealth;

                    foreach (Image hpBar in hpBars)
                    {
                        hpBar.fillAmount = 0f;
                        hpBar.transform.GetChild(0).GetComponent<Image>().fillAmount = 0f;

                        if (healthRemaining > 0f)
                        {
                            float size = (float)healthRemaining / HPBAR.Size;
                            hpBar.fillAmount = size;
                            healthRemaining -= HPBAR.Size;

                            // Since you can have shields while not being full health, dynamically calculate the shields to be in front of health.
                            if (shieldsRemaining > 0f && size < 1f)
                            {
                                if (shieldsRemaining < HPBAR.Size)
                                {
                                    hpBar.transform.GetChild(0).GetComponent<Image>().fillOrigin = (int)Image.OriginHorizontal.Left;
                                    hpBar.transform.GetChild(0).GetComponent<Image>().fillAmount = size + ((float)shieldsRemaining / HPBAR.Size);
                                }
                                else
                                {
                                    hpBar.transform.GetChild(0).GetComponent<Image>().fillOrigin = (int)Image.OriginHorizontal.Right;
                                    hpBar.transform.GetChild(0).GetComponent<Image>().fillAmount = 1f - size;
                                }

                                shieldsRemaining -= (1f - size) * HPBAR.Size;
                            }
                            else
                            {
                                hpBar.transform.GetChild(0).GetComponent<Image>().fillAmount = 0f;
                            }
                        }
                        else if (armourRemaining > 0f)
                        {
                            hpBar.fillAmount = (float)armourRemaining / HPBAR.Size;
                            armourRemaining -= HPBAR.Size;
                        }
                        else if (shieldsRemaining > 0f)
                        {
                            float size = (float)shieldsRemaining / HPBAR.Size;

                            hpBar.transform.GetChild(0).GetComponent<Image>().fillOrigin = (int)Image.OriginHorizontal.Left;
                            hpBar.transform.GetChild(0).GetComponent<Image>().fillAmount = size;

                            shieldsRemaining -= HPBAR.Size;
                        }

                        totalHealthRemaining -= HPBAR.Size;
                    }
                }
            }
        }

        /// <summary>
        /// By default, enemy's HP UI is hidden until you do damage to them or their HP is less than 25% of their total health.
        /// </summary>
        /// <param name="from">The entity that dealt damage to this hero.</param>
        private void ShowUIHealth(Entity from)
        {
            if (from.gameObject.tag == "Player" || TotalHP() / HeroData.TotalHP < 0.25)
            {
                if (ui != null)
                {
                    // Show the HP UI.
                    if (hpUI.alpha == 0f) hpUI.alpha = 1f;
                }
            }
        }

        #endregion
        #region Ultimate & Perk

        /// <summary>
        /// Calculates the ultimate charge for a hero, and adds it to their current charge if valid.
        /// </summary>
        /// <param name="value">The amount of ultimate charge to add.</param>
        /// <param name="target">The entity to add the ultimate charge to.</param>
        public void CalculateUltCharge(double value, Entity target)
        {
            // Make sure that the entity is a hero entity.
            if (target.Hero == null) return;

            // Don't add ultimate charge if the entity has max ultimate charge already.
            if (target.Hero.UltimateSystem.Current == target.Hero.UltimateSystem.Cost) return;

            if (target.Hero.UltimateSystem.Current + value > target.Hero.UltimateSystem.Cost)
            {
                target.Hero.UltimateSystem.Current = target.Hero.UltimateSystem.Cost;
                target.Hero.UltimateSystem.Ready = true;
            }
            else
            {
                target.Hero.UltimateSystem.Current += value;
            }
        }

        /// <summary>
        /// Calculates the perk progress for a hero, and adds it to their current progress if valid.
        /// </summary>
        /// <param name="value">The amount of perk progress to add.</param>
        /// <param name="target">The entity to add the perk progress to.</param>
        public void CalculatePerkProgress(double value, Entity target)
        {
            // Make sure that the entity is a hero entity.
            if (target.Hero == null) return;

            // Don't add perk progress if the entity has reached the max perk level already.
            if (target.Hero.PerkSystem.CurrentLevel == 3) return;

            target.Hero.PerkSystem.Current += value;
            if (target.Hero.PerkSystem.CurrentLevel == 1 && target.Hero.PerkSystem.Current >= target.Hero.PerkSystem.Levels[0])
            {
                // Allows the entity to pick their level 2 perk.
                target.Hero.PerkAvailable = true;
                target.Hero.PerkSystem.CurrentLevel = 2;
                target.Hero.perkSelectNotification.SetActive(true);

                target.Hero.SetLevel2Perks();
            }
            else if (target.Hero.PerkSystem.CurrentLevel == 2 && target.Hero.PerkSystem.Current >= target.Hero.PerkSystem.Levels[1])
            {
                // Allows the entity to pick their level 3 perk.
                target.Hero.PerkAvailable = true;
                target.Hero.PerkSystem.CurrentLevel = 3;
                target.Hero.perkSelectNotification.SetActive(true);

                target.Hero.SetLevel3Perks();
            }
        }
        
        #endregion
        #region Perks

        /// <summary>
        /// Sets up the relevant details on the perk select HUD for this hero's level 2 perks.
        /// </summary>
        private void SetLevel2Perks()
        {
            perk1Name.text = level2Perk1Data.Name;
            perk2Name.text = level2Perk2Data.Name;

            perk1Desc.text = level2Perk1Data.PerkDesc;
            perk2Desc.text = level2Perk2Data.PerkDesc;

            perk1Icon.sprite = level2Perk1Data.PerkImg;
            perk2Icon.sprite = level2Perk2Data.PerkImg;
        }

        /// <summary>
        /// Sets up the relevant details on the perk select HUD for this hero's level 3 perks.
        /// </summary>
        private void SetLevel3Perks()
        {
            perk1Name.text = level3Perk1Data.Name;
            perk2Name.text = level3Perk2Data.Name;

            perk1Desc.text = level3Perk1Data.PerkDesc;
            perk2Desc.text = level3Perk2Data.PerkDesc;

            perk1Icon.sprite = level3Perk1Data.PerkImg;
            perk2Icon.sprite = level3Perk2Data.PerkImg;
        }

        /// <summary>
        /// Called when the (real) player initiates the 'select perk' keybind.
        /// </summary>
        public void SelectPerk()
        {
            bool show = !perkSelect.activeInHierarchy;
            perkSelectNotification.SetActive(!show);

            perkSelect.SetActive(show);
        }

        #endregion
        #region Player Actions

        // Methods to return the specific weapon and ultimate ability of a specific hero. Expected to be overridden by a specific child hero.
        public virtual Weapon GetWeapon() { return null; }
        public virtual Ability GetUltimate() { return null; }

        // Methods for each of the actions that the (real) player can take. Most are expected to be overriden by a specific child hero.
        public virtual void PrimaryFire(bool action) { }
        public virtual void SecondaryFire(bool action) { }
        public virtual IEnumerator Ability1() { yield return null; }
        public virtual IEnumerator Ability2() { yield return null; }
        public virtual IEnumerator UltimateAbility() { yield return null; }
        // public virtual void Interact() { }
        // public virtual void EquipWeapon1() { }
        // public virtual void EquipWeapon2() { }
        public virtual IEnumerator QuickMelee()
        {
            while (true)
            {
                if (quickMeleeActive && quickMelee.CanExecute)
                {
                    quickMelee.CanExecute = false;
                    quickMelee.Execute(animator);
                }

                yield return new WaitForSeconds(Time.deltaTime);
            }
        }

        public virtual void Reload() { }
        // public virtual void NextWeapon() { }
        // public virtual void PreviousWeapon() { }

        /// <summary>
        /// Called when this hero is affected by an area of effect.
        /// </summary>
        public virtual void OnAreaOfEffectHit() { }

        /// <summary>
        /// Manages the state of choosing the first level 2 perk.
        /// </summary>
        public virtual void Level2Perk1()
        {
            perkAvailable = false;
            perkSelect.SetActive(false);

            selectedPerkIcon.sprite = level2Perk1Data.PerkImg;
            selectedLvl2PerkIcon.sprite = level2Perk1Data.PerkImg;
            selectedPerk.SetActive(true);
            selectedLvl2Perk.SetActive(true);

            StartCoroutine(HideSelectedPerk());

            level2Perk1 = true;
        }

        /// <summary>
        /// Manages the state of choosing the second level 2 perk.
        /// </summary>
        public virtual void Level2Perk2()
        {
            perkAvailable = false;
            perkSelect.SetActive(false);

            selectedPerkIcon.sprite = level2Perk2Data.PerkImg;
            selectedLvl2PerkIcon.sprite = level2Perk2Data.PerkImg;
            selectedPerk.SetActive(true);
            selectedLvl2Perk.SetActive(true);

            StartCoroutine(HideSelectedPerk());

            level2Perk2 = true;
        }

        /// <summary>
        /// Manages the state of choosing the first level 3 perk.
        /// </summary>
        public virtual void Level3Perk1()
        {
            perkAvailable = false;
            perkSelect.SetActive(false);

            selectedPerkIcon.sprite = level3Perk1Data.PerkImg;
            selectedLvl3PerkIcon.sprite = level3Perk1Data.PerkImg;
            selectedPerk.SetActive(true);
            selectedLvl3Perk.SetActive(true);

            StartCoroutine(HideSelectedPerk());

            level3Perk1 = true;
        }

        /// <summary>
        /// Manages the state of choosing the second level 3 perk.
        /// </summary>
        public virtual void Level3Perk2()
        {
            perkAvailable = false;
            perkSelect.SetActive(false);

            selectedPerkIcon.sprite = level3Perk2Data.PerkImg;
            selectedLvl3PerkIcon.sprite = level3Perk2Data.PerkImg;
            selectedPerk.SetActive(true);
            selectedLvl3Perk.SetActive(true);

            StartCoroutine(HideSelectedPerk());

            level3Perk2 = true;
        }

        /// <summary>
        /// Hides the 'selected perk' UI after 1.5 seconds.
        /// Also calls the 'CalculatePerkProgress' method in case the hero entity has unlocked their level 3 perks as well.
        /// </summary>
        private IEnumerator HideSelectedPerk()
        {
            yield return new WaitForSeconds(1.5f);
            selectedPerk.SetActive(false);
            CalculatePerkProgress(0, player);
        }
        
        #endregion
    }
}