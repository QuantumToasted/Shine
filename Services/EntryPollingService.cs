using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Hosting;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shine.Common;
using Shine.Database;

namespace Shine.Services
{
    public sealed class EntryPollingService : DiscordClientService
    {
        private const ulong CHANNEL_ID = 839785153215332362; // #challenger-approaching
        private const int TOURNEY_ID = 28;
        
        private readonly DiscordBotBase _bot;
        private int _entryCount;

        public EntryPollingService(ILogger<EntryPollingService> logger, DiscordBotBase bot)
            : base(logger, bot)
        {
            _bot = bot;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            // Note: set to 0 to start from the beginning
            var text = await File.ReadAllTextAsync("entry_count.txt", cancellationToken);
            _entryCount = int.Parse(text);
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                using var scope = _bot.Services.CreateScope();
                await using var ctx = scope.ServiceProvider.GetRequiredService<ShineDbContext>();

                var entries = await ctx.Entries
                    .OrderBy(x => x.Id)
                    .Where(x => x.TourneyId == TOURNEY_ID)
                    .ToListAsync(stoppingToken);

                if (entries.Count > _entryCount)
                {
                    // only get 1 entry at a time to prevent spam
                    var entry = entries[_entryCount]; // +1 thanks to zero-indexing
                    var profile = await ctx.Profiles.FindAsync(new object[] {entry.PlayerId}, stoppingToken);
                    var tourney = await ctx.Tourneys.FirstAsync(x => x.Id == entry.TourneyId, stoppingToken);
                    
                    await Client.SendMessageAsync(CHANNEL_ID, 
                        new LocalMessageBuilder()
                            .WithEmbed(new LocalEmbedBuilder()
                                .WithTitle($"{profile} has just registered for {tourney}!")
                                .WithDescription("Insert funny remark here.")
                                .WithColor(Colors.ShineBlue)
                                .WithThumbnailUrl(profile.GetIconUrl())
                                .WithFooter("Remind Kiel to mention @everyone eventually."))
                            .Build());

                    _entryCount++;
                    await File.WriteAllTextAsync("entry_count.txt", _entryCount.ToString(), stoppingToken);
                }

                await delay;
            }
        }
    }
}