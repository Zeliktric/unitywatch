using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using NaughtyAttributes;

namespace Unitywatch
{
    /// <summary>
    /// Base class for all damage heroes.
    /// </summary>
    public class DamageHero : Hero
    {
        [Foldout("Scoreboard")]
        public int FinalBlows,
            SoloKills;
    }
}