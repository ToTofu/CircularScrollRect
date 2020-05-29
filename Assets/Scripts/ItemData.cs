
/// <summary>
/// Item数据实体类.
/// </summary>
public class ItemData {

    private string name;
    private string num;

    public string Name { get { return name; } set { name = value; } }
    public string Num { get { return num; } set { num = value; } }

    public ItemData() { }
    public ItemData(string name, string num)
    {
        this.name = name;
        this.num = num;
    }
}
