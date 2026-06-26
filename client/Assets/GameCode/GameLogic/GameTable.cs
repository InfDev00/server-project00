using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameTable : MonoBehaviour
{
    public PlayerHand MyHand;
    public PlayerHand OtherHand;

    public Button RollButton;
    public Button StopButton;
    public TMP_Text RollNumber;

    private void Start()
    {
        if (MyHand & OtherHand & RollButton & StopButton & RollNumber)
        {
            MyHand.SetNumber(0);
            OtherHand.SetNumber(0);

            RollNumber.text = "";
            RollButton.onClick.AddListener(OnClickRollButton);
            StopButton.onClick.AddListener(OnClickStopButton);

            ActiveButton(SystemManager.Instance.IsMyTurn());

            NetworkSystem.Instance.Register<TurnResultNotifyPacket>(OnTurnResult);
        }
    }

    private void OnDestroy()
    {
        if (NetworkSystem.Instance != null)
            NetworkSystem.Instance.Unregister<TurnResultNotifyPacket>(OnTurnResult);
    }

    private void OnClickRollButton()
    {
        ActiveButton(false);   // 중복 전송 방지 — 응답/턴 갱신 전까지 비활성화

        var packet = Packet.Create<UserSelectReqPacket>();
        packet.Stop = false;

        NetworkSystem.Instance.Send(packet);
    }

    private void OnClickStopButton()
    {
        ActiveButton(false);   // 중복 전송 방지

        var packet = Packet.Create<UserSelectReqPacket>();
        packet.Stop = true;

        NetworkSystem.Instance.Send(packet);
    }

    private void ActiveButton(bool active)
    {
        RollButton.interactable = active;
        StopButton.interactable = active;
    }

    private void OnTurnResult(TurnResultNotifyPacket p)
    {
        // 굴린 결과 표시
        RollNumber.text = p.Result.ToString();

        // 굴린 플레이어의 누적 합으로 손패 갱신 (서버 권위값)
        if (p.UID == SystemManager.Instance.MyID)
            MyHand.SetNumber(p.Total);
        else
            OtherHand.SetNumber(p.Total);

        if (p.IsGameOver == 1)
        {
            // 승자 ID로 승패 표시 (-1=무승부)
            if (p.WinnerID == -1)
                RollNumber.text = "DRAW";
            else
                RollNumber.text = (p.WinnerID == SystemManager.Instance.MyID) ? "WIN" : "LOSE";
            ActiveButton(false);
            return;
        }

        // 턴 갱신 → 내 턴일 때만 버튼 활성화
        SystemManager.Instance.TurnPlayerID = p.TurnPlayerID;
        ActiveButton(SystemManager.Instance.IsMyTurn());
    }
}