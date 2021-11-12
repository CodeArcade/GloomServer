using System;

namespace GloomServer
{
    public class FunctionAttribute : Attribute
    {
        public string Name { get; private set; }

        public FunctionAttribute(string name)
        {
            Name = name;
        }
    }
}
