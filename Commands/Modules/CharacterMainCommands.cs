using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Qmmands;
using Shine.Database;

namespace Shine.Commands
{
    [Name("Mains")]
    [Description("Commands for adding and removing characters you prefer playing, a.k.a. \"mains\".")]
    public sealed class CharacterMainCommands : DiscordGuildModuleBase
    {
        private static readonly IDictionary<Snowflake, Snowflake> EmojiToRoleMap = new Dictionary<Snowflake, Snowflake>
        {
            [839998757445828619] = 840028186175209482, // Bayonetta
            [839998757466144789] = 840028186527268864, // Bowser
            [839998757616615435] = 840028187081179226, // Bowser Jr.
            [839998757743362058] = 840028187622113280, // Banjo-Kazooie
            [839998757877448764] = 840028188120973312, // Byleth
            [839998803724599298] = 840028188721020928, // Dark Pit
            [839998803771392020] = 840028188775940097, // Captain Falcon
            [839998803791839252] = 840028189463281714, // Cloud
            [839998803846496256] = 840028190067261471, // Chrom
            [839998803909279755] = 840028190557863977, // Corrin
            [839998837321629747] = 840028190978474014, // Dr. Mario
            [839998837506703422] = 840028191383879720, // Dark Samus
            [839998837716418560] = 840028191828869150, // Dedede
            [839998837720612884] = 840028192235323413, // Donkey Kong
            [839998837736996904] = 840028192726450206, // Diddy Kong
            [839998940795240479] = 840028193146142800, // Duck Hunt
            [839998940798779412] = 840028193666105384, // Game And Watch
            [839998940811362304] = 840028194211889153, // Ganondorf
            [839998940866674708] = 840028194702229535, // Falco
            [839998940874932224] = 840028195150495744, // Fox
            [839998981219155979] = 840028195369910284, // Ike
            [839998981417467975] = 840028196094083092, // Greninja
            [839998981558763600] = 840028196669095946, // Hero
            [839998981626265660] = 840028197110153267, // Incineroar
            [839998981672796180] = 840028197516345385, // Ice Climbers
            [840001357029048322] = 840028198074318868, // Lucario
            [840001357108084797] = 840028198694944808, // Isabelle
            [840001357313474571] = 840028199069024257, // Link
            [840001357326450688] = 840028201770287105, // Jigglypuff
            [840001357326843914] = 840028202344120360, // Kirby
            [840001357334970368] = 840028202872864788, // Little Mac
            [840001357338902548] = 840028203246813234, // King K. Rool
            [840001357388840980] = 840028203464654869, // Inkling
            [840001357427638312] = 840028204190007317, // Ken
            [840001357435633664] = 840028204697649162, // Joker
            [840001421654622249] = 840028205205159936, // Lucas
            [840001421960806440] = 840028205612007425, // Marth
            [840001421980991528] = 840028206106017812, // Luigi
            [840001422031847445] = 840028206597406730, // Lucina
            [840001422048100362] = 840028207201910804, // Mario
            [840001463232102461] = 840028207197716481, // Mii Brawler
            [840001463450861598] = 840028207738519563, // Mewtwo
            [840001463530029076] = 840028208464265247, // Mega Man
            [840001463576690728] = 840028209294475264, // Meta Knight
            [840001463676567562] = 840028209365647392, // Mii Gunner
            [840001510196510741] = 840028210351439902, // Mii Swordfighter
            [840001510330073099] = 840028210749112381, // Min Min
            [840001510452494366] = 840028210900369439, // Mythra
            [840001510455902238] = 840028211718782977, // Ness
            [840001510465208350] = 840028212406779945, // Olimar
            [840025819690565664] = 840028212855046154, // Pacman
            [840025819836579901] = 840028213383528518, // Pichu
            [840025819879702579] = 840028213643444265, // Pit
            [840025819933179915] = 840028214474571786, // Piranha Plant
            [840025819946680351] = 840028214784950313, // Peach
            [840025820117336095] = 840028215602053120, // Palutena
            [840025820180381716] = 840028216281530389, // Richter
            [840025820201746432] = 840028216744083507, // Pyra
            [840025820235038720] = 840028216802148354, // Pokemon Trainer
            [840025820268724234] = 840028217644810272, // Pikachu
            [840025878687383603] = 840028218454048829, // Rosalina
            [840025878841917450] = 840028218807681055, // Ridley
            [840025878855548968] = 840028219293564949, // Roy
            [840025878981247006] = 840028220056141834, // Rob
            [840025878985310218] = 840028220295872513, // Robin
            [840025924888035338] = 840028220682534923, // Ryu
            [840025924891967488] = 840028221352181771, // Shulk
            [840025924895768646] = 840028222112006144, // Samus
            [840025924942299187] = 840028222334828555, // Sheik
            [840025924946624522] = 840028223185223710, // Sephiroth
            [840025961508372512] = 840028223697453066, // Sonic
            [840025961712975922] = 840028224058163241, // Snake
            [840025961760161803] = 840028224591233114, // Steve
            [840025961763438602] = 840028225115258890, // Terry
            [840025961851519006] = 840028225904181288, // Simon
            [840025997034389505] = 840028226419032104, // Wario
            [840025997280542751] = 840028227110961193, // Villager
            [840025997381861477] = 840028227687022623, // Toon Link
            [840025997474005052] = 840028228298080287, // Wii Fit Trainer
            [840025997546225694] = 840028228708991029, // Wolf
            [840026028842680341] = 840028229457018920, // Young Link
            [840026028864438283] = 840028230027182080, // Zero Suit Samus
            [840026028947669032] = 840028230139248691, // Yoshi
            [840026028965101578] = 840028230865518693, // Zelda
        };
        
        public ShineDbContext Database { get; set; }

        [Command("add-main", "main", "add")]
        [Description("Adds a character as a main.")]
        public async Task<DiscordCommandResult> AddMainAsync(IGuildEmoji emoji)
        {
            if (!EmojiToRoleMap.TryGetValue(emoji.Id, out var roleId))
                return Response("That emoji doesn't belong to a character!");

            if (Context.Author.RoleIds.Contains(roleId))
                return Response("You already added that character as a main!");

            if (await Database.Config.FindAsync("MAXIMUM_MAINS") is not { } maximumMains ||
                !int.TryParse(maximumMains.Value, out var maximum))
            {
                return Response("This command isn't properly configured. Please contact one of the admins.");
            }

            if (EmojiToRoleMap.Values.Count(x => Context.Author.RoleIds.Contains(x)) >= maximum)
            {
                return Response($"You are not allowed to have more than {maximum} characters you main at once.\n" +
                                $"You'll need to remove some first via \"{Context.Prefix}remove <character>\".");
            }

            await Context.Author.GrantRoleAsync(roleId);
            return Response($"You've added {Markdown.Bold(Context.Guild.Roles[roleId].Name)} as a main.");
        }
        
        [Command("remove-main", "remove", "abandon")]
        [Description("Removes a character as a main.")]
        public async Task<DiscordCommandResult> RemoveMainAsync(IGuildEmoji emoji)
        {
            if (!EmojiToRoleMap.TryGetValue(emoji.Id, out var roleId))
                return Response("That emoji doesn't belong to a character!");

            if (!Context.Author.RoleIds.Contains(roleId))
                return Response("You haven't added that character as a main!");

            await Context.Author.RevokeRoleAsync(roleId);
            return Response($"You've abandoned {Markdown.Bold(Context.Guild.Roles[roleId].Name)} as a main. For shame!");
        }
    }
}