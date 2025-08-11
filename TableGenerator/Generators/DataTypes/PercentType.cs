
using System.Globalization;
using System.Text.RegularExpressions;

namespace DesignTable.Generators.DataTypes;

public class PercentType : IDataType
{
	public string Name => "percent";
	public string GeneratedType => "Percent";
	public bool GenerateImportOnlyField => true;
	public bool ReadFormattedString => true;
	public object Parse (string data)
	{
		var match = Regex.Match(data, @"^\d+(\.\d+)?%$");
		if (!(match.Success && double.TryParse(data.TrimEnd('%'), out _)))
			throw new InvalidDataException();

		return data;
	}

	public static string TypeDefinitionScript() => 
@"
public class Percent(double percent)
{
	private static readonly int RandomPrecision = 1000000;
	public double Value => percent;
	public bool Try() => Random.Shared.Next(0, RandomPrecision + 1) < percent * RandomPrecision;
}
";
}

