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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shine.Common;
using Shine.Database;

namespace Shine.Services
{
    public sealed class EntryPollingService : DiscordClientService
    {
        private static readonly string[] CharacterQuips =
        {
            "Watch out! We heard their {0} can zero-to-death you.",
            "Last time they played {0} against Leffen, he had to buy a new controller.",
            "Rumor has it, Wizzy is still in the bathroom to this day preparing for his match against their {0}."
        };
        
        private static readonly string[] NoCharacterQuips =
        {
            "Watch out! We heard they can zero-to-death you.",
            "Last time they played against Leffen, he had to buy a new controller.",
            "Rumor has it, Wizzy is still in the bathroom to this day preparing for his match against them."
        };
        
        private readonly DiscordBotBase _bot;
        private readonly Random _random;
        private readonly Snowflake _channelId;
        private readonly int _tourneyId;
        private int _entryCount;

        public EntryPollingService(ILogger<EntryPollingService> logger, DiscordBotBase bot)
            : base(logger, bot)
        {
            _bot = bot;
            _random = bot.Services.GetRequiredService<Random>();

            var config = bot.Services.GetRequiredService<IConfiguration>();
            _channelId = config.GetValue<ulong>("EntryPolling:ChannelId");
            _tourneyId = config.GetValue<int>("EntryPolling:TourneyId");
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
                    .Where(x => x.TourneyId == _tourneyId)
                    .ToListAsync(stoppingToken);

                if (entries.Count > _entryCount)
                {
                    // only get 1 entry at a time to prevent spam
                    var entry = entries[_entryCount]; // +1 thanks to zero-indexing
                    var profile = await ctx.Profiles.FindAsync(new object[] {entry.PlayerId}, stoppingToken);
                    var tourney = await ctx.Tourneys.FirstAsync(x => x.Id == entry.TourneyId, stoppingToken);
                    var character = profile.GetCharacterName();

                    _entryCount++;

                    var field = new LocalEmbedFieldBuilder()
                        .WithName($"Current number of entrants: {_entryCount}/{tourney.EntrantCap}");

                    if (_entryCount == tourney.EntrantCap)
                    {
                        field.WithValue(Markdown.Link("Visit the tourney page",
                                            $"http://vjasmash.com/tournaments/tournament/{tourney.Id}/") +
                                        " to view who is currently registered!");
                    }
                    else
                    {
                        field.WithValue(Markdown.Link("Sign up now",
                                            $"http://vjasmash.com/tournaments/tournament/{tourney.Id}/") +
                                        " to secure your position in the tournament!");
                    }
                    
                    await Client.SendMessageAsync(_channelId, 
                        new LocalMessageBuilder()
                            .WithContent("@everyone")
                            .WithEmbed(new LocalEmbedBuilder()
                                .WithTitle($"{profile} has just registered for {tourney}!")
                                .WithDescription(string.IsNullOrWhiteSpace(character)
                                    ? NoCharacterQuips[_random.Next(NoCharacterQuips.Length)]
                                    : string.Format(CharacterQuips[_random.Next(CharacterQuips.Length)], character))
                                .AddField(field)
                                .WithColor(Colors.ShineBlue)
                                .WithThumbnailUrl(profile.GetIconUrl()))
                            .WithMentions(new LocalMentionsBuilder()
                                .WithParsedMentions(ParsedMention.Everyone))
                            .Build());

                    await File.WriteAllTextAsync("entry_count.txt", _entryCount.ToString(), stoppingToken);
                }

                await delay;
            }
        }
    }
}