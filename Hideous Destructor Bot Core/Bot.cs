using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

public class Bot : IDisposable
{
	private bool active = true;
	public readonly string TokenID;
	public readonly DiscordSocketClient socketClient;
	private Thread? autoUpdateThread;
	public event Func<LogMessage, Task> Log;
	public async Task SendLog(LogMessage msg) => await Log.Invoke(msg);

	public IReadOnlyDictionary<ulong, GuildConfig> Configs => configs;//public readonly GuildConfig botState = new() { AutoFlush = true };
	private readonly Dictionary<ulong, GuildConfig> configs = new();

	public IReadOnlyDictionary<ulong, List<Plugin>> ActivePlugins => activePlugins;
	private readonly Dictionary<ulong, List<Plugin>> activePlugins = new();
	public Task AddPlugin(Plugin plugin)
	{
		if (activePlugins.TryGetValue(plugin.CurrentGuild.Id, out var list))
		{
			configs[plugin.CurrentGuild.Id] = new GuildConfig(plugin.CurrentGuild.Id) { AutoFlush = true };
			list.Add(plugin);
		}
		else
			activePlugins.Add(plugin.CurrentGuild.Id, new List<Plugin>() { plugin });
		plugin.OnEnable(this);
		return Task.CompletedTask;
	}
	//public Task RemovePlugin(Plugin plugin)
	//{
	//	activePlugins.Remove(plugin.CurrentGuild.Id);
	//	plugin.OnDisable(this);
	//	return Task.CompletedTask;
	//}

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
		socketClient.Disconnected += async (ex) =>
		{
			TaskCompletionSource connectionSource = new();
			socketClient.Ready += Wait;
			while (!connectionSource.Task.IsCompleted)
				await Connect();
			Task Wait()
			{
				connectionSource.SetResult();
				return Task.CompletedTask;
			}
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
					Console.WriteLine("Cycle Starting..");
					await Update();
					Console.WriteLine("Cycle Completed!");
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
		if (!active)
			return;
		await socketClient.LoginAsync(TokenType.Bot, TokenID, true);
		await socketClient.StartAsync();
		TaskCompletionSource source = new();
		socketClient.Ready += Wait;
		await source.Task;
		socketClient.Ready -= Wait;
		Task Wait()
		{
			source.SetResult();
			return Task.CompletedTask;
		}
	}
	public async Task Update()
	{
		List<Task> plugins = new(activePlugins.Count);
		using var enumerator = activePlugins.GetEnumerator();
		while (enumerator.MoveNext())
		{
			plugins.AddRange(enumerator.Current.Value.Select(item => item.Update(this)));
		}
		await Task.WhenAll(plugins);
	}

	public void Dispose()
	{
		socketClient?.StopAsync().Wait();
		active = false;
	}
}