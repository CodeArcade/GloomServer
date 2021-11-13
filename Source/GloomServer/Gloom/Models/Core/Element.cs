namespace GloomServer.Gloom.Models
{
    public class Element
    {
        public ElementName Name { get; set; }
        public ElementStages Stage { get; set; }
    }

    public enum ElementStages
    {
        Full,
        Half,
        Empty
    }

    public enum ElementName
    {
        Fire,
        Ice,
        Ground,
        Air,
        Light,
        Dark
    }
}
