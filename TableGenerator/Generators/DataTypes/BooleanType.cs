
namespace DesignTable.Generators.DataTypes;

public class BooleanType : IDataType
{
	public string Name => "bool";
	public string GeneratedType => "bool";
	public bool GenerateImportOnlyField => false;
	public bool ReadFormattedString => false;

	public object Parse(string data)
	{
		return bool.Parse(data);
	}
}
