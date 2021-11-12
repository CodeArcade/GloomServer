using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GloomServer.Gloom.Repositories
{
    public class DummyRepository : WebSocketRepository
    {
        public override string Name => "Dummy";

        [Function("Echo")]
        public string Echo(string message)
        {
            return message;
        }

        [Function("GetOwnSocketId")]
        public int Get(RequestHeader header)
        {
            return header.SocketId;
        }

        [Function("Pair")]
        public string Pair(RequestHeader header, string message)
        {
            return message;
        }

    }
}
