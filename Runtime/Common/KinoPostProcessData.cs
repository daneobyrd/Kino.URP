﻿#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kino.PostProcessing
{
    using static KinoCore;
    [Serializable]
    public class KinoPostProcessData : ScriptableObject
    {
        private static string defaultFileName = "KinoPostProcessData.asset";
        
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreatePostProcessDataAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<KinoPostProcessData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, packagePath);
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/Custom Post Processing/KinoPostProcessData", priority = CoreUtils.Sections.section5 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void CreatePostProcessData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreatePostProcessDataAsset>(), defaultFileName, null, null);
        }

        public static KinoPostProcessData GetDefaultCustomPostProcessData()
        {
            var path = System.IO.Path.Combine(packagePath, $"Runtime/Data/{defaultFileName}/");
            return AssetDatabase.LoadAssetAtPath<KinoPostProcessData>(path);
        }
#endif

        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Resources/Overrides/Streak.shader", ReloadAttribute.Package.Root)]
            public Shader StreakPS;

            [Reload("Resources/Overrides/Overlay.shader", ReloadAttribute.Package.Root)]
            public Shader OverlayPS;

            [Reload("Resources/Overrides/Recolor.shader", ReloadAttribute.Package.Root)]
            public Shader RecolorPS;

            [Reload("Resources/Overrides/Glitch.shader", ReloadAttribute.Package.Root)]
            public Shader GlitchPS;

            [Reload("Resources/Overrides/Sharpen.shader", ReloadAttribute.Package.Root)]
            public Shader SharpenPS;

            [Reload("Resources/Overrides/Utility.shader", ReloadAttribute.Package.Root)]
            public Shader UtilityPS;

            [Reload("Resources/Overrides/Slice.shader", ReloadAttribute.Package.Root)]
            public Shader SlicePS;

            [Reload("Resources/Overrides/TestCard.shader", ReloadAttribute.Package.Root)]
            public Shader TestCardPS;
        }

        public ShaderResources shaders;
    }
}