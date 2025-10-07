using UnityEngine;
using UnityEngine.InputSystem;

using NaughtyAttributes;

namespace Unitywatch
{
    /// <summary>
    /// Scriptable object for storing information on abilities.
    /// </summary>
    [CreateAssetMenu(fileName = "Ability Data", menuName = "Unitywatch/Ability Data")]
    public class AbilityData : ScriptableObject
    {
        [SerializeField]
        private string abilityName;
        public string Name => abilityName;

        [SerializeField]
        private Sprite abilityIcon;
        public Sprite AbilityIcon => abilityIcon;

        [SerializeField, OnValueChanged("GetKeybinds")]
        private InputActionAsset inputActions;

        [SerializeField, Dropdown("GetKeybinds")]
        private string keybind;
        public string Keybind => keybind;

        /// <summary>
        /// Retrieves the bindings of the input actions map.
        /// </summary>
        /// <returns>The string list of keybinds</returns>
        private DropdownList<string> GetKeybinds()
        {
            if (inputActions == null) return new DropdownList<string>();

            DropdownList<string> keybinds = new DropdownList<string>();
            InputActionMap map = inputActions.FindActionMap("Player");

            foreach (InputAction action in map.actions)
            {
                string keybindName = action.bindings[0].name;
                if (!action.bindings[0].isComposite)
                {
                    keybindName = InputControlPath.ToHumanReadableString(
                        action.bindings[0].path,
                        InputControlPath.HumanReadableStringOptions.OmitDevice
                    );
                }

                keybinds.Add($"{action.name} ({keybindName.Replace("/", " ")})", keybindName);
            }

            return keybinds;
        }

        [SerializeField]
        private bool startDisabled;
        public bool StartDisabled => startDisabled;

        [SerializeField]
        private bool isUltimate;
        public bool IsUltimate => isUltimate;

        [SerializeField]
        private float cooldown;
        public float Cooldown => cooldown;

        [SerializeField]
        private int charges = 1;
        public int Charges => charges;

        [SerializeField]
        private float health;
        public float Health => health;

        [SerializeField]
        private float armour;
        public float Armour => armour;

        [SerializeField]
        private float shields;
        public float Shields => shields;

        [SerializeField]
        private float overhealth;
        public float Overhealth => overhealth;

        [SerializeField]
        private float barrierHealth;
        public float BarrierHealth => barrierHealth;

        [SerializeField]
        private HealthDelta healing;
        public HealthDelta Healing => healing;

        [SerializeField]
        private HealthDelta damage;
        public HealthDelta Damage => damage;

        [SerializeField]
        private HealthDelta areaOfEffect;
        public HealthDelta AreaOfEffect => areaOfEffect;

        [SerializeField]
        private bool losRequired;
        public bool LOSRequired => losRequired;

        [SerializeField, ShowIf("losRequired")]
        private bool blockedByBarrier;
        public bool BlockedByBarrier => blockedByBarrier;

        [SerializeField]
        private bool affectSelf;
        public bool AffectSelf => affectSelf;

        [SerializeField]
        private bool friendlyFire;
        public bool FriendlyFire => friendlyFire;

        [SerializeField]
        private Vector2 falloffRange;
        public Vector2 FalloffRange => falloffRange;

        [SerializeField]
        private float headshotMultiplier = 1f;
        public float HeadshotMultiplier => headshotMultiplier;

        [SerializeField]
        private float damageAmplification;
        public float DamageAmplification => damageAmplification;

        [SerializeField]
        private float damageReduction;
        public float DamageReduction => damageReduction;

        [SerializeField]
        private float healingModification;
        public float HealingModification => healingModification;

        [SerializeField]
        private float movementSpeed;
        public float MovementSpeed => movementSpeed;

        [SerializeField]
        private float movementSpeedBuff;
        public float MovementSpeedBuff => movementSpeedBuff;

        [SerializeField]
        private float movementSpeedPenalty;
        public float MovementSpeedPenalty => movementSpeedPenalty;

        [SerializeField]
        private float movementSlow;
        public float MovementSlow => movementSlow;

        [SerializeField]
        private Vector2 knockbackSpeed;
        public Vector2 KnockbackSpeed => knockbackSpeed;

        [SerializeField]
        private float projectileSpeed;
        public float ProjectileSpeed => projectileSpeed;

        [SerializeField]
        private float projectileRadius;
        public float ProjectileRadius => projectileRadius;

        [SerializeField]
        private float range;
        public float Range => range;

        [SerializeField]
        private Vector2 areaOfEffectRadius;
        public Vector2 AreaOfEffectRadius => areaOfEffectRadius;

        [SerializeField]
        private float castTime;
        public float CastTime => castTime;

        [SerializeField]
        private bool cancelCast;
        public bool CancelCast => cancelCast;

        [SerializeField]
        private float duration;
        public float Duration => duration;

        [SerializeField]
        private float recoveryDuration;
        public float RecoveryDuration => recoveryDuration;

        [SerializeField]
        private float minDetails;
        public float MinDetails => minDetails;

        [SerializeField]
        private float maxDetails;
        public float MaxDetails => maxDetails;
    }
}