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
    [Group("configuration", "config")]
    [RequireBotOwner]
    public sealed class ConfigurationCommands : DiscordGuildModuleBase
    {
        public ShineDbContext Database { get; set; }
        
        public EntrantPollingService EntrantService { get; set; }
        
        [Command("maximum-mains")]
        public async Task<DiscordCommandResult> GetMaximumMainsAsync()
        {
            var maximumMains = await Database.Config.FindAsync("MAXIMUM_MAINS");
            return Response($"Users can currently choose up to **{maximumMains.Value}** characters as their main(s).");
        }
        
        [Command("maximum-mains")]
        public async Task<DiscordCommandResult> SetMaximumMainsAsync([Minimum(1)] int maximum)
        {
            var maximumMains = await Database.Config.FindAsync("MAXIMUM_MAINS");
            maximumMains.Value = maximum.ToString();
            await Database.SaveChangesAsync();
            return Response($"Users can now choose up to **{maximum}** characters as their main(s).");
        }
        
        [Command("tourney-id", "tourney")]
        public async Task<DiscordCommandResult> GetTourneyIdAsync()
        {
            var tourneyId = await Database.Config.FindAsync("CURRENT_TOURNEY_ID");
            var id = int.Parse(tourneyId.Value);
            var tourney = await Database.Tourneys.FirstAsync(x => x.Id == id);
            return Response($"The current tournament ID has been set as **{tourneyId.Value}** ({tourney}).");
        }
        
        [Command("tourney-id", "tourney")]
        public async Task<DiscordCommandResult> SetTourneyIdAsync([Minimum(1)] int id)
        {
            if (await Database.Tourneys.FirstOrDefaultAsync(x => x.Id == id) is not { } tourney)
            {
                return Response("No tournament could be found with that ID.");
            }
            
            var tourneyId = await Database.Config.FindAsync("CURRENT_TOURNEY_ID");
            tourneyId.Value = id.ToString();
            await Database.SaveChangesAsync();
            return Response($"The current tournament ID has been updated to **{id}** ({tourney}).\n" +
                            $"Run \"{Context.Prefix}config reset-entrants\" to reset the entrant counter to 0,\n" +
                            $"or \"{Context.Prefix}config sync-entrants\" to set the counter to the number of entrants.");
        }
        
        [Command("entrant-channel-id")]
        public async Task<DiscordCommandResult> GetEntrantChannelIdAsync()
        {
            var channelId = await Database.Config.FindAsync("NEW_ENTRANT_CHANNEL_ID");
            var channel = Context.Guild.GetChannel(Snowflake.Parse(channelId.Value)) as ITextChannel;
            return Response($"New entrants to the current tournament are currently announced in {channel!.Mention}.");
        }
        
        [Command("entrant-channel-id")]
        public async Task<DiscordCommandResult> SetEntrantChannelIdAsync(ITextChannel channel)
        {
            var channelId = await Database.Config.FindAsync("NEW_ENTRANT_CHANNEL_ID");
            channelId.Value = channel.Id.ToString();
            await Database.SaveChangesAsync();
            return Response($"New entrants to the current tournament will now be announced in {channel.Mention}.");
        }
        
        [Command("reset-entrants")]
        public async Task<DiscordCommandResult> ResetEntrantCountAsync()
        {
            EntrantService.LastEntrantCount = 0;
            
            var entrantCount = await Database.Config.FindAsync("LAST_ENTRANT_COUNT");
            entrantCount.Value = 0.ToString(); // lol
            await Database.SaveChangesAsync();
            return Response("Entrant count has been reset to **0** for this tournament.\n" +
                            "Announcements may be re-posted if entrants already existed!");
        }
        
        [Command("sync-entrants")]
        public async Task<DiscordCommandResult> SyncEntrantCountAsync()
        {
            var tourneyId = await Database.Config.FindAsync("CURRENT_TOURNEY_ID");
            var id = int.Parse(tourneyId.Value);
            var tourney = await Database.Tourneys.FirstAsync(x => x.Id == id);
            var tourneyEntrants = await Database.Entrants.Where(x => x.TourneyId == tourney.Id).ToListAsync();

            EntrantService.LastEntrantCount = tourneyEntrants.Count;
            
            var entrantCount = await Database.Config.FindAsync("LAST_ENTRANT_COUNT");
            entrantCount.Value = tourneyEntrants.Count.ToString();
            await Database.SaveChangesAsync();

            return Response($"Entrant count has been set to **{tourneyEntrants.Count}** for this tournament (the current number of entrants).");
        }
    }
}