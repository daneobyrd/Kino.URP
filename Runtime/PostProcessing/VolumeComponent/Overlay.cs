using UnityEngine.Rendering.Universal;

namespace Kino.PostProcessing
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using static KinoCore;
    using SerializableAttribute = System.SerializableAttribute;

    #region Local enums and parameters

    public enum SourceType
    {
        Color,
        Gradient,
        Texture
    }

    [Serializable]
    public sealed class SourceTypeParameter : VolumeParameter<SourceType> { }

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
    public sealed class BlendModeParameter : VolumeParameter<BlendMode> { }

    #endregion

    [Serializable]
    [VolumeComponentMenu("Post-processing/Kino/Overlay")]
    public sealed class Overlay : PostProcessVolumeComponent
    {
        public SourceTypeParameter sourceType = new() {value = SourceType.Gradient};
        public BlendModeParameter blendMode = new() {value   = BlendMode.Overlay};
        public ClampedFloatParameter opacity = new(0, 0, 1);

        #region Color source parameter

        public ColorParameter color = new(Color.red, false, false, true);

        #endregion

        #region Gradient source parameters

        public GradientParameter gradient = new();
        public ClampedFloatParameter angle = new(0, -180, 180);

        #endregion

        #region Texture source parameters

        public TextureParameter texture = new(null);
        public BoolParameter sourceAlpha = new(true);

        #endregion

        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        public override bool IsActive() => opacity.value > 0;
        
        GradientColorKey[] _gradientCache;

        public override void Initialize(Material material)
        {
            m_Material = material;
            // m_Material ??= CoreUtils.CreateEngineMaterial(UserPostProcessData.shaders.OverlayPS);
#if !UNITY_EDITOR
            // At runtime, copy gradient color keys only once on initialization.
            _gradientCache = gradient.value.colorKeys;
#endif
        }

        public override void Render(ref CommandBuffer cmd, ref CameraData cameraData, RenderTargetIdentifier source, RenderTargetIdentifier dest)
        {
            m_Material.SetFloat(ShaderIDs.OverlayOpacity, opacity.value);

            var pass = (int) blendMode.value * 3;

            if (sourceType == SourceType.Color)
            {
                // Single color mode parameters
                m_Material.SetColor(ShaderIDs.OverlayColor, color.value);
                m_Material.SetTexture(ShaderIDs.OverlayTexture, Texture2D.whiteTexture);
                m_Material.SetFloat(ShaderIDs.UseTextureAlpha, 0);
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
                m_Material.SetVector(ShaderIDs.GradientDirection, dir);
                GradientUtility.SetColorKeys(m_Material, _gradientCache);
                pass += _gradientCache.Length > 3 ? 2 : 1;
            }
            else // Overlay.Source.Texture
            {
                // Skip when no texture is given.
                if (texture.value == null) return;

                // Texture mode parameters
                m_Material.SetColor(ShaderIDs.OverlayColor, Color.white);
                m_Material.SetTexture(ShaderIDs.OverlayTexture, texture.value);
                m_Material.SetFloat(ShaderIDs.UseTextureAlpha, sourceAlpha.value ? 1 : 0);
            }

            // Blit to dest with the overlay shader.
            cmd.SetPostProcessSourceTexture(source);
            cmd.DrawFullScreenTriangle(m_Material, dest, pass);
        }
    }
}