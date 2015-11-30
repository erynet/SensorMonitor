using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Helios.Net;
using Helios.Net.Bootstrap;
using Helios.Reactor;
using Helios.Reactor.Bootstrap;
using Helios.Reactor.Udp;
using Helios.Exceptions;
using Helios.Topology;

using SensorMonitor.Core;
using SensorMonitor.Core.Event;
using SensorMonitor.Core.Interfaces;

namespace SensorMonitor.Component.Tx
{
    public class Entry : ISubComponent
    {
        #region Class Specific
        public string NameSpace = "SensorMonitor.Component.Tx.Entry";

        private BusHub _busHub;

        private bool _loopContinue;

        private INode _remoteHost;
        private IConnection _connection;

        public Entry()
        {
            Log = null;
            _loopContinue = true;

            _connection =
                    new ClientBootstrap()
                        .SetTransport(TransportType.Udp)
                        .RemoteAddress(Node.Loopback())
                        .OnConnect(ConnectionEstablishedCallback)
                        .OnReceive(ReceivedDataCallback)
                        .OnDisconnect(ConnectionTerminatedCallback)
                        .Build().NewConnection(NodeBuilder.BuildNode().Host(IPAddress.Any).WithPort(10001), RemoteHost);
            _connection.OnError += ConnectionOnOnError;
            _connection.Open();
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