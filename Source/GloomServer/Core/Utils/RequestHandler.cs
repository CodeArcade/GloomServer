using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GloomServer
{
    /// <summary>
    /// Beantwortet alle einkommenden Requests
    /// </summary>
    public class RequestHandler
    {
        public static List<WebSocketController> Controllers { get; set; }
        private Logger Logger { get; }
        private Converter Converter { get; } = new Converter();

        public RequestHandler()
        {
            Logger = LogManager.GetLogger(typeof(RequestHandler).Name);
        }

        public string HandleRequest(string requestData, out List<string> targets, Client client)
        {
            Response response = new();
            Request request = Converter.ConvertJsonToObject<Request>(requestData);
            request.Header.SocketId = client.SocketId;

            Logger.Info($"Received {Converter.ConvertObjectToJson(request)} from {client.SocketId}");

            WebSocketController module = Controllers.FirstOrDefault(x => x.Name.ToLower() == request.Header.Identifier.Module.ToLower());
            if (module is null)
            {
                response = GetErrorResponse($"Failed to call function {request.Header.Identifier.Function} at unkown module {request.Header.Identifier.Module}",
                    new List<string> { client.SocketId });
                Logger.Error($"Failed to call function {request.Header.Identifier.Function} at unkown module {request.Header.Identifier.Module}");
            }
            else
            {
                try
                {
                    response = module.ProcessRequest(request);
                }
                catch (Exception ex)
                {
                    response = GetErrorResponse($"Failed to process request for function {request.Header.Identifier.Function} at module {request.Header.Identifier.Module}: {Environment.NewLine} {ex}",
                        new List<string> { client.SocketId });
                    Logger.Error($"Failed to process request for function {request.Header.Identifier.Function} at module {request.Header.Identifier.Module}: {Environment.NewLine} {ex}");
                }
            }

            response.Header.MessageNumber = request.Header.MessageNumber;

            if (response.Header?.TargetSockets != null && response.Header.TargetSockets.Any())
                targets = response.Header.TargetSockets.ToList();
            else
            {
                targets = new List<string>() { client.SocketId };
                response.Header.TargetSockets = targets;
            }

            Logger.Info($"Sending {Converter.ConvertObjectToJson(response)} to {string.Join(",", response.Header?.TargetSockets)}");

            return Converter.ConvertObjectToJson(response);
        }

        private Response GetErrorResponse(string message, List<string> targets)
        {
            ResponseHeader header = new() { TargetSockets = targets, StatusCode = 400, Identifier = new() { Module = "Error", Function = "Error" } };
            return new Response { Header = header, Body = message };
        }

        public string GetBroadcastResponse(string message)
        {
            ResponseHeader header = new() { StatusCode = 200, Identifier = new() { Module = "Broadcast", Function = "Broadcast" } };
            return Converter.ConvertObjectToJson(new Response { Header = header, Body = message });
        }
    }
}
