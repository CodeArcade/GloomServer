using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GloomServer
{
    public class WebSocketMiddleware : IMiddleware
    {
        private static int SocketCounter = 0;

        /// <summary>
        /// The key is a socket id
        /// </summary>
        private static readonly ConcurrentDictionary<int, Client> Clients = new();

        private static readonly CancellationTokenSource SocketLoopTokenSource = new();

        private static bool ServerIsRunning = true;

        private static CancellationTokenRegistration AppShutdownHandler;
        private static RequestHandler RequestHandler { get; set; } = new RequestHandler();

        private static ILogger<WebSocketMiddleware> Logger { get; set; }

        // use dependency injection to grab a reference to the hosting container's lifetime cancellation tokens
        public WebSocketMiddleware(IHostApplicationLifetime hostLifetime, ILogger<WebSocketMiddleware> logger)
        {
            // gracefully close all websockets during shutdown (only register on first instantiation)
            if (AppShutdownHandler.Token.Equals(CancellationToken.None))
                AppShutdownHandler = hostLifetime.ApplicationStopping.Register(ApplicationShutdownHandler);

            if (Logger is null) Logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                if (ServerIsRunning)
                {
                    //  Socket requests are handeld here
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        int socketId = Interlocked.Increment(ref SocketCounter);
                        var socket = await context.WebSockets.AcceptWebSocketAsync();
                        var completion = new TaskCompletionSource<object>();
                        var client = new Client(socketId, socket, completion, Logger);
                        Clients.TryAdd(socketId, client);
                        Logger?.LogInformation($"Socket {socketId}: New connection.");

                        // TaskCompletionSource<> is used to keep the middleware pipeline alive;
                        // SocketProcessingLoop calls TrySetResult upon socket termination
                        _ = Task.Run(() => SocketProcessingLoopAsync(client).ConfigureAwait(false));
                        await completion.Task;
                    }
                    else // everything else is treated as html request
                        await context.Response.WriteAsync($"<html>Hallo Welt</html>");
                }
                else
                {
                    // ServerIsRunning = false
                    // HTTP 409 Conflict (with server's current state)
                    context.Response.StatusCode = 409;
                }
            }
            catch (Exception ex)
            {
                // HTTP 500 Internal server error
                context.Response.StatusCode = 500;
                Program.ReportException(ex);
            }
            finally
            {
                // if this middleware didn't handle the request, pass it on
                if (!context.Response.HasStarted)
                    await next(context);
            }
        }

        public static void Broadcast(string message)
        {
            Logger?.LogDebug($"Broadcast: {message}");
            foreach (var kvp in Clients)
                kvp.Value.BroadcastQueue.Add(RequestHandler.GetBroadcastResponse(message));
        }

        // event-handlers are the sole case where async void is valid
        public static async void ApplicationShutdownHandler()
        {
            Logger?.LogInformation($"Server shutting down");
            ServerIsRunning = false;
            await CloseAllSocketsAsync();
        }

        private static async Task CloseAllSocketsAsync()
        {
            // We can't dispose the sockets until the processing loops are terminated,
            // but terminating the loops will abort the sockets, preventing graceful closing.
            var disposeQueue = new List<WebSocket>(Clients.Count);

            while (!Clients.IsEmpty)
            {
                var client = Clients.ElementAt(0).Value;
                Logger.LogInformation($"Closing Socket {client.SocketId}");

                Logger?.LogDebug("... ending broadcast loop");
                client.BroadcastLoopTokenSource.Cancel();

                if (client.Socket.State != WebSocketState.Open)
                {
                    Logger?.LogDebug($"... socket not open, state = {client.Socket.State}");
                }
                else
                {
                    var timeout = new CancellationTokenSource(Program.CLOSE_SOCKET_TIMEOUT_MS);
                    try
                    {
                        Logger?.LogDebug("... starting close handshake");
                        await client.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token);
                    }
                    catch (OperationCanceledException ex)
                    {
                        Program.ReportException(ex);
                        // normal upon task/token cancellation, disregard
                    }
                }

                if (Clients.TryRemove(client.SocketId, out _))
                {
                    // only safe to Dispose once, so only add it if this loop can't process it again
                    disposeQueue.Add(client.Socket);
                }

                Logger?.LogDebug("... done");
            }

            // now that they're all closed, terminate the blocking ReceiveAsync calls in the SocketProcessingLoop threads
            SocketLoopTokenSource.Cancel();

            // dispose all resources
            foreach (var socket in disposeQueue)
                socket.Dispose();
        }

        private static async Task SocketProcessingLoopAsync(Client client)
        {
            _ = Task.Run(() => client.BroadcastLoopAsync().ConfigureAwait(false));

            var socket = client.Socket;
            var loopToken = SocketLoopTokenSource.Token;
            var broadcastTokenSource = client.BroadcastLoopTokenSource; // store a copy for use in finally block
            try
            {
                var buffer = WebSocket.CreateServerBuffer(4096);
                while (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted && !loopToken.IsCancellationRequested)
                {
                    var receiveResult = await client.Socket.ReceiveAsync(buffer, loopToken);
                    // if the token is cancelled while ReceiveAsync is blocking, the socket state changes to aborted and it can't be used
                    if (!loopToken.IsCancellationRequested)
                    {
                        // the client is notifying us that the connection will close; send acknowledgement
                        if (client.Socket.State == WebSocketState.CloseReceived && receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            Logger?.LogDebug($"Socket {client.SocketId}: Acknowledging Close frame received from client");
                            broadcastTokenSource.Cancel();
                            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close frame", CancellationToken.None);
                            // the socket state changes to closed at this point
                        }

                        // echo text or binary data to the broadcast queue
                        if (client.Socket.State == WebSocketState.Open)
                        {
                            Logger?.LogDebug($"Socket {client.SocketId}: Received {receiveResult.MessageType} frame ({receiveResult.Count} bytes).");

                            string message = Encoding.UTF8.GetString(buffer.Array, 0, receiveResult.Count);
                            List<int> targetSockets = new();
                            string response = RequestHandler.HandleRequest(message, out targetSockets, client);

                            foreach (int target in targetSockets)
                            {
                                try
                                {
                                    Client targetClient = null;

                                    if (Clients.TryGetValue(target, out targetClient))
                                        targetClient.BroadcastQueue.Add(response);
                                    else
                                        throw new Exception($"Socket not connected");
                                }
                                catch (Exception ex)
                                {
                                    Logger?.LogError($"Socket {target}:");
                                    Program.ReportException(ex);
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Socket {client.SocketId}:");
                Program.ReportException(ex);
            }
            finally
            {
                broadcastTokenSource.Cancel();

                Logger?.LogDebug($"Socket {client.SocketId}: Ended processing loop in state {socket.State}");

                // don't leave the socket in any potentially connected state
                if (client.Socket.State != WebSocketState.Closed)
                    client.Socket.Abort();

                // by this point the socket is closed or aborted, the ConnectedClient object is useless
                if (Clients.TryRemove(client.SocketId, out _))
                    socket.Dispose();

                // signal to the middleware pipeline that this task has completed
                client.TaskCompletion.SetResult(true);
            }
        }
    }
}
