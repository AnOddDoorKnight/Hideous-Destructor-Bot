using Discord.WebSocket;
using HideousDestructor.DiscordServer.IO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static HideousDestructor.DiscordServer.GlobalPluginManager;

namespace HideousDestructor.DiscordServer;

public class GuildManager
{
	public readonly SocketGuild guild;
	public readonly PluginEnablerConfig pluginEnablerConfig;
	public readonly Bot bot;
	public IReadOnlyDictionary<string, (ServerPlugin plugin, bool? enabled)> Plugins => m_plugins;
	private readonly Dictionary<string, (ServerPlugin plugin, bool? enabled)> m_plugins = new();
	public GuildManager(Bot bot, SocketGuild socketGuild)
	{
		this.bot = bot;
		guild = socketGuild;
		pluginEnablerConfig = new PluginEnablerConfig(socketGuild.Id);
	}
	/// <summary>
	/// Updates all plugins stored in the guild manager.
	/// </summary>
	public async Task Update()
	{
		await Task.WhenAll(m_plugins.Values
			.Where(pair => pair.enabled == true)
			.Select(pair => pair.plugin.Update(bot)));
	}
	internal async Task SetActive(string key, bool value)
	{
		bool? capturedValue = m_plugins[key].enabled;
		if (value == capturedValue && capturedValue.HasValue)
			return;
		m_plugins[key] = (m_plugins[key].plugin, value);
		pluginEnablerConfig[key] = value;
		if (value)
			await m_plugins[key].plugin.OnEnable(bot);
		else if (capturedValue == true)
			await m_plugins[key].plugin.OnDisable(bot);
	}

	public async Task<ServerPluginMetadata> AddPlugin(ServerPlugin serverPlugin)
	{
		if (serverPlugin.CurrentGuild.Id != guild.Id)
			throw new InvalidConstraintException($"{serverPlugin.CurrentGuild.Name} is not {guild.Name}!");
		string pluginName = serverPlugin.GetType().Name;
		if (Plugins.ContainsKey(pluginName))
			throw new DuplicateNameException($"There is already a guild '{serverPlugin.CurrentGuild.Name}' with plugin '{pluginName}'!");
		m_plugins[pluginName] = (serverPlugin, null); 
		if (pluginEnablerConfig[pluginName] == null)
			pluginEnablerConfig[pluginName] = (bool)serverPlugin.GetType().GetProperty(nameof(ServerPlugin.StartEnabled), BindingFlags.Static | BindingFlags.Public)!.GetValue(null)!;
		ServerPluginMetadata metaData = new(guild, serverPlugin, this);
		await metaData.SetActive(pluginEnablerConfig[pluginName]!.Value);
		return metaData;
	}

	public readonly struct ServerPluginMetadata
	{
		private readonly GuildManager guildManager;
		public readonly SocketGuild guild;
		public bool? Enabled
		{
			get => guildManager.pluginEnablerConfig[name.Value];
		}
		public async Task SetActive(bool value) => await guildManager.SetActive(serverPlugin.GetType().Name, value);

		private readonly ServerPlugin serverPlugin;
		private readonly Lazy<string> name;

		internal ServerPluginMetadata(SocketGuild guild, ServerPlugin serverPlugin, GuildManager guildManager)
		{
			this.guildManager = guildManager;
			this.guild = guild;
			this.serverPlugin = serverPlugin;
			name = new Lazy<string>(() => serverPlugin.GetType().Name);
		}
	}
}
