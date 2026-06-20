using System.Net.Sockets;

namespace Shared.Common;

// SocketAsyncEventArgs(SAEA) 객체를 미리 만들어 보관해 두는 풀(pool).
//
// SAEA는 비동기 송수신 1건을 표현하는 "작업 주문서"인데, 연결마다 새로 만들면
// GC 부담이 크다. 그래서 서버 시작 시 최대 동접 수만큼 미리 만들어 쌓아두고
// 접속할 때 Pop(꺼내 쓰고), 끊을 때 Push(돌려놓는)로 재사용한다.
//
// 여러 IOCP 워커 스레드가 동시에 Pop/Push 할 수 있으므로 lock으로 보호한다.
public class SocketAsyncEventArgsPool
{
    Stack<SocketAsyncEventArgs> pool_;

    public int Count => pool_.Count;

    public SocketAsyncEventArgsPool(int capacity)
    {
        pool_ = new Stack<SocketAsyncEventArgs>(capacity);
    }

    // 사용이 끝난 SAEA를 풀에 돌려놓는다.
    public void Push(SocketAsyncEventArgs item)
    {
        if (item != null)
        {
            lock (pool_)
            {
                pool_.Push(item);
            }
        }
    }

    // 풀에서 SAEA 하나를 꺼낸다. (풀이 비어 있으면 예외 — 동접 한도를 넘은 상황)
    public SocketAsyncEventArgs Pop()
    {
        lock (pool_)
        {
            return pool_.Pop();
        }
    }
}
