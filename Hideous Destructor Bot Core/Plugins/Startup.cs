using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace HideousDestructor.DiscordServer.Plugins;

public class Startup : GlobalPlugin
{
	public readonly Stopwatch stopwatch = new();

	protected internal override async Task OnEnable(Bot bot)
	{
		stopwatch.Start();
		var aboutCommand = new GlobalSlashCommandInfo()
		{
			Bot = bot,
			Builder = new SlashCommandBuilder()
			{
				Name = "about",
				Description = "Shows some stats about the bot",
				IsDefaultPermission = true,
				IsDMEnabled = true,
			},
			Action = async (command) =>
			{
				var embed = new EmbedBuilder()
				{
					Author = new EmbedAuthorBuilder()
					{
						Name = bot.socketClient.GetUser(264575345141743618).ToString(),
						IconUrl = bot.socketClient.GetUser(264575345141743618).GetAvatarUrl(),
					},
					Footer = new EmbedFooterBuilder()
					{
						IconUrl = "https://api.nuget.org/v3-flatcontainer/discord.net/2.2.0/icon",
						Text = "Made with Discord.Net!"
					},
					Timestamp = new DateTimeOffset(DateTime.Now),
					Title = "About",
				};
				embed.Description = $"{bot.socketClient.Guilds.Count} servers";
				await command.RespondAsync(embeds: new Embed[] { embed.Build() });
			},
		};
		var pingCommand = new GlobalSlashCommandInfo()
		{
			Bot = bot,
			Builder = new SlashCommandBuilder()
			{
				Name = "ping",
				Description = "Checks the response time for the bot.",
				IsDefaultPermission = true,
				IsDMEnabled = true,
			},
			Action = async (command) =>
			{
				Stopwatch watch = Stopwatch.StartNew();
				await command.DeferAsync(false);
				watch.Stop();

				long ms = watch.ElapsedMilliseconds;
				await command.ModifyOriginalResponseAsync(msg => msg.Content = new Optional<string>($"🏓 Pong! {ms} ms" + ms switch
				{
					<= 0 => ", Thought ahead!",
					<= 68 => ", Zip, zoom!",
					69 => ", Nice!",
					> 500 => ", Feeling sluggish today..",
					_ => "",
				}));
			},
		};
		await Task.WhenAll(bot.slashMessageHandler.AddListener(pingCommand),
			bot.slashMessageHandler.AddListener(aboutCommand),
			bot.socketClient.SetGameAsync("Deep Rock Galactic", type: ActivityType.Playing));
	}
}