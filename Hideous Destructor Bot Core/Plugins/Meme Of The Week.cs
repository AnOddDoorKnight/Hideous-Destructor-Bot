using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using HideousDestructor.DiscordServer.IO;

namespace HideousDestructor.DiscordServer.Plugins;

public sealed class MemeOfTheWeek : ServerPlugin
{
	private SimpleXmlDocument xmlDocument;

	private const string lastDate = "lastDateDone";
	public DateTime LastDate
	{
		get => DateTime.Parse(xmlDocument.GetOrDefault(lastDate, default(DateTime).ToString()));
		set => xmlDocument[lastDate] = value.ToString();
	}

	private const string doOnDayOfWeek = "doOnDayOfWeek";
	public DayOfWeek DayOfWeek
	{
		get => Enum.Parse<DayOfWeek>(xmlDocument.GetOrDefault(doOnDayOfWeek, ((DayOfWeek)0).ToString()));
		set => xmlDocument[doOnDayOfWeek] = value.ToString();
	}
	public bool IsOnDayOfWeek =>
		(DateTime.Today.DayOfWeek == DayOfWeek && (DateTime.Today - LastDate).TotalDays >= 1.15d)
		|| (DateTime.Today - LastDate).TotalDays > 8d;

	private const string winnerCount = "winnerCount";
	public int WinnerCount
	{
		get => int.Parse(xmlDocument.GetOrDefault(winnerCount, "3"));
		set => xmlDocument[winnerCount] = value.ToString();
	}

	public static new bool StartEnabled => true;


	public SocketTextChannel Submissions { get; private set; }
	private const string submissionsKey = "submissionsID";
	public ulong SubmissionsID
	{
		get => ulong.Parse(xmlDocument.GetOrDefault(submissionsKey, "1048792493711425608"));
		set
		{
			xmlDocument[submissionsKey] = value.ToString();
			Submissions = CurrentGuild.GetTextChannel(value);
		}
	}
	public SocketTextChannel Leaderboard { get; private set; }
	private const string leaderboardKey = "leaderboardID";
	public ulong LeaderboardID
	{
		get => ulong.Parse(xmlDocument.GetOrDefault(leaderboardKey, "1053082648840519750"));
		set
		{
			xmlDocument[leaderboardKey] = value.ToString();
			Submissions = CurrentGuild.GetTextChannel(value);
		}
	}

	public IEmote Emote { get; private set; }
	private readonly Bot bot;

	public MemeOfTheWeek(Bot bot, ulong guildID) : base(bot, guildID)
	{
		xmlDocument = new(guildID, "Meme Of The Week");
		Submissions = CurrentGuild.GetTextChannel(SubmissionsID);
		Leaderboard = CurrentGuild.GetTextChannel(LeaderboardID);
		Emote = CurrentGuild.Emotes.First(emote => emote.Name == "upvote");
		this.bot = bot;
	}

	protected internal override async Task OnEnable(Bot bot)
	{
		bot.socketClient.MessageReceived += MessageRecieved;
		await SetGlobalCommands(bot);

		// Upvotes all previous messages
		Thread addReactions = new Thread(async () =>
		{
			List<Task> queuedMessages = new(byte.MaxValue);
			var enumerable = Submissions.GetMessagesAsync().GetAsyncEnumerator();
			while (await enumerable.MoveNextAsync())
			{
				IMessage[] messageCollection = enumerable.Current.ToArray();
				for (int i = 0; i < messageCollection.Length; i++)
				{
					// Manual wait limit
					await Task.Delay(1600);
					bool hasBot = messageCollection[i].GetReactionUsersAsync(Emote, int.MaxValue)
						.ToEnumerable().SelectMany(collection => collection.Select(user => user.Id))
						.ToHashSet().Contains(bot.socketClient.CurrentUser.Id);
					if (!hasBot)
					{
						Console.WriteLine($"Adding reaction to '{messageCollection[i]}'");
						queuedMessages.Add(messageCollection[i].AddReactionAsync(Emote));
					}
					else
						goto end;
				}
			}
		end:
			await Task.WhenAll(queuedMessages);
		});
		addReactions.Start();
	}
	private async Task SetGlobalCommands(Bot client)
	{
		const string name = "motw";
		//IReadOnlyCollection<SocketApplicationCommand> collection = await client.GetGlobalApplicationCommandsAsync();
		//if (collection.Any(item => item.Name == name))
		//	return;
		var command = new SlashCommandBuilder()
		{
			Name = name,
			Description = "Meme of the week settings",
			IsDefaultPermission = true,
			IsDMEnabled = false,
		};
		command.AddOption("force", ApplicationCommandOptionType.SubCommand, "forces the motw to pass");
		await client.socketClient.Rest.CreateGuildCommand(command.Build(), CurrentGuild.Id);
		client.socketClient.SlashCommandExecuted += async (msg) =>
		{
			if (CurrentGuild.GetChannel(msg.ChannelId!.Value) == null)
				return;
			if (msg.CommandName != "motw" || msg.Data.Options.First().Name != "force")
				return;
			_ = ForceLeaderboardChangeCommand(msg);
		};
	}
	protected internal override async Task Update(Bot bot)
	{
		if (!IsOnDayOfWeek)
			return;
		await DoLeaderboard(bot);
	}

	private async Task ForceLeaderboardChangeCommand(SocketSlashCommand msg)
	{
		await msg.DeferAsync(false);
		DateTime time = DateTime.Today;
		do
			time = time.AddDays(1d);
		while (time.DayOfWeek != DayOfWeek.Wednesday);
		await DoLeaderboard(bot);
		await msg.ModifyOriginalResponseAsync(properties =>
		{
			properties.Content = new Optional<string>($"Forced! Next expected time to start again is at {time.ToShortDateString()}, approximately {(time - DateTime.Today).TotalDays:N0} days from now!");
		});
	}
	public async Task DoLeaderboard(Bot bot)
	{

		new List<IMessage>(Leaderboard.GetMessagesAsync()
			.ToEnumerable().SelectMany(collection => collection)).ForEach(msg => msg.DeleteAsync().Wait());
		List<IMessage> allMessages = new(Submissions.GetMessagesAsync()
			.ToEnumerable().SelectMany(collection => collection)
			.OrderByDescending(message => message.Reactions[Emote].ReactionCount));
		for (int i = 0; i < Math.Min(allMessages.Count, WinnerCount); i++)
		{
			if (allMessages[i].Attachments.Count > 0)
			{
				var attachments = FileDownloader.GetImages(allMessages[i].Attachments).Result.AsAttachments();
				await Leaderboard.SendFilesAsync(attachments, $"**{i + 1}. {allMessages[i].Author.Username} with {allMessages[i].Reactions[Emote].ReactionCount - 1} votes**\n-----:\n\n{allMessages[i].Content}");
				attachments.DisposeAll();
			}
			else
				await Leaderboard.SendMessageAsync($"**{i + 1}. {allMessages[i].Author.Username} with {allMessages[i].Reactions[Emote].ReactionCount - 1} votes**\n-----:\n\n{allMessages[i].Content}");

		}
		LastDate = DateTime.Today;
		await Submissions.DeleteMessagesAsync(allMessages);
	}

	private async Task MessageRecieved(SocketMessage socketMessage)
	{
		if (socketMessage.Channel.Id != Submissions.Id)
			return;
		await socketMessage.AddReactionAsync(Emote);

	}
}