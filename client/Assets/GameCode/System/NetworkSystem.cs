using UnityEngine;

public class NetworkSystem : MonoBehaviour
{
    NetworkService _service;

    void Start()
    {
        _service = new NetworkService();
        _service.Initialize();
    }
}
