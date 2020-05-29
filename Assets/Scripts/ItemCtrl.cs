using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Item.
/// </summary>
public class ItemCtrl : MonoBehaviour {

    private Transform m_Transform;
    private Text m_Name;
    private Text m_Num;
    private Button m_Button;

	void Awake () 
    {
        m_Transform = gameObject.GetComponent<Transform>();
        m_Name = m_Transform.Find("Name").GetComponent<Text>();
        m_Num = m_Transform.Find("Num").GetComponent<Text>();
        m_Button = m_Transform.GetComponent<Button>();

        //m_Button.onClick.AddListener(() => Debug.Log("点击了：" + m_Name.text));
	}
	
    /// <summary>
    /// 初始化.
    /// </summary>
    /// <param name="num"></param>
    public void Init(string name, string num)
    {
        m_Name.text = name;
        m_Num.text = num;

        m_Button.onClick.RemoveAllListeners();
        m_Button.onClick.AddListener(() => Debug.Log("点击了：" + m_Name.text));
    }

}
