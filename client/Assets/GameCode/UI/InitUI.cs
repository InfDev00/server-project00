using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InitUI : IUI
{
    public TMP_InputField NicknameField;
    public Button StartButton;

    public override void OnStart()
    {
        if (NicknameField && StartButton)
        {
            NetworkSystem.Instance.Register<LoginAckPacket>(OnLoginAck);
            StartButton.onClick.AddListener(OnStartButtonClick);
        }
    }

    public override void OnEnd()
    {
        NetworkSystem.Instance.Unregister<LoginAckPacket>(OnLoginAck);
        gameObject.SetActive(false);
    }
    public void OnStartButtonClick()
    {
        var login = NetworkSystem.Instance.CreatePacket<LoginReqPacket>();
        login.Username = NicknameField.text;

        NetworkSystem.Instance.Send(login);
        StartButton.interactable = false;   // 중복 요청 방지
    }

    // 서버 응답 도착 — 메인 스레드에서 호출되므로 UI 조작 안전
    private void OnLoginAck(LoginAckPacket ack)
    {
        if (ack.Success)
        {
            Debug.Log("로그인 성공 → 화면 전환");

            UISystem.Instance.Next();
        }
        else
        {
            Debug.Log("로그인 실패");
            StartButton.interactable = true;
        }
    }
}
