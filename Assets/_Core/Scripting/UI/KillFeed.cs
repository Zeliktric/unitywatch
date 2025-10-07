using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Unitywatch
{
    /// <summary>
    /// Manages the killfeed when there has been a new final blow.
    /// </summary>
    public class KillFeed : MonoBehaviour
    {
        [SerializeField]
        private GameObject friendlyToEnemyPrefab,
            enemyToFriendlyPrefab;

        [SerializeField]
        private Transform killFeedList;

        private GameObject entry;

        /// <summary>
        /// Creates a new entry in the killfeed list.
        /// </summary>
        /// <param name="from">The entity that committed the final blow.</param>
        /// <param name="to">The entity that died.</param>
        /// <param name="isFriendly">Whether the 'from' entity is part of the same team as the (real) player.</param>
        /// <param name="headshot">Whether the final blow came from a headshot.</param>
        /// <param name="ability">The ability that was used to commit the final blow, if applicable.</param>
        /// <param name="ultimate">The ultimate ability that was used to commit the final blow, if applicable.</param>
        public void NewEntry(Entity from, Entity to, bool isFriendly, bool headshot, AbilityData ability = null, AbilityData ultimate = null)
        {
            Entity friendly,
                enemy;

            if (isFriendly)
            {
                entry = Instantiate(friendlyToEnemyPrefab);
                friendly = from;
                enemy = to;
            }
            else
            {
                entry = Instantiate(enemyToFriendlyPrefab);
                friendly = to;
                enemy = from;
            }

            entry.transform.Find("Friendly/Player Name").GetComponent<TMP_Text>().text = friendly.EntityName;
            entry.transform.Find("Friendly/Icon/Entity Icon").GetComponent<Image>().sprite = friendly.Hero.HeroData.Hero2DIcon;

            if (headshot) entry.transform.Find("Action/Headshot").gameObject.SetActive(true);
            if (ability)
            {
                entry.transform.Find("Action/Ability").gameObject.SetActive(true);
                entry.transform.Find("Action/Ability/Icon").GetComponent<Image>().sprite = ability.AbilityIcon;
            }
            if (ultimate)
            {
                entry.transform.Find("Action/Ultimate").gameObject.SetActive(true);
                entry.transform.Find("Action/Ultimate/Background/Icon").GetComponent<Image>().sprite = ultimate.AbilityIcon;
            }

            entry.transform.Find("Enemy/Player Name").GetComponent<TMP_Text>().text = enemy.EntityName;
            entry.transform.Find("Enemy/Icon/Entity Icon").GetComponent<Image>().sprite = enemy.Hero.HeroData.Hero2DIcon;

            entry.transform.SetParent(killFeedList);

            StartCoroutine(RebuildNextFrame());
            entry.transform.localScale = Vector3.one;

        }

        private IEnumerator RebuildNextFrame()
        {
            yield return null;

            LayoutRebuilder.ForceRebuildLayoutImmediate(killFeedList.GetComponent<RectTransform>());
        }
    }
}