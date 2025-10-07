using NaughtyAttributes;

namespace Unitywatch
{
    /// <summary>
    /// Base class for all support heroes.
    /// </summary>
    public class SupportHero : Hero
    {
        [Foldout("Scoreboard")]
        public int PlayersSaved;
    }
}