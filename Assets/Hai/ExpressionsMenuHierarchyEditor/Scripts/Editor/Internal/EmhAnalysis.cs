using System;
using System.Collections.Generic;
using Hai.ExpressionsMenuHierarchyEditor.Scripts.Components;
using UnityEngine;

namespace Hai.ExpressionsMenuHierarchyEditor.Scripts.Editor.Internal
{
    public class EmhAnalysis
    {
        private readonly List<ExpressionsMenuHierarchyItem> _visited = new List<ExpressionsMenuHierarchyItem>();

        public ExpressionsMenuHierarchyItem[] Analyze(ExpressionsMenuHierarchyItem mainMenu)
        {
            return Sweep(mainMenu);
        }

        private ExpressionsMenuHierarchyItem[] Sweep(ExpressionsMenuHierarchyItem currentItem)
        {
            _visited.Add(currentItem);

            foreach (var item in currentItem.AllActiveSubMenuItems())
            {
                if (item.type != Emh.EmhType.SubMenu) continue;

                switch (item.subMenuSource)
                {
                    case Emh.EmhSubMenuSource.HierarchyChildren:
                        SweepIfApplicable(item.transform);
                        break;
                    case Emh.EmhSubMenuSource.ExpressionMenuAsset:
                        break;
                    case Emh.EmhSubMenuSource.HierarchyReference:
                        SweepIfApplicable(item.subMenuHierarchyReference);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return _visited.ToArray();
        }

        private void SweepIfApplicable(Transform containingTransform)
        {
            var reference = containingTransform != null ? containingTransform.GetComponent<ExpressionsMenuHierarchyItem>() : null;
            if (reference != null && !_visited.Contains(reference))
            {
                // var newSubMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                // newSubMenu.name = "_" + _visited.Count + "_" + reference.name;
                // AssetDatabase.AddObjectToAsset(newSubMenu, _mainAssetContainer);
                Sweep(reference);
            }
        }
    }
}