using System.Collections.Generic;

namespace GloomServer.Gloom.Models
{
    public class Game
    {
        public string Id { get; set; }
        public List<Player> Players { get; set; }
        public IEnumerable<Element> Elements { get; set; }
    }
}
