using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Hai.Av3MenuToHierarchy.Scripts.Components
{
    public class HaiAv3MenuToHierarchyControl : MonoBehaviour
    {
        public Av3M2HType type;

        public Av3M2HNameSource nameSource;
        public string customName;
        public Texture2D icon;
        public string parameter;
        public float value = 1f;

        public Av3M2HSubMenuSource subMenuSource;
        public VRCExpressionsMenu subMenuAsset;
        public Transform subMenuHierarchyReference;

        public string puppetParameter0; // Up | Horizontal | Rotation
        public string puppetParameter1; // Right | Vertical
        public string puppetParameter2; // Down
        public string puppetParameter3; // Left

        public Av3M2HPuppetLabel puppetLabelUp;
        public Av3M2HPuppetLabel puppetLabelRight;
        public Av3M2HPuppetLabel puppetLabelDown;
        public Av3M2HPuppetLabel puppetLabelLeft;

        public string subTitle;
        public string documentation;

        [System.Serializable]
        public struct Av3M2HPuppetLabel
        {
            public string name;
            public Texture2D icon;
        }

        [System.Serializable]
        public enum Av3M2HType
        {
            SubMenu,
            Button,
            Toggle,
            TwoAxisPuppet,
            FourAxisPuppet,
            RadialPuppet
        }

        [System.Serializable]
        public enum Av3M2HNameSource
        {
            UseHierarchyObjectName,
            UseCustomName
        }

        [System.Serializable]
        public enum Av3M2HSubMenuSource
        {
            HierarchyChildren,
            ExpressionMenuAsset,
            HierarchyReference,
            FlattenedSubMenu
        }

        public bool HasTooManySubControls()
        {
            if (type == Av3M2HType.SubMenu && (subMenuSource == Av3M2HSubMenuSource.HierarchyReference || subMenuSource == Av3M2HSubMenuSource.ExpressionMenuAsset))
            {
                return false;
            }

            var count = 0;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;
                if (child.GetComponent<HaiAv3MenuToHierarchyControl>() == null) continue;

                count++;
                if (count > 8)
                {
                    return true;
                }
            }

            return false;
        }

        public string ResolveName()
        {
            return nameSource == Av3M2HNameSource.UseCustomName ? customName : transform.name;
        }
    }
}
