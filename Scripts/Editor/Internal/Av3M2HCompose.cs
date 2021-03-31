using System;
using System.Collections.Generic;
using System.Linq;
using Hai.Av3MenuToHierarchy.Scripts.Components;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Hai.Av3MenuToHierarchy.Scripts.Editor.Internal
{
    public class Av3M2HCompose
    {
        private readonly Dictionary<HaiAv3MenuToHierarchyControl, VRCExpressionsMenu> _visited = new Dictionary<HaiAv3MenuToHierarchyControl, VRCExpressionsMenu>();
        private readonly VRCExpressionsMenu _mainAssetContainer;

        public Av3M2HCompose(VRCExpressionsMenu mainAssetContainer)
        {
            _mainAssetContainer = mainAssetContainer;
        }

        public void Compose(HaiAv3MenuToHierarchyControl compilerMainMenu, VRCExpressionsMenu compilerEasyTreeDestination)
        {
            Sweep(compilerMainMenu, compilerEasyTreeDestination);
            Build();
        }

        public string[] FindAssets()
        {
            return _visited.Values.Select(AssetDatabase.GetAssetPath).ToArray();
        }

        private void Sweep(HaiAv3MenuToHierarchyControl currentControl, VRCExpressionsMenu destination)
        {
            _visited.Add(currentControl, destination);

            foreach (Transform child in currentControl.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                var control = child.GetComponent<HaiAv3MenuToHierarchyControl>();
                if (control == null) continue;
                if (control.type != HaiAv3MenuToHierarchyControl.Av3M2HType.SubMenu) continue;

                switch (control.subMenuSource)
                {
                    case HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.HierarchyChildren:
                        SweepIfApplicable(control.transform);
                        break;
                    case HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.ExpressionMenuAsset:
                        break;
                    case HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.HierarchyReference:
                        SweepIfApplicable(control.subMenuHierarchyReference);
                        break;
                    case HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.FlattenedSubMenu:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void SweepIfApplicable(Transform containingTransform)
        {
            var reference = containingTransform != null ? containingTransform.GetComponent<HaiAv3MenuToHierarchyControl>() : null;
            if (reference != null && !_visited.ContainsKey(reference))
            {
                var newSubMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                newSubMenu.name = "_" + _visited.Count + "_" + reference.name;
                AssetDatabase.AddObjectToAsset(newSubMenu, _mainAssetContainer);
                Sweep(reference, newSubMenu);
            }
        }

        private void Build()
        {
            foreach (var pair in _visited)
            {
                var currentSubMenu = pair.Key;
                var destination = pair.Value;

                destination.controls.Clear();
                foreach (Transform child in currentSubMenu.transform)
                {
                    if (!child.gameObject.activeSelf) continue;

                    var childControl = child.GetComponent<HaiAv3MenuToHierarchyControl>();
                    if (childControl == null) continue;

                    destination.controls.Add(new VRCExpressionsMenu.Control
                    {
                        type = TypeIn(childControl.type),
                        name = childControl.ResolveName(),
                        icon = childControl.icon,
                        value = childControl.value,
                        parameter = new VRCExpressionsMenu.Control.Parameter
                        {
                            name = childControl.parameter
                        },
                        // style = control.style,
                        subMenu = SubMenu(childControl, _visited),
                        subParameters = SubParams(childControl),
                        labels = SubLabels(childControl),
                    });
                }
            }
        }

        private VRCExpressionsMenu.Control.ControlType TypeIn(HaiAv3MenuToHierarchyControl.Av3M2HType controlType)
        {
            switch (controlType)
            {
                case HaiAv3MenuToHierarchyControl.Av3M2HType.SubMenu:
                    return VRCExpressionsMenu.Control.ControlType.SubMenu;
                case HaiAv3MenuToHierarchyControl.Av3M2HType.Button:
                    return VRCExpressionsMenu.Control.ControlType.Button;
                case HaiAv3MenuToHierarchyControl.Av3M2HType.Toggle:
                    return VRCExpressionsMenu.Control.ControlType.Toggle;
                case HaiAv3MenuToHierarchyControl.Av3M2HType.TwoAxisPuppet:
                    return VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet;
                case HaiAv3MenuToHierarchyControl.Av3M2HType.FourAxisPuppet:
                    return VRCExpressionsMenu.Control.ControlType.FourAxisPuppet;
                case HaiAv3MenuToHierarchyControl.Av3M2HType.RadialPuppet:
                    return VRCExpressionsMenu.Control.ControlType.RadialPuppet;
                default:
                    throw new ArgumentOutOfRangeException(nameof(controlType), controlType, null);
            }
        }

        private VRCExpressionsMenu SubMenu(HaiAv3MenuToHierarchyControl currentControl, Dictionary<HaiAv3MenuToHierarchyControl, VRCExpressionsMenu> mappings)
        {
            if (currentControl.type != HaiAv3MenuToHierarchyControl.Av3M2HType.SubMenu) return null;
            switch (currentControl.subMenuSource)
            {
                case HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.HierarchyChildren:
                    return mappings[currentControl];
                case HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.ExpressionMenuAsset:
                    return currentControl.subMenuAsset;
                case HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.HierarchyReference:
                    var subControl = currentControl.subMenuHierarchyReference.GetComponent<HaiAv3MenuToHierarchyControl>();
                    return currentControl.subMenuHierarchyReference != null && subControl != null ? mappings[subControl] : null;
                case HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.FlattenedSubMenu:
                    return currentControl.subMenuAsset;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private VRCExpressionsMenu.Control.Parameter[] SubParams(HaiAv3MenuToHierarchyControl currentControl)
        {
            switch (currentControl.type)
            {
                case HaiAv3MenuToHierarchyControl.Av3M2HType.TwoAxisPuppet:
                    return new[]
                    {
                        new VRCExpressionsMenu.Control.Parameter { name = currentControl.puppetParameter0 },
                        new VRCExpressionsMenu.Control.Parameter { name = currentControl.puppetParameter1 },
                    };
                case HaiAv3MenuToHierarchyControl.Av3M2HType.FourAxisPuppet:
                    return new[]
                    {
                        new VRCExpressionsMenu.Control.Parameter { name = currentControl.puppetParameter0 },
                        new VRCExpressionsMenu.Control.Parameter { name = currentControl.puppetParameter1 },
                        new VRCExpressionsMenu.Control.Parameter { name = currentControl.puppetParameter2 },
                        new VRCExpressionsMenu.Control.Parameter { name = currentControl.puppetParameter3 },
                    };
                case HaiAv3MenuToHierarchyControl.Av3M2HType.RadialPuppet:
                    return new[]
                    {
                        new VRCExpressionsMenu.Control.Parameter { name = currentControl.puppetParameter0 },
                    };
                case HaiAv3MenuToHierarchyControl.Av3M2HType.SubMenu:
                case HaiAv3MenuToHierarchyControl.Av3M2HType.Button:
                case HaiAv3MenuToHierarchyControl.Av3M2HType.Toggle:
                    return new VRCExpressionsMenu.Control.Parameter[0];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private VRCExpressionsMenu.Control.Label[] SubLabels(HaiAv3MenuToHierarchyControl currentControl)
        {
            switch (currentControl.type)
            {
                case HaiAv3MenuToHierarchyControl.Av3M2HType.TwoAxisPuppet:
                    return new[]
                    {
                        Av3M2HDecompose.LabelOf(currentControl.puppetLabelUp), Av3M2HDecompose.LabelOf(currentControl.puppetLabelRight),
                    };
                case HaiAv3MenuToHierarchyControl.Av3M2HType.FourAxisPuppet:
                    return new[]
                    {
                        Av3M2HDecompose.LabelOf(currentControl.puppetLabelUp), Av3M2HDecompose.LabelOf(currentControl.puppetLabelRight), Av3M2HDecompose.LabelOf(currentControl.puppetLabelLeft), Av3M2HDecompose.LabelOf(currentControl.puppetLabelDown),
                    };
                case HaiAv3MenuToHierarchyControl.Av3M2HType.RadialPuppet:
                case HaiAv3MenuToHierarchyControl.Av3M2HType.SubMenu:
                case HaiAv3MenuToHierarchyControl.Av3M2HType.Button:
                case HaiAv3MenuToHierarchyControl.Av3M2HType.Toggle:
                    return new VRCExpressionsMenu.Control.Label[0];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
