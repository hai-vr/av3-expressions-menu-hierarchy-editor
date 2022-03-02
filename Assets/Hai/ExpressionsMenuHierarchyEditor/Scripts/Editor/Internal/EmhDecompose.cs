using System;
using System.Collections.Generic;
using Hai.ExpressionsMenuHierarchyEditor.Scripts.Components;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Hai.ExpressionsMenuHierarchyEditor.Scripts.Editor.Internal
{
    public class EmhDecompose
    {
        private readonly Dictionary<VRCExpressionsMenu, Transform> _visited = new Dictionary<VRCExpressionsMenu, Transform>();

        public static VRCExpressionsMenu.Control.Label LabelOf(Emh.EmhPuppetLabel label)
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

                var m2h = gameObject.AddComponent<ExpressionsMenuHierarchyItem>();
                m2h.nameSource = control.name.Trim() == "" && control.icon != null && control.icon.name != ""
                    ? Emh.EmhNameSource.UseCustomName
                    : Emh.EmhNameSource.UseHierarchyObjectName;
                m2h.customName = m2h.nameSource == Emh.EmhNameSource.UseCustomName ? control.name : "";

                m2h.type = TypeOf(control);
                m2h.icon = control.icon;
                m2h.parameter = control.parameter.name;
                m2h.value = control.value;

                var subParamsLength = DefensiveArrayIfNullable(control.subParameters).Length;
                m2h.puppetParameter0 = subParamsLength > 0 ? control.subParameters[0]?.name : "";
                m2h.puppetParameter1 = subParamsLength > 1 ? control.subParameters[1]?.name : "";
                m2h.puppetParameter2 = subParamsLength > 2 ? control.subParameters[2]?.name : "";
                m2h.puppetParameter3 = subParamsLength > 3 ? control.subParameters[3]?.name : "";

                var labelLength = DefensiveArrayIfNullable(control.labels).Length;
                m2h.puppetLabelUp.icon = labelLength > 0 ? control.labels[0].icon : null;
                m2h.puppetLabelUp.name = labelLength > 0 ? control.labels[0].name : "";
                m2h.puppetLabelRight.icon = labelLength > 1 ? control.labels[1].icon : null;
                m2h.puppetLabelRight.name = labelLength > 1 ? control.labels[1].name : "";
                m2h.puppetLabelDown.icon = labelLength > 2 ? control.labels[2].icon : null;
                m2h.puppetLabelDown.name = labelLength > 2 ? control.labels[2].name : "";
                m2h.puppetLabelLeft.icon = labelLength > 3 ? control.labels[3].icon : null;
                m2h.puppetLabelLeft.name = labelLength > 3 ? control.labels[3].name : "";

                if (m2h.type == Emh.EmhType.SubMenu)
                {
                    var subMenuAsset = control.subMenu;
                    m2h.subMenuAsset = subMenuAsset;

                    if (subMenuAsset == null)
                    {
                        m2h.subMenuSource = Emh.EmhSubMenuSource.HierarchyReference;
                    }
                    else
                    {
                        if (_visited.ContainsKey(subMenuAsset))
                        {
                            m2h.subMenuSource = Emh.EmhSubMenuSource.HierarchyReference;
                            m2h.subMenuHierarchyReference = _visited[subMenuAsset];
                        }
                        else
                        {
                            m2h.subMenuSource = Emh.EmhSubMenuSource.HierarchyChildren;
                            Decompose(subMenuAsset, gameObject.transform);
                        }
                    }
                }

                // m2h.subTitle = "";
                // m2h.documentation = "";
            }
        }

        private T[] DefensiveArrayIfNullable<T>(T[] array)
        {
            return array == null ? new T[0] : array;
        }

        private static Emh.EmhType TypeOf(VRCExpressionsMenu.Control control)
        {
            switch (control.type)
            {
                case VRCExpressionsMenu.Control.ControlType.Button:
                    return Emh.EmhType.Button;
                case VRCExpressionsMenu.Control.ControlType.Toggle:
                    return Emh.EmhType.Toggle;
                case VRCExpressionsMenu.Control.ControlType.SubMenu:
                    return Emh.EmhType.SubMenu;
                case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet:
                    return Emh.EmhType.TwoAxisPuppet;
                case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet:
                    return Emh.EmhType.FourAxisPuppet;
                case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
                    return Emh.EmhType.RadialPuppet;
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
