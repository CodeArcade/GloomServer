using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GloomServer
{
    public class Client
    {

        private ILogger Logger { get; set; }

        public Client(string socketId, WebSocket socket, TaskCompletionSource<object> taskCompletion, ILogger logger)
        {
            SocketId = socketId;
            Socket = socket;
            TaskCompletion = taskCompletion;
            Logger = logger;
        }

        public string SocketId { get; private set; }

        public WebSocket Socket { get; private set; }

        public TaskCompletionSource<object> TaskCompletion { get; private set; }

        public BlockingCollection<string> BroadcastQueue { get; } = new BlockingCollection<string>();

        public CancellationTokenSource BroadcastLoopTokenSource { get; set; } = new CancellationTokenSource();

        public async Task BroadcastLoopAsync()
        {
            var cancellationToken = BroadcastLoopTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Program.BROADCAST_TRANSMIT_INTERVAL_MS, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested && Socket.State == WebSocketState.Open && BroadcastQueue.TryTake(out var message))
                    {
                        Logger.LogDebug($"Socket {SocketId}: Sending boradcast from queue.");
                        var msgbuf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                        await Socket.SendAsync(msgbuf, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
                    }
                }
                catch (OperationCanceledException)
                {
                    // normal upon task/token cancellation, disregard
                }
                catch (Exception ex)
                {
                    Program.ReportException(ex);
                }
            }
        }
    }
}
