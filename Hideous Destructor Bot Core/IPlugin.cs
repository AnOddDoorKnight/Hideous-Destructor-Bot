using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

public interface IPlugin : IEquatable<IPlugin>
{
	string Key { get; }

	void AddFunctionality(Bot bot);
	Task UpdateFunctionality(Bot bot)
	{
		return Task.CompletedTask;
	}
	void RemoveFunctionality(Bot bot);

	bool IEquatable<IPlugin>.Equals(IPlugin? other)
	{
		return other is not null && Key == other.Key;
	}
}