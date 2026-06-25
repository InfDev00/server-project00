using UnityEngine;

public class SystemManager : MonoBehaviour
{
    static SystemManager _instance;
    public static SystemManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindFirstObjectByType<SystemManager>();
            }
            return _instance;
        }
    }

    public int TurnPlayerID;

    void Start()
    {
        DontDestroyOnLoad(this);
    }
}
