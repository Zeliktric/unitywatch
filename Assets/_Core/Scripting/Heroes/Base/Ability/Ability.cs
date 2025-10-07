using UnityEngine;
using UnityEngine.UI;

using NaughtyAttributes;
using TMPro;

namespace Unitywatch
{
    /// <summary>
    /// Base class for abilities (including ultimate abilities).
    /// </summary>
    public class Ability : MonoBehaviour
    {
        [SerializeField]
        protected AbilityData abilityData;
        public AbilityData AbilityData { get { return abilityData; } }

        [SerializeField, ShowIf("IsUltimate")]
        private Image ultimateOuterCircle;

        private bool IsUltimate => abilityData.IsUltimate;

        protected bool canExecute = true;
        public bool CanExecute
        {
            get { return canExecute; }
            set { canExecute = value; }
        }

        protected bool onCooldown;
        public bool OnCooldown => onCooldown;

        protected bool disabled;
        public bool Disabled => disabled;

        protected double currentCooldown;

        protected GameObject abilityParent,
            abilityObject,
            abilityDisabled,
            abilityKeybind,
            abilityBar;
        protected Image cooldownImage;
        protected TMP_Text cooldownText;

        protected Entity entity;

        protected virtual void Start()
        {
            entity = transform.root.GetComponent<Entity>();

            if (!abilityData.IsUltimate)
            {
                // If the ability is not an ultimate, then get the required gameobjects from the abilities list on the HUD.
                abilityParent = GameObject.Find($"Canvas/HUD/Bottom/Abilities & Gun/Abilities/{abilityData.Name}");

                // Ability Box/Ability
                abilityObject = abilityParent.transform.GetChild(0).GetChild(0).gameObject;

                // Ability Box/Ability/Disabled
                abilityDisabled = abilityParent.transform.GetChild(0).GetChild(0).Find("Disabled").gameObject;

                // Ability Box/Bottom Bar
                abilityBar = abilityParent.transform.GetChild(0).GetChild(1).gameObject;
            }

            if (abilityData.Cooldown != 0f)
            {
                // If the ability has a cooldown, then get the required cooldown-related components from the ability on the HUD.
                abilityKeybind = abilityParent.transform.GetChild(1).gameObject;

                // Icon
                cooldownImage = abilityObject.transform.GetChild(0).GetComponent<Image>();

                // Cooldown Text
                cooldownText = abilityObject.transform.GetChild(2).GetComponent<TMP_Text>();
            }
        }

        protected virtual void Update()
        {
            if (currentCooldown > 0)
            {
                if (!abilityData.IsUltimate)
                {
                    // If the ability is not an ultimate, then update the cooldown image and text.
                    cooldownImage.fillAmount = 1f - (float)currentCooldown / abilityData.Cooldown;
                    cooldownText.text = $"{Mathf.Ceil((float)currentCooldown)}";
                }

                currentCooldown -= Time.deltaTime;

                if (currentCooldown <= 0) OnEnd();
            }
        }
        
        /// <summary>
        /// Method to execute the ability.
        /// Expected to be overridden by child classes.
        /// </summary>
        public virtual void Execute() { }

        /// <summary>
        /// Sets and starts the cooldown of the non-ultimate ability.
        /// </summary>
        /// <param name="cooldown">The length of the cooldown.</param>
        protected void SetCooldown(float cooldown)
        {
            if (!abilityData.IsUltimate)
            {
                onCooldown = true;
                abilityBar.SetActive(false);

                DisableAbilityKeybind(true);

                abilityObject.GetComponent<Image>().color = new Color32(100, 100, 100, 175);
            }

            currentCooldown = cooldown;
        }

        /// <summary>
        /// Called when the cooldown for the non-ultimate ability has expired.
        /// </summary>
        protected virtual void OnEnd()
        {
            if (abilityData.IsUltimate) return;

            // Reset the ability's look in the HUD.
            onCooldown = false;
            abilityBar.SetActive(true);
            DisableAbilityKeybind(false);

            abilityObject.GetComponent<Image>().color = new Color32(255, 255, 255, 175);

            cooldownImage.fillAmount = 0f;
            cooldownText.text = "";

            canExecute = true;
        }

        /// <summary>
        /// Sets the look of the ability in the HUD to or from "disabled".
        /// </summary>
        /// <param name="disable">Whether to disable the ability or not.</param>
        public void DisableAbility(bool disable)
        {
            disabled = disable;
            abilityDisabled.SetActive(disable);

            if (abilityData.IsUltimate)
            {
                ultimateOuterCircle.color = new Color32(255, disable ? (byte)0 : (byte)255, disable ? (byte)0 : (byte)255, 100);
            }
            else
            {
                abilityBar.transform.GetChild(0).GetComponent<Image>().color = disable ? Color.red : Color.white;
                DisableAbilityKeybind(disable);
            }
        }

        /// <summary>
        /// Sets the look of the ability's keybind to or from "disabled".
        /// </summary>
        /// <param name="disable">Whether the ability is being disabled or not.</param>
        private void DisableAbilityKeybind(bool disable)
        {
            Color32 colour = abilityKeybind.GetComponent<Image>().color;
            colour.a = disable ? (byte)100 : (byte)255;
            abilityKeybind.GetComponent<Image>().color = colour;
        }

        /// <summary>
        /// Sets the look of the ability to "active" (orange).
        /// </summary>
        protected void SetActive()
        {
            abilityObject.GetComponent<Image>().color = new Color32(240, 100, 20, 175);
            abilityBar.SetActive(false);
        }

        /// <summary>
        /// Resets the look of the ability from "active" (white).
        /// </summary>
        protected void SetInactive()
        {
            abilityObject.GetComponent<Image>().color = new Color32(255, 255, 255, 175);
            abilityBar.SetActive(true);
        }
    }
}