using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qmmands;
using Serilog;
using Serilog.Events;
using Shine.Commands;
using Shine.Common;
using Shine.Database;
using Shine.Extensions;

namespace Shine
{
    public static class Program
    {
        private const string PREFIX = "s!";
        
        private static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();
            
            if (configuration.GetSection("GenerateMarkdown").Exists())
            {
                var service = new CommandService();
                service.AddModules(typeof(CharacterMainCommands).Assembly);
                GenerateCommandMarkdown(service);
                return;
            }
            
            using var host = new HostBuilder()
                .ConfigureAppConfiguration(x =>
                {
                    x.AddEnvironmentVariables("SHINE_");
                })
                .ConfigureLogging(x =>
                {
                    var logger = new LoggerConfiguration()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                        .WriteTo.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                        .WriteTo.File("Logs/log_.txt",
                            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                            rollingInterval: RollingInterval.Day)
                        .CreateLogger();
                    
                    x.AddSerilog(logger, true);
        
                    x.Services.Remove(x.Services.First(y => y.ServiceType == typeof(ILogger<>)));
                    x.Services.AddSingleton(typeof(ILogger<>), typeof(DummyLogger<>));
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<Random>();
                    services.AddSingleton<HttpClient>();
                    services.AddDbContext<ShineDbContext>(options =>
                    {
                        var connectionString = context.Configuration["DB_CONNECTION_STRING"];
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    });
                })
                .ConfigureDiscordBot((context, bot) =>
                {
                    bot.Token = context.Configuration["TOKEN"];
                    bot.OwnerIds = new Snowflake[]
                    {
                        167452465317281793,
                        114926832440180738,
                        176081685702639616 
                    };
                    bot.Prefixes = new[] {PREFIX};
                    bot.Activities = new[]
                    {
                        new LocalActivity(PREFIX, ActivityType.Playing)
                    };
                })
                .Build();

            host.Run();
        }

        private static void GenerateCommandMarkdown(CommandService commandService)
        {
            var builder = new StringBuilder();

            foreach (var module in commandService.TopLevelModules.OrderBy(x => x.Name))
            {
                builder.AppendNewline($"## {module.Name}")
                    .AppendNewline(module.Description)
                    .AppendNewline("|Command|Description|")
                    .AppendNewline("|---|---|");

                foreach (var command in CommandUtilities.EnumerateAllCommands(module))
                {
                    builder.Append('|')
                        .Append(string.Join("<br>",
                            command.FullAliases.Select(x => Markdown.Code($"{PREFIX}{x}{command.FormatArguments()}"))))
                        .Append('|')
                        .Append(command.Description.Replace("\n", "<br>"));

                    foreach (var parameter in command.Parameters)
                    {
                        builder.Append("<br>")
                            .Append(Markdown.Code(parameter.Name))
                            .Append(": ")
                            .Append(parameter.Description.Replace("\n", "<br>"))
                            .Append(parameter.IsOptional ? $" {Markdown.Bold("(optional)")}" : string.Empty);
                    }

                    builder.AppendNewline("|");
                }
            }

            Directory.CreateDirectory("docs");
            File.WriteAllText("docs/Command-List.md", builder.ToString());
        }
    }
}