using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

public class Bot : IDisposable
{
	private static void CreateTextWriter()
	{
		DirectoryInfo info = new(Directory.GetCurrentDirectory());
		info = info.CreateSubdirectory("Debug");
		TextWriterTraceListener listener = new($"{info.FullName}/{DateTime.Now}.txt");
		Trace.Listeners.Add(listener);
		Debug.AutoFlush = true;
	}

	public readonly string TokenID;
	public readonly DiscordSocketClient? socketClient;

	public Bot(string tokenID)
	{
		CreateTextWriter();
		Debug.WriteLine($"Creating new bot with token '{tokenID}'..");
		TokenID = tokenID;
		socketClient = new DiscordSocketClient(new DiscordSocketConfig()
		{
			LogLevel = LogSeverity.Verbose,
			AlwaysDownloadUsers = true,
			GatewayIntents = GatewayIntents.AllUnprivileged,
		});
		socketClient.Log += (message) =>
		{
			Console.WriteLine(message);
			Debug.WriteLine(message);
			return Task.CompletedTask;
		};
		socketClient.LoginAsync(TokenType.Bot, tokenID, true).Wait();
		socketClient.StartAsync().Wait();
		//socketClient.SetStatusAsync(UserStatus.Idle);
	}

	public void Dispose()
	{
		socketClient?.StopAsync().Wait();
	}
}