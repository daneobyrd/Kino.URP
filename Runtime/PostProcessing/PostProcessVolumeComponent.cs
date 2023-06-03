using System;

namespace Kino.PostProcessing
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// An injection point for the full screen pass. This is similar to RenderPassEvent enum but limits to only supported events.
    /// </summary>
    [System.Serializable]
    public enum InjectionPoint
    {
        BeforeTransparents = RenderPassEvent.BeforeRenderingTransparents,
        BeforePostProcess = RenderPassEvent.BeforeRenderingPostProcessing,
        AfterPostProcess = RenderPassEvent.AfterRenderingPostProcessing
    }

    public abstract class PostProcessVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        protected PostProcessVolumeComponent()
        {
            string className = GetType().ToString();
            int dotIndex = className.LastIndexOf(".", StringComparison.Ordinal) + 1;
            displayName = className[dotIndex..];
        }

        protected Material m_Material;

        public virtual InjectionPoint InjectionPoint { get; } = InjectionPoint.BeforePostProcess;

        public virtual bool visibleInSceneView { get; } = true;

        public abstract bool IsActive();
        public          bool IsTileCompatible() { return false; }

        protected virtual void Initialize() { }

        public virtual void Initialize(Material material) { m_Material = material; }

        public abstract void Render(ref CommandBuffer cmd, ref CameraData cameraData, RenderTargetIdentifier source, RenderTargetIdentifier dest);

        internal bool isInitialized = false;

        internal void SetupIfNeed()
        {
            if (isInitialized == true)
                return;

            Initialize();
            isInitialized = true;
        }
        
        internal void SetupIfNeed(Material material)
        {
            if (isInitialized == true)
                return;

            Initialize(material);
            isInitialized = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            isInitialized = false;
            Cleanup();
        }

        protected virtual void Cleanup() { }

        public static bool Equals(PostProcessVolumeComponent lhs, PostProcessVolumeComponent rhs) { return lhs.GetType() == rhs.GetType(); }

        public static bool operator <(PostProcessVolumeComponent lhs, PostProcessVolumeComponent rhs) { return lhs.InjectionPoint < rhs.InjectionPoint; }

        public static bool operator >(PostProcessVolumeComponent lhs, PostProcessVolumeComponent rhs) { return lhs.InjectionPoint > rhs.InjectionPoint; }
    }
}