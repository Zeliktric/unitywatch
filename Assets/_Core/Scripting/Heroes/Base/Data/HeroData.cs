using System;
using System.Collections.Generic;

using UnityEngine;

using NaughtyAttributes;

namespace Unitywatch
{
    /// <summary>
    /// Class as a structure for hit points.
    /// </summary>
    [Serializable]
    public class HitPoints
    {
        [SerializeField]
        private double health,
            armour,
            shields,
            selfDamage;

        public double Health { get { return health; } set { health = value; } }
        public double Armour { get { return armour; } set { armour = value; } }
        public double Shields { get { return shields; } set { shields = value; } }
        public double SelfDamage { get { return selfDamage; } set { selfDamage = value; } }
    }

    /// <summary>
    /// Class as a structure for the ultimate system.
    /// </summary>
    [Serializable]
    public class UltimateSystem
    {
        [SerializeField]
        private double cost,
            current;
        [SerializeField]
        private bool ready,
            active;

        public double Cost { get { return cost; } set { cost = value; } }
        public double Current { get { return current; } set { current = value; } }
        public bool Ready { get { return ready; } set { ready = value; } }
        public bool Active { get { return active; } set { active = value; } }
    }

    /// <summary>
    /// Class as a structure for the perk system.
    /// </summary>
    [Serializable]
    public class PerkSystem
    {
        [SerializeField]
        private int currentLevel = 1;
        public int CurrentLevel { get { return currentLevel; } set { currentLevel = value; } }

        [SerializeField]
        private double[] levels = new double[2];
        public double[] Levels => levels;

        [SerializeField]
        private double current;
        public double Current { get { return current; } set { current = value; } }
    }

    /// <summary>
    /// Scriptable object for storing information on heroes.
    /// </summary>
    [CreateAssetMenu(fileName = "Hero Data", menuName = "Unitywatch/Hero Data")]
    public class HeroData : ScriptableObject
    {
        [SerializeField]
        private string heroName;
        public string Name => heroName;

        [SerializeField]
        private Sprite heroIcon;
        public Sprite HeroIcon => heroIcon;

        [SerializeField]
        private Sprite hero2DIcon;
        public Sprite Hero2DIcon => hero2DIcon;

        [SerializeField]
        private float baseMoveSpeed = 5.5f;
        public float BaseMoveSpeed => baseMoveSpeed;

        [SerializeField]
        private float baseCrouchSpeed = 3f;
        public float BaseCrouchSpeed => baseCrouchSpeed;

        [SerializeField]
        private float backwardsMoveReduction = 0.9f;
        public float BackwardsMoveReduction => backwardsMoveReduction;

        [SerializeField]
        private float crouchHeight = 0.7f;
        public float CrouchHeight => crouchHeight;

        [SerializeField]
        private float jumpHeight = 0.98f;
        public float JumpHeight => jumpHeight;

        [SerializeField, Dropdown("roles")]
        private string role;
        public string Role => role;

        private List<string> roles = new List<string>
        {
            "Tank", "Damage", "Support"
        };

        [SerializeField, OnValueChanged("CalculateTotalHP")]
        private HitPoints hp;
        public HitPoints HP => hp;
 
        [SerializeField, ReadOnly]
        private double totalHP;
        public double TotalHP => totalHP;

        [SerializeField]
        private UltimateSystem ultimateSystem;
        public UltimateSystem UltimateSystem => ultimateSystem;

        [SerializeField]
        private PerkSystem perkSystem;
        public PerkSystem PerkSystem => perkSystem;

        private void CalculateTotalHP()
        {
            totalHP = HP.Health + HP.Armour + HP.Shields;
        }
    }
}