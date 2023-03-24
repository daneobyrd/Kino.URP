using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using SerializableAttribute = System.SerializableAttribute;

namespace Kino.PostProcessing
{
<<<<<<< HEAD:Runtime/Overlay.cs
    #region Effect settings

    // Base implementation (shared with PreStackOverlay)
    public class OverlayBase : PostProcessEffectSettings
    {
        #region Nested types

        public enum Source { Color, Gradient, Texture }
        public enum BlendMode { Normal, Screen, Overlay, Multiply, SoftLight, HardLight }

        [System.Serializable] public sealed class SourceParameter : ParameterOverride<Source> {}
        [System.Serializable] public sealed class BlendModeParameter : ParameterOverride<BlendMode> {}

        #endregion

        #region Common parameters

        public SourceParameter source = new SourceParameter { value = Source.Gradient };
        public BlendModeParameter blendMode = new BlendModeParameter { value = BlendMode.Overlay };
        [Range(0, 1)] public FloatParameter opacity = new FloatParameter { value = 0 };

        #endregion

        #region Single color mode parameters

        [ColorUsage(false)] public ColorParameter color = new ColorParameter() { value = Color.red };

        #endregion

        #region Gradient mode parameters

        public GradientParameter gradient = new GradientParameter();
        [Range(-180, 180)] public FloatParameter angle = new FloatParameter { value = 0 };

        #endregion

        #region Texture mode parameters

        public TextureParameter texture = new TextureParameter();
        public BoolParameter sourceAlpha = new BoolParameter { value = true };

        #endregion
    }

    // Specialization for the post-stack overlay effect
    [System.Serializable]
    [PostProcess(typeof(OverlayRenderer), PostProcessEvent.AfterStack, "Kino/Overlay")]
    public sealed class Overlay : OverlayBase {}

    #endregion
=======
    [Serializable, VolumeComponentMenu("Post-processing/Kino/Overlay")]
    public sealed class Overlay : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        #region Local enums and wrapper classes

        public enum SourceType { Color, Gradient, Texture }
        public enum BlendMode { Normal, Screen, Overlay, Multiply, SoftLight, HardLight }

        [Serializable] public sealed class SourceTypeParameter : VolumeParameter<SourceType> {}
        [Serializable] public sealed class BlendModeParameter : VolumeParameter<BlendMode> {}

        #endregion
>>>>>>> master:Packages/jp.keijiro.kino.post-processing/Runtime/Overlay.cs

        #region Common parameters

        public SourceTypeParameter sourceType = new SourceTypeParameter { value = SourceType.Gradient };
        public BlendModeParameter blendMode = new BlendModeParameter { value = BlendMode.Overlay };
        public ClampedFloatParameter opacity = new ClampedFloatParameter(0, 0, 1);

        #endregion

        #region Single color mode paremter

        public ColorParameter color = new ColorParameter(Color.red, false, false, true);

        #endregion

        #region Gradient mode parameters

        public GradientParameter gradient = new GradientParameter();
        public ClampedFloatParameter angle = new ClampedFloatParameter(0, -180, 180);

        #endregion

        #region Texture mode parameters

        public TextureParameter texture = new TextureParameter(null);
        public BoolParameter sourceAlpha = new BoolParameter(true);

        #endregion

        #region Private members

<<<<<<< HEAD:Runtime/Overlay.cs
    // Base implementation (shared with PreStackOverlayRenderer)
    public class OverlayRendererBase<T> : PostProcessEffectRenderer<T> where T : OverlayBase
    {
=======
>>>>>>> master:Packages/jp.keijiro.kino.post-processing/Runtime/Overlay.cs
        static class ShaderIDs
        {
            internal static readonly int Color = Shader.PropertyToID("_Color");
            internal static readonly int Direction = Shader.PropertyToID("_Direction");
            internal static readonly int Opacity = Shader.PropertyToID("_Opacity");
<<<<<<< HEAD:Runtime/Overlay.cs
            internal static readonly int SourceTex = Shader.PropertyToID("_SourceTex");
            internal static readonly int UseSourceAlpha = Shader.PropertyToID("_UseSourceAlpha");
=======
            internal static readonly int InputTexture = Shader.PropertyToID("_InputTexture");
            internal static readonly int OverlayTexture = Shader.PropertyToID("_OverlayTexture");
            internal static readonly int UseTextureAlpha = Shader.PropertyToID("_UseTextureAlpha");
>>>>>>> master:Packages/jp.keijiro.kino.post-processing/Runtime/Overlay.cs
        }

        Material _material;
        GradientColorKey[] _gradientCache;

        #endregion

        #region IPostProcessComponent implementation

        public bool IsActive() => _material != null && opacity.value > 0;

        #endregion

        #region CustomPostProcessVolumeComponent implementation

        public override CustomPostProcessInjectionPoint injectionPoint =>
            CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup()
        {
            _material = CoreUtils.CreateEngineMaterial("Hidden/Kino/PostProcess/Overlay");

        #if !UNITY_EDITOR
            // At runtime, copy gradient color keys only once on initialization.
            _gradientCache = gradient.value.colorKeys;
        #endif
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle srcRT, RTHandle destRT)
        {
<<<<<<< HEAD:Runtime/Overlay.cs
            // Skip it when opacity is zero.
            if (settings.opacity == 0) return;

            // Common parameters
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Kino/PostProcessing/Overlay"));
            sheet.properties.SetFloat(ShaderIDs.Opacity, settings.opacity);

            var pass = (int)settings.blendMode.value * 3;

            if (settings.source == Overlay.Source.Color)
            {
                // Single color mode parameters
                sheet.properties.SetColor(ShaderIDs.Color, settings.color);
                sheet.properties.SetTexture(ShaderIDs.SourceTex, RuntimeUtilities.whiteTexture);
                sheet.properties.SetFloat(ShaderIDs.UseSourceAlpha, 0);
            }
            else if (settings.source == Overlay.Source.Gradient)
            {
            #if UNITY_EDITOR
                // In editor, copy gradient color keys every frame.
                _gradientCache = settings.gradient.value.colorKeys;
            #endif

                // Gradient mode parameters
                sheet.properties.SetVector(ShaderIDs.Direction, DirectionVector);
                GradientUtility.SetColorKeys(sheet, _gradientCache);
                pass += _gradientCache.Length > 3 ? 2 : 1;
            }
            else // Overlay.Source.Texture
            {
                // Skip it when no texture is given.
                if (settings.texture.value == null) return;

                // Texture mode parameters
                sheet.properties.SetColor(ShaderIDs.Color, Color.white);
                sheet.properties.SetTexture(ShaderIDs.SourceTex, settings.texture);
                sheet.properties.SetFloat(ShaderIDs.UseSourceAlpha, settings.sourceAlpha ? 1 : 0);
            }

            // Blit with the shader
            var cmd = context.command;
            cmd.BeginSample("Overlay");
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, pass);
            cmd.EndSample("Overlay");
=======
            _material.SetFloat(ShaderIDs.Opacity, opacity.value);

            var pass = (int)blendMode.value * 3;

            if (sourceType == Overlay.SourceType.Color)
            {
                // Single color mode parameters
                _material.SetColor(ShaderIDs.Color, color.value);
                _material.SetTexture(ShaderIDs.OverlayTexture, Texture2D.whiteTexture);
                _material.SetFloat(ShaderIDs.UseTextureAlpha, 0);
            }
            else if (sourceType == Overlay.SourceType.Gradient)
            {
            #if UNITY_EDITOR
                // In editor, copy gradient color keys every frame.
                _gradientCache = gradient.value.colorKeys;
            #endif

                // Gradient direction vector
                var rad = Mathf.Deg2Rad * angle.value;
                var dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));

                // Gradient mode parameters
                _material.SetVector(ShaderIDs.Direction, dir);
                GradientUtility.SetColorKeys(_material, _gradientCache);
                pass += _gradientCache.Length > 3 ? 2 : 1;
            }
            else // Overlay.Source.Texture
            {
                // Skip when no texture is given.
                if (texture.value == null) return;

                // Texture mode parameters
                _material.SetColor(ShaderIDs.Color, Color.white);
                _material.SetTexture(ShaderIDs.OverlayTexture, texture.value);
                _material.SetFloat(ShaderIDs.UseTextureAlpha, sourceAlpha.value ? 1 : 0);
            }

            // Blit to destRT with the overlay shader.
            _material.SetTexture(ShaderIDs.InputTexture, srcRT);
            HDUtils.DrawFullScreen(cmd, _material, destRT, null, pass);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(_material);
>>>>>>> master:Packages/jp.keijiro.kino.post-processing/Runtime/Overlay.cs
        }

<<<<<<< HEAD:Runtime/Overlay.cs
    // Specialization for the post-stack overlay effect
    public sealed class OverlayRenderer : OverlayRendererBase<Overlay> {}

    #endregion
=======
        #endregion
    }
>>>>>>> master:Packages/jp.keijiro.kino.post-processing/Runtime/Overlay.cs
}
