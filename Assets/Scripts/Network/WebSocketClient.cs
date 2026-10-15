using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Poligon.Network
{
    public class WebSocketClient : IDisposable
    {
        ClientWebSocket _ws;
        CancellationTokenSource _cts;
        readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();
        public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;
        public event Action OnOpen;
        public event Action OnClose;
        public event Action<string> OnError;

        public async Task ConnectAsync(string url)
        {
            Disconnect();
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();
            try
            {
                await _ws.ConnectAsync(new Uri(url), _cts.Token);
                OnOpen?.Invoke();
                _ = ReceiveLoop();
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
                Disconnect();
            }
        }

        async Task ReceiveLoop()
        {
            var buf = new byte[8 * 1024];
            var sb = new StringBuilder();
            try
            {
                while (_ws != null && _ws.State == WebSocketState.Open)
                {
                    sb.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Disconnect();
                            OnClose?.Invoke();
                            return;
                        }
                        sb.Append(Encoding.UTF8.GetString(buf, 0, result.Count));
                    } while (!result.EndOfMessage);
                    _incoming.Enqueue(sb.ToString());
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
                Disconnect();
                OnClose?.Invoke();
            }
        }

        public bool TryDequeue(out string msg) => _incoming.TryDequeue(out msg);

        public void Send(string json)
        {
            if (!IsConnected) return;
            var bytes = Encoding.UTF8.GetBytes(json);
            _ = _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }

        public void Disconnect()
        {
            try { _cts?.Cancel(); } catch { }
            try { _ws?.Dispose(); } catch { }
            _ws = null;
            _cts = null;
        }

        public void Dispose() => Disconnect();
    }
}
