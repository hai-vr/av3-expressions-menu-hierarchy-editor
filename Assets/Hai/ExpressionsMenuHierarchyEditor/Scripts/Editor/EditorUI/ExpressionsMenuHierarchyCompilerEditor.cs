using System.Linq;
using Hai.ExpressionsMenuHierarchyEditor.Scripts.Components;
using Hai.ExpressionsMenuHierarchyEditor.Scripts.Editor.Internal;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Hai.ExpressionsMenuHierarchyEditor.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(ExpressionsMenuHierarchyCompiler))]
    public class ExpressionsMenuHierarchyCompilerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ExpressionsMenuHierarchyCompiler.mainMenu)));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ExpressionsMenuHierarchyCompiler.generatedMenu)));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ExpressionsMenuHierarchyCompiler.defaultIcon)));

            var autoDiscard = serializedObject.FindProperty(nameof(ExpressionsMenuHierarchyCompiler.autoDiscard));
            EditorGUILayout.LabelField("Discard", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoDiscard);
            if (autoDiscard.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ExpressionsMenuHierarchyCompiler.discardType)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ExpressionsMenuHierarchyCompiler.expressionParameters)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ExpressionsMenuHierarchyCompiler.discardTags)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ExpressionsMenuHierarchyCompiler.discardGrayOut)));

                var compiler = (ExpressionsMenuHierarchyCompiler) target;
                if (compiler.discardTags == "" && compiler.expressionParameters == null)
                {
                    EditorGUILayout.HelpBox("Nothing will be discarded (no tags specified and no expression parameters asset specified).", MessageType.Info);

                    LayoutGenerate();
                }
                else
                {
                    var analysis = new EmhAnalysis().Analyze(compiler.mainMenu);
                    var tagsSplit = !string.IsNullOrEmpty(compiler.discardTags) ? compiler.discardTags.Split(',') : new string[0];
                    var disqualifiedItems = analysis
                        .SelectMany(item => item.AllActiveSubMenuItems())
                        .Where(item =>
                        {
                            var emhQualification = item.Qualify(compiler.expressionParameters, tagsSplit);
                            return emhQualification == Emh.EmhQualification.ElementIsNonFunctional || emhQualification == Emh.EmhQualification.ElementIsPartiallyFunctional;
                        })
                        .ToArray();

                    if (disqualifiedItems.Length > 0)
                    {
                        EditorGUILayout.HelpBox($"{disqualifiedItems.Length} items will be discarded.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("All items in the menu are valid.", MessageType.Info);
                    }

                    LayoutGenerate();

                    EditorGUILayout.LabelField($"Discarded items ({disqualifiedItems.Length})", EditorStyles.boldLabel);
                    foreach (var item in disqualifiedItems)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (item.discardType == Emh.EmhDiscardType.Default)
                        {
                            EditorGUILayout.LabelField(item.name);
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"{item.name} ({item.discardType.ToString()})");
                        }
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(item, typeof(ExpressionsMenuHierarchyItem));
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                LayoutGenerate();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void LayoutGenerate()
        {
            EditorGUILayout.LabelField("Generate", EditorStyles.boldLabel);
            if (GUILayout.Button("Generate"))
            {
                HierarchyToMenu();
            }
        }

        internal void HierarchyToMenu()
        {
            var compiler = (ExpressionsMenuHierarchyCompiler) target;

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(compiler.generatedMenu));
            foreach (var subAsset in subAssets)
            {
                if (subAsset != compiler.generatedMenu && subAsset.GetType() == typeof(VRCExpressionsMenu))
                {
                    AssetDatabase.RemoveObjectFromAsset(subAsset);
                }
            }

            var compose = new EmhCompose(compiler);
            compose.Compose();

            AssetDatabase.SaveAssets();
            AssetDatabase.ForceReserializeAssets(compose.FindAssets());
        }

        // https://answers.unity.com/questions/656869/foldunfold-gameobject-from-code.html?childToView=858132#comment-858132
        public static void SetExpandedRecursive(GameObject go, bool expand)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var methodInfo = type.GetMethod("SetExpandedRecursive");

            EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
            var window = EditorWindow.focusedWindow;

            methodInfo.Invoke(window, new object[] { go.GetInstanceID(), expand });
        }
    }
}
