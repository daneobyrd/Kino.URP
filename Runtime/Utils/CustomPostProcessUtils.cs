using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kino.PostProcessing
{
    public static class CustomPostProcessUtils
    {
        #region GetVolumeCollections

        public static VolumeStack GetCameraVolumeStack(in CameraData cameraData) { return cameraData.camera.GetUniversalAdditionalCameraData().volumeStack; }

        public static void GetVolumeComponentLists(in VolumeStack stack,
                                                   out List<PostProcessVolumeComponent> effectsBeforeTransparents,
                                                   out List<PostProcessVolumeComponent> effectsBeforePostProcess,
                                                   out List<PostProcessVolumeComponent> effectsAfterPostProcess)
        {
            var customVolumeTypes = CoreUtils.GetAllTypesDerivedFrom<PostProcessVolumeComponent>().ToList();
            effectsBeforeTransparents = new List<PostProcessVolumeComponent>();
            effectsBeforePostProcess  = new List<PostProcessVolumeComponent>();
            effectsAfterPostProcess   = new List<PostProcessVolumeComponent>();

            foreach (var volumeType in customVolumeTypes.Where(type => !type.IsAbstract))
            {
                var component = stack.GetComponent(volumeType) as PostProcessVolumeComponent;
                if (!component) continue;

                switch (component.InjectionPoint)
                {
                    case InjectionPoint.BeforeTransparents:
                        effectsBeforeTransparents.Add(component);
                        break;
                    case InjectionPoint.BeforePostProcess:
                        effectsBeforePostProcess.Add(component);
                        break;
                    case InjectionPoint.AfterPostProcess:
                        effectsAfterPostProcess.Add(component);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static void GetCustomPostProcessCollections(in VolumeStack stack,
                                                           out List<Type> customVolumeTypes,
                                                           out List<PostProcessVolumeComponent> allPostProcessVolumeComponents,
                                                           out Dictionary<Type, InjectionPoint> volumeTypeInjectionPointDictionary)
        {
            customVolumeTypes                  = CoreUtils.GetAllTypesDerivedFrom<PostProcessVolumeComponent>().ToList();
            allPostProcessVolumeComponents     = new List<PostProcessVolumeComponent>();
            volumeTypeInjectionPointDictionary = new Dictionary<Type, InjectionPoint>();

            foreach (Type volumeType in customVolumeTypes.Where(t => !t.IsAbstract))
            {
                var component = stack.GetComponent(volumeType) as PostProcessVolumeComponent;
                if (!component) continue;

                // Populate List with PostProcessVolumeComponent instances from VolumeStack
                allPostProcessVolumeComponents.Add(component);

                // Populates Dictionary with derived types and corresponding injectionPoint.
                volumeTypeInjectionPointDictionary.TryAdd(volumeType, component.InjectionPoint);
            }

            customVolumeTypes.RemoveAll(t => t == null);
        }

        public static void GetCustomPostProcessCollections(in VolumeStack stack,
                                                           out List<PostProcessVolumeComponent> allPostProcessVolumeComponents,
                                                           out Dictionary<Type, InjectionPoint> volumeTypeInjectionPointDictionary)
        {
            GetCustomPostProcessCollections(stack, out var customVolumeTypes, out allPostProcessVolumeComponents, out volumeTypeInjectionPointDictionary);
        }


        public static List<PostProcessVolumeComponent> GetComponentListForInjectionPoint(InjectionPoint requestedInjectionPoint,
                                                                                         in List<PostProcessVolumeComponent> allPostProcessVolumeComponents,
                                                                                         in Dictionary<Type, InjectionPoint> volumeTypeInjectionPointDictionary)
        {
            var filteredComponentList = new List<PostProcessVolumeComponent>();

            foreach (var (type, injectionPoint) in volumeTypeInjectionPointDictionary)
            {
                // Filter out PostProcessVolumeComponents for other injectionPoints
                if (injectionPoint != requestedInjectionPoint)
                {
                    continue;
                }

                var component = allPostProcessVolumeComponents.Find(c => c.GetType() == type);
                if (component is null)
                {
                    // Debug.Log("Could not find component.");
                    continue;
                }

                filteredComponentList.Add(component);
            }

            return filteredComponentList;
        }

        public static void GetPostProcessVolumeComponents(in List<PostProcessVolumeComponent> allPostProcessVolumeComponents,
                                                          in Dictionary<Type, InjectionPoint> volumeTypeInjectionPointDictionary,
                                                          out List<PostProcessVolumeComponent> effectsBeforeTransparents,
                                                          out List<PostProcessVolumeComponent> effectsBeforePostProcess,
                                                          out List<PostProcessVolumeComponent> effectsAfterPostProcess)
        {
            effectsBeforeTransparents = new List<PostProcessVolumeComponent>();
            effectsBeforePostProcess  = new List<PostProcessVolumeComponent>();
            effectsAfterPostProcess   = new List<PostProcessVolumeComponent>();

            // Populates Lists for each InjectionPoint found in type Dictionary
            foreach (var (type, injectionPoint) in volumeTypeInjectionPointDictionary)
            {
                var component = allPostProcessVolumeComponents.Find(c => c.GetType() == type);
                if (component is null)
                {
                    // Debug.Log("Could not find component.");
                    continue; // continue to next KeyValuePair
                }

                switch (injectionPoint)
                {
                    case InjectionPoint.BeforeTransparents:
                        effectsBeforeTransparents.Add(component);
                        break;
                    case InjectionPoint.BeforePostProcess:
                        effectsBeforePostProcess.Add(component);
                        break;
                    case InjectionPoint.AfterPostProcess:
                        effectsAfterPostProcess.Add(component);
                        break;
                    default:
                        throw new ArgumentNullException(nameof(volumeTypeInjectionPointDictionary));
                }
                // continue to next KeyValuePair
            }
        }

        public static bool ListIsActive(in List<PostProcessVolumeComponent> listToCheck) { return listToCheck.Any(x => x.IsActive()); }


        private static bool TryGetKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value, out List<TKey> keys)
        {
            keys = dictionary.AsParallel()
                             .Where(pair => EqualityComparer<TValue>.Default.Equals(pair.Value, value)) // if has matching `value`
                             .Select(pair => pair.Key).ToList();                                        // select keys and output ToList(); 

            return false;
        }

        public static List<string> GetTypeNameListFromInjectionPoint(InjectionPoint requestedInjectionPoint, in Dictionary<Type, InjectionPoint> volumeTypeInjectionPointDictionary)
        {
            var filteredList = new List<string>();
            if (!volumeTypeInjectionPointDictionary.TryGetKeys(requestedInjectionPoint, out var keys)) return filteredList;
            filteredList.AddRange(keys.Select(t => t.AssemblyQualifiedName));

            return filteredList;
        }

        private static void GetTypeNameList<T>(in List<T> inputTypeList, out List<string> outputNameList) where T : Type
        {
            outputNameList = inputTypeList.Where(t => !t.IsAbstract).Select(type => type.Name).ToList();
        }

        public static void GetTypeNameList(in List<Type> inputTypeList, out List<string> outputNameList) { GetTypeNameList<Type>(inputTypeList, out outputNameList); }

        private static void GetTypeNameList<T>(this List<string> stringNameList, in List<T> inputTypeList) where T : Type
        {
            // Sanitize the list
            stringNameList.RemoveAll(s => Type.GetType(s) == null);

            GetTypeNameList(inputTypeList, out stringNameList);
        }

        #endregion

        private static readonly int PostBufferID = Shader.PropertyToID("_InputTexture");
        private static readonly int scaleBiasID = Shader.PropertyToID("_ScaleBias");
        
        public static void SetPostProcessInputTexture(this CommandBuffer cmd, RenderTargetIdentifier identifier)
        {
            cmd.SetGlobalTexture(PostBufferID, identifier);
        }

        public static void SetPostProcessRenderTarget(this ScriptableRenderer renderer,
                                                      CommandBuffer cmd,
                                                      RenderTargetIdentifier colorAttachment,
                                                      RenderBufferLoadAction colorLoadAction,
                                                      RenderBufferStoreAction colorStoreAction,
                                                      RenderTargetIdentifier depthAttachment,
                                                      RenderBufferLoadAction depthLoadAction,
                                                      RenderBufferStoreAction depthStoreAction,
                                                      ClearFlag clearFlags,
                                                      Color clearColor)
        {
            // XRTODO: Revisit the logic. Why treat CameraTarget depth specially?
            if (depthAttachment == BuiltinRenderTextureType.CameraTarget)
                CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction,
                                          colorAttachment, depthLoadAction, depthStoreAction, clearFlags, clearColor);
            else
                CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction,
                                          depthAttachment, depthLoadAction, depthStoreAction, clearFlags, clearColor);
        }

        #region Fullscreen Mesh

        static Mesh s_TriangleMesh;
        static Mesh s_QuadMesh;
        
        // Should match Common.hlsl
        static Vector3[] GetFullScreenTriangleVertexPosition(float z /*= UNITY_NEAR_CLIP_VALUE*/)
        {
            var r = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                Vector2 uv = new Vector2((i << 1) & 2, i & 2);
                r[i] = new Vector3(uv.x * 2.0f - 1.0f, uv.y * 2.0f - 1.0f, z);
            }
            return r;
        }

        // Should match Common.hlsl
        static Vector2[] GetFullScreenTriangleTexCoord()
        {
            var r = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                if (SystemInfo.graphicsUVStartsAtTop)
                    r[i] = new Vector2((i << 1) & 2, 1.0f - (i & 2));
                else
                    r[i] = new Vector2((i << 1) & 2, i & 2);
            }
            return r;
        }

        // Should match Common.hlsl
        static Vector3[] GetQuadVertexPosition(float z /*= UNITY_NEAR_CLIP_VALUE*/)
        {
            var r = new Vector3[4];
            for (uint i = 0; i < 4; i++)
            {
                uint topBit = i >> 1;
                uint botBit = (i & 1);
                float x = topBit;
                float y = 1 - (topBit + botBit) & 1; // produces 1 for indices 0,3 and 0 for 1,2
                r[i] = new Vector3(x, y, z);
            }
            return r;
        }

        // Should match Common.hlsl
        static Vector2[] GetQuadTexCoord()
        {
            var r = new Vector2[4];
            for (uint i = 0; i < 4; i++)
            {
                uint topBit = i >> 1;
                uint botBit = (i & 1);
                float u = topBit;
                float v = (topBit + botBit) & 1; // produces 0 for indices 0,3 and 1 for 1,2
                if (SystemInfo.graphicsUVStartsAtTop)
                    v = 1.0f - v;

                r[i] = new Vector2(u, v);
            }
            return r;
        }

        #endregion
        
        public static void DrawFullScreenTriangle(this CommandBuffer cmd, Material material, RenderTargetIdentifier destination, int shaderPass = 0)
        {
            // CoreUtils.SetRenderTarget(cmd, destination);
            cmd.SetRenderTarget
            (
                new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, -1),
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            // cmd.DrawProcedural(Matrix4x4.identity, material, shaderPass, MeshTopology.Triangles, 3, 1, null);
            
            if (SystemInfo.graphicsShaderLevel < 30)
                cmd.DrawMesh(s_TriangleMesh, Matrix4x4.identity, material, 0, shaderPass, null);
            else
                cmd.DrawProcedural(Matrix4x4.identity, material, shaderPass, MeshTopology.Triangles, 3, 1, null);
        }

        // Based on PostProcessPass.Blit() which is used for Bloom pyramid prefilter pass.
        // Assumes Shader Texture Property has already been set using Material.SetTexture() or cmd.SetGlobalTexture().
        public static void PostProcessBlit(CommandBuffer cmd, RenderTargetIdentifier destination, Material material, int passIndex = 0)
        {
            if (material.shader == Shader.Find("Hidden/Universal Render Pipeline/Blit"))
            {
                Vector4 scaleBias = new Vector4(1, 1, 0, 0);
                cmd.SetGlobalVector(scaleBiasID, scaleBias);
            }

            cmd.SetRenderTarget
            (
                new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, -1),
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            cmd.DrawProcedural(Matrix4x4.identity, material, passIndex, MeshTopology.Quads, 4, 1, null);
        }

        public static BuiltinRenderTextureType BlitDstDiscardContent(CommandBuffer cmd, RenderTargetIdentifier rt)
        {
            // We set depth to DontCare because rt might be the source of PostProcessing used as a temporary target
            // Source typically comes with a depth buffer and right now we don't have a way to only bind the color attachment of a RenderTargetIdentifier
            cmd.SetRenderTarget
            (
                new RenderTargetIdentifier(rt, 0, CubemapFace.Unknown, -1),
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare
            );
            return BuiltinRenderTextureType.CurrentActive;
        }

        public static void SetKeyword(this Material mat, string keyWord, bool active)
        {
            if (active)
                mat.EnableKeyword(keyWord);
            else
                mat.DisableKeyword(keyWord);
        }
    }
}