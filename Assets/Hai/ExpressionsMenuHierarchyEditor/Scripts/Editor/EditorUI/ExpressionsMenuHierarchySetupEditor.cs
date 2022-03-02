using Hai.ExpressionsMenuHierarchyEditor.Scripts.Components;
using Hai.ExpressionsMenuHierarchyEditor.Scripts.Editor.Internal;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Hai.ExpressionsMenuHierarchyEditor.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(ExpressionsMenuHierarchySetup))]
    public class ExpressionsMenuHierarchySetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ExpressionsMenuHierarchySetup.menuToExtract)));

            EditorGUI.BeginDisabledGroup(serializedObject.FindProperty(nameof(ExpressionsMenuHierarchySetup.menuToExtract)).objectReferenceValue == null);
            if (GUILayout.Button("Create a new Expression Menu..."))
            {
                MenuToHierarchy();
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void MenuToHierarchy()
        {
            var newMenuAsset = MaybeCreateNewExpressionMenuAsset();
            if (newMenuAsset == null) return;

            var setup = (ExpressionsMenuHierarchySetup) target;

            var expressionMenu = EmhDecompose.NewGameObject(setup.menuToExtract.name, setup.transform);
            var mainMenu = expressionMenu.AddComponent<ExpressionsMenuHierarchyItem>();
            mainMenu.subMenuAsset = setup.menuToExtract;
            mainMenu.subMenuSource = Emh.EmhSubMenuSource.HierarchyChildren;

            new EmhDecompose().Decompose(setup.menuToExtract, expressionMenu.transform);

            SetExpandedRecursive(setup.gameObject, true);
            Selection.SetActiveObjectWithContext(expressionMenu, null);

            var compiler = Undo.AddComponent<ExpressionsMenuHierarchyCompiler>(setup.gameObject);
            compiler.mainMenu = mainMenu;
            compiler.generatedMenu = newMenuAsset;

            ((ExpressionsMenuHierarchyCompilerEditor)CreateEditor(compiler)).HierarchyToMenu();

            Undo.DestroyObjectImmediate(setup);
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
            var exists = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(assetPath);
            if (exists)
            {
                EditorUtility.DisplayDialog("Invalid save path", "Asset already exists.", "OK");

                return null;
            }

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
