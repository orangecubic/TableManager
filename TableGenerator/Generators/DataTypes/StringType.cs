
namespace DesignTable.Generators.DataTypes;

public class StringType : IDataType
{
	public string Name => "string";
	public string GeneratedType => "string";
	public bool GenerateImportOnlyField => false;
	public bool ReadFormattedString => false;

	public object Parse(string data)
	{
		return data;
	}
}
