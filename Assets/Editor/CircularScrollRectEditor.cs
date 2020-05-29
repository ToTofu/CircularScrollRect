using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 无限循环列表 定制编辑器.
/// </summary>
[CustomEditor(typeof(CircularScrollRect))]
public class CircularScrollRectEditor : Editor
{
    private CircularScrollRect m_CSR;

    public override void OnInspectorGUI()
    {
        m_CSR = (CircularScrollRect)base.target;

        m_CSR.m_Direction = (ScrollRect_Direction)EditorGUILayout.EnumPopup("方向: ", m_CSR.m_Direction);
        m_CSR.m_ItemPivot = (Item_Pivot)EditorGUILayout.EnumPopup("Item的Pivot：", m_CSR.m_ItemPivot); 

        m_CSR.m_Row = EditorGUILayout.IntField("行或列 个数: ", m_CSR.m_Row);
        m_CSR.m_Spacing = EditorGUILayout.FloatField("间距: ", m_CSR.m_Spacing);
        m_CSR.m_ItemObj = (GameObject)EditorGUILayout.ObjectField("Item预制体: ", m_CSR.m_ItemObj, typeof(GameObject), true);
        m_CSR.m_IsShowArrow = EditorGUILayout.ToggleLeft("是否显示指示箭头", m_CSR.m_IsShowArrow);
        if (m_CSR.m_IsShowArrow)
        {
            m_CSR.m_PointingFirstArrow = (GameObject)EditorGUILayout.ObjectField("上或右 箭头指示物体: ", m_CSR.m_PointingFirstArrow, typeof(GameObject), true);
            m_CSR.m_PointingEndArrow = (GameObject)EditorGUILayout.ObjectField("下或左 箭头指示物体: ", m_CSR.m_PointingEndArrow, typeof(GameObject), true);
        }
    }
}
