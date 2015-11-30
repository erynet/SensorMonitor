using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Helios.Net;
using Helios.Reactor;
using Helios.Reactor.Bootstrap;
using Helios.Reactor.Udp;
using Helios.Topology;

using SensorMonitor.Core;
using SensorMonitor.Core.Event;
using SensorMonitor.Core.Interfaces;

namespace SensorMonitor.Component.Rx
{
    public class Entry : ISubComponent
    {
        #region Class Specific
        public string NameSpace = "SensorMonitor.Component.Rx.Entry";

        private BusHub _busHub;
        private bool _loopContinue;

        public IPAddress HostIp { get; private set; }
        public int HostPort { get; private set; }

        public Entry()
        {
            Log = null;
            _loopContinue = true;

            HostIp = IPAddress.Any;
            HostPort = 1337;
        }

        private void Run()
        {
            var bootstrapper =
                new ServerBootstrap()
                    .WorkerThreads(2)
                    .SetTransport(TransportType.Udp)
                    .Build();
            IReactor reactor =
                bootstrapper.NewReactor(NodeBuilder.BuildNode().Host(HostIp).WithPort(HostPort));
            reactor.OnConnection += (node, connection) =>
            {
                //ServerPrint(node,
                //    string.Format("Accepting connection from... {0}:{1}", node.Host, node.Port));
                connection.BeginReceive(UdpPackerReceiveCallback);
            };
            //reactor.OnDisconnection += (reason, address) => ServerPrint(address.RemoteHost,
            //    string.Format("Closed connection to... {0}:{1} [Reason:{2}]", address.RemoteHost.Host, address.RemoteHost.Port, reason.Type));
            reactor.Start();

            while (_loopContinue)
            {
                Thread.Sleep(250);
            }
        }

        public static void UdpPackerReceiveCallback(NetworkData data, IConnection connection)
        {
            var node = connection.RemoteHost;

            //ServerPrint(connection.RemoteHost, string.Format("recieved {0} bytes", data.Length));
            var str = Encoding.UTF8.GetString(data.Buffer).Trim();
            if (str.Trim().Equals("close"))
            {
                connection.Close();
                return;
            }
            //ServerPrint(connection.RemoteHost, string.Format("recieved \"{0}\"", str));
            //ServerPrint(connection.RemoteHost,
            //    string.Format("sending \"{0}\" back to {1}:{2}", str, node.Host, node.Port));
            var sendBytes = Encoding.UTF8.GetBytes(str + Environment.NewLine);
            connection.Send(new NetworkData() { Buffer = sendBytes, Length = sendBytes.Length, RemoteHost = node });
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