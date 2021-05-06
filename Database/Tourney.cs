using System;

namespace Shine.Database
{
    public sealed class Tourney
    {
        public int Id { get; set; }
        
        public string Name { get; set; }
        
        public DateTime Start { get; set; }
        
        public int EntrantCap { get; set; }

        public override string ToString()
            => Name;
    }
}
