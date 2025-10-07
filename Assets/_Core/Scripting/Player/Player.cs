using System.Collections;

using UnityEngine;

using NaughtyAttributes;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of (real) player-controlled entities.
    /// </summary>
    public class Player : Entity
    {
        [SerializeField, Foldout("Debug")]
        private bool lockPrimaryFire;
        public bool LockPrimaryFire
        {
            get { return lockPrimaryFire; }
            set { lockPrimaryFire = value; }
        }

        [SerializeField, Foldout("Debug")]
        private bool lockSecondaryFire;
        public bool LockSecondaryFire
        {
            get { return lockSecondaryFire; }
            set { lockSecondaryFire = value; }
        }

        [SerializeField, Foldout("HUD")]
        private Transform finalBlowSkull;

        [SerializeField, Foldout("HUD")]
        private AnimationCurve hitMarkerCurve,
            finalBlowCurve;

        [SerializeField, Foldout("HUD")]
        private GameObject deathScreen;

        [SerializeField]
        protected Transform mesh;

        protected Weapon weapon;

        protected override void Start()
        {
            base.Start();

            weapon = hero.GetWeapon();
        }

        protected override void Update()
        {
            base.Update();

            // Used to set the scale and alpha of the final blow icon.
            if (skullTime >= 0f && skullTime < 0.75f)
            {
                finalBlowSkull.GetComponent<CanvasGroup>().alpha = hitMarkerCurve.Evaluate(skullTime);

                float scale = finalBlowCurve.Evaluate(skullTime);
                finalBlowSkull.localScale = new Vector3(scale, scale, scale);
                skullTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// Called when this (real) player interacts with a trigger collider (a collider that you can pass through without physically colliding with).
        /// This functin is used primarily for interacting with consumables.
        /// </summary>
        /// <param name="col"></param>
        protected void OnTriggerEnter(Collider col)
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Pick Up"))
            {
                Consumable consumable = col.transform.parent.GetComponent<Consumable>();

                switch (col.transform.root.name)
                {
                    case "Health Pack":
                        if (hero.TotalHP() != hero.HeroData.TotalHP)
                        {
                            // Only apply the health pack if the (real) player's health is not full.
                            hero.UpdateHP(consumable.value, false, null, false);
                            consumable.Interact();
                        }
                        break;
                    case "Ultimate Accelerator":
                        if (!hero.UltimateSystem.Ready && !hero.UltimateSystem.Active)
                        {
                            // Only apply the ultimate charge if the (real) player's ultimate is not already full nor the ultimate is active.
                            hero.CalculateUltCharge(hero.UltimateSystem.Cost, this);
                            consumable.Interact();

                            if (hero.PerkSystem.CurrentLevel != 3)
                            {
                                // Also award perk progres if the player is not at the max level (3).
                                hero.CalculatePerkProgress(hero.PerkSystem.Levels[1], this);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Called when the (real) player is awarded a final blow on another entity.
        /// Source of damage that inflicted the final blow is done purely to show its icon in the killfeed.
        /// </summary>
        /// <param name="to">The entity that received the final blow.</param>
        /// <param name="abilityData">The data of the ability used to inflict the final blow, if applicable.</param>
        /// <param name="headshot">Whether the final blow was inflicted by a headshot.</param>
        /// <param name="isUltimate">Whether an ultimate ability was used to inflict the final blow.</param>
        public override void FinalBlow(Entity to, AbilityData abilityData, bool headshot, bool isUltimate)
        {
            base.FinalBlow(to, abilityData, headshot, isUltimate);

            skullTime = 0f;
        }

        /// <summary>
        /// Called when the (real) player's health is 0 or less.
        /// </summary>
        protected override IEnumerator Respawn()
        {
            deathScreen.SetActive(true);
            mesh.gameObject.SetActive(false);

            yield return new WaitForSeconds(respawnTime);

            hero.CurrentHP.Health = hero.HeroData.HP.Health;
            hero.CurrentHP.Armour = hero.HeroData.HP.Armour;
            hero.CurrentHP.Shields = hero.HeroData.HP.Shields;

            transform.position = spawnPosition;
            deathScreen.SetActive(false);
            mesh.gameObject.SetActive(true);

            isDead = false;
        }
    }
}