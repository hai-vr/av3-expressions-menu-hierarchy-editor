using System;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Hai.ExpressionsMenuHierarchyEditor.Scripts.Components
{
    public class ExpressionsMenuHierarchyItem : MonoBehaviour
    {
        public Emh.EmhType type;

        public Emh.EmhNameSource nameSource;
        public string customName;
        public Texture2D icon;
        public string parameter;
        public float value = 1f;

        public Emh.EmhSubMenuSource subMenuSource;
        public VRCExpressionsMenu subMenuAsset;
        public Transform subMenuHierarchyReference;

        public string puppetParameter0; // Up | Horizontal | Rotation
        public string puppetParameter1; // Right | Vertical
        public string puppetParameter2; // Down
        public string puppetParameter3; // Left

        public Emh.EmhPuppetLabel puppetLabelUp;
        public Emh.EmhPuppetLabel puppetLabelRight;
        public Emh.EmhPuppetLabel puppetLabelDown;
        public Emh.EmhPuppetLabel puppetLabelLeft;

        // public string subTitle;
        // public string documentation;

        // public bool discardable;
        public Emh.EmhDiscardType discardType;
        public string discardTags = "";

        public Material shader;

        public bool HasTooManySubControls()
        {
            if (type == Emh.EmhType.SubMenu && (subMenuSource == Emh.EmhSubMenuSource.HierarchyReference || subMenuSource == Emh.EmhSubMenuSource.ExpressionMenuAsset))
            {
                return false;
            }

            var count = 0;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;
                if (child.GetComponent<ExpressionsMenuHierarchyItem>() == null) continue;

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
            return nameSource == Emh.EmhNameSource.UseCustomName ? customName : transform.name;
        }

        public Emh.EmhQualification Qualify(VRCExpressionParameters expParametersOptional, string[] tags)
        {
            var myTags = !string.IsNullOrEmpty(discardTags) ? discardTags.Split(',') : new string[0];
            foreach (var discardTag in tags)
            {
                if (myTags.Contains(discardTag))
                {
                    return Emh.EmhQualification.ElementIsNonFunctional;
                }
            }

            switch (type)
            {
                case Emh.EmhType.SubMenu:
                    var functional = Functional(expParametersOptional, parameter);
                    return functional == Emh.EmhQualification.ElementIsAesthetic ? Emh.EmhQualification.ElementIsFunctional : functional ;
                case Emh.EmhType.Button:
                case Emh.EmhType.Toggle:
                    return Functional(expParametersOptional, parameter);
                case Emh.EmhType.TwoAxisPuppet:
                    return Functional(expParametersOptional, parameter, puppetParameter0, puppetParameter1);
                case Emh.EmhType.FourAxisPuppet:
                    return Functional(expParametersOptional, parameter, puppetParameter0, puppetParameter1, puppetParameter2, puppetParameter3);
                case Emh.EmhType.RadialPuppet:
                    return Functional(expParametersOptional, parameter, puppetParameter0);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Emh.EmhQualification Functional(VRCExpressionParameters expressionParametersOptional, params string[] parameters)
        {
            if (parameters.All(s => s == ""))
            {
                return Emh.EmhQualification.ElementIsAesthetic;
            }

            if (expressionParametersOptional == null)
            {
                return Emh.EmhQualification.ElementIsFunctional;
            }

            var allExpParams = expressionParametersOptional.parameters
                .Select(p => p.name)
                .Distinct()
                .Where(p => p != "")
                .ToList();

            var allAreValid = parameters
                .Where(s => !string.IsNullOrEmpty(s))
                .All(s => allExpParams.Contains(s));
            if (allAreValid)
            {
                return Emh.EmhQualification.ElementIsFunctional;
            }

            var allAreInvalid = parameters
                .Where(s => !string.IsNullOrEmpty(s))
                .All(s => !allExpParams.Contains(s));
            if (allAreInvalid)
            {
                return Emh.EmhQualification.ElementIsNonFunctional;
            }

            return Emh.EmhQualification.ElementIsPartiallyFunctional;
        }

        public ExpressionsMenuHierarchyItem[] AllActiveSubMenuItems()
        {
            if (type != Emh.EmhType.SubMenu) return new ExpressionsMenuHierarchyItem[0];

            return Enumerable.Range(0, transform.childCount)
                .Select(i => transform.GetChild(i))
                .Where(that => that.gameObject.activeSelf)
                .Select(that => that.GetComponent<ExpressionsMenuHierarchyItem>())
                .Where(that => that != null)
                .ToArray();
        }
    }
}
