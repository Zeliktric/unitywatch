using System.Collections;

using UnityEngine;
using UnityEngine.Events;

using TMPro;
using NaughtyAttributes;
using RaycastPro.Bullets;
using DamageNumbersPro;

namespace Unitywatch
{
    /// <summary>
    /// Base class to manage the state of any entity.
    /// </summary>
    public class Entity : MonoBehaviour
    {
        #region Init Attributes

        protected Hero hero;
        public Hero Hero;

        [SerializeField, Foldout("Entity Details")]
        protected int team;
        public int Team => team;

        [SerializeField, Foldout("Entity Details")]
        protected string entityName,
            entityID;
        public string EntityName => entityName;
        public string EntityID => entityID;

        [SerializeField, Foldout("Entity Details")]
        private TMP_Text entityNameText;
        [Foldout("Entity Details")]
        public DamageNumber hitValuePrefab;
        [Foldout("Entity Details")]
        public Transform hitValueParent;

        [SerializeField, Foldout("Entity Details")]
        protected float respawnTime = 5f;
        public float RespawnTime => respawnTime;

        [SerializeField, Foldout("HUD")]
        private HitMarker headshotHitMarker,
            bodyHitMarker;        

        [SerializeField, Foldout("Debug"), ReadOnly]
        protected bool isDead;
        public bool IsDead => isDead;

        [SerializeField, Foldout("Debug"), ReadOnly]
        protected bool isHealing;
        public bool IsHealing
        {
            get { return isHealing; }
            set { isHealing = value; }
        }

        [SerializeField, Foldout("Ping")]
        private Transform ping;

        [SerializeField, Foldout("Ping")]
        private AnimationCurve pingScaleCurve,
            pingHeightCurve;
        
        [SerializeField, Foldout("Ping")]
        private double pingCooldown;
        public double PingCooldown
        {
            get { return pingCooldown; }
            set { pingCooldown = value; }
        }

        [SerializeField, Foldout("Ping")]
        private bool isPinged;
        public bool IsPinged
        {
            get { return isPinged; }
            set { isPinged = value; }
        }

        [SerializeField, Foldout("Ping")]
        private TMP_Text pingDistance;

        [HideInInspector]
        public PlayerController playerController;

        [HideInInspector]
        public UnityEvent OnDead;

        protected bool grounded = true;
        public bool Grounded => grounded;

        // Time for the final blow skull icon for its animation (to match to its animation curve).
        protected float skullTime = -1f;
        public float SkullTime
        {
            get { return skullTime; }
            set { skullTime = value; }
        }

        protected Rigidbody rb;
        protected Vector3 spawnPosition;
        protected Entity player;

        private float lastLinearVelocityY;

        #endregion
        #region Unity Functions

        protected virtual void Start()
        {
            rb = GetComponent<Rigidbody>();
            playerController = GetComponent<PlayerController>();

            hero = transform.GetChild(0).GetComponent<Hero>();
            player = GameObject.FindWithTag("Player").GetComponent<Entity>();

            if (!playerController) entityNameText.text = entityName;

            spawnPosition = transform.position;

            OnDead.AddListener(OnDeath);
        }

        protected virtual void Update()
        {
            if (isDead) return;

            CheckPing();
        }

        #endregion
        #region Other

        /// <summary>
        /// Continuously updates the ping icon & distance if this entity was pinged by the (real) player.
        /// </summary>
        public void CheckPing()
        {
            if (!isPinged) return;

            // Sets the scale and height based on the distance away from the (real) player using a pre-defined linear animation curve.
            float distance = Vector3.Distance(player.transform.position, transform.position);
            float scale = pingScaleCurve.Evaluate(distance);
            float height = pingHeightCurve.Evaluate(distance);

            Vector3 pos = ping.localPosition;
            pos.y = height;
            ping.localPosition = pos;
            ping.localScale = new Vector3(scale, scale, scale);
            pingDistance.text = $"{Mathf.Floor(distance)}m";

            if (pingCooldown > 0)
            {
                pingCooldown -= Time.deltaTime;
            }
            else
            {
                ping.gameObject.SetActive(false);
                isPinged = false;
            }
        }

        /// <summary>
        /// Called when the entity's health is 0 or less.
        /// </summary>
        protected virtual void OnDeath()
        {
            isDead = true;

            StartCoroutine(Respawn());
        }

        /// <summary>
        /// Method to respawn an entity.
        /// Expected to be overridden by child classes.
        /// </summary>
        protected virtual IEnumerator Respawn() { yield return null; }

        /// <summary>
        /// Checks whether the entity is grounded or not.
        /// </summary>
        protected virtual void Movement()
        {
            if (Mathf.Abs(rb.linearVelocity.y) < 0.1f && lastLinearVelocityY < 0.1f)
            {
                grounded = true;
            }
            else
            {
                grounded = false;
            }

            lastLinearVelocityY = rb.linearVelocity.y;
        }

        #endregion
        #region Hit Functions

        /// <summary>
        /// Called when this entity is awarded a final blow on another entity.
        /// The team of this entity is compared to the (real) player's team to see if they are friendly or not.
        /// Source of damage that inflicted the final blow is done purely to show its icon in the killfeed.
        /// </summary>
        /// <param name="to">The entity that received the final blow.</param>
        /// <param name="abilityData">The data of the ability used to inflict the final blow, if applicable.</param>
        /// <param name="headshot">Whether the final blow was inflicted by a headshot.</param>
        /// <param name="isUltimate">Whether an ultimate ability was used to inflict the final blow.</param>
        public virtual void FinalBlow(Entity to, AbilityData abilityData, bool headshot, bool isUltimate)
        {
            if (isUltimate && abilityData != null)
            {
                // Final blow by ultimate ability.
                GameObject.FindWithTag("KillFeed").GetComponent<KillFeed>().NewEntry(this, to, team == player.Team, headshot, ultimate: abilityData);
            }
            else if (!isUltimate && abilityData != null)
            {
                // Final blow by regular ability.
                GameObject.FindWithTag("KillFeed").GetComponent<KillFeed>().NewEntry(this, to, team == player.Team, headshot, ability: abilityData);
            }
            else
            {
                // Final blow by weapon.
                GameObject.FindWithTag("KillFeed").GetComponent<KillFeed>().NewEntry(this, to, team == player.Team, headshot);
            }

        }

        /// <summary>
        /// Called when this entity receives a hit from a bullet.
        /// </summary>
        /// <param name="bullet">The bullet that collided with this entity.</param>
        public void OnHit(Bullet bullet)
        {
            if (isDead) return;

            // The entity that executed the shot.
            Entity hitFrom = bullet.ownerReference.transform.root.GetComponent<Entity>();

            double hitValue = 0;
            float headshotMultiplier = 0f;
            AbilityData abilityData = null;

            bool isHeadshot = bullet.bulletHit.tag == "Head";

            // Check whether the bullet came from a weapon or an ability weapon.
            if (bullet.ownerReference.TryGetComponent<Weapon>(out Weapon hitWeapon))
            {
                hitValue = hitWeapon.OnHit(GetComponent<Rigidbody>().position, team != hitFrom.Team, isHeadshot);
                headshotMultiplier = hitWeapon.WeaponData.HeadshotMultiplier;

                // Check whether the bullet actually hit a barrier instead.
                if (bullet.bulletHit.gameObject.layer == LayerMask.NameToLayer("Barrier"))
                {
                    bullet.bulletHit.transform.parent.GetComponent<Barrier>().OnHit(hitValue);
                    return;
                }

                if (hitFrom.hero.UltimateSystem.Active) abilityData = hitFrom.hero.GetUltimate().AbilityData;                
            }
            else if (bullet.ownerReference.TryGetComponent<AbilityWeapon>(out AbilityWeapon hitAbilityWeapon))
            {
                hitValue = hitAbilityWeapon.OnHit(GetComponent<Rigidbody>().position, team != hitFrom.Team, isHeadshot);
                headshotMultiplier = hitAbilityWeapon.AbilityData.HeadshotMultiplier;

                // Check whether the bullet actually hit a barrier instead.
                if (bullet.bulletHit.gameObject.layer == LayerMask.NameToLayer("Barrier"))
                {
                    bullet.bulletHit.transform.parent.GetComponent<Barrier>().OnHit(hitValue);
                    return;
                }

                abilityData = hitAbilityWeapon.AbilityData;
            }

            // Set the hitmarker UI for the (real) player if the shot came from the (real) player.
            if (hitFrom.gameObject.tag == "Player")
            {
                // If the weapon's headshot multiplier <= 1f, then it is automatically not a headshot regardless of where the bullet hit.
                hitFrom.SetHitMarker(isHeadshot && headshotMultiplier > 1f);
            }

            // Update this entity's HP.
            hero.UpdateHP(hitValue, team != hitFrom.Team, hitFrom, hitFrom.hero.UltimateSystem.Active, abilityUsed: abilityData, headshot: isHeadshot && headshotMultiplier > 1f);
        }

        /// <summary>
        /// Method to execute the hitmarker code based on whether the hit was a headshot or not.
        /// </summary>
        /// <param name="headshot">Whether the hit was a headshot or not.</param>
        public void SetHitMarker(bool headshot)
        {
            if (headshot)
            {
                headshotHitMarker.Execute();
            }
            else
            {
                bodyHitMarker.Execute();
            }
        }

        #endregion
    }
}