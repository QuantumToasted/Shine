using System;
using System.Linq;
using System.Net.Http;
using Disqord;
using Disqord.Bot.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Shine.Common;

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
                .ConfigureLogging(x =>
                {
                    var logger = new LoggerConfiguration()
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