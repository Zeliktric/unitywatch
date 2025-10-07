using UnityEngine;

namespace Unitywatch
{
    /// <summary>
    /// Class for useful constants within the gameplay.
    /// </summary>
    public class Constants
    {
        // Normal TR: 64hz; Healing TR: 16hz; Damage over Time TR: 20hz; Burn DoT: 2hz.
        public static float HEALING_TICK_RATE = 4f,
            DAMAGE_TICK_RATE = 3.2f,
            BURN_TICK_RATE = 32f;

        public static float DAMAGE_PASSIVE = 0.3f,
            DAMAGE_PASSIVE_TANKS = 0.15f,
            BEAM_ON_ARMOUR = 0.3f,
            DOT_ON_ARMOUR = 0f;

        public static float AUTO_REGEN_CONSTANT = 10f,
            AUTO_REGEN_PERCENTAGE = 0.05f,
            SHIELD_REGEN_CONSTANT = 30f;

        public static float CalculateFalloffValue(float distance, HealthDelta healthDelta, Vector2 falloff)
        {
            float falloffStart = falloff.x;
            float falloffEnd = falloff.y;

            float maxValue = healthDelta.ValueRange.x;
            float minValue = healthDelta.ValueRange.y;

            if (distance <= falloffStart) return maxValue;
            if (distance >= falloffEnd) return minValue;

            // Linearly interpolate between maxValue and minValue
            float t = (distance - falloffStart) / (falloffEnd - falloffStart);
            return Mathf.Lerp(maxValue, minValue, t);
        }
    }
}