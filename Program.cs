using System;
using System.Net.Http;
using Disqord;
using Disqord.Bot.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shine
{
    public static class Program
    {
        private static void Main()
        {
            using var host = new HostBuilder()
                .ConfigureAppConfiguration(x =>
                {
                    x.AddEnvironmentVariables("SHINE_");
                })
                .ConfigureServices(x =>
                {
                    x.AddSingleton<Random>();
                    x.AddSingleton<HttpClient>();
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
                    bot.Prefixes = new[] {"s!"};
                })
                .Build();
            
            host.Run();
        }
    }
}