using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HideousDestructor.DiscordServer;

public class BotState
{
	private static DirectoryInfo CurrentDirectory { get; } =
		new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
			.CreateSubdirectory("Hideous Destructor Bot");
	public static FileInfo PersistentData { get; } =
		new FileInfo(CurrentDirectory.FullName + "/botState.xml");
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

	public BotState()
	{
		contents = new Dictionary<string, string>();
		if (PersistentData.Exists)
		{
			XmlDocument document = new XmlDocument();
			document.Load(PersistentData.FullName);
			XmlNodeList contentNodeData = document.LastChild!.ChildNodes;
			for (int i = 0; i < contentNodeData.Count; i++)
			{
				XmlNode currentNode = contentNodeData.Item(i)!;
				contents.Add(currentNode.Name, currentNode.Value!);
			}
			using XmlWriter writer = XmlWriter.Create(PersistentData.FullName);
			document.WriteTo(writer);
		}
		else
		{
			using XmlWriter writer = XmlWriter.Create(PersistentData.FullName);
			writer.WriteStartElement("BotState");
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
		document.Load(PersistentData.FullName);
		document.LastChild!.RemoveAll();
		using var enumerator = contents.GetEnumerator();
		while (enumerator.MoveNext())
			document.CreateElement(enumerator.Current.Key).InnerText = enumerator.Current.Value;
		using XmlWriter writer = XmlWriter.Create(PersistentData.FullName);
		document.WriteTo(writer);
	}
}
