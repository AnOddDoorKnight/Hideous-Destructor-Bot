using Discord;
using Discord.Rest;
using Discord.WebSocket;
using HideousDestructor.DiscordServer.IO;
using HideousDestructor.DiscordServer.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

public class Bot : IDisposable
{
	public readonly string TokenID;
	public readonly DiscordSocketClient socketClient;
	public event Func<LogMessage, Task> Log;
	public async Task SendLog(LogMessage msg) => await Log.Invoke(msg);

	public SocketGuild[] AllServers => socketClient.Guilds.ToArray();

	public readonly PluginManager pluginManager;
	internal readonly SlashMessageHandler slashMessageHandler;

	//public IReadOnlyDictionary<ulong, GuildConfig> Configs => configs;//public readonly GuildConfig botState = new() { AutoFlush = true };
	//private readonly Dictionary<ulong, GuildConfig> configs = new();
	//
	//public IReadOnlyDictionary<ulong, List<Plugin>> ActivePlugins => activePlugins;
	//private readonly Dictionary<ulong, List<Plugin>> activePlugins = new();
	//public Task AddPlugin(Plugin plugin)
	//{
	//	if (activePlugins.TryGetValue(plugin.CurrentGuild.Id, out var list))
	//	{
	//		configs[plugin.CurrentGuild.Id] = new GuildConfig(plugin.CurrentGuild.Id) { AutoFlush = true };
	//		list.Add(plugin);
	//	}
	//	else
	//		activePlugins.Add(plugin.CurrentGuild.Id, new List<Plugin>() { plugin });
	//	plugin.OnEnable(this);
	//	return Task.CompletedTask;
	//}
	//public Task RemovePlugin(Plugin plugin)
	//{
	//	activePlugins.Remove(plugin.CurrentGuild.Id);
	//	plugin.OnDisable(this);
	//	return Task.CompletedTask;
	//}

	public Bot(string tokenID)
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
		pluginManager = new PluginManager(this);
		slashMessageHandler = new SlashMessageHandler(this);
	}
	public async Task Connect()
	{
		await socketClient.LoginAsync(TokenType.Bot, TokenID, true);
		await socketClient.StartAsync();
		TaskCompletionSource source = new();
		socketClient.Ready += Wait;
		await source.Task;
		socketClient.Ready -= Wait;
		DataGroup.UpdateDirectories(socketClient.Guilds);
		pluginManager.AddPlugin(new Startup()).Wait();
		Task Wait()
		{
			source.SetResult();
			return Task.CompletedTask;
		}
	}

	public void Dispose()
	{
		socketClient?.StopAsync().Wait();
	}
}