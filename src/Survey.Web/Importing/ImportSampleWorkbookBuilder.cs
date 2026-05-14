using System.IO.Compression;
using System.Security;
using System.Text;

namespace Survey.Web.Importing;

internal static class ImportSampleWorkbookBuilder
{
	private const string SpreadsheetContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

	private static readonly IReadOnlyDictionary<string, ImportSampleDefinition> Definitions =
		new Dictionary<string, ImportSampleDefinition>(StringComparer.OrdinalIgnoreCase)
		{
			["countries"] = new("countries-import-sample.xlsx", "Countries", ["Name", "Iso2"]),
			["state-provinces"] = new("state-provinces-import-sample.xlsx", "States", ["CountryCode", "Name", "Code"]),
			["counties"] = new("counties-import-sample.xlsx", "Counties", ["CountryCode", "StateCode", "Name", "Fips"]),
			["addresses"] = new("addresses-import-sample.xlsx", "Addresses", ["CountryCode", "StateCode", "AddressLine1", "City", "PostalCode"]),
			["zip-mappings"] = new("zip-mappings-import-sample.xlsx", "ZipMappings", ["ZIP", "COUNTY"])
		};

	public static bool TryGetDefinition(string key, out ImportSampleDefinition definition)
	{
		return Definitions.TryGetValue(key, out definition!);
	}

	public static string ContentType => SpreadsheetContentType;

	public static byte[] BuildWorkbook(ImportSampleDefinition definition)
	{
		using var stream = new MemoryStream();
		using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
		{
			WriteEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
			WriteEntry(archive, "_rels/.rels", BuildRootRelationshipsXml());
			WriteEntry(archive, "xl/workbook.xml", BuildWorkbookXml(definition.SheetName));
			WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationshipsXml());
			WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(definition.Headers));
		}

		return stream.ToArray();
	}

	private static void WriteEntry(ZipArchive archive, string entryName, string content)
	{
		var entry = archive.CreateEntry(entryName, CompressionLevel.SmallestSize);
		using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
		writer.Write(content);
	}

	private static string BuildContentTypesXml()
	{
		return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>
""";
	}

	private static string BuildRootRelationshipsXml()
	{
		return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
""";
	}

	private static string BuildWorkbookXml(string sheetName)
	{
		return $"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="{EscapeAttribute(sheetName)}" sheetId="1" r:id="rId1"/>
  </sheets>
</workbook>
""";
	}

	private static string BuildWorkbookRelationshipsXml()
	{
		return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
</Relationships>
""";
	}

	private static string BuildWorksheetXml(IReadOnlyList<string> headers)
	{
		var builder = new StringBuilder();
		builder.AppendLine("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
		builder.AppendLine("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
		builder.AppendLine("""  <sheetData>""");
		builder.AppendLine("""    <row r="1">""");

		for (var index = 0; index < headers.Count; index++)
		{
			var cellReference = $"{GetColumnName(index)}1";
			builder.Append("      <c r=\"");
			builder.Append(cellReference);
			builder.Append("\" t=\"inlineStr\"><is><t>");
			builder.Append(EscapeText(headers[index]));
			builder.AppendLine("</t></is></c>");
		}

		builder.AppendLine("""    </row>""");
		builder.AppendLine("""  </sheetData>""");
		builder.AppendLine("""</worksheet>""");
		return builder.ToString();
	}

	private static string GetColumnName(int index)
	{
		var dividend = index + 1;
		var columnName = string.Empty;

		while (dividend > 0)
		{
			var modulo = (dividend - 1) % 26;
			columnName = Convert.ToChar('A' + modulo) + columnName;
			dividend = (dividend - modulo) / 26;
		}

		return columnName;
	}

	private static string EscapeText(string value)
	{
		return SecurityElement.Escape(value) ?? string.Empty;
	}

	private static string EscapeAttribute(string value)
	{
		return SecurityElement.Escape(value) ?? string.Empty;
	}
}

internal sealed record ImportSampleDefinition(
	string FileName,
	string SheetName,
	IReadOnlyList<string> Headers);
