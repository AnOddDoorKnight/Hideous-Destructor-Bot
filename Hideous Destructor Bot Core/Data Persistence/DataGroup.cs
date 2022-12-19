using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace HideousDestructor.DiscordServer.IO;

/// <summary>
/// Retrieves all data as a dictionary, split into guilds/servers with their IDs
/// </summary>
public sealed class DataGroup
{
	/// <summary>
	/// Default settings for <see cref="XmlWriter"/>s.
	/// </summary>
	public static XmlWriterSettings Settings { get; } = new XmlWriterSettings()
	{
		Indent = true,
		IndentChars = "\t"
	};
	/// <summary>
	/// The source directory.
	/// </summary>
	public static DirectoryInfo CurrentDirectory { get; } = 
		new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
			.CreateSubdirectory("Hideous Destructor Bot");
	
	public static DirectoryInfo GuildsDirectory { get; } = CurrentDirectory
		.CreateSubdirectory("Guild Info");
	/// <summary>
	/// Gets the guild directory via the ID.
	/// </summary>
	public static DirectoryInfo GetSpecificGuildDirectory(ulong guildID) =>
		GuildsDirectory.CreateSubdirectory(guildID.ToString());
	/// <summary>
	/// Updates all guild directories
	/// </summary>
	public static void UpdateDirectories(IEnumerable<IGuild> guild)
	{
		HashSet<ulong> allDirectories = GuildsDirectory.EnumerateDirectories()
			.Where(dir => ulong.TryParse(dir.Name, out _))
			.Select(dir => ulong.Parse(dir.Name)).ToHashSet();
		using var enumerator = guild.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (allDirectories.Contains(enumerator.Current.Id))
				continue;
			GetSpecificGuildDirectory(enumerator.Current.Id);
		}
	}
}
