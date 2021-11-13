using System;
using System.Collections.Generic;

namespace GloomServer
{
    public class RequestHeader : BaseHeader
    {
        public string SocketId { get; set; }
        public RequestHeader() { MessageNumber = DateTime.Now.Ticks + " - " + new Random().Next(0, 999999) + ""; }
    }
}
