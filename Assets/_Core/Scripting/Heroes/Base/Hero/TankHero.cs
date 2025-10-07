using NaughtyAttributes;

namespace Unitywatch
{
    /// <summary>
    /// Base class for all tank heroes.
    /// </summary>
    public class TankHero : Hero
    {
        [Foldout("Scoreboard")]
        public float ObjectiveContestTime;
    }
}