using System;

namespace Shine.Database
{
    public sealed class Tourney
    {
        public int Id { get; set; }
        
        public string Name { get; set; }
        
        public DateTime Start { get; set; }
        
        public int EntrantCap { get; set; }

        public string GetUrl()
            => $"http://vjasmash.com/tournaments/tournament/{Id}/";

        public override string ToString()
            => Name;
    }
}
