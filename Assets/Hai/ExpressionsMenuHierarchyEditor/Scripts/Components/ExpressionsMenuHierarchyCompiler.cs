using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Hai.ExpressionsMenuHierarchyEditor.Scripts.Components
{
    public class ExpressionsMenuHierarchyCompiler : MonoBehaviour
    {
        public ExpressionsMenuHierarchyItem mainMenu;
        public VRCExpressionsMenu generatedMenu;
        public Texture2D defaultIcon;

        public bool autoDiscard = true;
        public VRCExpressionParameters expressionParameters;
        public Emh.EmhDiscardType discardType;
        public string discardTags = "";
        public Material discardGrayOut;
    }
}
