using System.Collections.Generic;

namespace GloomServer.Gloom.Models.PlayerInfoRepository
{
    public class ElementsRequest
    {
        public IEnumerable<Element> Elements { get; set; }
        public string GameId { get; set; }
    }
}
