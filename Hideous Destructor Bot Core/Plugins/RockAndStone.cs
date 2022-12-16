using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer.Plugins;

public class RockAndStone : IPlugin
{
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
	public string Key => nameof(RockAndStone);
	public void AddFunctionality(Bot bot)
	{
		bot.socketClient.MessageReceived += (msg) =>
		{
			if (msg.Author.Id == bot.socketClient.CurrentUser.Id)
				return Task.CompletedTask;
			string content = msg.Content.ToLower();
			if (rockAndStone.Any(line => content.Contains(line)))
				return msg.Channel.SendMessageAsync(RandomPhraseGenerators.RockAndStone[Random.Shared.Next(RandomPhraseGenerators.RockAndStone.Count)]);
			if (cockAndBone.Any(line => content.Contains(line)))
				return msg.Channel.SendMessageAsync(RandomPhraseGenerators.CockAndBone[Random.Shared.Next(RandomPhraseGenerators.CockAndBone.Count)]);
			return Task.CompletedTask;
		};
	}

	public void RemoveFunctionality(Bot bot)
	{
		
	}
}
