using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

/// <summary>
/// The original plugin, or a global plugin, that is not specific to any server
/// or guild.
/// </summary>
public abstract class GlobalPlugin
{
	protected internal virtual Task OnEnable(Bot bot)
	{
		return Task.CompletedTask;
	}
	protected internal virtual Task Update(Bot bot)
	{
		return Task.CompletedTask;
	}
	protected internal virtual Task OnDisable(Bot bot)
	{
		return Task.CompletedTask;
	}
}
/// <summary>
/// A plugin that is localized in a server.
/// </summary>
public abstract class ServerPlugin : GlobalPlugin
{
	public static bool StartEnabled => false;
	public SocketGuild CurrentGuild { get; }

	public ServerPlugin(Bot bot, ulong guildID)
	{
		CurrentGuild = bot.socketClient.GetGuild(guildID);
	}
}