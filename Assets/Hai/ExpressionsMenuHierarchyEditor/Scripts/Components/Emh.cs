using System;
using UnityEngine;

namespace Hai.ExpressionsMenuHierarchyEditor.Scripts.Components
{
    public static class Emh
    {
        [Serializable]
        public struct EmhPuppetLabel
        {
            public string name;
            public Texture2D icon;
        }

        [Serializable]
        public enum EmhType
        {
            SubMenu,
            Button,
            Toggle,
            TwoAxisPuppet,
            FourAxisPuppet,
            RadialPuppet,
        }

        [Serializable]
        public enum EmhNameSource
        {
            UseHierarchyObjectName,
            UseCustomName
        }

        [Serializable]
        public enum EmhSubMenuSource
        {
            HierarchyChildren,
            ExpressionMenuAsset,
            HierarchyReference
        }

        [Serializable]
        public enum EmhDiscardType
        {
            Default,
            BlankOut,
            GrayOut,
            Remove
        }

        public enum EmhQualification
        {
            ElementIsAesthetic, // Used for blank items, or toggles that do nothing
            ElementIsFunctional,
            ElementIsPartiallyFunctional,
            ElementIsNonFunctional
        }

        public struct EmhQualifier
        {
            public ExpressionsMenuHierarchyItem item;
            public Emh.EmhQualification qualification;
        }
    }
}