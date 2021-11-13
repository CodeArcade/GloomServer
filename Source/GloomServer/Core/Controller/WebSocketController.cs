using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GloomServer
{
    public abstract class WebSocketController
    {
        public abstract string Name { get; }
        protected Logger Logger { get; private set; }

        public WebSocketController()
        {
            Logger = LogManager.GetLogger(GetType().Name);
        }

        public Response ProcessRequest(Request request)
        {
            foreach (MethodInfo m in GetType().GetRuntimeMethods())
            {
                object[] attributes = m.GetCustomAttributes(typeof(FunctionAttribute), true);
                if (attributes.Length <= 0) continue;

                FunctionAttribute attribute = (FunctionAttribute)attributes[0];

                if (attribute.Name.ToLower() == request.Header.Identifier.Function.ToLower())
                {
                    List<ParameterInfo> parameters = m.GetParameters().ToList();
                    object[] functionParameters = Array.Empty<object>();

                    if (parameters.Count == 2)
                    {
                        if (parameters[0].ParameterType == typeof(RequestHeader))
                            functionParameters = new object[] { request.Header, Convert.ChangeType(request.Body, parameters[1].ParameterType) };
                        else
                            functionParameters = new object[] { Convert.ChangeType(request.Body, parameters[0].ParameterType), request.Header };
                    }
                    else if (parameters.Count == 1)
                    {
                        if (parameters[0].ParameterType == typeof(RequestHeader))
                            functionParameters = new object[] { request.Header };
                        else
                            functionParameters = new object[] { Convert.ChangeType(request.Body, parameters[0].ParameterType) };
                    }
                    else if (parameters.Count == 0) { }
                    else throw new ArgumentException("Function contains to many arguments.");

                    Response response = new();
                    response.Header = new() {
                        Identifier = request.Header.Identifier,
                        TargetSockets = request.Header?.TargetSockets
                    };
                    response.Body = m.Invoke(this, functionParameters);

                    return response;
                }
            }
            throw new NotImplementedException($"Function {request.Header.Identifier.Function} is not part of module {request.Header.Identifier.Module}");
        }
    }
}
