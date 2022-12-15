using Discord;
using Discord.WebSocket;
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
	static void Main(string[] arguments)
	{
		// Find token
		if (arguments.Length > 0)
			currentBot = new Bot(arguments[0]);
		else
		{
			Console.Write("Type in the bot token: ");
			string tokenString = Console.ReadLine() ?? "";
			currentBot = new Bot(tokenString);
		}

		Console.ReadLine();
		currentBot.socketClient!.MessageReceived += (message) =>
		{
			Console.WriteLine(message);
			return Task.CompletedTask;
		};
		currentBot.socketClient!.Rest.GetGuildAsync(334151720546598915).Result.GetTextChannelAsync(734127505723621417).Result.SendMessageAsync("Haha you foolish mortals").Wait();
		//var channel = currentBot.socketClient!.Rest.GetUserAsync(497048142848720897).Result.CreateDMChannelAsync().Result;
		//string output;
		//do
		//{
		//	output = Console.ReadLine() ?? "";
		//	channel.SendMessageAsync(output).Wait();
		//} while (output != "quit") ;
		//.GetUser(264575345141743618).Mention);//.GetDMChannelAsync(876614003378507796).Result?.SendMessageAsync("Bruh");
		//IReadOnlyCollection<SocketGuild> guilds = currentBot.socketClient!.Guilds;
		//Console.WriteLine(guilds.Count);
		//IReadOnlyCollection<SocketGuildChannel> channels = guilds.First().Channels;
		//Console.WriteLine(channels.Count);
		//((IMessageChannel)channels.First()).SendMessageAsync("Test").Wait();
		//var user = currentBot.socketClient!..GetUser("AnOddDoorKnight", "#0068");
		//Console.WriteLine(user);
		//if (user != null)
		//	user.CreateDMChannelAsync().Result.SendMessageAsync("Bruh").Wait();
		//socketClient.Guilds.First().Channels.First();

		// End
		Console.WriteLine("Ending on exit");
		Console.ReadLine();
		currentBot.Dispose();
	}


}