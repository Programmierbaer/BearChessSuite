using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml.Serialization;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication
{
    public class BearChessComServer : IBearChessComServer
    {
        private class ClientAddrStream
        {
            public string ClientAddr { get;  }
            public NetworkStream ClientStream { get;  }

            public ClientAddrStream(string clientAddr, NetworkStream clientStream)
            {
                ClientAddr = clientAddr;
                ClientStream = clientStream;
            }
        }

        private readonly TcpListener _listener;
        private readonly int _portNumber;
        private readonly ILogging _logging;
        private bool _stopServer;
        private bool _isRunning;
        private Thread _serverThread;
        private ConcurrentDictionary<string, ConcurrentQueue<BearChessServerMessage>> _clientQueue = new ConcurrentDictionary<string, ConcurrentQueue<BearChessServerMessage>>();

        public event EventHandler<string> ClientConnected;
        public event EventHandler<string> ClientDisconnected;
        public event EventHandler<BearChessServerMessage> ClientMessage;
        public event EventHandler ServerStarted;
        public event EventHandler ServerStopped;
        public int CurrentPortNumber { get; set; }
        public void SendToClient(string clientAddr, BearChessServerMessage message)
        {
            if (_clientQueue.ContainsKey(clientAddr))
            {
                _clientQueue[clientAddr].Enqueue(message);
            }
            else
            {
                _logging?.LogWarning($"ComServer: Discard enqueue message for unknow address {clientAddr}: Addr: {message.Address} Action: {message.ActionCode} Msg: {message.Message}");
            }
        }


        public bool IsRunning => _isRunning;

        public BearChessComServer(int portNumber, ILogging logging)
        {
            _portNumber = portNumber;
            _logging = logging;
            _listener = new TcpListener(IPAddress.Any, _portNumber);
            _stopServer = false;
            _isRunning = false;
        }

        public void StopServer()
        {
            if (_isRunning)
            {
                _logging?.LogInfo("ComServer: Stop Server");
                _stopServer = true;
            }
        }

        private void RunServerLoop()
        {
            try
            {
                _isRunning = true;
                _listener.Start();
                CurrentPortNumber = ((IPEndPoint)_listener.LocalEndpoint).Port;
                while (!_stopServer)
                {
                    if (!_listener.Pending())
                    {
                        Thread.Sleep(100);
                        if (_stopServer)
                        {
                            break;
                        }

                        continue;
                    }

                    var client = _listener.AcceptTcpClient();
                    var clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm))
                    {
                        IsBackground = true
                    };

                    clientThread.Start(client);
                }
            }
            finally
            {
                _listener.Stop();
                _isRunning = false;
                ServerStopped?.Invoke(this, null);
            }
        }

        public void RunServer()
        {
            if (!_isRunning)
            {
                _logging?.LogInfo("ComServer: Run Server");
                _stopServer = false;
                _serverThread = new Thread(RunServerLoop)
                {
                    IsBackground = true
                };
                _serverThread.Start();
                ServerStarted?.Invoke(this, null);
            }
        }

        private void HandleSendToClient(object aStream)
        {
            var clientStream = (ClientAddrStream)aStream;
            _logging?.LogInfo($"ComServer: Send to client thread started: {clientStream.ClientAddr}");
            _clientQueue[clientStream.ClientAddr] = new ConcurrentQueue<BearChessServerMessage>();
            while (!_stopServer)
            {
                if (_clientQueue[clientStream.ClientAddr].TryDequeue(out var message))
                {
                    _logging?.LogInfo($"ComServer: Send to client {clientStream.ClientAddr}: {message.ActionCode} {message.Message}");
                    var serverString = JsonSerializer.Serialize(message);
                    var buffer = Encoding.Default.GetBytes(serverString);
                    clientStream.ClientStream.Write(buffer, 0, buffer.Length);
                    clientStream.ClientStream.Flush();
                    _logging?.LogInfo($"ComServer: {clientStream.ClientAddr} ...send ");
                }
                Thread.Sleep(10);
            }
            _logging?.LogInfo($"ComServer: stop send to client thread: {clientStream.ClientAddr}");
            while (_clientQueue[clientStream.ClientAddr].TryDequeue(out _))
            {
                ;
            }

            _clientQueue.TryRemove(clientStream.ClientAddr, out _);
        }

        private void HandleClientComm(object client)
        {
            var ipAddr = string.Empty;
            var tokenAddr = string.Empty;
            var tcpClient = (TcpClient)client;
            try
            {
                ipAddr = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                _logging?.LogInfo($"ComServer: Client connected: {ipAddr}");
                ClientConnected?.Invoke(this, $"{ipAddr}");
                using (var clientStream = tcpClient.GetStream())
                {
                    var clientThread = new Thread(new ParameterizedThreadStart(HandleSendToClient))
                    {
                        IsBackground = true
                    };

                   
                    var message = new byte[4096];
                    var completeMessage = new StringBuilder();
                    while (!_stopServer)
                    {
                        completeMessage.Clear();
                        var bytesRead = 0;

                        try
                        {
                            // blocks until a client sends a message
                            bytesRead = clientStream.Read(message, 0, message.Length);
                            while (bytesRead > 0)
                            {
                                _logging?.LogDebug($"ComServer {ipAddr}: bytes read: {bytesRead}");
                                string partMsg = Encoding.Default.GetString(message, 0, bytesRead);
                                _logging?.LogDebug($"ComServer {ipAddr}: Received: {partMsg}");
                                completeMessage.Append(partMsg);
                                if (clientStream.DataAvailable)
                                {
                                    _logging?.LogDebug($"ComServer {ipAddr}: more data available");
                                    bytesRead = clientStream.Read(message, 0, message.Length);
                                    _logging?.LogDebug($"ComServer {ipAddr}: bytes read: {bytesRead}");
                                }
                                else
                                {
                                    break;
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            // a socket error has occured
                            _logging?.LogWarning($"ComServer {ipAddr}: {ex.Message}");
                            break;
                        }

                        if (bytesRead == 0)
                        {
                            //the client has disconnected from the server
                            //  break;
                        }

                        //message has successfully been received
                        var msg = completeMessage.ToString();

                        if (string.IsNullOrWhiteSpace(msg))
                        {
                            continue;
                        }

                        _logging?.LogDebug($"ComServer {ipAddr}: Read completed for message: {msg}");
                        var msgArray = msg.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < msgArray.Length; i++)
                        {
                            _logging?.LogDebug($"ComServer {ipAddr}: Deserialize as BearChessServerMessage {i}/{msgArray.Length}: {msgArray[i]}");
                            try
                            {
                                var clientMessage = JsonSerializer.Deserialize<BearChessServerMessage>(msgArray[i]);
                                _logging?.LogDebug($"ComServer {ipAddr}: Actioncode: {clientMessage.ActionCode}");
                                if (clientMessage.ActionCode.Equals("CONNECT"))
                                {
                                    tokenAddr = Guid.NewGuid().ToString("N");
                                    var connectMessage = new BearChessServerMessage()
                                    {
                                        Ack = "ACK",
                                        Address = tokenAddr,
                                        ActionCode = clientMessage.ActionCode,
                                        Message = clientMessage.Message
                                    };
                                    var jsonString = JsonSerializer.Serialize(connectMessage);
                                    var bufferConnect = Encoding.Default.GetBytes(jsonString);
                                    clientStream.Write(bufferConnect, 0, bufferConnect.Length);
                                    clientStream.Flush();
                                    _logging?.LogDebug($"ComServer {ipAddr}/{tokenAddr}: Send: {jsonString}");
                                    ClientMessage?.Invoke(this, connectMessage);
                                    clientThread.Start(new ClientAddrStream(tokenAddr, clientStream));
                                    continue;
                                }

                                ClientMessage?.Invoke(this, clientMessage);
                                if (clientMessage.ActionCode.Equals("PUBLISH"))
                                {
                                    continue;
                                }
                                var serverMessage = new BearChessServerMessage()
                                { 
                                    Ack = "ACK", 
                                    ActionCode = clientMessage.ActionCode, 
                                    Message = clientMessage.Message
                                };
                                var serverString = JsonSerializer.Serialize(serverMessage);
                                var buffer = Encoding.Default.GetBytes(serverString);
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                                _logging?.LogDebug($"ComServer {ipAddr}/{tokenAddr}: Send ACK: {serverString}");
                            }
                            catch (Exception ex)
                            {
                                _logging?.LogWarning($"ComServer {ipAddr}: Error deserializing message: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogWarning($"ComServer {ipAddr}/{tokenAddr}: {ex.Message}");
            }
            finally
            {
                _logging?.LogDebug($"ComServer {ipAddr}/{tokenAddr}: Disconnected");
                ClientDisconnected?.Invoke(this, $"{tokenAddr}");
                tcpClient.Dispose();
            }
        }
    }
}
