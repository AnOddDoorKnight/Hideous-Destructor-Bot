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
		if (BotState.TokenDirectory.Exists)
			token = BotState.Token!;
		else
		{
			Console.Write("Type in the bot token: ");
			token = Console.ReadLine() ?? "";
			BotState.Token = token;
		}
		currentBot = new Bot(token, true);

		await currentBot.Connect();
		_ = currentBot.AddPlugin(new MemeOfTheWeek(currentBot, 334151720546598915, 1048792493711425608, 1053082648840519750));
		_ = currentBot.AddPlugin(new DebugQueue(currentBot, 334151720546598915, 1053370494071607316));
		_ = currentBot.AddPlugin(new Startup(currentBot, 334151720546598915));
		_ = currentBot.AddPlugin(new RockAndStone());

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
			case "force-motw":
				((MemeOfTheWeek)currentBot.ActivePlugins[0]).DoLeaderboard(currentBot).Wait();
				goto default;
			case "quit":
				break;
			default:
				goto input;
		}
		currentBot.Dispose();
	}
}

