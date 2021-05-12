using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Shine.Database;
using Shine.Services;

namespace Shine.Commands
{
    [Name("Configuration")]
    [Description("Commands for configuring the bot's features and settings.")]
    [Group("configuration", "config")]
    [RequireBotOwner]
    public sealed class ConfigurationCommands : DiscordGuildModuleBase
    {
        public ShineDbContext Database { get; set; }
        
        public EntrantPollingService EntrantService { get; set; }

        [Command("maximum-mains")]
        [Description("Configures the maximum number of mains a user can have at once.")]
        public async Task<DiscordCommandResult> SetMaximumMainsAsync(
            [Description("The maximum number of mains allowed.\nSupply nothing to view the current value.")]
            [Minimum(1)]
                int maximum = -1)
        {
            var maximumMains = await Database.Config.FindAsync("MAXIMUM_MAINS");
            if (maximum < 0)
                return Response($"Users can currently choose up to **{maximumMains.Value}** characters as their main(s).");
            
            maximumMains.Value = maximum.ToString();
            await Database.SaveChangesAsync();
            return Response($"Users can now choose up to **{maximum}** characters as their main(s).");
        }

        [Command("tourney-id", "tourney")]
        [Description("Configures the ID of the currently active tournament.")]
        public async Task<DiscordCommandResult> SetTourneyIdAsync(
            [Description("The ID of the tournament you wish to use.\nSupply nothing to view the current value.")]
            [Minimum(1)] 
                int id = -1)
        {
            var tourneyId = await Database.Config.FindAsync("CURRENT_TOURNEY_ID");

            if (id < 0)
            {
                id = int.Parse(tourneyId.Value);
                var existingTourney = await Database.Tourneys.FirstAsync(x => x.Id == id);
                return Response($"The current tournament ID has been set as **{id}** ({existingTourney}).");
            }
            
            if (await Database.Tourneys.FirstOrDefaultAsync(x => x.Id == id) is not { } tourney)
            {
                return Response("No tournament could be found with that ID.");
            }
            
            tourneyId.Value = id.ToString();
            await Database.SaveChangesAsync();
            return Response($"The current tournament ID has been updated to **{id}** ({tourney}).\n" +
                            $"Run \"{Context.Prefix}config reset-entrants\" to reset the entrant counter to 0,\n" +
                            $"or \"{Context.Prefix}config sync-entrants\" to set the counter to the number of entrants.");
        }

        [Command("entrant-channel")]
        [Description("Configures the channel you wish to have new entrants announced in.")]
        public async Task<DiscordCommandResult> SetEntrantChannelIdAsync(
            [Description("The channel you wish to announce entrants in.\nSupply nothing to view the current value.")]
                ITextChannel channel = null)
        {
            var channelId = await Database.Config.FindAsync("NEW_ENTRANT_CHANNEL_ID");
            
            if (channel is null)
            {
                channel = Context.Guild.GetChannel(Snowflake.Parse(channelId.Value)) as ITextChannel;
                return Response($"New entrants to the current tournament are currently announced in {channel!.Mention}.");
            }
            
            channelId.Value = channel.Id.ToString();
            await Database.SaveChangesAsync();
            return Response($"New entrants to the current tournament will now be announced in {channel.Mention}.");
        }
        
        [Command("reset-entrants")]
        [Description("Resets the entrant count to 0 for this tournament, starting tracking from the beginning.")]
        public async Task<DiscordCommandResult> ResetEntrantCountAsync()
        {
            var entrantCount = await Database.Config.FindAsync("LAST_ENTRANT_COUNT");
            entrantCount.Value = 0.ToString(); // lol
            await Database.SaveChangesAsync();
            return Response("Entrant count has been reset to **0** for this tournament.\n" +
                            "Announcements may be re-posted if entrants already existed!");
        }
        
        [Command("sync-entrants")]
        [Description("Sets the entrant count for this tournament to the number of currently entered players.")]
        public async Task<DiscordCommandResult> SyncEntrantCountAsync()
        {
            var tourneyId = await Database.Config.FindAsync("CURRENT_TOURNEY_ID");
            var id = int.Parse(tourneyId.Value);
            var tourney = await Database.Tourneys.FirstAsync(x => x.Id == id);
            var tourneyEntrants = await Database.Entrants.Where(x => x.TourneyId == tourney.Id).ToListAsync();
            
            var entrantCount = await Database.Config.FindAsync("LAST_ENTRANT_COUNT");
            entrantCount.Value = tourneyEntrants.Count.ToString();
            await Database.SaveChangesAsync();

            return Response($"Entrant count has been set to **{tourneyEntrants.Count}** for this tournament (the current number of entrants).");
        }
    }
}