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

        public string GetCharacterName()
        {
            return IconId switch
            {
                1 => "Bowser",
                2 => "Captain Falcon",
                3 => "Donkey Kong",
                4 => "Dr. Mario",
                5 => "Falco",
                6 => "Fox",
                7 => "Ganondorf",
                8 => "Ice Climbers",
                9 => "Jigglypuff",
                10 => "Kirby",
                11 => "Link",
                12 => "Luigi",
                13 => "Mario",
                14 => "Marth",
                15 => "Mewtwo",
                16 => "Mr. Game & Watch",
                17 => "Ness",
                18 => "Peach",
                19 => "Pichu",
                20 => "Pikachu",
                21 => "Roy",
                22 => "Samus",
                23 => "Sheik",
                24 => "Yoshi",
                25 => "Young Link",
                26 => "Zelda",
                _ => string.Empty
            };
        }

        public override string ToString()
            => $"{Tag} ({FullName} from {Town})";
    }
}
