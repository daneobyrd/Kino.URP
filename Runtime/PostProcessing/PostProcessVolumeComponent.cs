using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SerializableAttribute = System.SerializableAttribute;

namespace Kino.PostProcessing
{
    /// <summary>
    /// An injection point for the full screen pass. This is similar to RenderPassEvent enum but limits to only supported events.
    /// </summary>
    public enum InjectionPoint
    {
        AfterSkybox = RenderPassEvent.AfterRenderingSkybox,
        BeforeTransparents = RenderPassEvent.BeforeRenderingTransparents,
        BeforePostProcess = RenderPassEvent.BeforeRenderingPostProcessing,
        AfterPostProcess = RenderPassEvent.AfterRenderingPostProcessing,
        AfterRendering = RenderPassEvent.AfterRendering
    }

    public abstract class PostProcessVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        private bool isInitialized = false;
        internal string typeName;

        protected PostProcessVolumeComponent()
        {
            string className = GetType().ToString();
            int dotIndex = className.LastIndexOf(".", System.StringComparison.Ordinal) + 1;
            displayName = className[dotIndex..];
        }

        #region IPostProcessComponent

        public abstract bool IsActive();
        public          bool IsTileCompatible() { return false; }

        #endregion

        public Material material;

        public virtual InjectionPoint InjectionPoint { get; } = InjectionPoint.AfterPostProcess;

        public virtual bool visibleInSceneView { get; } = true;

        /// <summary>
        /// Setup function, called once before render is called.
        /// </summary>
        public virtual void Setup() { }

        // Set m_Material directly
        public virtual void Setup(Material initMaterial) { this.material = initMaterial; }

        // Set m_Material using resources or reference to shaders stored in ScriptableObject.
        // Example: Unity's PostProcessData
        public virtual void Setup(ScriptableObject scriptableObject) { }
        
        internal void SetupIfNeeded()
        {
            if (isInitialized)
                return;

            Setup();
            isInitialized = true;
            typeName      = GetType().Name;
        }

        internal void SetupIfNeeded(Material initMaterial)
        {
            if (isInitialized)
                return;

            Setup(initMaterial);
            isInitialized = true;
            typeName      = GetType().Name;
        }

        internal void SetupIfNeeded(ScriptableObject scriptableObject)
        {
            if (isInitialized)
                return;

            Setup(scriptableObject);
            isInitialized = true;
            typeName      = GetType().Name;
        }

        public abstract void Render(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination);
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            isInitialized = false;
            Cleanup();
        }

        public virtual void Cleanup()
        {
            if (material != null)
            {
                CoreUtils.Destroy(material);
            }
        }
    }
}