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
public sealed class GlobalPluginManager : IDisposable
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

	public GlobalPluginManager(Bot bot)
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
	/// <summary>
	/// Updates itself with its own plugins.
	/// </summary>
	/// <returns>How many plugins are iterated through. </returns>
	internal async Task<int> Update()
	{
		using IEnumerator<GlobalPlugin> globalEnumerator = GlobalPlugins.Values.GetEnumerator();
		while (globalEnumerator.MoveNext())
			allTasks.Add(globalEnumerator.Current.Update(bot));
		int output = allTasks.Count;
		await Task.WhenAll(allTasks);
		allTasks.Clear();
		return output;
	}


	public void Dispose()
	{
		threadCanceller.Cancel();
		threadCanceller.Dispose();
	}
}