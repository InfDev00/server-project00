using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ============================================================
// LoadingUI - 로딩 화면(IUI)
// 방 정원 충족(LoadingNotify) 후 전환되면 로딩을 수행하고, 완료 시
// LoadingComplete를 서버로 보낸다. 서버가 방 전원의 완료를 모아
// GameStart를 broadcast하면 그때 게임 화면으로 넘어간다(준비 배리어).
// ============================================================
public class LoadingUI : IUI
{
    public TMP_Text StatusText;
    public Slider LoadingBar;

    AsyncOperation _oper;

    public override void OnStart()
    {
        if (StatusText && LoadingBar)
        {
            // 전원 로딩 완료 시 서버가 보내는 게임 시작 통지 구독
            NetworkSystem.Instance.Register<GameStartNotifyPacket>(OnGameStart);

            gameObject.SetActive(true);
            StartCoroutine(LoadRoutine());
        }
    }

    public override void OnEnd()
    {
        NetworkSystem.Instance.Unregister<GameStartNotifyPacket>(OnGameStart);
        gameObject.SetActive(false);
    }

    // 실제 로딩(에셋/씬 등). 지금은 임시로 잠깐 대기.
    private IEnumerator LoadRoutine()
    {
        StatusText.text = "Loading...";

        _oper = SceneManager.LoadSceneAsync("Game Scene");
        _oper.allowSceneActivation = false;

        while (_oper.progress < 0.9f)
        {
            float pct = Mathf.Clamp01(_oper.progress / 0.9f);   // 0~0.9 → 0~1로 환산
            UpdateUI(pct, "Loading...");
            yield return null;
        }

        UpdateUI(1f, "Waiting for other players...");        // 로딩 완료 보고
        NetworkSystem.Instance.Send(NetworkSystem.Instance.CreatePacket<LoadingCompleteReqPacket>());

        Debug.Log("[Loading] 완료 → 완료 보고 전송");
    }

    // 전원 로딩 완료 → 게임 시작
    private void OnGameStart(GameStartNotifyPacket p)
    {
        Debug.Log($"[GameStart] room={p.RoomId} → 게임 시작");
        SystemManager.Instance.TurnPlayerID = p.TurnPlayerID;

        StartCoroutine(StartRoutine());
    }

    private IEnumerator StartRoutine()
    {
        _oper.allowSceneActivation = true;

        while (!_oper.isDone)
            yield return null;

        UISystem.Instance.Next();

        _oper = null;
        Debug.Log("[Loading] 씬 활성화 완료");
    }

    void UpdateUI(float pct, string text)
    {
        LoadingBar.value = pct;
        StatusText.text = $"{text} ({(int)(pct * 100)}%)";
    }
}
