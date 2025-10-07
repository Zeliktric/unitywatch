using System.Collections;

using UnityEngine;
using UnityEngine.UI;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of a killfeed entry, namely deleting it after a few seconds.
    /// </summary>
    public class KillFeedEntry : MonoBehaviour
    {
        [SerializeField]
        private float timeUntilDeletion = 8f;

        private Transform killFeedList;

        private void Start()
        {
            killFeedList = transform.parent;
            StartCoroutine(Delete());
        }

        /// <summary>
        /// Method to delete the gameObject after a few seconds.
        /// </summary>
        private IEnumerator Delete()
        {
            yield return new WaitForSeconds(timeUntilDeletion);

            gameObject.SetActive(false);

            yield return null;

            LayoutRebuilder.ForceRebuildLayoutImmediate(killFeedList.GetComponent<RectTransform>());

            Destroy(gameObject);
        }
    }
}