using UnityEngine;
using UnityEngine.Serialization;
using static Kino.PostProcessing.KinoCore;

namespace Kino.PostProcessing
{
    using UnityEngine;
    using static KinoCore;
    using System;
    using System.Collections.Generic;

    [CreateAssetMenu(menuName = "Rendering/Custom Post Processing/PostProcess Order Config")]
    public class PostProcessOrderConfig : ScriptableObject
    {
        public List<string> beforeTransparents = new List<string>();
        public List<string> beforePostProcess = new List<string>();
        public List<string> afterPostProcess = new List<string>();

        public List<string> GetVolumeList(InjectionPoint point)
        {
            switch (point)
            {
                case InjectionPoint.BeforeTransparents:
                    return beforeTransparents;
                case InjectionPoint.BeforePostProcess:
                    return beforePostProcess;
                case InjectionPoint.AfterPostProcess:
                    return afterPostProcess;
            }

            return null;
        }

#if UNITY_EDITOR
        public Action OnDataChange;

        private void OnValidate() { OnDataChange?.Invoke(); }
#endif
    }
}

#if UNITY_EDITOR
namespace Kino.PostProcessing
{
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine.Rendering;
    using static UnityEditor.GenericMenu;
    using static UnityEditorInternal.ReorderableList;

    [CustomEditor(typeof(PostProcessOrderConfig))]
    [CanEditMultipleObjects]
    public class PostProcessOrderConfigEditor : Editor
    {
        private ReorderableList beforePost;
        private ReorderableList afterPost;
        private ReorderableList beforeTransparents;

        private PostProcessOrderConfig instance;

        public override void OnInspectorGUI()
        {
            //base.DrawDefaultInspector();
            serializedObject.Update();
            {
                beforeTransparents.DoLayoutList();
                EditorGUILayout.Space();
                beforePost.DoLayoutList();
                EditorGUILayout.Space();
                afterPost.DoLayoutList();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            instance = (PostProcessOrderConfig) target;

            var beforeTransparentsProperty = serializedObject.FindProperty("beforeTransparents");
            beforeTransparents                       = Util_ReorderableList.CreateAutoLayout(beforeTransparentsProperty, "Before Transparents");
            beforeTransparents.drawElementCallback   = DrawElement(beforeTransparents);
            beforeTransparents.onAddDropdownCallback = DrawDropdownMenu(InjectionPoint.BeforeTransparents);

            var beforePostProperty = serializedObject.FindProperty("beforePostProcess");
            beforePost                       = Util_ReorderableList.CreateAutoLayout(beforePostProperty, "Before Post-Process");
            beforePost.drawElementCallback   = DrawElement(beforePost);
            beforePost.onAddDropdownCallback = DrawDropdownMenu(InjectionPoint.BeforePostProcess);

            var afterPostProperty = serializedObject.FindProperty("afterPostProcess");
            afterPost                       = Util_ReorderableList.CreateAutoLayout(afterPostProperty, "After Post-Process");
            afterPost.drawElementCallback   = DrawElement(afterPost);
            afterPost.onAddDropdownCallback = DrawDropdownMenu(InjectionPoint.AfterPostProcess);
        }

        private AddDropdownCallbackDelegate DrawDropdownMenu(InjectionPoint injectionPoint)
        {
            return (buttonRect, list) =>
            {
                var menu = new GenericMenu();

                foreach (var item in VolumeManager.instance.baseComponentTypeArray)
                {
                    var comp = VolumeManager.instance.stack.GetComponent(item) as PostProcessVolumeComponent;

                    if (comp == null)
                        continue;

                    if (comp.InjectionPoint != injectionPoint)
                        continue;

                    menu.AddItem(new GUIContent(comp.GetType().ToString()), false, tryAddVolumeComp(comp, injectionPoint));
                }

                menu.ShowAsContext();
            };

            MenuFunction tryAddVolumeComp(object userData, InjectionPoint customInjectionPoint)
            {
                return () =>
                {
                    var data = userData as PostProcessVolumeComponent;
                    var typeName = data.GetType().ToString();
                    var list = instance.GetVolumeList(customInjectionPoint);
                    if (list.Contains(typeName) == false)
                    {
                        list.Add(typeName);
                        instance.OnDataChange?.Invoke();
                        EditorUtility.SetDirty(instance);
                    }
                };
            }
        }

        private ElementCallbackDelegate DrawElement(ReorderableList list)
        {
            return (rect, index, isActive, isFocused) =>
            {
                var prop = list.serializedProperty;
                var item = prop.GetArrayElementAtIndex(index);
                rect.height = EditorGUI.GetPropertyHeight(item);
                EditorGUI.LabelField(rect, item.stringValue);
            };
        }
    }
}
#endif