using Hai.Av3MenuToHierarchy.Scripts.Components;
using Hai.Av3MenuToHierarchy.Scripts.Editor.Internal;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using static Hai.Av3MenuToHierarchy.Scripts.Components.HaiAv3MenuToHierarchyControl;

namespace Hai.Av3MenuToHierarchy.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(HaiAv3MenuToHierarchyCompiler))]
    public class HaiAv3MenuToHierarchyCompilerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var extracted = serializedObject.FindProperty("sourceExtracted").boolValue;
            var management = serializedObject.FindProperty("management");
            if (!extracted)
            {
                EditorGUILayout.PropertyField(management);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("source"));

                EditorGUI.BeginDisabledGroup(serializedObject.FindProperty("source").objectReferenceValue == null);
                if (GUILayout.Button("Extract Av3 Menu To Hierarchy"))
                {
                    MenuToHierarchy();
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(management);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("source"));
                EditorGUI.EndDisabledGroup();

                if ((HaiAv3MenuToHierarchyCompiler.Av3M2HManagement) management.intValue == HaiAv3MenuToHierarchyCompiler.Av3M2HManagement.SimpleTree)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mainMenu"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("easyTreeDestination"));

                    if (GUILayout.Button("Compile Hierarchy To Av3 Menu"))
                    {
                        HierarchyToMenu();
                    }
                }
                else
                {

                }
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceExtracted"), new GUIContent("(Advanced: Source extracted?)"));

            serializedObject.ApplyModifiedProperties();
        }

        private void MenuToHierarchy()
        {
            var management = (HaiAv3MenuToHierarchyCompiler.Av3M2HManagement) serializedObject.FindProperty("management").intValue;

            if (management == HaiAv3MenuToHierarchyCompiler.Av3M2HManagement.SimpleTree)
            {
                var newMenuAsset = MaybeCreateNewExpressionMenuAsset();
                if (newMenuAsset == null) return;

                serializedObject.FindProperty("easyTreeDestination").objectReferenceValue = newMenuAsset;
            }

            var compiler = (HaiAv3MenuToHierarchyCompiler) target;

            var expressionMenu = Av3M2HDecompose.NewGameObject(compiler.source.name, compiler.transform);
            var mainMenu = expressionMenu.AddComponent<HaiAv3MenuToHierarchyControl>();
            mainMenu.subMenuAsset = compiler.source;
            mainMenu.subMenuSource = management == HaiAv3MenuToHierarchyCompiler.Av3M2HManagement.SimpleTree ? Av3M2HSubMenuSource.HierarchyChildren : Av3M2HSubMenuSource.FlattenedSubMenu;

            serializedObject.FindProperty("mainMenu").objectReferenceValue = mainMenu;

            new Av3M2HDecompose(management, compiler.transform).Decompose(compiler.source, expressionMenu.transform);

            serializedObject.FindProperty("sourceExtracted").boolValue = true;

            SetExpandedRecursive(compiler.gameObject, true);
            Selection.SetActiveObjectWithContext(expressionMenu, null);
        }

        private void HierarchyToMenu()
        {
            var compiler = (HaiAv3MenuToHierarchyCompiler) target;

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(compiler.easyTreeDestination));
            foreach (var subAsset in subAssets)
            {
                if (subAsset != compiler.easyTreeDestination && subAsset.GetType() == typeof(VRCExpressionsMenu))
                {
                    AssetDatabase.RemoveObjectFromAsset(subAsset);
                }
            }

            var compose = new Av3M2HCompose(compiler.easyTreeDestination);
            compose.Compose(compiler.mainMenu, compiler.easyTreeDestination);

            AssetDatabase.SaveAssets();
            AssetDatabase.ForceReserializeAssets(compose.FindAssets());
        }

        private VRCExpressionsMenu MaybeCreateNewExpressionMenuAsset()
        {
            var savePath = EditorUtility.SaveFilePanel("Create...", Application.dataPath, "", "asset");
            if (savePath == null || savePath.Trim() == "") return null;
            if (!savePath.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("Invalid save path", "Save path must be in the project's /Assets path.", "OK");
                return null;
            }

            var assetPath = "Assets" + savePath.Substring(Application.dataPath.Length);
            var newMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            AssetDatabase.CreateAsset(newMenu, assetPath);
            EditorGUIUtility.PingObject(newMenu);
            AssetDatabase.SaveAssets();

            return newMenu;
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
