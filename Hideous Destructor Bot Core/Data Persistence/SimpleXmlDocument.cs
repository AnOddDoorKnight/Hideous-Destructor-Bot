using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HideousDestructor.DiscordServer.IO;

internal sealed class SimpleXmlDocument : IDisposable
{
	public XmlDocument SourceDocument { get; private set; } = new();
	public readonly FileInfo FileLocation;
	public bool AutoFlush { get; set; } = true;
	public SimpleXmlDocument(ulong guildID, string fileName)
	{
		DirectoryInfo guildInfo = DataGroup.GetSpecificGuildDirectory(guildID);
		FileLocation = new FileInfo($"{guildInfo.FullName}\\{fileName}.xml");
		if (!FileLocation.Exists)
		{
			using XmlWriter writer = XmlWriter.Create(FileLocation.FullName, DataGroup.Settings);
			writer.WriteStartElement(fileName.Replace(" ", ""));
			writer.WriteEndElement();
			writer.Flush();
		}
		SourceDocument.Load(FileLocation.FullName);
	}

	public string? this[string key]
	{
		get
		{
			var list = SourceDocument.LastChild!.ChildNodes;
			for (int i = 0; i < list.Count; i++)
				if (list[i]!.Name == key)
					return string.IsNullOrEmpty(list[i]!.InnerText) ? null : list[i]!.InnerText;
			return null;
		}
		set
		{
			var list = SourceDocument.LastChild!.ChildNodes;
			for (int i = 0; i < list.Count; i++)
				if (list[i]!.Name == key)
				{
					list[i]!.InnerText = value ?? "";
					goto autoFlush;
				}
			// Creating the value if it doesn't exist
			var element = SourceDocument.CreateElement(key);
			element.InnerText = value ?? "";
			SourceDocument.LastChild!.AppendChild(element);
		autoFlush:
			if (AutoFlush)
				Dispose();
		}
	}
	public string GetOrDefault(string key, string defValue)
	{
		string? output = this[key];
		if (output == null)
		{
			this[key] = defValue;
			return defValue;
		}
		return output;
	}

	public void Dispose()
	{
		using XmlWriter writer = XmlWriter.Create(FileLocation.FullName, DataGroup.Settings);
		SourceDocument.WriteTo(writer);
		writer.Flush();
	}
}
