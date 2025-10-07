using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Manages the state of a hitmarker.
    /// </summary>
    public class HitMarker : MonoBehaviour
    {
        [SerializeField]
        private AnimationCurve hitCurve;

        private float alpha,
            currentTime;

        private CanvasGroup hitMarker;
        private AudioSource hitSound;

        private void Start()
        {
            hitMarker = GetComponent<CanvasGroup>();
            hitSound = GetComponent<AudioSource>();
            currentTime = -1f;
        }

        private void Update()
        {
            if (currentTime >= 0f && currentTime < 0.75f)
            {
                alpha = hitCurve.Evaluate(currentTime);
                hitMarker.alpha = alpha;
                currentTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// Method to start the hit marker animation.
        /// </summary>
        public void Execute()
        {
            hitSound.Play();
            currentTime = 0f;
        }
    }
}