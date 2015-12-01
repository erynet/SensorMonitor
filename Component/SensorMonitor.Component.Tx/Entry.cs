using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
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

using SensorMonitor.Component.Common.Interface;

namespace SensorMonitor.Component.Tx
{
    public class Entry : ISubComponent
    {
        #region Class Specific
        public string NameSpace = "SensorMonitor.Component.Tx.Entry";

        private BusHub _busHub;
        private bool _loopContinue;

        private readonly IConnectionFactory _connectionFactory;

        private readonly Regex rx;
        private readonly Dictionary<string, INode> _nodeDictionary;
        private readonly Dictionary<string, IConnection> _connectionDictionary;

        public Entry()
        {
            Log = null;
            _loopContinue = true;

            rx = new Regex(@"([\d.]+):(\d+)",
                    RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            try
            {
                var clientBootstrap = new ClientBootstrap()
                    .SetTransport(TransportType.Udp)
                    .OnConnect(ConnectionEstablishedCallback)
                    .OnReceive(ReceivedDataCallback)
                    .OnDisconnect(ConnectionTerminatedCallback)
                    .OnError(ConnectionOnOnError);
                _connectionFactory = clientBootstrap.Build();
            }
            catch (Exception)
            {

                throw;
            }
            _nodeDictionary = new Dictionary<string, INode>();
            _connectionDictionary = new Dictionary<string, IConnection>();
        }

        private void Run()
        {
            while (_loopContinue)
            {
                Thread.Sleep(250);
            }
        }

        private bool SendMessage(string connectString, byte[] data = null)
        {
            if (AttemptConnect(connectString))
            {
                try
                {
                    var networkData = NetworkData.Create(_nodeDictionary[connectString], data, data.Length);
                    _connectionDictionary[connectString].Send(networkData);
                    return true;
                }
                catch (Exception)
                {
                    // 에러메시지를 기록한다.
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool AttemptConnect(string connectString)
        {
            try
            {
                if (_connectionDictionary.ContainsKey(connectString) && _nodeDictionary.ContainsKey(connectString))
                {
                    // 이미 만들어진 엔트리가 있는 경우
                    return true;
                }

                var rxConnectString = rx.Match(connectString);
                if (rxConnectString.Success)
                {
                    IPAddress host = IPAddress.Parse(rxConnectString.Groups[1].Value);
                    int port = int.Parse(rxConnectString.Groups[2].Value);

                    INode remoteHost = NodeBuilder.BuildNode()
                        .Host(host)
                        .WithPort(port)
                        .WithTransportType(TransportType.Udp);
                    _nodeDictionary[connectString] = remoteHost;

                    IConnection connection = _connectionFactory.NewConnection(
                        NodeBuilder.BuildNode()
                        .Host(IPAddress.Any)
                        .WithPort(10001), remoteHost);
                    connection.Open();
                    _connectionDictionary[connectString] = connection;
                }
                else
                {
                    // connectString 이 잘못된 경우이니 로그를 남기든지 한다.
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                //AppendStatusText(ex.Message);
                //AppendStatusText(ex.StackTrace);
                //AppendStatusText(ex.Source);
                return false;
            }
        }

        private void ConnectionEstablishedCallback(INode remoteAddress, IConnection responseChannel)
        {
            responseChannel.BeginReceive();
            //Invoke((Action)(() =>
            //{
            //    AppendStatusText(string.Format("Connected to {0}", remoteAddress));
            //    responseChannel.BeginReceive();
            //    tsStatusLabel.Text = string.Format("Connected to {0}", remoteAddress);
            //    btnSend.Enabled = true;
            //    tbSend.Enabled = true;
            //}));
        }

        private void ConnectionTerminatedCallback(HeliosConnectionException reason, IConnection closedChannel)
        {
            //Invoke((Action)(() =>
            //{
            //    AppendStatusText(string.Format("Disconnected from {0}", closedChannel.RemoteHost));
            //    AppendStatusText(string.Format("Reason: {0}", reason.Message));
            //    tsStatusLabel.Text = string.Format("Disconnected from {0}", closedChannel.RemoteHost);
            //    btnSend.Enabled = false;
            //    tbSend.Enabled = false;
            //}));
        }

        private void ReceivedDataCallback(NetworkData incomingData, IConnection responseChannel)
        {
           //Invoke((Action)(() =>
           //{
           //    AppendStatusText(string.Format("Received {0} bytes from {1}", incomingData.Length,
           //    comingData.RemoteHost));
           //    AppendStatusText(Encoding.UTF8.GetString(incomingData.Buffer));
           //}));
        }


        private void ConnectionOnOnError(Exception exception, IConnection connection)
        {
            //Invoke((Action)(() =>
            //{
            //    AppendStatusText(string.Format("Exception {0} sending data to {1}", exception.Message, connection.RemoteHost));
            //    AppendStatusText(exception.StackTrace);
            //}));
        }
        #endregion


        #region ISubCompoenet Implement
        public event EventHandler Log;

        public bool ConnectBus(BusHub busHub)
        {
            _busHub = busHub;
            _busHub.Subscribe<ITxMessage>((m) => { SendMessage(m.ConnectString); });

            //_packageFlowQueue = new BlockingCollection<ICPackage>();
            //busHub.Subscribe<ICPackage>((m) => { _packageFlowQueue.TryAdd(m); });


            return true;
        }

        public void Dispose()
        {
            foreach (var connectString in _connectionDictionary.Keys)
            {
                try
                {
                    _connectionDictionary[connectString].Close();
                }
                catch (Exception)
                {
                    continue;
                }
            }

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