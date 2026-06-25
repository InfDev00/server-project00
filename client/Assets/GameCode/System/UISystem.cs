using System.Collections.Generic;
using UnityEngine;

public class UISystem : MonoBehaviour
{
    static UISystem _instance;
    public static UISystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindFirstObjectByType<UISystem>();
            }
            return _instance;
        }
    }

    public List<IUI> UI_List = new List<IUI>();

    void Start()
    {
        if (UI_List.Count > 0)
        {
            UI_List[0].OnStart();
        }
    }

    public void Next()
    {
        if (UI_List.Count > 0)
        {
            var cur = UI_List[0];
            cur.OnEnd();
            UI_List.RemoveAt(0);

            if (UI_List.Count > 0)
                UI_List[0].OnStart();
        }
    }
}

public abstract class IUI : MonoBehaviour
{
    public abstract void OnStart();
    public abstract void OnEnd();
}