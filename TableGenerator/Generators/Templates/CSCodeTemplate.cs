// Assumptions:
// - DataTable.ExtendedProperties[ETableProperty.TableType] == ETableType
// - Column.ExtendedProperties[EColumnProperty.IsArray] == true if it's an array column
// - Column.ExtendedProperties[EColumnProperty.DataType] == string like "int", "string", or another table name (for references)
// - EnumTable is converted into enum class and not loaded from CSV at runtime

using CsvHelper.Configuration;
using DesignTable.Generators.DataTypes;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DesignTable.Generators.Templates;

public static class CsCodeTemplate
{
	public static async Task GenerateAsync(List<DataTable> tables, string outputDir, string prefixNamespace)
	{
		Directory.CreateDirectory(outputDir);
		var rowsDir = Path.Combine(outputDir, "Rows");
		Directory.CreateDirectory(rowsDir);
		var enumsDir = Path.Combine(outputDir, "Enums");
		Directory.CreateDirectory(enumsDir);

		var objectTables = tables.Where(t => t.ExtendedProperties[ETableProperty.TableType] is ETableType.ObjectTable).ToList();
		var enumTables = tables.Where(t => t.ExtendedProperties[ETableProperty.TableType] is ETableType.EnumTable).ToList();

		var enumTypeSet = new HashSet<string>(enumTables.Select(t => t.TableName));
		var tasks = new List<Task>();

		var typeString = @$"
using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;
using TableGenerator.Tables.Enums;
namespace {string.Join(".", prefixNamespace, "Rows")};
{PercentType.TypeDefinitionScript()}
{RangeType.TypeDefinitionScript()}

public static class DoubleParser
{{
    public static double Parse(string val) => val is """" ? 0 : double.Parse(val);
}}

public static class IntParser
{{
    public static int Parse(string val) => val is """" ? 0 : int.Parse(val);
}}
";

		tasks.Add(File.WriteAllTextAsync(Path.Combine(rowsDir, "CustomType.cs"), typeString));
		tasks.Add(File.WriteAllTextAsync(Path.Combine(rowsDir, "TypeConvertor.cs"), $@"

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace {string.Join(".", prefixNamespace, "Rows")};

public class UniversalDelimitedArrayConverter<T> : DefaultTypeConverter
{{
    private readonly string delimiter;

    public UniversalDelimitedArrayConverter(string delimiter = "";"")
    {{
        this.delimiter = delimiter;
    }}

    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {{
        if (string.IsNullOrWhiteSpace(text))
            return new List<T>();

        var elements = text
            .Split(delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => ConvertValue(token))
            .Where(value => value != null)
            .Cast<T>()
            .ToList();

        return elements;
    }}

    private object? ConvertValue(string token)
    {{
        try
        {{
            var targetType = typeof(T);

            if (targetType == typeof(string)) return token;

            if (targetType.IsEnum)
                return Enum.Parse(targetType, token, ignoreCase: true);

            if (targetType == typeof(bool))
                return token is not """" && bool.Parse(token);

            if (targetType == typeof(int))
                return token is """" ? 0 : int.Parse(token, CultureInfo.InvariantCulture);

            if (targetType == typeof(float))
                return token is """" ? 0 : float.Parse(token, CultureInfo.InvariantCulture);

            if (targetType == typeof(double))
                return token is """" ? 0 : double.Parse(token, CultureInfo.InvariantCulture);

            if (targetType == typeof(DateTime))
                return token is """" ? DateTime.MinValue : DateTime.Parse(token, CultureInfo.InvariantCulture);

            return token;
        }}
        catch
        {{
            // 무시하거나 로그 가능
            return null;
        }}
    }}

    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {{
        if (value is IEnumerable<T> enumerable)
        {{
            return string.Join(delimiter, enumerable.Select(v =>
            {{
                if (v is DateTime dt)
                    return dt.ToString(""o"", CultureInfo.InvariantCulture); // ISO 8601
                return Convert.ToString(v, CultureInfo.InvariantCulture);
            }}));
        }}

        return base.ConvertToString(value, row, memberMapData)!;
    }}
}}


public class UniversalConverter<T> : DefaultTypeConverter
{{
	private readonly string delimiter;

	public UniversalConverter(string delimiter = "";"")
	{{
		this.delimiter = delimiter;
	}}

	public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{{
		return ConvertValue(text ?? """");
	}}

	private object ConvertValue(string token)
	{{
		var targetType = typeof(T);

		if (targetType == typeof(string)) return token;

		if (targetType.IsEnum)
			return Enum.Parse(targetType, token, ignoreCase: true);

		if (targetType == typeof(bool))
			return token is not """" && bool.Parse(token);

		if (targetType == typeof(int))
			return token is """" ? 0 : int.Parse(token, CultureInfo.InvariantCulture);

		if (targetType == typeof(float))
			return token is """" ? 0 : float.Parse(token, CultureInfo.InvariantCulture);

		if (targetType == typeof(double))
			return token is """" ? 0 : double.Parse(token, CultureInfo.InvariantCulture);

		if (targetType == typeof(DateTime))
			return token is """" ? DateTime.MinValue : DateTime.Parse(token, CultureInfo.InvariantCulture);

		return token;
	}}

	public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
	{{
		if (value is IEnumerable<T> enumerable)
		{{
			return string.Join(delimiter, enumerable.Select(v =>
			{{
				if (v is DateTime dt)
					return dt.ToString(""o"", CultureInfo.InvariantCulture); // ISO 8601
				return Convert.ToString(v, CultureInfo.InvariantCulture);
			}}));
		}}

		return base.ConvertToString(value, row, memberMapData)!;
	}}
}}


"));
		// Generate enum C# files
		foreach (var enumTable in enumTables)
		{
			var sb = new StringBuilder();
			sb.AppendLine("// Auto-generated enum");
			sb.AppendLine($"namespace { string.Join(".", prefixNamespace, "Enums")};");
			sb.AppendLine("public enum " + enumTable.TableName);
			sb.AppendLine("{");
			foreach (DataRow row in enumTable.Rows)
			{
				
				var enumName = row["Name"].ToString();
				var enumValue = Convert.ToInt32(row["Value"]);
				sb.AppendLine($"    {enumName} = {enumValue},");
			}
			sb.AppendLine("}");
			tasks.Add(File.WriteAllTextAsync(Path.Combine(enumsDir, enumTable.TableName + ".cs"), sb.ToString()));
		}

		// Generate Table class (Tables.cs)
		var tablesSb = new StringBuilder();
		tablesSb.AppendLine("using System;");
		tablesSb.AppendLine("using System.IO;");
		tablesSb.AppendLine("using System.Threading.Tasks;");
		tablesSb.AppendLine($"namespace {prefixNamespace};");
		tablesSb.AppendLine("public class DesignTable");
		tablesSb.AppendLine("{");
		foreach (var table in objectTables)
		{
			tablesSb.AppendLine($"    public {table.TableName}Table {table.TableName} {{ get; set; }} = new();");
		}
		tablesSb.AppendLine();
		tablesSb.AppendLine("    public static async Task<DesignTable> LoadFromDirectoryAsync(string dir)");
		tablesSb.AppendLine("    {");
		tablesSb.AppendLine("        var tables = new DesignTable();");
		foreach (var table in objectTables)
		{
			tablesSb.AppendLine($"        await tables.{table.TableName}.LoadAsync(Path.Combine(dir, \"{table.TableName}.csv\"));");
		}
		tablesSb.AppendLine("        tables.LinkAll();");
		tablesSb.AppendLine("        return tables;");
		tablesSb.AppendLine("    }");
		tablesSb.AppendLine();
		tablesSb.AppendLine("    public void LinkAll()");
		tablesSb.AppendLine("    {");
		foreach (var table in objectTables)
		{
			tablesSb.AppendLine($"        {table.TableName}.Link(this);");
		}
		tablesSb.AppendLine("    }");
		tablesSb.AppendLine("}");
		tasks.Add(File.WriteAllTextAsync(Path.Combine(outputDir, "Tables.cs"), tablesSb.ToString()));

		// Generate Row & Table classes
		foreach (var table in objectTables)
		{
			var rowClass = new StringBuilder();
			var tableClass = new StringBuilder();
			string rowName = table.TableName + "Row";
			string tableName = table.TableName + "Table";

			rowClass.AppendLine("using System.Collections.Generic;");
			rowClass.AppendLine("using CsvHelper.Configuration.Attributes;");
			rowClass.AppendLine($"using {string.Join(".", prefixNamespace, "Enums")};");
			rowClass.AppendLine($"namespace {string.Join(".", prefixNamespace, "Rows")};");
			rowClass.AppendLine($"public class {rowName}");
			rowClass.AppendLine("{");

			var arrayGroups = new Dictionary<string, List<(int index, DataColumn col, IDataType dataType)>>();
			var columns = new List<DataColumn>();
			foreach (DataColumn col in table.Columns)
			{
				columns.Add(col);
				var exportName = col.ColumnName;
				var referenceName = col.ExtendedProperties[EColumnProperty.ReferenceName] as string ?? "";
				var isArray = col.ExtendedProperties[EColumnProperty.IsArray] is true;
				var index = col.ExtendedProperties[EColumnProperty.ArrayIndex];
				var dataType = (col.ExtendedProperties[EColumnProperty.DataType] as IDataType)!;
				var isObjectType = dataType is ObjectType;

				if (isArray)
				{
					var name = col.ExtendedProperties[EColumnProperty.Name] as string ?? "";

					if (!arrayGroups.ContainsKey(name))
					{
						arrayGroups[name] = new();
						if (dataType.GenerateImportOnlyField)
						{
							rowClass.AppendLine($"    internal List<string> {name}_imp {{ get; set; }} = [];");
							rowClass.AppendLine($"    [Ignore]");
						}

						rowClass.AppendLine($"    public {( (isObjectType && (!(dataType as ObjectType)!.IsEnum)) ? $"List<{dataType.GeneratedType}Row>" : $"List<{dataType.GeneratedType}>")} {name} {{ get; set; }} = [];");
					}
					arrayGroups[name].Add(((int)index!, col, dataType));
					continue;
				}

				if (dataType.GenerateImportOnlyField)
				{
					rowClass.AppendLine($"    internal string {exportName} {{ get; set; }} = \"\";");
					rowClass.AppendLine($"    [Ignore]");
					rowClass.AppendLine($"    {(isArray ? "internal" : "public")} {(isObjectType ? dataType.GeneratedType + "Row?" : dataType.GeneratedType)} {referenceName} {{ get; set; }}");
				}
				else
				{
					rowClass.AppendLine($"    {(isArray ? "internal" : "public")} {dataType.GeneratedType} {exportName} {{ get; set; }}");
				}
			}

			rowClass.AppendLine("}");
			tasks.Add(File.WriteAllTextAsync(Path.Combine(rowsDir, rowName + ".cs"), rowClass.ToString()));

			tableClass.AppendLine("using System.Collections.Generic;");
			tableClass.AppendLine("using System.IO;");
			tableClass.AppendLine("using System.Globalization;");
			tableClass.AppendLine("using CsvHelper;");
			tableClass.AppendLine("using CsvHelper.Configuration;");
			tableClass.AppendLine("using System.Linq;");
			tableClass.AppendLine("using System.Threading.Tasks;");
			tableClass.AppendLine($"using {string.Join(".", prefixNamespace, "Enums")};");
			tableClass.AppendLine($"using {string.Join(".", prefixNamespace, "Rows")};");
			tableClass.AppendLine($"namespace {prefixNamespace};");
			tableClass.AppendLine($"public class {tableName}");
			tableClass.AppendLine("{");
			tableClass.AppendLine($"    public List<{rowName}> Rows {{ get; set; }} = new();");
			tableClass.AppendLine($"    public Dictionary<string, {rowName}> IdMap {{ get; set; }} = new();");
			tableClass.AppendLine("    public async Task LoadAsync(string path)");
			tableClass.AppendLine("    {");
			tableClass.AppendLine("        if (!File.Exists(path)) throw new FileNotFoundException(path);");
			tableClass.AppendLine("        await using var stream = File.OpenRead(path);");
			tableClass.AppendLine("        using var reader = new StreamReader(stream);");
			tableClass.AppendLine("        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { IncludePrivateMembers = true, MissingFieldFound = null, IgnoreBlankLines = true });");
			tableClass.AppendLine($"        csv.Context.RegisterClassMap<{table.TableName}Map>();");
			tableClass.AppendLine($"        Rows = csv.GetRecords<{rowName}>().ToList();");
			tableClass.AppendLine("        IdMap = Rows.ToDictionary(r => r.Id);");
			tableClass.AppendLine("    }");

			tableClass.AppendLine("    public void Link(DesignTable tables)");
			tableClass.AppendLine("    {");
			tableClass.AppendLine("        foreach (var r in Rows)");
			tableClass.AppendLine("        {");


			foreach (DataColumn col in table.Columns)
			{
				var dataType = (col.ExtendedProperties[EColumnProperty.DataType] as IDataType)!;
				var referenceName = col.ExtendedProperties[EColumnProperty.ReferenceName] as string ?? "";
				var isArray = col.ExtendedProperties[EColumnProperty.IsArray] is true;

				if (isArray)
					continue;

				var script = dataType switch
				{
					PercentType => $"""
					                            r.{referenceName} = new Percent(DoubleParser.Parse(r.{referenceName}_imp.Split("%")[0]) / 100);
					                """,
					RangeType => $"""
					                          var {referenceName}_imp_range = r.{referenceName}_imp.Split("~");
					                          if ({referenceName}_imp_range.Count() == 1)
					                              r.{referenceName} = new Ranges(IntParser.Parse({referenceName}_imp_range[0]), IntParser.Parse({referenceName}_imp_range[0]));
					                          else
					                              r.{referenceName} = new Ranges(IntParser.Parse({referenceName}_imp_range[0]), IntParser.Parse({referenceName}_imp_range[1]));
					              """,
					ObjectType obj => obj.IsEnum ? "" : 
						$"""
                                    if (r.{referenceName}_imp is not "")
                                        r.{referenceName} = tables.{obj.GeneratedType}.IdMap[r.{referenceName}_imp];
                        """,
					_ => ""
				};

				if (script is not "")
					tableClass.AppendLine(script);
			}

			arrayGroups.Where(g => g.Value[0].dataType.GenerateImportOnlyField).ToList().ForEach(g =>
			{
				var col = g.Value[0].col;
				var dataType = (col.ExtendedProperties[EColumnProperty.DataType] as IDataType)!;
				var name = col.ExtendedProperties[EColumnProperty.Name] as string ?? "";
				var isArray = col.ExtendedProperties[EColumnProperty.IsArray] is true;

				var script = dataType switch
				{
					PercentType => $"""
					                return new Percent(DoubleParser.Parse(val.Split("%")[0]) / 100);
					                """,
					RangeType => $"""
					                              var range = val.Split("~");
					                              if (range.Count() == 1)
					                                  return new Ranges(IntParser.Parse(range[0]), IntParser.Parse(range[0]));
					                              else
					                                  return new Ranges(IntParser.Parse(range[0]), IntParser.Parse(range[1]));
					              """,
					ObjectType obj => obj.IsEnum ? "" :
						$"""
						 if (val is not "")
						                     return tables.{obj.GeneratedType}.IdMap[val];
						                 return null;
						 """,
					_ => ""
				};

				tableClass.AppendLine(
					    $@"
            r.{name} = r.{name}_imp.Select(val => 
            {{
                {script}
			}}).Where(val => val is not null).ToList()!;
                        ");
			});

			tableClass.AppendLine("        }");
			tableClass.AppendLine("    }");
			tableClass.AppendLine("}");
			tableClass.AppendLine(
$@"
public class {table.TableName}Map : ClassMap<{table.TableName}Row>
{{
    public {table.TableName}Map()
    {{
        AutoMap(new CsvConfiguration(CultureInfo.InvariantCulture) {{ IncludePrivateMembers = true, MissingFieldFound = null, IgnoreBlankLines = true }});
        {string.Join("\n        ", columns.Where(c => c.ColumnName is not "Id" && c.ExtendedProperties[EColumnProperty.IsArray] is not true)
	        .Select(c => (c, c.ExtendedProperties[EColumnProperty.DataType] as IDataType)!)
	        .Where(t => t.Item2!.GenerateImportOnlyField is false)
	        .Select(t => $"Map(m => m.{t.c.ColumnName}).TypeConverter(new UniversalConverter<{t.Item2!.GeneratedType}>());"))}
        {string.Join("\n        ", arrayGroups.Select(g => $"Map(m => m.{g.Key}{(g.Value[0].dataType.GenerateImportOnlyField ? "_imp" : "")}).TypeConverter(new UniversalDelimitedArrayConverter<{(g.Value[0].dataType.GenerateImportOnlyField ? "string" : g.Value[0].dataType.GeneratedType)}>());"))}
    }}
}}
");
			tasks.Add(File.WriteAllTextAsync(Path.Combine(outputDir, table.TableName + "Table.cs"), tableClass.ToString()));
		}

		await Task.WhenAll(tasks);
	}

}
