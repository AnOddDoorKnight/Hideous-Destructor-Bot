using HideousDestructor.DiscordServer.IO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

/// <summary>
/// Manages all the guilds w/ plugins.
/// </summary>
public sealed class PluginManager : IDisposable
{
	/// <summary>
	/// All the plugins separated by the guild ID, and then the plugin name.
	/// </summary>
	private readonly Dictionary<ulong, Dictionary<string, (ServerPlugin plugin, bool? enabled)>> ServerPlugins = new();
	/// <summary>
	/// The configurations to dictate if the plugin should start on launch or not
	/// from individual guilds.
	/// </summary>
	private readonly Dictionary<ulong, PluginEnablerConfig> GuildConfigs = new();
	/// <summary>
	/// A list of global plugins.
	/// </summary>
	private readonly Dictionary<string, GlobalPlugin> GlobalPlugins = new();
	/// <summary>
	/// A custom thread for keeping the servers up to date.
	/// </summary>
	private readonly Thread autoUpdateThread;
	private readonly CancellationTokenSource threadCanceller = new();
	private readonly Bot bot;

	public PluginManager(Bot bot)
	{
		this.bot = bot;
		autoUpdateThread = new Thread(async () =>
		{
			const int baseCount = 5000,
				serverAdditive = 500;
			while (!threadCanceller.IsCancellationRequested)
			{
				Console.WriteLine("Cycling...");
				await Update();
				await Task.Delay(baseCount + (serverAdditive * ServerPlugins.Count), threadCanceller.Token);
			}
		});
		autoUpdateThread.Start();
	}

	public ServerPluginMetadata this[string pluginName, ulong guildID]
	{
		get => new(guildID, ServerPlugins[guildID][pluginName].plugin, this);
	}

	/// <summary>
	/// Adds and enables the specified plugin. This is guild-specific, and there
	/// can only be one running per guild.
	/// </summary>
	/// <param name="serverPlugin"> The server plugin to run. </param>
	/// <exception cref="DuplicateNameException"/>
	public async Task<ServerPluginMetadata> AddPlugin(ServerPlugin serverPlugin)
	{
		string pluginName = serverPlugin.GetType().Name;
		if (ServerPlugins.TryGetValue(serverPlugin.CurrentGuild.Id, out var guildDictionary))
		{
			if (guildDictionary.ContainsKey(pluginName))
				throw new DuplicateNameException($"There is already a guild '{serverPlugin.CurrentGuild.Name}' with plugin '{pluginName}'!");
			guildDictionary[pluginName] = (serverPlugin, null);
			if (GuildConfigs[serverPlugin.CurrentGuild.Id][pluginName] == null)
				GuildConfigs[serverPlugin.CurrentGuild.Id][pluginName] = (bool)serverPlugin.GetType().GetProperty(nameof(ServerPlugin.StartEnabled), BindingFlags.Static | BindingFlags.Public)!.GetValue(null)!;
			var metaData = this[pluginName, serverPlugin.CurrentGuild.Id];
			metaData.SetActive(GuildConfigs[serverPlugin.CurrentGuild.Id][pluginName]!.Value);
			return metaData;
		}
		ServerPlugins.Add(serverPlugin.CurrentGuild.Id, new Dictionary<string, (ServerPlugin plugin, bool? enabled)>());
		GuildConfigs.Add(serverPlugin.CurrentGuild.Id, new PluginEnablerConfig(serverPlugin.CurrentGuild.Id));
		return await AddPlugin(serverPlugin);
	}
	public async Task AddPlugin(GlobalPlugin globalPlugin)
	{
		string name = globalPlugin.GetType().Name;
		if (GlobalPlugins.ContainsKey(name))
			throw new DuplicateNameException($"There is already a global plugin named {name}!");
		GlobalPlugins.Add(name, globalPlugin);
		await globalPlugin.OnEnable(bot);
	}

	//public Task RemovePlugin(ServerPlugin serverPlugin)
	//{
	//	return RemovePlugin(serverPlugin.CurrentGuild.Id, serverPlugin.GetType().Name);
	//}
	//public async Task RemovePlugin(ulong guildID, string pluginName)
	//{
	//	int index = serverPluginKeys[guildID].Count(set => set != pluginName);
	//	Console.WriteLine($"Moved item '{ServerPlugins[guildID][index].GetType().Name}' with '{pluginName}");
	//	ServerPlugin plugin = ServerPlugins[guildID][index];
	//	ServerPlugins[guildID].RemoveAt(index);
	//	serverPluginKeys[guildID].Remove(pluginName);
	//	await plugin.Update(bot);
	//}


	private readonly List<Task> allTasks = new();
	internal async Task Update()
	{
		using IEnumerator<GlobalPlugin> globalEnumerator = GlobalPlugins.Values.GetEnumerator();
		while (globalEnumerator.MoveNext())
			allTasks.Add(globalEnumerator.Current.Update(bot));
		using IEnumerator<ServerPlugin> serverEnumerator = ServerPlugins.SelectMany(coll => coll.Value.Values)
			.Where(pair => pair.enabled == true).Select(pair => pair.plugin).GetEnumerator();
		while (serverEnumerator.MoveNext())
			allTasks.Add(serverEnumerator.Current.Update(bot));
		await Task.WhenAll(allTasks);
		allTasks.Clear();
	}


	public void Dispose()
	{
		threadCanceller.Cancel();
		threadCanceller.Dispose();
	}




	public readonly struct ServerPluginMetadata
	{
		private readonly PluginManager pluginManager;
		private readonly ulong guildID;
		public bool? Enabled
		{
			get => pluginManager.ServerPlugins[guildID][name.Value].enabled;
		}
		public void SetActive(bool value)
		{
			bool? capturedValue = Enabled;
			if (value == Enabled && capturedValue.HasValue)
				return;
			pluginManager.ServerPlugins[guildID][name.Value] = (serverPlugin, value);
			pluginManager.GuildConfigs[guildID][name.Value] = value;
			if (value)
				serverPlugin.OnEnable(pluginManager.bot);
			else if (capturedValue == true)
				serverPlugin.OnDisable(pluginManager.bot);
		}

		private readonly ServerPlugin serverPlugin;
		private readonly Lazy<string> name;

		internal ServerPluginMetadata(ulong guildID, ServerPlugin serverPlugin, PluginManager manager)
		{
			pluginManager = manager;
			this.guildID = guildID;
			this.serverPlugin = serverPlugin;
			name = new Lazy<string>(() => serverPlugin.GetType().Name);
		}
	}
}