using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hai.Av3MenuToHierarchy.Scripts.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Hai.Av3MenuToHierarchy.Scripts.Components.HaiAv3MenuToHierarchyControl;

namespace Hai.Av3MenuToHierarchy.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(HaiAv3MenuToHierarchyControl))]
    [CanEditMultipleObjects]
    public class HaiAv3MenuToHierarchyControlEditor : UnityEditor.Editor
    {
        private const int MaxSubMenuControlCount = 8;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            BuildMenuOf(serializedObject);

            DisplaySubMenuControlsIfApplicable();
            if (serializedObject.isEditingMultipleObjects)
            {
                foreach (var m2h in serializedObject.targetObjects.Select(o => (HaiAv3MenuToHierarchyControl) o).Reverse().ToList())
                {
                    var serializedSubControl = new SerializedObject(m2h);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    BuildMenuOf(serializedSubControl, true);
                    EditorGUILayout.EndVertical();
                    serializedSubControl.ApplyModifiedProperties();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DisplaySubMenuControlsIfApplicable()
        {
            var type = (Av3M2HType) serializedObject.FindProperty("type").intValue;
            var subMenuSource = (Av3M2HSubMenuSource) serializedObject.FindProperty("subMenuSource").intValue;

            if (serializedObject.isEditingMultipleObjects || type != Av3M2HType.SubMenu || (subMenuSource != Av3M2HSubMenuSource.HierarchyChildren && subMenuSource != Av3M2HSubMenuSource.FlattenedSubMenu)) return;

            EditorGUILayout.Separator();
            int controlRank = 1;
            foreach (Transform child in ((HaiAv3MenuToHierarchyControl) serializedObject.targetObject).transform)
            {
                var m2h = child.GetComponent<HaiAv3MenuToHierarchyControl>();
                if (m2h == null) continue;

                var serializedSubControl = new SerializedObject(m2h);
                if (m2h.gameObject.activeSelf)
                {
                    if (controlRank == MaxSubMenuControlCount + 1)
                    {
                        EditorGUILayout.HelpBox("SubMenus cannot have more than 8 controls.\n\nControls below will not be included:", MessageType.Error);
                    }

                    controlRank++;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                BuildMenuOf(serializedSubControl, true);
                EditorGUILayout.EndVertical();
                serializedSubControl.ApplyModifiedProperties();
            }

            EditorGUI.BeginDisabledGroup(controlRank > MaxSubMenuControlCount);
            if (GUILayout.Button("Add Control"))
            {
                var parent = ((HaiAv3MenuToHierarchyControl) serializedObject.targetObject).transform;
                var go = NewGameObject("Control #" + controlRank, parent);
                Undo.RegisterCreatedObjectUndo(go, "x");
                var m2h = Undo.AddComponent<HaiAv3MenuToHierarchyControl>(go);
                Undo.RecordObject(go, "x");
                m2h.type = Av3M2HType.Toggle;
            }
            EditorGUI.EndDisabledGroup();

        }

        private static void BuildMenuOf(SerializedObject focus, bool inSubMenu = false)
        {
            SerializedObject gameObjectSerialized = null;
            if (!focus.isEditingMultipleObjects)
            {
                gameObjectSerialized = new SerializedObject(((HaiAv3MenuToHierarchyControl) focus.targetObject).gameObject);
            }

            if (!inSubMenu)
            {
                LinkToParentIfApplicable(focus);
            }
            else
            {
                GUILayout.BeginHorizontal();
                LinkToCurrentIfApplicable(focus);
                GUILayout.Space(20);
                GUILayout.Label("Enabled", GUILayout.Width(50));
                if (!focus.isEditingMultipleObjects)
                {
                    EditorGUILayout.PropertyField(gameObjectSerialized.FindProperty("m_IsActive"), new GUIContent(), GUILayout.Width(EditorGUIUtility.singleLineHeight));
                }
                GUILayout.EndHorizontal();
            }

            if (inSubMenu && !gameObjectSerialized.FindProperty("m_IsActive").boolValue)
            {
                EditorGUILayout.HelpBox("This control is disabled. It will not be included in the Expression Menu.", MessageType.Info);
                gameObjectSerialized.ApplyModifiedProperties();
                return;
            }

            if (!focus.isEditingMultipleObjects && !gameObjectSerialized.FindProperty("m_IsActive").boolValue)
            {
                EditorGUILayout.HelpBox("This control is disabled. It will not be included in the Expression Menu.", MessageType.Info);
                if (GUILayout.Button("Re-enable control"))
                {
                    gameObjectSerialized.FindProperty("m_IsActive").boolValue = true;
                }
            }

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            try
            {
                EditorGUIUtility.labelWidth = Screen.width * 0.2f;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                if (!focus.isEditingMultipleObjects)
                {
                    NameOf(focus, gameObjectSerialized);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField(new GUIContent("(name not shown during multi-editing)"));
                    EditorGUI.EndDisabledGroup();
                }

                if (focus.targetObjects.Select(o => o.name).Any(s => Regex.IsMatch(s, ".* \\(\\d+\\)")))
                {
                    if (GUILayout.Button("Remove number in parentheses from name"))
                    {
                        foreach (var item in focus.targetObjects.Where(o => Regex.IsMatch(o.name, ".* \\(\\d+\\)")))
                        {
                            var objectsThatHaveName = (HaiAv3MenuToHierarchyControl) item;
                            var xgo = new SerializedObject(objectsThatHaveName.gameObject);
                            xgo.FindProperty("m_Name").stringValue = Regex.Replace(xgo.FindProperty("m_Name").stringValue, "(.*) \\(\\d+\\)", "$1");
                            xgo.ApplyModifiedProperties();
                        }
                    }
                }

                EditorGUILayout.PropertyField(focus.FindProperty("icon"));
                EditorGUILayout.PropertyField(focus.FindProperty("type"));
                EditorGUILayout.PropertyField(focus.FindProperty("parameter"));
                if (!focus.FindProperty("parameter").hasMultipleDifferentValues && (focus.FindProperty("parameter").stringValue != "" && IsIntOrFloat(focus.FindProperty("parameter"))))
                {
                    EditorGUILayout.PropertyField(focus.FindProperty("value"));
                }

                EditorGUILayout.EndVertical();

                var fp = focus.FindProperty("icon");
                if (!fp.hasMultipleDifferentValues)
                {
                    // fp.objectReferenceValue = EditorGUILayout.ObjectField(new GUIContent(), fp.objectReferenceValue, typeof(Texture2D), false, GUILayout.Width(64));
                    if (focus.FindProperty("icon").objectReferenceValue != null)
                    {
                        var col = GUI.color;
                        try
                        {
                            Texture2D result = new Texture2D(1, 1);
                            result.SetPixel(0, 0, new Color(0.01f, 0.2f, 0.2f));
                            result.Apply();
                            GUILayout.Box(
                                // EditorGUILayout.GetControlRect(false, 64, GUILayout.Width(64)),
                                AssetPreview.GetAssetPreview(focus.FindProperty("icon").objectReferenceValue),
                                new GUIStyle("box")
                                {
                                    normal = new GUIStyleState
                                    {
                                        background = result
                                    }
                                },
                                GUILayout.Width(64), GUILayout.Height(64)
                            );
                        }
                        finally
                        {
                            GUI.color = col;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(fp, new GUIContent(), GUILayout.Width(64));
                }

                EditorGUILayout.EndHorizontal();
            }
            finally
            {
                EditorGUIUtility.labelWidth = originalLabelWidth;
            }

            EditorGUILayout.Separator();

            if (!focus.FindProperty("type").hasMultipleDifferentValues)
            {
                var type = (Av3M2HType) focus.FindProperty("type").intValue;
                switch (type)
                {
                    case Av3M2HType.SubMenu:
                        EditorGUILayout.LabelField("Sub Menu", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(focus.FindProperty("subMenuSource"));

                        var subMenuSource = (Av3M2HSubMenuSource) focus.FindProperty("subMenuSource").intValue;
                        switch (subMenuSource)
                        {
                            case Av3M2HSubMenuSource.HierarchyChildren:
                                if (inSubMenu)
                                {
                                    if (GUILayout.Button("Open"))
                                    {
                                        SelectSubMenu((HaiAv3MenuToHierarchyControl) focus.targetObject);
                                    }
                                }

                                break;
                            case Av3M2HSubMenuSource.ExpressionMenuAsset:
                                EditorGUILayout.PropertyField(focus.FindProperty("subMenuAsset"));
                                break;
                            case Av3M2HSubMenuSource.HierarchyReference:
                                EditorGUILayout.PropertyField(focus.FindProperty("subMenuHierarchyReference"));
                                break;
                            case Av3M2HSubMenuSource.FlattenedSubMenu:
                                EditorGUILayout.PropertyField(focus.FindProperty("subMenuAsset"));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (!focus.isEditingMultipleObjects && ((HaiAv3MenuToHierarchyControl) focus.targetObject).HasTooManySubControls())
                        {
                            EditorGUILayout.HelpBox("This SubMenu has too many controls; only the first 8 active controls will be included.", MessageType.Error);
                        }

                        break;
                    case Av3M2HType.Button:
                        if (IsEmptyParameter(focus, "parameter"))
                        {
                            EditorGUILayout.HelpBox("This button does nothing.", MessageType.Info);
                        }

                        break;
                    case Av3M2HType.Toggle:
                        if (IsEmptyParameter(focus, "parameter"))
                        {
                            EditorGUILayout.HelpBox("This toggles nothing.", MessageType.Warning);
                        }

                        break;
                    case Av3M2HType.TwoAxisPuppet:
                        EditorGUILayout.LabelField("Two Axis Puppet", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(focus.FindProperty("puppetParameter0"), new GUIContent("Parameter Horizontal"));
                        EditorGUILayout.PropertyField(focus.FindProperty("puppetParameter1"), new GUIContent("Parameter Vertical"));
                        if (IsEmptyParameter(focus, "puppetParameter0") && IsEmptyParameter(focus, "puppetParameter1"))
                        {
                            EditorGUILayout.HelpBox("This puppet does not change any float value.", MessageType.Warning);
                        }
                        if (IsParameterDefinedButSameAsAPuppet(focus, 2))
                        {
                            EditorGUILayout.HelpBox("A puppet parameter is the same as the Parameter. This will cause the puppet menu to instantly close when you change the value.\n\nLeaving the Parameter as an empty value is usually what you want to do.", MessageType.Error);
                        }

                        break;
                    case Av3M2HType.FourAxisPuppet:
                        EditorGUILayout.LabelField("Four Axis Puppet", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(focus.FindProperty("puppetParameter0"), new GUIContent("Parameter Up"));
                        EditorGUILayout.PropertyField(focus.FindProperty("puppetParameter1"), new GUIContent("Parameter Right"));
                        EditorGUILayout.PropertyField(focus.FindProperty("puppetParameter2"), new GUIContent("Parameter Down"));
                        EditorGUILayout.PropertyField(focus.FindProperty("puppetParameter3"), new GUIContent("Parameter Left"));
                        if (IsEmptyParameter(focus, "puppetParameter0") && IsEmptyParameter(focus, "puppetParameter1") && IsEmptyParameter(focus, "puppetParameter2") && IsEmptyParameter(focus, "puppetParameter3"))
                        {
                            EditorGUILayout.HelpBox("This puppet does not change any float value.", MessageType.Warning);
                        }
                        if (IsParameterDefinedButSameAsAPuppet(focus, 4))
                        {
                            EditorGUILayout.HelpBox("A puppet parameter is the same as the Parameter. This will cause the puppet menu to instantly close when you change the value.\n\nLeaving the Parameter as an empty value is usually what you want to do.", MessageType.Error);
                        }

                        break;
                    case Av3M2HType.RadialPuppet:
                        EditorGUILayout.LabelField("Radial Puppet", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(focus.FindProperty("puppetParameter0"), new GUIContent("Parameter Rotation"));
                        if (IsEmptyParameter(focus, "puppetParameter0"))
                        {
                            if (!IsEmptyParameter(focus, "parameter"))
                            {
                                EditorGUILayout.HelpBox("This puppet does not change any float value.\n\nDid you confuse Parameter with Parameter Rotation?", MessageType.Warning);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox("This puppet does not change any float value.", MessageType.Warning);
                            }
                        }
                        if (IsParameterDefinedButSameAsAPuppet(focus, 1))
                        {
                            EditorGUILayout.HelpBox("A puppet parameter is the same as the Parameter. This will cause the puppet menu to instantly close when you change the value.\n\nLeaving the Parameter as an empty value is usually what you want to do.", MessageType.Error);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (type == Av3M2HType.TwoAxisPuppet || type == Av3M2HType.FourAxisPuppet)
                {
                    EditorGUILayout.BeginHorizontal();
                    PuppetField("Left", focus.FindProperty("puppetLabelLeft"), true);
                    EditorGUILayout.BeginVertical();
                    PuppetField("Up", focus.FindProperty("puppetLabelUp"), false);
                    PuppetField("Down", focus.FindProperty("puppetLabelDown"), false);
                    EditorGUILayout.EndVertical();
                    PuppetField("Right", focus.FindProperty("puppetLabelRight"), true);
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField(new GUIContent("(different types cannot be edited during multi-editing)"));
                EditorGUI.EndDisabledGroup();
            }

            if (focus.isEditingMultipleObjects)
            {
                EditorGUILayout.LabelField("Multi-selection actions", EditorStyles.boldLabel);
                if (GUILayout.Button("Group into a new SubMenu"))
                {
                    var sameParent = focus.targetObjects.Select(o => ((HaiAv3MenuToHierarchyControl) o).transform.parent).Distinct().Count() == 1;
                    var firstSiblingIndex = sameParent
                        ? focus.targetObjects
                            .Select(o => (HaiAv3MenuToHierarchyControl) o)
                            .Select(o => o.transform.GetSiblingIndex())
                            .Min()
                        : ((HaiAv3MenuToHierarchyControl)focus.targetObject).transform.GetSiblingIndex();

                    Undo.SetCurrentGroupName("Create submenu");
                    var newSubMenuObject = NewGameObject("SubMenu", ((HaiAv3MenuToHierarchyControl) focus.targetObject).transform.parent);
                    Undo.RegisterCreatedObjectUndo(newSubMenuObject, "x");
                    Undo.RecordObject(newSubMenuObject.transform, "x");
                    newSubMenuObject.transform.SetSiblingIndex(firstSiblingIndex);
                    Undo.AddComponent<HaiAv3MenuToHierarchyControl>(newSubMenuObject);

                    foreach (var control in sameParent
                        ? focus.targetObjects
                        .Select(o => (HaiAv3MenuToHierarchyControl) o)
                        .OrderBy(o => o.transform.GetSiblingIndex())
                        : focus.targetObjects
                            .Select(o => (HaiAv3MenuToHierarchyControl) o)
                            .OrderBy(o => o.transform.GetHierarchyPath()))
                    {
                        Undo.SetTransformParent(control.transform, newSubMenuObject.transform, "x");
                    }

                    HaiAv3MenuToHierarchyCompilerEditor.SetExpandedRecursive(newSubMenuObject, true);
                    EditorGUIUtility.PingObject(newSubMenuObject);
                    Selection.SetActiveObjectWithContext(newSubMenuObject, null);
                }
            }

            if (gameObjectSerialized != null)
            {
                gameObjectSerialized.ApplyModifiedProperties();
            }
        }

        private static bool IsParameterDefinedButSameAsAPuppet(SerializedObject focus, int totalNumberOfPuppets)
        {
            var parameter = focus.FindProperty("parameter").stringValue;
            if (parameter == "") return false;

            if (focus.FindProperty("puppetParameter0").stringValue == parameter) return true;
            if (totalNumberOfPuppets <= 1) return false;

            if (focus.FindProperty("puppetParameter1").stringValue == parameter) return true;
            if (totalNumberOfPuppets <= 2) return false;

            if (focus.FindProperty("puppetParameter2").stringValue == parameter) return true;
            if (totalNumberOfPuppets <= 3) return false;

            return focus.FindProperty("puppetParameter3").stringValue == parameter;
        }

        private static void PuppetField(string title, SerializedProperty puppetLabel, bool side)
        {
            // EditorGUILayout.BeginHorizontal()
            EditorGUILayout.BeginVertical();
            if (side)
            {
                GUILayout.Label("");
                GUILayout.Label("");
                GUILayout.Label("");
            }
            GUILayout.Label(title);
            EditorGUILayout.PropertyField(puppetLabel.FindPropertyRelative("name"), new GUIContent());
            EditorGUILayout.PropertyField(puppetLabel.FindPropertyRelative("icon"), new GUIContent());
            EditorGUILayout.EndVertical();
            // EditorGUILayout.EndHorizontal();
        }

        private static void SelectSubMenu(HaiAv3MenuToHierarchyControl focusTargetObject)
        {
            Selection.SetActiveObjectWithContext(focusTargetObject, null);
        }

        private static bool IsEmptyParameter(SerializedObject focus, string propertyPath)
        {
            return !focus.FindProperty(propertyPath).hasMultipleDifferentValues && focus.FindProperty(propertyPath).stringValue == "";
        }

        private static void LinkToCurrentIfApplicable(SerializedObject focus)
        {
            if (focus.isEditingMultipleObjects) return;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(new GUIContent(), (HaiAv3MenuToHierarchyControl)focus.targetObject, typeof(HaiAv3MenuToHierarchyControl), true);
            EditorGUI.EndDisabledGroup();
        }

        private static void LinkToParentIfApplicable(SerializedObject focus)
        {
            if (focus.isEditingMultipleObjects) return;

            var parent = ((HaiAv3MenuToHierarchyControl) focus.targetObject).transform.parent;
            if (parent == null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Parent", null, typeof(HaiAv3MenuToHierarchyControl), true);
                EditorGUI.EndDisabledGroup();
                return;
            }

            var m2hOfParent = parent.GetComponent<HaiAv3MenuToHierarchyControl>();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Parent", m2hOfParent, typeof(HaiAv3MenuToHierarchyControl), true);
            EditorGUI.EndDisabledGroup();

            if (m2hOfParent == null)
            {
                if (((HaiAv3MenuToHierarchyControl) focus.targetObject).transform.GetComponent<HaiAv3MenuToHierarchyCompiler>() != null|| parent.GetComponent<HaiAv3MenuToHierarchyCompiler>() != null)
                {
                    EditorGUILayout.HelpBox("This is the main menu root.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("The parent of this control has no Av3 Menu To Hierarchy Component.", MessageType.Error);
                    if (GUILayout.Button("(Fix) Create component in parent"))
                    {
                        Undo.AddComponent<HaiAv3MenuToHierarchyControl>(parent.gameObject);
                    }
                }
            }
            else if (m2hOfParent.type != Av3M2HType.SubMenu)
            {
                EditorGUILayout.HelpBox("The parent of this control is not of type SubMenu.\n\nThis control will be ignored.", MessageType.Error);
            }
            else if (m2hOfParent.subMenuSource != Av3M2HSubMenuSource.HierarchyChildren
                     && m2hOfParent.subMenuSource != Av3M2HSubMenuSource.FlattenedSubMenu)
            {
                EditorGUILayout.HelpBox("The parent of this control is a SubMenu that cannot accept sub-controls.\n\nThis control will be ignored.", MessageType.Error);
            }
            else if (m2hOfParent.HasTooManySubControls())
            {
                EditorGUILayout.HelpBox("The parent of this control is a SubMenu that has too many controls.\n\nA SubMenu can only have 8 controls; only the first 8 active controls will be included.", MessageType.Error);
            }
            else
            {
                if (GUILayout.Button(focus.FindProperty("type").intValue == (int)Av3M2HType.SubMenu ? "Back" : "Open containing menu"))
                {
                    SelectSubMenu(m2hOfParent);
                }
            }
        }

        private static void NameOf(SerializedObject focus, SerializedObject gameObjectSerialized)
        {
            EditorGUILayout.BeginHorizontal();
            var nameSource = (Av3M2HNameSource)focus.FindProperty("nameSource").intValue;
            if (nameSource == Av3M2HNameSource.UseCustomName)
            {
                EditorGUILayout.PropertyField(focus.FindProperty("customName"), new GUIContent("Custom Name"));
            }
            else
            {
                EditorGUILayout.PropertyField(gameObjectSerialized.FindProperty("m_Name"), new GUIContent("Name"));
            }

            focus.FindProperty("nameSource").intValue = EditorGUILayout.Toggle(new GUIContent(), nameSource == Av3M2HNameSource.UseCustomName, GUILayout.Width(EditorGUIUtility.singleLineHeight))
                ? (int)Av3M2HNameSource.UseCustomName
                : (int)Av3M2HNameSource.UseHierarchyObjectName;
            EditorGUILayout.EndHorizontal();
        }

        private static bool IsIntOrFloat(SerializedProperty parameter)
        {
            return true;
        }

        private static GameObject NewGameObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(gameObject, "x");
            return gameObject;
        }
    }
}
