
namespace DesignTable.Generators.DataTypes;

public class ObjectType : IDataType
{
	public bool IsEnum { get; set; }
	public string Name { get; set; } = "";
	public string GeneratedType => Name;
	public bool GenerateImportOnlyField => IsEnum is false;
	public bool ReadFormattedString => false;

	public object Parse (string data) => data;
}