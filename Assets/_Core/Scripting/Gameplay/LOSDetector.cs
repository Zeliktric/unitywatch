using System.Collections.Generic;

using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Manages the LOS (Line of Sight) state of entities for an object.
    /// </summary>
    public class LOSDetector : MonoBehaviour
    {
        private List<Entity> detectedEntities = new List<Entity>();
        public List<Entity> DetectedEntities => detectedEntities;

        /// <summary>
        /// Called when a new collider is detected.
        /// An internal list is kept of actual detected entities as entities can contain more than one collider.
        /// </summary>
        /// <param name="col">The collider that was detected.</param>
        public void OnEntityDetect(Collider col)
        {
            Entity detectedEntity = col.transform.root.GetComponent<Entity>();
            if (detectedEntity.IsDead)
            {
                detectedEntities.Remove(detectedEntity);
                return;
            }

            if (!detectedEntities.Contains(detectedEntity)) detectedEntities.Add(detectedEntity);
        }

        /// <summary>
        /// Called when an existing collider is lost from LOS.
        /// An internal list is kept of actual detected entities as entities can contain more than one collider.
        /// </summary>
        /// <param name="col">The collider that was lost from LOS.</param>
        public void OnEntityLost(Collider col)
        {
            Entity detectedEntity = col.transform.root.GetComponent<Entity>();
            if (detectedEntities.Contains(detectedEntity)) detectedEntities.Remove(detectedEntity);
        }

    }
}