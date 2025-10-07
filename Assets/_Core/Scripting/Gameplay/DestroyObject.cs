using System.Collections;

using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Destroys an object when its particle system has finished.
    /// </summary>
    public class DestroyObject : MonoBehaviour
    {
        private ParticleSystem ps;

        public void Start()
        {
            ps = GetComponent<ParticleSystem>();
            StartCoroutine(DeleteEffect());
        }

        /// <summary>
        /// Waits until the particle system has stopped, before destroying the gameobject.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DeleteEffect()
        {
            while (!ps.isStopped)
            {
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}