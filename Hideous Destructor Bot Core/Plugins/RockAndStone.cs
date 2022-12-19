using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer.Plugins;

public sealed class RockAndStone : ServerPlugin
{
	const string key = "RockAndStone";

	private static readonly string[] rockAndStone = new string[] 
	{
		"rock",
		"stone",
		"karl",
		"dwarf",
	};
	private static readonly string[] cockAndBone = new string[]
	{
		"cock",
	};

	public RockAndStone(Bot bot, ulong guildID) : base(bot, guildID)
	{
		
	}

	public static new bool StartEnabled => false;

	protected internal override Task OnEnable(Bot bot)
	{
		bot.socketClient.MessageReceived += (msg) =>
		{
			if (CurrentGuild.GetChannel(msg.Channel.Id) == null)
				return Task.CompletedTask;
			if (msg.Author.Id == bot.socketClient.CurrentUser.Id)
				return Task.CompletedTask;
			string content = msg.Content.ToLower();
			if (rockAndStone.Any(line => content.Contains(line)))
				return msg.Channel.SendMessageAsync(RandomPhraseGenerators.RockAndStone[Random.Shared.Next(RandomPhraseGenerators.RockAndStone.Count)]);
			if (cockAndBone.Any(line => content.Contains(line)))
				return msg.Channel.SendMessageAsync(RandomPhraseGenerators.CockAndBone[Random.Shared.Next(RandomPhraseGenerators.CockAndBone.Count)]);
			return Task.CompletedTask;
		};
		return Task.CompletedTask;
	}
}
