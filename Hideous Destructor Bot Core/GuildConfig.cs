using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HideousDestructor.DiscordServer;

public class GuildConfig
{
	private static DirectoryInfo CurrentDirectory { get; } =
		new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
			.CreateSubdirectory("Hideous Destructor Bot");
	public FileInfo PersistentData(ulong guildID) =>
		new FileInfo(CurrentDirectory.FullName + $"/{guildID}.xml");
	public static FileInfo TokenDirectory { get; } =
		new FileInfo(CurrentDirectory.FullName + "/token.txt");
	public static string? Token
	{
		get => TokenDirectory.Exists ? File.ReadAllText(TokenDirectory.FullName) : null;
		set
		{
			var text = File.CreateText(TokenDirectory.FullName);
			text.Write(value);
			text.Flush();
		}
	}

	public IReadOnlyDictionary<string, string> Contents => contents;
	private readonly Dictionary<string, string> contents;
	public bool AutoFlush { get; set; } = false;
	private readonly ulong guildID;

	public GuildConfig(ulong guildID)
	{
		this.guildID = guildID;
		contents = new Dictionary<string, string>();
		if (PersistentData(guildID).Exists)
		{
			XmlDocument document = new XmlDocument();
			document.Load(PersistentData(guildID).FullName);
			XmlNodeList contentNodeData = document.LastChild!.ChildNodes;
			for (int i = 0; i < contentNodeData.Count; i++)
			{
				contents.Add(contentNodeData[i]!.Name, contentNodeData[i]!.InnerText);
			}
			using XmlWriter writer = XmlWriter.Create(PersistentData(guildID).FullName, new XmlWriterSettings() { Indent = true, IndentChars = "\t" });
			document.WriteTo(writer);
		}
		else
		{
			using XmlWriter writer = XmlWriter.Create(PersistentData(guildID).FullName);
			writer.WriteStartElement("GuildConfig");
			writer.WriteEndElement();
		}
	}
	public string this[string key]
	{
		get => contents[key];
		set
		{
			contents[key] = value;
			if (AutoFlush)
				Flush();
		}
	}
	public string GetOrDefault(string key, in string @default)
	{
		if (Contents.TryGetValue(key, out var value))
			return value;
		this[key] = @default;
		return @default;
	}

	public void AddElement(string key, string value)
	{
		contents.Add(key, value);
		if (AutoFlush)
			Flush();
	}

	public void Flush()
	{
		XmlDocument document = new();
		document.Load(PersistentData(guildID).FullName);
		document.LastChild!.RemoveAll();
		using var enumerator = contents.GetEnumerator();
		while (enumerator.MoveNext())
		{
			var element = document.CreateElement(enumerator.Current.Key);
			element.InnerText = enumerator.Current.Value;
			document.LastChild!.AppendChild(element);
		}
		using XmlWriter writer = XmlWriter.Create(PersistentData(guildID).FullName, new XmlWriterSettings() { Indent = true, IndentChars = "\t" });
		document.WriteTo(writer);
	}
}
