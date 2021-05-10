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
    public sealed class EntrantPollingService : DiscordClientService
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

        public EntrantPollingService(ILogger<EntrantPollingService> logger, DiscordBotBase bot)
            : base(logger, bot)
        {
            _bot = bot;
            _random = bot.Services.GetRequiredService<Random>();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<ShineDbContext>();

            if (await ctx.Config.FindAsync(new object[] {"LAST_ENTRANT_COUNT"}, cancellationToken) is not
                { } entrantCountConfig)
            {
                Logger.LogError("LAST_ENTRANT_COUNT could not be found in the config table!");
            }
            else if (!int.TryParse(entrantCountConfig.Value, out _))
            {
                Logger.LogError("LAST_ENTRANT_COUNT could not be parsed! Expected integer, got `{Value}`",
                    entrantCountConfig.Value);
            }
            else if (await ctx.Config.FindAsync(new object[] {"CURRENT_TOURNEY_ID"}, cancellationToken) is not
                { } tourneyIdConfig)
            {
                Logger.LogError("CURRENT_TOURNEY_ID could not be found in the config table!");
            }
            else if (!int.TryParse(tourneyIdConfig.Value, out _))
            {
                Logger.LogError("CURRENT_TOURNEY_ID could not be parsed! Expected integer, got `{Value}`",
                    tourneyIdConfig.Value);
            }
            else if (await ctx.Config.FindAsync(new object[] {"NEW_ENTRANT_CHANNEL_ID"}, cancellationToken) is not
                { } channelIdConfig)
            {
                Logger.LogError("NEW_ENTRANT_CHANNEL_ID could not be found in the config table!");
            }
            else if (!Snowflake.TryParse(channelIdConfig.Value, out _))
            {
                Logger.LogError("NEW_ENTRANT_CHANNEL_ID could not be parsed! Expected snowflake, got `{Value}`",
                    channelIdConfig.Value);
            }
            else
            {
                await base.StartAsync(cancellationToken);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                using var scope = _bot.Services.CreateScope();
                await using var ctx = scope.ServiceProvider.GetRequiredService<ShineDbContext>();

                var dict = await ctx.Config.ToDictionaryAsync(x => x.Key, x => x.Value, stoppingToken);
                var tourneyId = int.Parse(dict["CURRENT_TOURNEY_ID"]);
                var lastEntrantCount = int.Parse(dict["LAST_ENTRANT_COUNT"]);
                var channelId = Snowflake.Parse(dict["NEW_ENTRANT_CHANNEL_ID"]);

                var entrants = await ctx.Entrants
                    .OrderBy(x => x.Id)
                    .Where(x => x.TourneyId == tourneyId)
                    .ToListAsync(stoppingToken);

                if (entrants.Count < lastEntrantCount)
                {
                    var configEntry = await ctx.Config.FindAsync(new object[] {"LAST_ENTRANT_COUNT"}, stoppingToken);
                    configEntry.Value = entrants.Count.ToString();
                    await ctx.SaveChangesAsync(stoppingToken);
                }
                else if (entrants.Count > lastEntrantCount)
                {
                    // only get 1 entry at a time to prevent spam
                    var entrant = entrants[lastEntrantCount]; // +1 thanks to zero-indexing
                    var profile = await ctx.Profiles.FindAsync(new object[] {entrant.PlayerId}, stoppingToken);
                    var tourney = await ctx.Tourneys.FirstAsync(x => x.Id == entrant.TourneyId, stoppingToken);
                    var character = profile.GetCharacterName();

                    lastEntrantCount++;

                    var field = new LocalEmbedFieldBuilder()
                        .WithName($"Current number of entrants: {lastEntrantCount}/{tourney.EntrantCap}");

                    if (lastEntrantCount == tourney.EntrantCap)
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

                    await Client.SendMessageAsync(channelId,
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

                    var configEntry = await ctx.Config.FindAsync(new object[] {"LAST_ENTRANT_COUNT"}, stoppingToken);
                    configEntry.Value = lastEntrantCount.ToString();
                    await ctx.SaveChangesAsync(stoppingToken);
                }
                
                await delay;
            }
        }
    }
}