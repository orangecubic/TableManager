
using System.Text.RegularExpressions;

namespace DesignTable.Generators.DataTypes;

public class RangeType : IDataType
{
	public string Name => "range";
	public string GeneratedType => "Ranges";
	public bool GenerateImportOnlyField => true;
	public bool ReadFormattedString => false;
	public object Parse(string data)
	{
		var match = Regex.Match(data, @"^(\d+)~(\d+)$");
		if (!(match.Success && int.Parse(match.Groups[1].Value) <= int.Parse(match.Groups[2].Value)))
			throw new InvalidDataException();

		return data;
	}

	public static string TypeDefinitionScript()
	{
		return @"
public class Ranges(int min, int max)
{
	public int Min => min;
	public int Max => max;

	public int Peak() => Random.Shared.Next(min, max + 1);
}
";
	}
}

