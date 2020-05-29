using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour {

    private Transform m_Transform;
    private Transform m_ScrollView;
    private CircularScrollRect m_CSR;

    private Button addBTN;
    private Button closeBTN;
    private Button showBTN;

    private List<ItemData> m_DataList;  //数据集合.

    private int num = 0;

	void Start ()
    {
        //查找.
        m_Transform = gameObject.GetComponent<Transform>();
        m_ScrollView = m_Transform.Find("Scroll View").GetComponent<Transform>();
        m_CSR = m_ScrollView.GetComponent<CircularScrollRect>();
        addBTN = m_Transform.Find("Title/Add_BTN").GetComponent<Button>();
        closeBTN = m_Transform.Find("Title/Close_BTN").GetComponent<Button>();
        showBTN = m_Transform.Find("Title/Init_BTN").GetComponent<Button>();
        m_DataList = new List<ItemData>();

        //绑定按钮点击事件.
        addBTN.onClick.AddListener(AddItemMethod);
        closeBTN.onClick.AddListener(CloseScrollViewMethod);
        showBTN.onClick.AddListener(ShowScrollViewMethod);

        //伪造Item个数和数据信息.
        m_DataList.Add(new ItemData("壹", "1"));
        m_DataList.Add(new ItemData("贰", "2"));
        m_DataList.Add(new ItemData("叁", "3"));
        m_DataList.Add(new ItemData("肆", "4"));
        m_DataList.Add(new ItemData("伍", "5"));
        m_DataList.Add(new ItemData("陆", "6"));
        m_DataList.Add(new ItemData("柒", "7"));
        m_DataList.Add(new ItemData("捌", "8"));
        m_DataList.Add(new ItemData("玖", "9"));
        m_DataList.Add(new ItemData("拾", "10"));

        //生成Item.
        //m_CSR.Init((item, index) => item.GetComponent<ItemCtrl>().Init(m_DataList[index].Name, m_DataList[index].Num));
        m_CSR.Init(UpdateItemMethod);
        m_CSR.ShowAndUpdateList(m_DataList.Count);
	}

    void Update()
    {
        //新增Item.
        if (Input.GetKeyDown(KeyCode.A))
        {
            AddItemMethod();
        }

        //关闭Item.
        if (Input.GetKeyDown(KeyCode.C))
        {
            CloseScrollViewMethod();
        }

        //显示Item.
        if (Input.GetKeyDown(KeyCode.I))
        {
            ShowScrollViewMethod();
        }
    }

    /// <summary>
    /// 显示Item.
    /// </summary>
    private void ShowScrollViewMethod()
    {
        if (!m_ScrollView.gameObject.activeSelf) { m_ScrollView.gameObject.SetActive(true); }

        m_CSR.Init(UpdateItemMethod);
        m_CSR.ShowAndUpdateList(m_DataList.Count);
    }

    /// <summary>
    /// 关闭Item.
    /// </summary>
    private void CloseScrollViewMethod()
    {
        m_CSR.ClearScrollRectAllItem();

        if (m_ScrollView.gameObject.activeSelf) { m_ScrollView.gameObject.SetActive(false); }
    }

    /// <summary>
    /// 新增Item.
    /// </summary>
    private void AddItemMethod()
    {
        num++;
        m_DataList.Add(new ItemData("新增_" + num, num.ToString()));

        m_CSR.ShowAndUpdateList(m_DataList.Count);
    }

    /// <summary>
    /// Item信息更新回调.
    /// </summary>
    /// <param name="item">content内的Item物体</param>
    /// <param name="index">对应Item索引</param>
    private void UpdateItemMethod(GameObject item, int index)
    {
        item.GetComponent<ItemCtrl>().Init(m_DataList[index].Name, m_DataList[index].Num);
    }
}
