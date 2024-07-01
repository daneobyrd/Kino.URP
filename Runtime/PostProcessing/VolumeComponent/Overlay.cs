using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CustomPostProcessing.UniversalRP;

namespace Kino.PostProcessing
{
    using SerializableAttribute = System.SerializableAttribute;

    #region Local enums and parameters

    public enum SourceType
    {
        Color,
        Gradient,
        Texture
    }

    public enum BlendMode
    {
        Normal,
        Screen,
        Overlay,
        Multiply,
        SoftLight,
        HardLight
    }

    [Serializable]
    public class SourceTypeParameter : VolumeParameter<SourceType> { }

    [Serializable]
    public class BlendModeParameter : VolumeParameter<BlendMode> { }

    #endregion

    [Serializable, VolumeComponentMenu("Post-processing/Kino/Overlay")]
    public sealed class Overlay : CustomPostProcessVolumeComponent
    {
        #region Common parameters

        public SourceTypeParameter sourceType = new() {value = SourceType.Gradient};
        public BlendModeParameter blendMode = new() {value   = BlendMode.Overlay};
        public ClampedFloatParameter opacity = new(0, 0, 1);

        #endregion

        #region Single color mode parameter

        public ColorParameter color = new(Color.red, false, false, true);

        #endregion

        #region Gradient mode parameters

        public GradientParameter gradient = new();
        public ClampedFloatParameter angle = new(0, -180, 180);

        #endregion

        #region Texture mode parameters

        public TextureParameter texture = new(null);
        public BoolParameter sourceAlpha = new(true);

        #endregion

        #region Private members

        private static class ShaderIDs
        {
            // Overlay
            internal static readonly int OverlayColor = Shader.PropertyToID("_OverlayColor");
            internal static readonly int GradientDirection = Shader.PropertyToID("_GradientDirection");
            internal static readonly int OverlayOpacity = Shader.PropertyToID("_OverlayOpacity");
            internal static readonly int OverlayTexture = Shader.PropertyToID("_OverlayTexture");

            internal static readonly int UseTextureAlpha = Shader.PropertyToID("_UseTextureAlpha");
        }

        GradientColorKey[] _gradientCache;

        #endregion

        public override bool IsActive() => material != null && opacity.value > 0;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        protected override void Setup(ScriptableObject scriptableObject)
        {
            Assert.IsNotNull(scriptableObject);
            var data = (KinoPostProcessData) scriptableObject;
            Initialize(data.shaders.OverlayPS);
#if !UNITY_EDITOR
            // At runtime, copy gradient color keys only once on initialization.
            _gradientCache = gradient.value.colorKeys;
#endif
        }
        
        public override void Render(CommandBuffer cmd, CameraData unused, RTHandle source, RTHandle destination)
        {
            if (material == null)
                return;
            
            material.SetFloat(ShaderIDs.OverlayOpacity, opacity.value);

            var pass = (int) blendMode.value * 3;

            if (sourceType == SourceType.Color)
            {
                // Single color mode parameters
                material.SetColor(ShaderIDs.OverlayColor, color.value.linear);
                material.SetTexture(ShaderIDs.OverlayTexture, Texture2D.whiteTexture);
                material.SetFloat(ShaderIDs.UseTextureAlpha, 0);
            }
            else if (sourceType == SourceType.Gradient)
            {
#if UNITY_EDITOR
                // In editor, copy gradient color keys every frame.
                _gradientCache = gradient.value.colorKeys;
#endif

                // Gradient direction vector
                var rad = Mathf.Deg2Rad * angle.value;
                var dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));

                // Gradient mode parameters
                material.SetVector(ShaderIDs.GradientDirection, dir);
                GradientUtility.SetColorKeys(material, _gradientCache);
                pass += _gradientCache.Length > 3 ? 2 : 1;
            }
            else // Overlay.Source.Texture
            {
                // Skip when no texture is given.
                if (texture.value == null) return;

                // Texture mode parameters
                material.SetColor(ShaderIDs.OverlayColor, Color.white);
                material.SetTexture(ShaderIDs.OverlayTexture, texture.value);
                material.SetFloat(ShaderIDs.UseTextureAlpha, sourceAlpha.value ? 1 : 0);
            }

            // Blit to dest with the overlay shader.
            cmd.SetPostProcessInputTexture(source);
            material.DrawFullScreen(cmd, source, destination, pass);
        }
    }
}