using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Shine.Common;
using Shine.Database;

namespace Shine.Commands
{
    [Name("Tournament")]
    [Description("Commands for displaying information about the currently active tournament.")]
    [Group("tournament", "tourney")]
    public sealed class TournamentCommands : DiscordGuildModuleBase
    {
        private Tourney _tourney;
        
        public ShineDbContext Database { get; set; }
        
        protected override async ValueTask BeforeExecutedAsync()
        {
            var currentTourneyId = await Database.Config.FindAsync("CURRENT_TOURNEY_ID");
            var id = int.Parse(currentTourneyId.Value);
            _tourney = await Database.Tourneys.FirstOrDefaultAsync(x => x.Id == id);
        }

        [Command("", "info")]
        [Description("Displays information about the current tournament.")]
        public async Task<DiscordCommandResult> GetTournamentInfoAsync()
        {
            var entrantIds = await Database.Entrants.Where(x => x.TourneyId == _tourney.Id)
                .OrderBy(x => x.Id)
                .Select(x => x.PlayerId)
                .ToListAsync();

            var entrants = await Database.Profiles.Where(x => entrantIds.Contains(x.Id))
                .ToListAsync();

            var field = new LocalEmbedFieldBuilder()
                .WithName($"Entrants ({entrants.Count}/{_tourney.EntrantCap})");

            field = entrants.Count == 0
                ? field.WithBlankValue()
                : field.WithValue(string.Join('\n', entrants.Select(x => x.Tag)));

            return Response(new LocalEmbedBuilder()
                .WithTitle(_tourney.Name)
                .WithTimestamp(_tourney.Start)
                .AddField(field)
                .WithFooter("Tournament begins")
                .WithColor(Colors.ShineBlue));
        }
    }
}