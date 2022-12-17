using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer.Plugins;

public class Startup : Plugin
{
	public Startup(Bot bot, ulong guildID) : base(bot, guildID)
	{

	}

	public override string Key => nameof(Startup);

	protected internal override async Task OnEnable(Bot bot)
	{
		//bot.socketClient.Rest.DeleteAllGlobalCommandsAsync().Wait();
		var command = new SlashCommandBuilder()
		{
			Name = "ping",
			Description = "Checks the response time for the bot.",
			IsDefaultPermission = true,
			IsDMEnabled = true,
		};
		await bot.socketClient.Rest.CreateGlobalCommand(command.Build());
		await bot.socketClient.SetGameAsync("Deep Rock Galactic", type: ActivityType.Playing);
		bot.socketClient.SlashCommandExecuted += async (command) =>
		{
			if (command.CommandName != "ping")
				return;
			double ms = (DateTime.UtcNow - command.CreatedAt.UtcDateTime).TotalMilliseconds;
			await command.RespondAsync($"Pong! {ms:N0} ms" + ms switch
			{
				<= 0 => ", Thought ahead!",
				<= 68 => ", Zip, zoom!",
				69 => ", Nice!",
				> 500 => ", Feeling sluggish today..",
				_ => "",
			});
		};
	}
}