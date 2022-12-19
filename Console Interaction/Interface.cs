using Discord;
using Discord.WebSocket;
using HideousDestructor.DiscordServer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer.ConsoleInteraction;

public static class Interface
{
#pragma warning disable CS8618
	public static Bot currentBot;
#pragma warning restore CS8618
	static async Task Main()
	{
		// Find token
		string token;
		if (GuildConfig.TokenDirectory.Exists)
			token = GuildConfig.Token!;
		else
		{
			Console.Write("Type in the bot token: ");
			token = Console.ReadLine() ?? "";
			GuildConfig.Token = token;
		}
		currentBot = new Bot(token);

		await currentBot.Connect();
		await currentBot.pluginManager.AddPlugin(new MemeOfTheWeek(currentBot, 334151720546598915));
		await currentBot.pluginManager.AddPlugin(new FunAndGames(currentBot, 334151720546598915));
		await currentBot.pluginManager.AddPlugin(new RockAndStone(currentBot, 334151720546598915));

		// End
		Console.WriteLine("Ending on exit");
	input:
		string output = (Console.ReadLine() ?? "").ToLower();
		currentBot.SendLog(new LogMessage(LogSeverity.Info, "User", output)).Wait();
		switch (output)
		{
			case "dm":
				ulong dmID = ulong.Parse(Console.ReadLine() ?? "264575345141743618");
				string message = Console.ReadLine() ?? "_ _";
				currentBot.socketClient.GetUser(dmID).SendMessageAsync(message).Wait();
				goto default;
			//case "force-motw":
			//	((MemeOfTheWeek)currentBot.ActivePlugins[0]).DoLeaderboard(currentBot).Wait();
			//	goto default;
			case "quit":
				break;
			default:
				goto input;
		}
		currentBot.Dispose();
	}
}

