
namespace DesignTable.Generators.DataTypes;

public class FloatType : IDataType
{
	public string Name => "float";
	public string GeneratedType => "double";
	public bool GenerateImportOnlyField => false;
	public bool ReadFormattedString => false;
	public object Parse(string data)
	{
		return double.Parse(data);
	}
}

