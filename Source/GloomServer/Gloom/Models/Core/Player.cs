using System.Collections.Generic;

namespace GloomServer.Gloom.Models
{
    public class Player
    {
        public string SocketId { get; set; }

        public string Name { get; set; }
        public string Icon { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Experience { get; set; }
        public int Shield { get; set; }
        public int Vengeance { get; set; }
        public int VengeanceRange { get; set; }
        public int AttackBonus { get; set; }
        public IEnumerable<Effect> Effects { get; set; }
    }
}
