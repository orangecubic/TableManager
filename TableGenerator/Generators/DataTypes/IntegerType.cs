
namespace DesignTable.Generators.DataTypes;

public class IntegerType : IDataType
{
	public string Name => "int";
	public string GeneratedType => "int";
	public bool GenerateImportOnlyField => false;
	public bool ReadFormattedString => false;

	public object Parse(string data)
	{
		return int.Parse(data);
	}
}
