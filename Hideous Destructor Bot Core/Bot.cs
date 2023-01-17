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

	public IReadOnlyDictionary<ulong, GuildManager> Guilds => guildPlugins;
	private Dictionary<ulong, GuildManager> guildPlugins = new();
	public GuildManager AddNewGuild(SocketGuild guild)
	{
		GuildManager output = new(this, guild);
		guildPlugins.Add(guild.Id, output);
		return output;
	}
	public Task<GuildManager.ServerPluginMetadata> AddServerPlugin(ServerPlugin serverPlugin)
	{
		return guildPlugins[serverPlugin.CurrentGuild.Id].AddPlugin(serverPlugin);
	}

	public GlobalPluginManager GlobalPlugins { get; }
	internal readonly SlashMessageHandler slashMessageHandler;

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
			Console.WriteLine(ex);
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
		GlobalPlugins = new GlobalPluginManager(this);
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
		GlobalPlugins.AddPlugin(new Startup()).Wait();
		Task Wait()
		{
			source.SetResult();
			return Task.CompletedTask;
		}
	}
	public void Dispose()
	{
		GC.SuppressFinalize(this);
		socketClient?.StopAsync().Wait();
	}
}