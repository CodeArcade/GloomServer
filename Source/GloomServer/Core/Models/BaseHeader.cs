using System.Collections.Generic;

namespace GloomServer
{
    public abstract class BaseHeader
    {
        public Identifier Identifier { get; set; }
        public string MessageNumber { get; set; }
        public IEnumerable<int> TargetSockets { get; set; }
    }
}
