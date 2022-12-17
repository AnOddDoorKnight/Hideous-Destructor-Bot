using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer.Plugins;

public class FunAndGames : Plugin
{
	public FunAndGames(Bot bot, ulong guildID) : base(bot, guildID)
	{

	}

	public override string Key => nameof(FunAndGames);

	protected internal override Task OnEnable(Bot bot)
	{
		// Guess the Number
		//var command = new SlashCommandBuilder()
		//{
		//	Name = "game",
		//	
		//	Description = "Checks the response time for the bot.",
		//	IsDefaultPermission = true,
		//	IsDMEnabled = true,
		//};
		//bot.socketClient.Rest.CreateGlobalCommand(command.Build());
		//bot.socketClient.SlashCommandExecuted += async (command) =>
		//{
		//	if (command.CommandName != "ping")
		//		return;
		//	double ms = (DateTime.UtcNow - command.CreatedAt.UtcDateTime).TotalMilliseconds;
		//	await command.RespondAsync($"Pong! {ms:N0} ms" + ms switch
		//	{
		//		<= 0 => "\nThought ahead!",
		//		> 5000 => "\nFeeling sluggish today..",
		//		_ => "",
		//	});
		//};
		return Task.CompletedTask;
	}
}
