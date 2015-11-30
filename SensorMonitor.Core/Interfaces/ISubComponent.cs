using System;
using SensorMonitor.Core.Event;

namespace SensorMonitor.Core.Interfaces
{
    public interface ISubComponent : IDisposable
    {
        bool Initialize();
        bool ConnectBus(BusHub busHub);
        // 스레드를 잠시 멈춘다.
        void Pause();
        // 스레드를 재개한다.
        void Resume();
        // 호출시 해당 스레드를 멈추고 종료한다.
        new void Dispose();
        // 로그를 출력하기 위한 이벤트 핸들러
        // 해당 핸들러의 정의는 ClientCore 에서 붙여준다.
        event EventHandler Log;
        // 해당 로그를 발송하기 위한 편의 함수
        void L(string message, LogEvt.MessageType type);
    }
}
