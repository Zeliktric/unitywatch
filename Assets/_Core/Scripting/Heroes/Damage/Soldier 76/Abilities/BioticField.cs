using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of Soldier: 76's biotic field.
    /// </summary>
    public class BioticField : Ability
    {
        [SerializeField]
        private GameObject modelPrefab;

        [SerializeField]
        private Transform spawnPoint;

        [SerializeField]
        private List<AudioClip> healingVoicelines = new List<AudioClip>();

        [SerializeField]
        private AudioSource healingVoiceline;

        /// <summary>
        /// Initiates the biotic field action.
        /// </summary>
        public override void Execute()
        {
            healingVoiceline.clip = healingVoicelines[Random.Range(0, healingVoicelines.Count - 1)];
            healingVoiceline.Play();

            StartCoroutine(StartAction());
        }

        /// <summary>
        /// Casts and starts the biotic field action.
        /// </summary>
        private IEnumerator StartAction()
        {
            GameObject bioticFieldObject = Instantiate(modelPrefab, spawnPoint.position, Quaternion.identity);

            entity.playerController.AbilityLock = true;

            SetActive();
            yield return new WaitForSeconds(AbilityData.CastTime);
            SetInactive();

            entity.playerController.AbilityLock = false;

            SetCooldown(AbilityData.Cooldown);

            Rigidbody rb = bioticFieldObject.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            bioticFieldObject.transform.position = new Vector3(bioticFieldObject.transform.position.x, 0f, bioticFieldObject.transform.position.z);

            // Create the area of effect field
            GameObject areaOfEffectObject = Instantiate(entity.Hero.areaOfEffectPrefab, bioticFieldObject.transform.position, Quaternion.identity);
            AreaOfEffect areaOfEffect = areaOfEffectObject.GetComponent<AreaOfEffect>();

            areaOfEffect.Owner = transform.root.GetComponent<Player>();
            areaOfEffect.AbilityData = AbilityData;
            areaOfEffect.DeleteObject = bioticFieldObject;
            areaOfEffect.Grounded = true;

            areaOfEffect.StartEffect();
        }
    }
}