using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

public class Bot : IDisposable
{
	public readonly string TokenID;
	public readonly DiscordSocketClient socketClient;
	private Thread? autoUpdateThread;
	public event Func<LogMessage, Task> Log;
	public async Task SendLog(LogMessage msg) => await Log.Invoke(msg);

	public readonly BotState botState = new() { AutoFlush = true };

	public IReadOnlyList<IPlugin> ActivePlugins => activePlugins;
	private readonly List<IPlugin> activePlugins = new();
	public Task AddPlugin(IPlugin plugin)
	{
		activePlugins.Add(plugin);
		plugin.AddFunctionality(this);
		return Task.CompletedTask;
	}
	public Task RemovePlugin(IPlugin plugin)
	{
		activePlugins.Remove(plugin);
		plugin.RemoveFunctionality(this);
		return Task.CompletedTask;
	}

	public Bot(string tokenID, bool autoUpdate)
	{
		Debug.WriteLine($"Creating new bot with token '{tokenID}'..");
		TokenID = tokenID;
		socketClient = new DiscordSocketClient(new DiscordSocketConfig()
		{
			LogLevel = LogSeverity.Verbose,
			AlwaysDownloadUsers = true,
			GatewayIntents = GatewayIntents.All,
		});
		Log += (msg) =>
		{
			Console.WriteLine(msg);
			return Task.CompletedTask;
		};
		socketClient.Log += (message) =>
		{
			return Log.Invoke(message);
		};
		// var option = new SlashCommandBuilder();
		// option.WithName("check-alive");
		// option.WithDescription("Checks if the bot is alive");
		// await socketClient.Rest.CreateGlobalCommand(option.Build());
		if (autoUpdate)
		{
			autoUpdateThread = new Thread(async () =>
			{
				TaskCompletionSource source = new();
				socketClient.Ready += Wait;
				await source.Task;
				while (true)
				{
					await Task.Delay(10000);
					await Update();
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