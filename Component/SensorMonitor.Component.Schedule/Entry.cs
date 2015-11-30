using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SensorMonitor.Core;
using SensorMonitor.Core.Event;
using SensorMonitor.Core.Interfaces;

namespace SensorMonitor.Component.Schedule
{
    public class Entry : ISubComponent
    {
        #region Class Specific
        public string NameSpace = "SensorMonitor.Component.Schedule.Entry";

        private BusHub _busHub;

        private bool _loopContinue;

        public Entry()
        {
            Log = null;
            _loopContinue = true;
        }

        private void Run()
        {
            while (_loopContinue)
            {
            }
        }
        #endregion


        #region ISubCompoenet Implement
        public event EventHandler Log;

        public bool ConnectBus(BusHub busHub)
        {
            _busHub = busHub;
            return true;
        }

        public void Dispose()
        {
            _loopContinue = false;
        }

        public bool Initialize()
        {
            throw new NotImplementedException();
        }

        public void L(string message, LogEvt.MessageType type = LogEvt.MessageType.Comment)
        {
            Log?.Invoke(this, new LogEvt(message, type, new StackTrace(), NameSpace));
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}