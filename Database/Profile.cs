namespace Shine.Database
{
    public sealed class Profile
    {
        public int PlayerId { get; set; }
        
        public string FullName { get; set; }
        
        public int IconId { get; set; }
        
        public string Tag { get; set; }
        
        public string Town { get; set; }

        public string GetIconUrl()
            => $"http://vjasmash.com/assets/tournaments/icons/{IconId}.png";

        public override string ToString()
            => $"{Tag} ({FullName} from {Town})";
    }
}
