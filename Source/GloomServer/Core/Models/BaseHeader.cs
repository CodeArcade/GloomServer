using System.Collections.Generic;

namespace GloomServer
{
    public class BaseHeader
    {
        public Identifier Identifier { get; set; }
        public string MessageNumber { get; set; }
        public IEnumerable<string> TargetSockets { get; set; }
    }
}
