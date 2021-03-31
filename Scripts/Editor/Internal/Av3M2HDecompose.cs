using System;
using System.Collections.Generic;
using Hai.Av3MenuToHierarchy.Scripts.Components;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Hai.Av3MenuToHierarchy.Scripts.Editor.Internal
{
    public class Av3M2HDecompose
    {
        private readonly Dictionary<VRCExpressionsMenu, Transform> _visited = new Dictionary<VRCExpressionsMenu, Transform>();
        private readonly HaiAv3MenuToHierarchyCompiler.Av3M2HManagement _management;
        private readonly Transform _compilerTransform;

        public Av3M2HDecompose(HaiAv3MenuToHierarchyCompiler.Av3M2HManagement management, Transform compilerTransform)
        {
            _management = management;
            _compilerTransform = compilerTransform;
        }

        public static VRCExpressionsMenu.Control.Label LabelOf(HaiAv3MenuToHierarchyControl.Av3M2HPuppetLabel label)
        {
            return new VRCExpressionsMenu.Control.Label { name = label.name, icon = label.icon };
        }

        public void Decompose(VRCExpressionsMenu visiting, Transform currentTransform)
        {
            _visited.Add(visiting, currentTransform);
            foreach (var control in visiting.controls)
            {
                var hierarchyName = control.name.Trim() != "" ? control.name : control.icon != null ? $"({control.icon.name})" : control.name;

                var gameObject = NewGameObject(hierarchyName, currentTransform);

                var m2h = gameObject.AddComponent<HaiAv3MenuToHierarchyControl>();
                m2h.nameSource = control.name.Trim() == "" && control.icon != null && control.icon.name != ""
                    ? HaiAv3MenuToHierarchyControl.Av3M2HNameSource.UseCustomName
                    : HaiAv3MenuToHierarchyControl.Av3M2HNameSource.UseHierarchyObjectName;
                m2h.customName = m2h.nameSource == HaiAv3MenuToHierarchyControl.Av3M2HNameSource.UseCustomName ? control.name : "";

                m2h.type = TypeOf(control);
                m2h.icon = control.icon;
                m2h.parameter = control.parameter.name;
                m2h.value = control.value;

                var subParamsLength = control.subParameters.Length;
                m2h.puppetParameter0 = subParamsLength > 0 ? control.subParameters[0]?.name : "";
                m2h.puppetParameter1 = subParamsLength > 1 ? control.subParameters[1]?.name : "";
                m2h.puppetParameter2 = subParamsLength > 2 ? control.subParameters[2]?.name : "";
                m2h.puppetParameter3 = subParamsLength > 3 ? control.subParameters[3]?.name : "";

                var labelLength = control.labels.Length;
                m2h.puppetLabelUp.icon = labelLength > 0 ? control.labels[0].icon : null;
                m2h.puppetLabelUp.name = labelLength > 0 ? control.labels[0].name : "";
                m2h.puppetLabelRight.icon = labelLength > 1 ? control.labels[1].icon : null;
                m2h.puppetLabelRight.name = labelLength > 1 ? control.labels[1].name : "";
                m2h.puppetLabelDown.icon = labelLength > 2 ? control.labels[2].icon : null;
                m2h.puppetLabelDown.name = labelLength > 2 ? control.labels[2].name : "";
                m2h.puppetLabelLeft.icon = labelLength > 3 ? control.labels[3].icon : null;
                m2h.puppetLabelLeft.name = labelLength > 3 ? control.labels[3].name : "";

                if (m2h.type == HaiAv3MenuToHierarchyControl.Av3M2HType.SubMenu)
                {
                    var subMenuAsset = control.subMenu;
                    m2h.subMenuAsset = subMenuAsset;

                    if (subMenuAsset == null)
                    {
                        m2h.subMenuSource = _management == HaiAv3MenuToHierarchyCompiler.Av3M2HManagement.SimpleTree ? HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.HierarchyReference : HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.ExpressionMenuAsset;
                    }
                    else if (_management == HaiAv3MenuToHierarchyCompiler.Av3M2HManagement.SimpleTree)
                    {
                        if (_visited.ContainsKey(subMenuAsset))
                        {
                            m2h.subMenuSource = HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.HierarchyReference;
                            m2h.subMenuHierarchyReference = _visited[subMenuAsset];
                        }
                        else
                        {
                            m2h.subMenuSource = HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.HierarchyChildren;
                            Decompose(subMenuAsset, gameObject.transform);
                        }
                    }
                    else
                    {
                        m2h.subMenuSource = HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.ExpressionMenuAsset;
                        if (!_visited.ContainsKey(subMenuAsset))
                        {
                            var expressionMenu = NewGameObject(subMenuAsset.name, _compilerTransform);
                            var derivedMenu = expressionMenu.AddComponent<HaiAv3MenuToHierarchyControl>();
                            derivedMenu.subMenuAsset = subMenuAsset;
                            derivedMenu.subMenuSource = HaiAv3MenuToHierarchyControl.Av3M2HSubMenuSource.FlattenedSubMenu;

                            Decompose(subMenuAsset, derivedMenu.transform);
                        }
                    }
                }

                m2h.subTitle = "";
                m2h.documentation = "";
            }
        }

        private static HaiAv3MenuToHierarchyControl.Av3M2HType TypeOf(VRCExpressionsMenu.Control control)
        {
            switch (control.type)
            {
                case VRCExpressionsMenu.Control.ControlType.Button:
                    return HaiAv3MenuToHierarchyControl.Av3M2HType.Button;
                case VRCExpressionsMenu.Control.ControlType.Toggle:
                    return HaiAv3MenuToHierarchyControl.Av3M2HType.Toggle;
                case VRCExpressionsMenu.Control.ControlType.SubMenu:
                    return HaiAv3MenuToHierarchyControl.Av3M2HType.SubMenu;
                case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet:
                    return HaiAv3MenuToHierarchyControl.Av3M2HType.TwoAxisPuppet;
                case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet:
                    return HaiAv3MenuToHierarchyControl.Av3M2HType.FourAxisPuppet;
                case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
                    return HaiAv3MenuToHierarchyControl.Av3M2HType.RadialPuppet;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static GameObject NewGameObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(gameObject, "x");
            return gameObject;
        }
    }
}
