//********************************** 无限循环列表(只生成可见Item) ***************************************
//author Tofu
//
//初始化:
//      Init(callBack)
//
//刷新整个列表(首次调用和数量变化时调用):
//      ShowAndUpdateList(int = 数量)
//
//清空所有Item:
//      ClearScrollRectAllItem()
//
//刷新单个项:
//      UpdateCell(int = 索引)
//
//刷新列表数据(无数量变化时调用):
//      UpdateList()
//
//执行 更新Item信息回调 函数:
//      ImplementActionMethod(GameObject = Item, int = Index)  //刷新列表.
//
//使用方法:
//      0.在Inspector赋值，Item预制体锚点需要是左上角.
//      1.调用 Init()函数 并传入带GameObject和int参数的回调函数 (如果跟Lua交互, 那么用闭包方式处理self, 然后再把Lua函数传进 Init )
//      2.调用 ShowAndUpdateList()函数 并传入Item数量.
//      3.列表滑动时每刷新出一个新的Item，都会回调初始化(Init)时所传的回调函数(m_itemUpdateCallBack).
//******************************************************************************************************

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Text;

public class UIUtils
{
    public static void SetActive(GameObject obj, bool isActive)
    {
        if (obj != null)
        {
            if (obj.activeSelf != isActive) { obj.SetActive(isActive); }
        }
    }
}

/// <summary>
/// 列表方向枚举.
/// </summary>
public enum ScrollRect_Direction : byte
{
    Horizontal,
    Vertical
}

/// <summary>
/// 预制体Item的Pivot枚举.
/// </summary>
public enum Item_Pivot : byte
{
    /// <summary>
    /// 左上角.
    /// </summary>
    LeftUp,    
    /// <summary>
    /// 中央.
    /// </summary>
    Center
}

/// <summary>
/// 无限循环的ScrollRect.
/// </summary>
public class CircularScrollRect : MonoBehaviour
{
    //CircularScrollRectEditor编辑器控制Inspector显示.
    public int m_Row = 1;                           //每行多少个Item 或 每列多少个Item.
    public float m_Spacing = 0f;                    //Item之间的间距.
    public string m_ItemName;                       //Item名称.(复制会出现(Clone)字样)
    public GameObject m_ItemObj;                    //指定的Item预制体.
    public ScrollRect_Direction m_Direction = ScrollRect_Direction.Horizontal;  //拖动方向.
    public Item_Pivot m_ItemPivot = Item_Pivot.LeftUp;
    public bool m_IsShowArrow = true;               //是否显示箭头.
    public GameObject m_PointingFirstArrow;         //指示箭头.
    public GameObject m_PointingEndArrow;           //指示箭头.

    #region Field
    private RectTransform m_RTrans;                 //RectTransfom.
    private ScrollRect m_ScrollRect;                //ScrollRect.

    private RectTransform m_Content_RTrans;         //content.
    private float m_Content_Width;                  //content的宽度.
    private float m_Content_Height;                 //content的高度.

    private float m_ItemObj_Width;                  //Item的宽度.
    private float m_ItemObj_Height;                 //Item的高度.

    private float m_PlaneWidth;
    private float m_PlaneHeight;

    private bool m_IsInit = false;                  //是否已初始化Init.
    private bool m_IsFirstShowList = false;         //是否第一次调用ShowList.

    /// <summary>
    /// 对象池.
    /// </summary>
    private Stack<GameObject> poolsObj = new Stack<GameObject>();

    /// <summary>
    /// 记录每个Item的坐标和物体.
    /// </summary>
    private struct ItemInfo
    {
        public Vector3 pos;                                 //对应位置.
        public GameObject obj;                              //对应物体.
    };
    private ItemInfo[] m_ItemInfos;                         //存放的Item数组.

    private int m_MaxCount = -1;                            //当前列表的总数量.
    private int m_MinIndex = -1;                            //当前显示区域的首位Index.
    private int m_MaxIndex = -1;                            //当前显示区域的末位Index.

    private bool m_IsClearList = false;                     //是否清空列表.

    private Action<GameObject, int> m_itemUpdateCallBack;   //刷新列表，Item数据更新回调<content内的Item物体, 对应Item索引>.
    #endregion

    void OnDestroy()
    {
        DisposeAll();
    }

    /// <summary>
    /// 初始化.
    /// </summary>
    /// <param name="callBack">Item数据更新回调</param>
    public void Init(Action<GameObject, int> callBack)
    {
        DisposeAll();       //清空所有回调.

        m_itemUpdateCallBack = callBack;

        FindAndInitField(); //只初始化一次，后续不再执行(查找、绑定事件相关).
    }

    /// <summary>
    /// 查找、绑定事件相关.
    /// </summary>
    private void FindAndInitField()
    {
        //只初始化一次. 
        if (m_IsInit) return;

        m_ScrollRect = this.GetComponent<ScrollRect>();

        //记录 Plane 信息
        m_RTrans = GetComponent<RectTransform>();
        Rect planeRect = m_RTrans.rect;
        m_PlaneHeight = planeRect.height;
        m_PlaneWidth = planeRect.width;

        //记录 Content 信息
        m_Content_RTrans = m_ScrollRect.content;
        m_Content_Height = m_Content_RTrans.rect.height;
        m_Content_Width = m_Content_RTrans.rect.width;
        m_Content_RTrans.pivot = new Vector2(0f, 1f);
        CheckAnchor(m_Content_RTrans);

        //Item 处理.
        if (m_ItemObj == null) { Debug.LogError("请赋值Item..."); }
        m_ItemName = m_ItemObj.name;
        RectTransform item_RTrans = m_ItemObj.GetComponent<RectTransform>();    //这里会直接修改prefab的值. 重用prefab的，最好在GetPoolsObj()中修改.
        if (m_ItemPivot == Item_Pivot.LeftUp)       //Item中心点在左上角.    
            { item_RTrans.pivot = new Vector2(0f, 1f); }
        else if (m_ItemPivot == Item_Pivot.Center)  //Item中心点在中央.
            { item_RTrans.pivot = new Vector2(0.5f, 0.5f); }
        CheckAnchor(item_RTrans);
        item_RTrans.anchoredPosition = Vector2.zero;

        //记录 Item 信息
        m_ItemObj_Height = item_RTrans.rect.height;
        m_ItemObj_Width = item_RTrans.rect.width;


        //绑定滑动事件. content移动，触发方法.
        m_ScrollRect.onValueChanged.RemoveAllListeners();
        m_ScrollRect.onValueChanged.AddListener((v2) => ScrollRectListener(v2));

        if (m_PointingFirstArrow != null || m_PointingEndArrow != null)
        {
            m_ScrollRect.onValueChanged.AddListener((v2) => ShowAndHideArrowListener(v2));
            ShowAndHideArrowListener(Vector2.zero);
        }

        //初始化完毕.
        m_IsInit = true;
    }

    /// <summary>
    /// 检查 Anchor 是否正确.
    /// </summary>
    /// <param name="rectTrans"></param>
    private void CheckAnchor(RectTransform rectTrans)
    {
        if (m_Direction == ScrollRect_Direction.Vertical)   //垂直方向.
        {
            if (!((rectTrans.anchorMin == new Vector2(0, 1) && rectTrans.anchorMax == new Vector2(0, 1)) ||
                (rectTrans.anchorMin == new Vector2(0, 1) && rectTrans.anchorMax == new Vector2(1, 1))))
            {
                rectTrans.anchorMin = new Vector2(0, 1);
                rectTrans.anchorMax = new Vector2(1, 1);
            }
        }
        else    //水平方向.
        {
            if (!((rectTrans.anchorMin == new Vector2(0, 1) && rectTrans.anchorMax == new Vector2(0, 1)) ||
                (rectTrans.anchorMin == new Vector2(0, 0) && rectTrans.anchorMax == new Vector2(0, 1))))
            {
                rectTrans.anchorMin = new Vector2(0, 0);
                rectTrans.anchorMax = new Vector2(0, 1);
            }
        }
    }

    /// <summary>
    /// 实时刷新列表时用
    /// </summary>
    public void UpdateList()
    {
        for (int i = 0, length = m_ItemInfos.Length; i < length; i++)
        {
            ItemInfo cellInfo = m_ItemInfos[i];
            if (cellInfo.obj != null)
            {
                float posXY = m_Direction == ScrollRect_Direction.Vertical ? cellInfo.pos.y : cellInfo.pos.x;
                if (!IsOutRange(posXY))
                {
                    ImplementActionMethod(m_itemUpdateCallBack, cellInfo.obj, true);
                }
            }
        }
    }

    /// <summary>
    /// 刷新某一项
    /// </summary>
    /// <param name="index"></param>
    public void UpdateCell(int index)
    {
        ItemInfo cellInfo = m_ItemInfos[index - 1];
        if (cellInfo.obj != null)
        {
            float posXY = m_Direction == ScrollRect_Direction.Vertical ? cellInfo.pos.y : cellInfo.pos.x;
            if (!IsOutRange(posXY))
            {
                ImplementActionMethod(m_itemUpdateCallBack, cellInfo.obj);
            }
        }
    }

    /// <summary>
    /// 刷新整个列表.(首次调用和数量变化时调用)
    /// </summary>
    /// <param name="num"></param>
    public void ShowAndUpdateList(int num)
    {
        //显示区域首末索引初始化.
        m_MinIndex = -1;
        m_MaxIndex = -1;

        //计算 Content 尺寸.
        CalcContentSize(num);

        //计算每个Item坐标并存储，且只显示范围内的Item.
        CalcAllItemInfo(num);

        //执行一次 检测箭头是否显示.
        ShowAndHideArrowListener(Vector2.zero);
    }

    /// <summary>
    /// 根据Item个数，计算 Content 总尺寸.
    /// </summary>
    /// <param name="num">Item个数</param>
    private void CalcContentSize(int num)
    {
        if (m_Direction == ScrollRect_Direction.Vertical)   //垂直方向.
        {
            float contentSize_y = (m_Spacing + m_ItemObj_Height) * Mathf.CeilToInt((float)num / m_Row);
            m_Content_Height = contentSize_y;
            m_Content_Width = m_Content_RTrans.sizeDelta.x;
            contentSize_y = contentSize_y < m_RTrans.rect.height ? m_RTrans.rect.height : contentSize_y;
            m_Content_RTrans.sizeDelta = new Vector2(m_Content_Width, contentSize_y);
            if (num != m_MaxCount)
            {
                m_Content_RTrans.anchoredPosition = new Vector2(m_Content_RTrans.anchoredPosition.x, 0);    //content返回原位.
            }
        }
        else    //水平方向.
        {
            float contentSize_x = (m_Spacing + m_ItemObj_Width) * Mathf.CeilToInt((float)num / m_Row);
            m_Content_Width = contentSize_x;
            m_Content_Height = m_Content_RTrans.sizeDelta.y;
            contentSize_x = contentSize_x < m_RTrans.rect.width ? m_RTrans.rect.width : contentSize_x;
            m_Content_RTrans.sizeDelta = new Vector2(contentSize_x, m_Content_Height);
            if (num != m_MaxCount)
            {
                m_Content_RTrans.anchoredPosition = new Vector2(0, m_Content_RTrans.anchoredPosition.y);    //content返回原位.
            }
        }
    }

    /// <summary>
    /// 计算每个Item坐标并存储，且只显示范围内的Item.
    /// </summary>
    /// <param name="num"></param>
    private void CalcAllItemInfo(int num)
    {
        //计算 开始索引.
        int lastEndIndex = 0;

        //过多的物体 扔到对象池. ( 首次调 ShowList函数时 则无效 )
        if (m_IsFirstShowList)
        {
            lastEndIndex = num - m_MaxCount > 0 ? m_MaxCount : num;
            lastEndIndex = m_IsClearList ? 0 : lastEndIndex;

            int count = m_IsClearList ? m_ItemInfos.Length : m_MaxCount;
            for (int i = lastEndIndex; i < count; i++)
            {
                if (m_ItemInfos[i].obj != null)
                {
                    EnterPoolsObj(m_ItemInfos[i].obj);
                    m_ItemInfos[i].obj = null;
                }
            }
        }

        //m_ItemInfos数组的赋值.
        ItemInfo[] tempCellInfos = m_ItemInfos; //临时Item数组.(首次调 ShowList函数时 不使用)
        m_ItemInfos = new ItemInfo[num];        //Item数组重置.等待赋值.
        for (int i = 0; i < num; i++)           //计算每个Item坐标并存储,只显示范围内的Item.(赋值m_ItemInfos)
        {
            #region 已生成的Item，直接更新ItemInfo数据即可. (首次调 ShowList函数时 则无效) (第一次执行后，Item个数没有大于之前总数，则不执行if外的代码)
            if (m_MaxCount != -1 && i < lastEndIndex)
            {
                ItemInfo tempItemInfo = tempCellInfos[i];

                //计算是否超出范围.
                float tempItemInfoPosXY = m_Direction == ScrollRect_Direction.Vertical ? tempItemInfo.pos.y : tempItemInfo.pos.x;
                if (!IsOutRange(tempItemInfoPosXY)) //没超出范围.
                {
                    //记录显示范围中的 首位index 和 末尾index.
                    m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex; //首位index.
                    m_MaxIndex = i;                                 //末尾index.

                    if (tempItemInfo.obj == null)
                    {
                        tempItemInfo.obj = GetPoolsObj();
                    }
                    tempItemInfo.obj.transform.GetComponent<RectTransform>().anchoredPosition = tempItemInfo.pos;
                    tempItemInfo.obj.name = m_ItemName + "_" + i.ToString();
                    tempItemInfo.obj.SetActive(true);

                    //更新Item信息回调.
                    ImplementActionMethod(m_itemUpdateCallBack, tempItemInfo.obj);
                }
                else //超出范围.
                {
                    EnterPoolsObj(tempItemInfo.obj); //进池.
                    tempItemInfo.obj = null;
                }
                m_ItemInfos[i] = tempItemInfo;
                continue;
            }
            #endregion

            #region 计算每个Item对应的itemInfo数据.

            ItemInfo itemInfo = new ItemInfo();

            float pos = 0;              //每一排的X或Y坐标值.( isVertical ? 记录Y : 记录X )
            float rowOrColumnPos = 0;   //计算每排里面的Item，X或Y坐标值.( isVertical ? 记录X : 记录Y )

            //计算每个Item的坐标.
            if (m_Direction == ScrollRect_Direction.Vertical)   //垂直方向.
            {
                pos = m_ItemObj_Height * Mathf.FloorToInt(i / m_Row) + m_Spacing * Mathf.FloorToInt(i / m_Row); //y.
                rowOrColumnPos = m_ItemObj_Width * (i % m_Row) + m_Spacing * (i % m_Row);                       //x.

                if (m_ItemPivot == Item_Pivot.LeftUp)       //中心点在Item左上角.
                    { itemInfo.pos = new Vector3(rowOrColumnPos, -pos, 0); }
                else if (m_ItemPivot == Item_Pivot.Center)  //中心点在Item中央.
                    { itemInfo.pos = new Vector3(rowOrColumnPos + m_ItemObj_Width / 2, -pos - m_ItemObj_Height / 2, 0); }
            }
            else    //水平方向.
            {
                pos = m_ItemObj_Width * Mathf.FloorToInt(i / m_Row) + m_Spacing * Mathf.FloorToInt(i / m_Row);  //x.
                rowOrColumnPos = m_ItemObj_Height * (i % m_Row) + m_Spacing * (i % m_Row);                      //y.

                if (m_ItemPivot == Item_Pivot.LeftUp)       //中心点在Item左上角.
                    { itemInfo.pos = new Vector3(pos, -rowOrColumnPos, 0); }
                else if (m_ItemPivot == Item_Pivot.Center)  //中心点在Item中央.
                    { itemInfo.pos = new Vector3(pos + m_ItemObj_Width / 2, -rowOrColumnPos - m_ItemObj_Height / 2, 0); }
            }

            //计算是否超出显示范围，超出范围ItemInfo.obj为null.
            float itemInfoPosXY = m_Direction == ScrollRect_Direction.Vertical ? itemInfo.pos.y : itemInfo.pos.x;
            if (IsOutRange(itemInfoPosXY)) 
            {
                itemInfo.obj = null;
                m_ItemInfos[i] = itemInfo;
                continue;       //Item超出content范围，不实例化.
            }

            //记录显示范围中的 首位index 和 末尾index.
            m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex; //首位index.
            m_MaxIndex = i;                                 //末尾index.

            //取或创建 Item.
            GameObject item = GetPoolsObj();
            item.transform.GetComponent<RectTransform>().anchoredPosition = itemInfo.pos;
            item.gameObject.name = m_ItemName + "_" + i.ToString();

            //存数据.
            itemInfo.obj = item;
            m_ItemInfos[i] = itemInfo;

            //更新Item信息回调.
            ImplementActionMethod(m_itemUpdateCallBack, item);
            #endregion
        }

        m_MaxCount = num;

        m_IsFirstShowList = true;
    }

    /// <summary>
    /// 清空所有Item.
    /// </summary>
    public void ClearScrollRectAllItem()
    {
        //未初始化，不需要清空Item.
        if (!m_IsInit) return; 

        //删除所有Item.
        Transform[] temp = m_Content_RTrans.GetComponentsInChildren<Transform>(true);
        List<GameObject> tempList = new List<GameObject>();
        for (int i = 1; i < temp.Length; i++)
        {
            if (temp[i].name.Contains(m_ItemName)) { tempList.Add(temp[i].gameObject); }
        }
        for (int i = 0; i < tempList.Count; i++)
        {
            GameObject.DestroyImmediate(tempList[i]);
        }

        //设置content位置.
        if (m_Direction == ScrollRect_Direction.Vertical)   //垂直方向.
        {
            m_Content_RTrans.anchoredPosition = new Vector2(m_Content_RTrans.anchoredPosition.x, 0); //返回原位.
        }
        else    //水平方向.
        {
            m_Content_RTrans.anchoredPosition = new Vector2(0, m_Content_RTrans.anchoredPosition.y); //返回原位.
        }
    }

    /// <summary>
    /// 更新滚动区域的大小.
    /// </summary>
    public void UpdateSize()
    {
        Rect rect = GetComponent<RectTransform>().rect;
        m_PlaneHeight = rect.height;
        m_PlaneWidth = rect.width;
    }

    /// <summary>
    /// 滑动content，触发事件.
    /// </summary>
    /// <param name="value"></param>
    private void ScrollRectListener(Vector2 value)
    {
        UpdateCheck();
    }

    /// <summary>
    /// 滑动content，更新Item.
    /// </summary>
    private void UpdateCheck()
    {
        if (m_ItemInfos == null) return;  //没有Item，不执行.
           
        //检查超出范围
        for (int i = 0, length = m_ItemInfos.Length; i < length; i++)
        {
            ItemInfo itemInfo = m_ItemInfos[i];
            GameObject obj = itemInfo.obj;
            Vector3 pos = itemInfo.pos;

            float posXY = m_Direction == ScrollRect_Direction.Vertical ? pos.y : pos.x;
            if (IsOutRange(posXY)) //超出显示范围.
            {
                //把超出范围的item 扔进 poolsObj里.
                if (obj != null)
                {
                    EnterPoolsObj(obj);
                    m_ItemInfos[i].obj = null;
                }
            }
            else    //在显示范围内.
            {
                //已在范围内的Item，不需要设置.
                if (obj == null)    
                {
                    GameObject item = GetPoolsObj();    //优先从 poolsObj中 取出. （poolsObj为空则返回 实例化的item）
                    item.transform.localPosition = pos;
                    item.gameObject.name = m_ItemName + "_" + i.ToString();
                    m_ItemInfos[i].obj = item;

                    ImplementActionMethod(m_itemUpdateCallBack, item);
                }
            }
        }
    }

    /// <summary>
    /// 判断是否超出显示范围.
    /// </summary>
    /// <param name="posXY">垂直为Y，水平为X</param>
    /// <returns></returns>
    private bool IsOutRange(float posXY)
    {
        Vector3 contentPos = m_Content_RTrans.anchoredPosition;

        #region if Item中心点在左上角.
        if (m_ItemPivot == Item_Pivot.LeftUp)                   //Item中心点在左上角.
        {
            if (m_Direction == ScrollRect_Direction.Vertical)   //垂直方向.
            {
                if (posXY + contentPos.y > m_ItemObj_Height ||         //上方超出范围.
                    posXY + contentPos.y < -m_RTrans.rect.height)      //下方超出范围.
                {
                    return true;
                }
            }
            else    //水平方向.
            { 
                if (posXY + contentPos.x < -m_ItemObj_Width ||         //左方超出范围.
                    posXY + contentPos.x > m_RTrans.rect.width)        //右方超出范围.
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region else if Item中心点在中央.
        else if (m_ItemPivot == Item_Pivot.Center)  //Item中心点在中央.
        {
            if (m_Direction == ScrollRect_Direction.Vertical)   //垂直方向.
            {
                if (posXY + (m_ItemObj_Height / 2) + contentPos.y > m_ItemObj_Height ||        //上方超出范围.
                    posXY + (m_ItemObj_Height / 2) + contentPos.y < -m_RTrans.rect.height)     //下方超出范围.
                {
                    return true;
                }
            }
            else    //水平方向.
            {
                if (posXY + (m_ItemObj_Width / 2) + contentPos.x < -m_ItemObj_Width ||        //左方超出范围.
                    posXY - (m_ItemObj_Width / 2) + contentPos.x > m_RTrans.rect.width)       //右方超出范围.
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region else Item中心点在其他位置.
        else    //Item中心点在其他位置.Obsolete
        {
            return true;
        }
        #endregion
    }

    /// <summary>
    /// 取对象池Item.
    /// </summary>
    /// <returns></returns>
    private GameObject GetPoolsObj()
    {
        GameObject item = null;
        if (poolsObj.Count > 0)
        {
            item = poolsObj.Pop();
        }

        if (item == null)
        {
            item = GameObject.Instantiate<GameObject>(m_ItemObj);
        }
        item.transform.SetParent(m_Content_RTrans, false);
        item.transform.localScale = Vector3.one;
        UIUtils.SetActive(item, true);

        return item;
    }

    /// <summary>
    /// 存入Item.
    /// </summary>
    /// <param name="item"></param>
    private void EnterPoolsObj(GameObject item)
    {
        if (item != null)
        {
            poolsObj.Push(item);
            UIUtils.SetActive(item, false);
        }
    }

    /// <summary>
    /// 执行 更新Item信息回调 方法.
    /// </summary>
    /// <param name="action">更新Item信息回调</param>
    /// <param name="selectObject"></param>
    /// <param name="isUpdate"></param>
    private void ImplementActionMethod(Action<GameObject, int> action, GameObject selectObject, bool isUpdate = false)
    {
        //根据名字获取对应索引.
        int num = int.Parse(selectObject.name.Replace(m_ItemName + "_", ""));   

        if (action != null)
        {
            action(selectObject, num);
        }
    }

    /// <summary>
    /// 清空所有回调.
    /// </summary>
    private void DisposeAll()
    {
        if (m_itemUpdateCallBack != null)
        {
            m_itemUpdateCallBack = null;
        }
    }

    /// <summary>
    /// 滑动content，触发检测箭头是否显示.
    /// </summary>
    /// <param name="value"></param>
    private void ShowAndHideArrowListener(Vector2 value)
    {
        float normalizedPos = m_Direction == ScrollRect_Direction.Vertical ? m_ScrollRect.verticalNormalizedPosition : m_ScrollRect.horizontalNormalizedPosition;
        //Debug.Log(normalizedPos);

        if (m_Direction == ScrollRect_Direction.Vertical)
        {
            if (m_Content_Height - m_RTrans.rect.height < 10)
            {
                UIUtils.SetActive(m_PointingFirstArrow, false);
                UIUtils.SetActive(m_PointingEndArrow, false);
                return;
            }
        }
        else
        {
            if (m_Content_Width - m_RTrans.rect.width < 10)
            {
                UIUtils.SetActive(m_PointingFirstArrow, false);
                UIUtils.SetActive(m_PointingEndArrow, false);
                return;
            }
        }

        if (normalizedPos >= 0.9)
        {
            UIUtils.SetActive(m_PointingFirstArrow, false);
            UIUtils.SetActive(m_PointingEndArrow, true);
        }
        else if (normalizedPos <= 0.1)
        {
            UIUtils.SetActive(m_PointingFirstArrow, true);
            UIUtils.SetActive(m_PointingEndArrow, false);
        }
        else
        {
            UIUtils.SetActive(m_PointingFirstArrow, true);
            UIUtils.SetActive(m_PointingEndArrow, true);
        }
    }
}

