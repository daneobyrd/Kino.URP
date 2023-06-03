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
            allPostProcessVolumeComponents          = new List<PostProcessVolumeComponent>();
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

        private static readonly int PostBufferID = Shader.PropertyToID("_PostSourceTexture");

        public static void SetPostProcessSourceTexture(this CommandBuffer cmd, RenderTargetIdentifier identifier)
        {
            cmd.SetGlobalTexture(PostBufferID, identifier);
        }

        public static void DrawFullScreenTriangle(this CommandBuffer cmd, Material material, RenderTargetIdentifier destination, int shaderPass = 0)
        {
            CoreUtils.SetRenderTarget(cmd, destination);
            cmd.DrawProcedural(Matrix4x4.identity, material, shaderPass, MeshTopology.Triangles, 3, 1, null);
        }

        public static void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int passIndex = 0)
        {
            cmd.SetPostProcessSourceTexture(source);
            cmd.Blit(source, destination, material, passIndex);
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