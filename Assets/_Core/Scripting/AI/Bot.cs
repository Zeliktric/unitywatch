
using System.Collections;

using UnityEngine;

using NaughtyAttributes;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of computer-controlled entities.
    /// </summary>
    public class Bot : Entity
    {
        [SerializeField, Foldout("Bot Details")]
        private ParticleSystem deathFx;

        [SerializeField, Foldout("Bot Details")]
        private Barrier barrier;

        [SerializeField, Foldout("Bot Details")]
        private bool canShoot;

        [SerializeField]
        private CanvasGroup hpUI;

        protected override void Start()
        {
            base.Start();

            if (canShoot) hero.PrimaryFire(true);
        }

        /// <summary>
        /// Called when the bot's health is 0 or less.
        /// </summary>
        protected override IEnumerator Respawn()
        {
            yield return null;

            deathFx.Play();

            // Hides the bot mesh and UI.
            transform.GetChild(0).gameObject.SetActive(false);

            yield return new WaitForSeconds(respawnTime);

            // Reset the bot's health and position.
            hero.CurrentHP.Health = hero.HeroData.HP.Health;
            hero.CurrentHP.Armour = hero.HeroData.HP.Armour;
            hero.CurrentHP.Shields = hero.HeroData.HP.Shields;

            transform.position = spawnPosition;
            hpUI.alpha = 0f;

            transform.GetChild(0).gameObject.SetActive(true);
            isDead = false;

            if (barrier) barrier.Respawn();
        }
    }
}