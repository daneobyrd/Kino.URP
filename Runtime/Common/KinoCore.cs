using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.Reflection;

namespace Kino.PostProcessing
{
    public static class KinoCore
    {
        public static string packagePath => "Packages/jp.keijiro.kino.post-processing";
        
        public enum KinoProfileId
        {
            Streak,  // BeforePostProcess
            Overlay, // AfterPostProcess ↓
            Recolor,
            Glitch,
            Sharpen,
            Utility, // Multipurpose: HueShift, Invert, Fade
            Slice,
            TestCard
        }
    }
}