namespace Unitywatch
{
    /// <summary>
    /// Manages the state of Soldier: 76's sprint.
    /// </summary>
    public class Sprint : Ability
    {
        private bool isSprinting;
        public bool IsSprinting
        {
            get { return isSprinting; }
            set { isSprinting = value; }
        }

        private Soldier76 hero;

        protected override void Start()
        {
            base.Start();

            if (hero == null) hero = (Soldier76)entity.Hero;
        }

        private void FixedUpdate()
        {
            if (isSprinting && entity.playerController.MoveInput.y <= 0f)
            {
                hero.CancelSprint();
                return;
            }
        }

        /// <summary>
        /// Starts the sprint action, if valid.
        /// </summary>
        public override void Execute()
        {
            if (entity.playerController.MoveInput.y <= 0f) return;

            if (isSprinting)
            {
                hero.CancelSprint();
                return;
            }

            // Allow the hero to move faster with the first major perk.
            entity.Hero.MovementModification += hero.level3Perk1 ? abilityData.MovementSpeedBuff * 1.2f : abilityData.MovementSpeedBuff;
            isSprinting = true;

            SetActive();

            hero.animator.Play("Walk to Sprint");
        }

        /// <summary>
        /// Stops the sprint and resets the hero's movement speed.
        /// </summary>
        public void StopSprint()
        {
            isSprinting = false;
            entity.Hero.MovementModification -= hero.level3Perk1 ? abilityData.MovementSpeedBuff * 1.2f : abilityData.MovementSpeedBuff;

            SetInactive();

            hero.animator.Play("Sprint to Walk");
        }
    }
}