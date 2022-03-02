using System;
using System.Linq;
using System.Text.RegularExpressions;
using Hai.ExpressionsMenuHierarchyEditor.Scripts.Components;
using Hai.ExpressionsMenuHierarchyEditor.Scripts.Editor.Internal;
using UnityEditor;
using UnityEngine;

namespace Hai.ExpressionsMenuHierarchyEditor.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(ExpressionsMenuHierarchyItem))]
    [CanEditMultipleObjects]
    public class ExpressionsMenuHierarchyControlEditor : UnityEditor.Editor
    {
        private static bool _showExtraOptions;
        private const int MaxSubMenuControlCount = 8;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _showExtraOptions = EditorGUILayout.Toggle("Advanced mode", _showExtraOptions);
            BuildMenuOf(serializedObject);

            DisplaySubMenuControlsIfApplicable();
            if (serializedObject.isEditingMultipleObjects)
            {
                foreach (var m2h in serializedObject.targetObjects.Select(o => (ExpressionsMenuHierarchyItem) o).Reverse().ToList())
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
            var type = (Emh.EmhType) serializedObject.FindProperty(nameof(ExpressionsMenuHierarchyItem.type)).intValue;
            var subMenuSource = (Emh.EmhSubMenuSource) serializedObject.FindProperty(nameof(ExpressionsMenuHierarchyItem.subMenuSource)).intValue;

            if (serializedObject.isEditingMultipleObjects || type != Emh.EmhType.SubMenu || (subMenuSource != Emh.EmhSubMenuSource.HierarchyChildren)) return;

            EditorGUILayout.Separator();
            int controlRank = 1;
            foreach (Transform child in ((ExpressionsMenuHierarchyItem) serializedObject.targetObject).transform)
            {
                var m2h = child.GetComponent<ExpressionsMenuHierarchyItem>();
                if (m2h == null) continue;

                var serializedSubControl = new SerializedObject(m2h);
                if (m2h.gameObject.activeSelf)
                {
                    if (controlRank == MaxSubMenuControlCount + 1)
                    {
                        EditorGUILayout.HelpBox("SubMenus cannot have more than 8 controls.\n\nUnless some items are discarded, controls below will not be included:", MessageType.Error);
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
                var parent = ((ExpressionsMenuHierarchyItem) serializedObject.targetObject).transform;
                var go = NewGameObject("Control #" + controlRank, parent);
                Undo.RegisterCreatedObjectUndo(go, "x");
                var m2h = Undo.AddComponent<ExpressionsMenuHierarchyItem>(go);
                Undo.RecordObject(go, "x");
                m2h.type = Emh.EmhType.Toggle;
            }
            EditorGUI.EndDisabledGroup();

        }

        private static void BuildMenuOf(SerializedObject focus, bool inSubMenu = false)
        {
            SerializedObject gameObjectSerialized = null;
            if (!focus.isEditingMultipleObjects)
            {
                gameObjectSerialized = new SerializedObject(((ExpressionsMenuHierarchyItem) focus.targetObject).gameObject);
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
                            var objectsThatHaveName = (ExpressionsMenuHierarchyItem) item;
                            var xgo = new SerializedObject(objectsThatHaveName.gameObject);
                            xgo.FindProperty("m_Name").stringValue = Regex.Replace(xgo.FindProperty("m_Name").stringValue, "(.*) \\(\\d+\\)", "$1");
                            xgo.ApplyModifiedProperties();
                        }
                    }
                }

                EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.icon)));
                EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.type)));
                EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.parameter)));
                if (!focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.parameter)).hasMultipleDifferentValues && (focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.parameter)).stringValue != "" && IsIntOrFloat(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.parameter)))))
                {
                    EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.value)));
                }

                EditorGUILayout.EndVertical();

                var fp = focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.icon));
                if (!fp.hasMultipleDifferentValues)
                {
                    // fp.objectReferenceValue = EditorGUILayout.ObjectField(new GUIContent(), fp.objectReferenceValue, typeof(Texture2D), false, GUILayout.Width(64));
                    if (focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.icon)).objectReferenceValue != null)
                    {
                        var col = GUI.color;
                        try
                        {
                            Texture2D result = new Texture2D(1, 1);
                            result.SetPixel(0, 0, new Color(0.01f, 0.2f, 0.2f));
                            result.Apply();
                            GUILayout.Box(
                                // EditorGUILayout.GetControlRect(false, 64, GUILayout.Width(64)),
                                EmhCompose.GenerateIcon((Texture2D)focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.icon)).objectReferenceValue, (Material)focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.shader)).objectReferenceValue),
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
                EditorGUILayout.BeginHorizontal();

                if (_showExtraOptions)
                {
                    EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.discardType)), new GUIContent("Discardable"));
                    EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.discardTags)), new GUIContent("Tags"));
                }
                else
                {
                    var dtype = focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.discardType));
                    var dtags = focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.discardTags));
                    if (dtype.intValue != 0 || dtags.stringValue != "")
                    {
                        EditorGUILayout.PropertyField(dtype, new GUIContent("Discardable"));
                        EditorGUILayout.PropertyField(dtags, new GUIContent("Tags"));
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (_showExtraOptions)
                {
                    EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.shader)));
                }
                else
                {
                    var shader = focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.shader));
                    if (shader.objectReferenceValue != null)
                    {
                        EditorGUILayout.PropertyField(shader);
                    }
                }
            }
            finally
            {
                EditorGUIUtility.labelWidth = originalLabelWidth;
            }

            EditorGUILayout.Separator();

            if (!focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.type)).hasMultipleDifferentValues)
            {
                var type = (Emh.EmhType) focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.type)).intValue;
                switch (type)
                {
                    case Emh.EmhType.SubMenu:
                        EditorGUILayout.LabelField("Sub Menu", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.subMenuSource)));

                        var subMenuSource = (Emh.EmhSubMenuSource) focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.subMenuSource)).intValue;
                        switch (subMenuSource)
                        {
                            case Emh.EmhSubMenuSource.HierarchyChildren:
                                if (inSubMenu)
                                {
                                    if (GUILayout.Button("Open"))
                                    {
                                        SelectSubMenu((ExpressionsMenuHierarchyItem) focus.targetObject);
                                    }
                                }

                                break;
                            case Emh.EmhSubMenuSource.ExpressionMenuAsset:
                                EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.subMenuAsset)));
                                break;
                            case Emh.EmhSubMenuSource.HierarchyReference:
                                EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.subMenuHierarchyReference)));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (!focus.isEditingMultipleObjects && ((ExpressionsMenuHierarchyItem) focus.targetObject).HasTooManySubControls())
                        {
                            EditorGUILayout.HelpBox("This SubMenu has too many controls; only the first 8 active controls will be included unless some are discarded.", MessageType.Error);
                        }

                        break;
                    case Emh.EmhType.Button:
                        if (IsEmptyParameter(focus, "parameter"))
                        {
                            EditorGUILayout.HelpBox("This button does nothing.", MessageType.Info);
                        }
                        else if (IsValueZero(focus))
                        {
                            EditorGUILayout.HelpBox(@"A toggle or button with a value of zero will:
- set the value to zero when clicked.
- show a spinning wheel when the value is equal to zero.
- if the parameter is a boolean then it will set the value to false; however if you open the menu asset in Unity Editor using the default inspector, the asset will be automatically edited so that the value will become true instead. If a value of false is what you want, make sure you do not inspect the asset after generating it.", MessageType.Info);
                        }

                        break;
                    case Emh.EmhType.Toggle:
                        if (IsEmptyParameter(focus, "parameter"))
                        {
                            EditorGUILayout.HelpBox("This toggles nothing.", MessageType.Warning);
                        }
                        else if (IsValueZero(focus))
                        {
                            EditorGUILayout.HelpBox(@"A toggle or button with a value of zero will:
- set the value to zero when clicked.
- show a spinning wheel when the value is equal to zero.
- if the parameter is a boolean then it will set the value to false; however if you open the menu asset in Unity Editor using the default inspector, the asset will be automatically edited so that the value will become true instead. If a value of false is what you want, make sure you do not inspect the asset after generating it.", MessageType.Info);
                        }

                        break;
                    case Emh.EmhType.TwoAxisPuppet:
                        EditorGUILayout.LabelField("Two Axis Puppet", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter0)), new GUIContent("Parameter Horizontal"));
                        EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter1)), new GUIContent("Parameter Vertical"));
                        if (IsEmptyParameter(focus, "puppetParameter0") && IsEmptyParameter(focus, "puppetParameter1"))
                        {
                            EditorGUILayout.HelpBox("This puppet does not change any float value.", MessageType.Warning);
                        }
                        if (IsParameterDefinedButSameAsAPuppet(focus, 2))
                        {
                            EditorGUILayout.HelpBox("A puppet parameter is the same as the Parameter. This will cause the puppet menu to instantly close when you change the value.\n\nLeaving the Parameter as an empty value is usually what you want to do.", MessageType.Error);
                        }

                        break;
                    case Emh.EmhType.FourAxisPuppet:
                        EditorGUILayout.LabelField("Four Axis Puppet", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter0)), new GUIContent("Parameter Up"));
                        EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter1)), new GUIContent("Parameter Right"));
                        EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter2)), new GUIContent("Parameter Down"));
                        EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter3)), new GUIContent("Parameter Left"));
                        if (IsEmptyParameter(focus, "puppetParameter0") && IsEmptyParameter(focus, "puppetParameter1") && IsEmptyParameter(focus, "puppetParameter2") && IsEmptyParameter(focus, "puppetParameter3"))
                        {
                            EditorGUILayout.HelpBox("This puppet does not change any float value.", MessageType.Warning);
                        }
                        if (IsParameterDefinedButSameAsAPuppet(focus, 4))
                        {
                            EditorGUILayout.HelpBox("A puppet parameter is the same as the Parameter. This will cause the puppet menu to instantly close when you change the value.\n\nLeaving the Parameter as an empty value is usually what you want to do.", MessageType.Error);
                        }

                        break;
                    case Emh.EmhType.RadialPuppet:
                        EditorGUILayout.LabelField("Radial Puppet", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter0)), new GUIContent("Parameter Rotation"));
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

                if (type == Emh.EmhType.TwoAxisPuppet || type == Emh.EmhType.FourAxisPuppet)
                {
                    EditorGUILayout.BeginHorizontal();
                    PuppetField("Left", focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetLabelLeft)), true);
                    EditorGUILayout.BeginVertical();
                    PuppetField("Up", focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetLabelUp)), false);
                    PuppetField("Down", focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetLabelDown)), false);
                    EditorGUILayout.EndVertical();
                    PuppetField("Right", focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetLabelRight)), true);
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
                    var sameParent = focus.targetObjects.Select(o => ((ExpressionsMenuHierarchyItem) o).transform.parent).Distinct().Count() == 1;
                    var firstSiblingIndex = sameParent
                        ? focus.targetObjects
                            .Select(o => (ExpressionsMenuHierarchyItem) o)
                            .Select(o => o.transform.GetSiblingIndex())
                            .Min()
                        : ((ExpressionsMenuHierarchyItem)focus.targetObject).transform.GetSiblingIndex();

                    Undo.SetCurrentGroupName("Create submenu");
                    var newSubMenuObject = NewGameObject("SubMenu", ((ExpressionsMenuHierarchyItem) focus.targetObject).transform.parent);
                    Undo.RegisterCreatedObjectUndo(newSubMenuObject, "x");
                    Undo.RecordObject(newSubMenuObject.transform, "x");
                    newSubMenuObject.transform.SetSiblingIndex(firstSiblingIndex);
                    Undo.AddComponent<ExpressionsMenuHierarchyItem>(newSubMenuObject);

                    foreach (var control in sameParent
                        ? focus.targetObjects
                        .Select(o => (ExpressionsMenuHierarchyItem) o)
                        .OrderBy(o => o.transform.GetSiblingIndex())
                        : focus.targetObjects
                            .Select(o => (ExpressionsMenuHierarchyItem) o)
                            .OrderBy(o => o.transform.GetHierarchyPath()))
                    {
                        Undo.SetTransformParent(control.transform, newSubMenuObject.transform, "x");
                    }

                    ExpressionsMenuHierarchyCompilerEditor.SetExpandedRecursive(newSubMenuObject, true);
                    EditorGUIUtility.PingObject(newSubMenuObject);
                    Selection.SetActiveObjectWithContext(newSubMenuObject, null);
                }
            }

            if (gameObjectSerialized != null)
            {
                gameObjectSerialized.ApplyModifiedProperties();
            }
        }

        private static bool IsValueZero(SerializedObject focus)
        {
            return !focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.value)).hasMultipleDifferentValues && focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.value)).floatValue == 0f;
        }

        private static bool IsParameterDefinedButSameAsAPuppet(SerializedObject focus, int totalNumberOfPuppets)
        {
            var parameter = focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.parameter)).stringValue;
            if (parameter == "") return false;

            if (focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter0)).stringValue == parameter) return true;
            if (totalNumberOfPuppets <= 1) return false;

            if (focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter1)).stringValue == parameter) return true;
            if (totalNumberOfPuppets <= 2) return false;

            if (focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter2)).stringValue == parameter) return true;
            if (totalNumberOfPuppets <= 3) return false;

            return focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.puppetParameter3)).stringValue == parameter;
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

        private static void SelectSubMenu(ExpressionsMenuHierarchyItem focusTargetObject)
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
            EditorGUILayout.ObjectField(new GUIContent(), (ExpressionsMenuHierarchyItem)focus.targetObject, typeof(ExpressionsMenuHierarchyItem), true);
            EditorGUI.EndDisabledGroup();
        }

        private static void LinkToParentIfApplicable(SerializedObject focus)
        {
            if (focus.isEditingMultipleObjects) return;

            var parent = ((ExpressionsMenuHierarchyItem) focus.targetObject).transform.parent;
            if (parent == null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Parent", null, typeof(ExpressionsMenuHierarchyItem), true);
                EditorGUI.EndDisabledGroup();
                return;
            }

            var m2hOfParent = parent.GetComponent<ExpressionsMenuHierarchyItem>();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Parent", m2hOfParent, typeof(ExpressionsMenuHierarchyItem), true);
            EditorGUI.EndDisabledGroup();

            if (m2hOfParent == null)
            {
                if (((ExpressionsMenuHierarchyItem) focus.targetObject).transform.GetComponent<ExpressionsMenuHierarchyCompiler>() != null|| parent.GetComponent<ExpressionsMenuHierarchyCompiler>() != null)
                {
                    EditorGUILayout.HelpBox("This is the main menu root.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("The parent of this control has no Expressions Menu Hierarchy component.", MessageType.Error);
                    if (GUILayout.Button("(Fix) Create component in parent"))
                    {
                        Undo.AddComponent<ExpressionsMenuHierarchyItem>(parent.gameObject);
                    }
                }
            }
            else if (m2hOfParent.type != Emh.EmhType.SubMenu)
            {
                EditorGUILayout.HelpBox("The parent of this control is not of type SubMenu.\n\nThis control will be ignored.", MessageType.Error);
            }
            else if (m2hOfParent.subMenuSource != Emh.EmhSubMenuSource.HierarchyChildren)
            {
                EditorGUILayout.HelpBox("The parent of this control is a SubMenu that cannot accept sub-controls.\n\nThis control will be ignored.", MessageType.Error);
            }
            else if (m2hOfParent.HasTooManySubControls())
            {
                EditorGUILayout.HelpBox("The parent of this control is a SubMenu that has too many controls.\n\nA SubMenu can only have 8 controls; only the first 8 active controls will be included unless some are discarded.", MessageType.Error);
            }
            else
            {
                if (GUILayout.Button(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.type)).intValue == (int)Emh.EmhType.SubMenu ? "Back" : "Open containing menu"))
                {
                    SelectSubMenu(m2hOfParent);
                }
            }
        }

        private static void NameOf(SerializedObject focus, SerializedObject gameObjectSerialized)
        {
            EditorGUILayout.BeginHorizontal();
            var nameSource = (Emh.EmhNameSource)focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.nameSource)).intValue;
            if (nameSource == Emh.EmhNameSource.UseCustomName)
            {
                EditorGUILayout.PropertyField(focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.customName)), new GUIContent("Custom Name"));
            }
            else
            {
                EditorGUILayout.PropertyField(gameObjectSerialized.FindProperty("m_Name"), new GUIContent("Name"));
            }

            focus.FindProperty(nameof(ExpressionsMenuHierarchyItem.nameSource)).intValue = EditorGUILayout.Toggle(new GUIContent(), nameSource == Emh.EmhNameSource.UseCustomName, GUILayout.Width(EditorGUIUtility.singleLineHeight))
                ? (int)Emh.EmhNameSource.UseCustomName
                : (int)Emh.EmhNameSource.UseHierarchyObjectName;
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
