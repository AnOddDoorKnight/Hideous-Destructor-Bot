using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

public class Bot : IDisposable
{
	static Bot()
	{
		CreateTextWriter();
	}
	private static void CreateTextWriter()
	{
		DirectoryInfo info = new(Directory.GetCurrentDirectory());
		info = info.CreateSubdirectory("Debug");
		TextWriterTraceListener listener = new($"{info.FullName}/{DateTime.Now}.txt");
		Trace.Listeners.Add(listener);
		Debug.AutoFlush = true;
	}



	public readonly string TokenID;
	public readonly DiscordSocketClient socketClient;
	private Thread? autoUpdateThread;

	private List<IPlugin> activePlugins = new();
	public async Task AddPlugin(IPlugin plugin)
	{
		activePlugins.Add(plugin);
		plugin.AddFunctionality(this);
		if (plugin.Commands == null)
			return;
		//var list = socketClient.GetGlobalApplicationCommandsAsync().Result.ToList();
		//using var enumerator = plugin.Commands.GetEnumerator();
		//ApplicationCommandOptionChoiceProperties applicationCommandOptionChoiceProperties = new()
		//{
		//	
		//};
		//while (enumerator.MoveNext())
		//	if (!list.Contains(enumerator.Current))
		//		socketClient.CreateGlobalApplicationCommandAsync();
			
	}
	//public void RemovePlugin(IPlugin plugin)
	//{
	//
	//}

	public Bot(string tokenID, bool autoUpdate)
	{
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
		if (autoUpdate)
		{

			autoUpdateThread = new Thread(async () =>
			{
				TaskCompletionSource source = new();
				socketClient.Ready += Wait;
				await source.Task;
				while (true)
				{
					await Update();
					await Task.Delay(10000);
				}
				Task Wait()
				{
					source.SetResult();
					return Task.CompletedTask;
				}
			});
			autoUpdateThread.Start();
		}	
	}
	public async Task Connect()
	{
		await socketClient.LoginAsync(TokenType.Bot, TokenID, true);
		await socketClient.StartAsync();
		TaskCompletionSource source = new();
		socketClient.Ready += Wait;
		await source.Task;
		Task Wait()
		{
			source.SetResult();
			return Task.CompletedTask;
		}
	}
	public async Task Update()
	{
		List<Task> plugins = new(activePlugins.Count);
		for (int i = 0; i < activePlugins.Count; i++)
		{
			plugins.Add(activePlugins[i].UpdateFunctionality(this));
		}
		await Task.WhenAll(plugins);
	}

	public void Dispose()
	{
		socketClient?.StopAsync().Wait();
	}
}