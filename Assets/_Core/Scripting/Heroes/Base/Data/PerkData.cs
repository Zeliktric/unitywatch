using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Scriptable object for storing information on perks.
    /// </summary>
    [CreateAssetMenu(fileName = "Perk Data", menuName = "Unitywatch/Perk Data")]
    public class PerkData : ScriptableObject
    {
        [SerializeField]
        private string perkName;
        public string Name => perkName;

        [SerializeField]
        private string perkDesc;
        public string PerkDesc => perkDesc;

        [SerializeField]
        private Sprite perkImg;
        public Sprite PerkImg => perkImg;
    }
}