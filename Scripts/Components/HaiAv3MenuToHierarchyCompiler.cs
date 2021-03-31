using System;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Hai.Av3MenuToHierarchy.Scripts.Components
{
    public class HaiAv3MenuToHierarchyCompiler : MonoBehaviour
    {
        public VRCExpressionsMenu source;
        public bool sourceExtracted;

        public HaiAv3MenuToHierarchyControl mainMenu;
        public Av3M2HManagement management;
        public VRCExpressionsMenu easyTreeDestination;

        [Serializable]
        public enum Av3M2HManagement
        {
            SimpleTree,
            AdvancedFlattenedSubMenus
        }
    }
}
