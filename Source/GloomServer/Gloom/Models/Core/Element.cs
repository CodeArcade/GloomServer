namespace GloomServer.Gloom.Models
{
    public class Element
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public ElementStages Stage { get; set; }
    }

    public enum ElementStages
    {
        Full,
        Half,
        Empty
    }
}
