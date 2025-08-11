using System.Data;

namespace DesignTable.Generators;

public enum ETableType
{
    ObjectTable = 1,
    EnumTable = 2,
}

public enum EDataType
{
    ScalarType = 1,
    ArrayType,
    ObjectType,
    EnumType
}

public enum EColumnOption
{
    Nullable = 1, // Object Type에만 적용 가능

}

public enum ETableProperty
{
	TableType,
	TablePath,
	IdDictionary,
	EnumDictionary
}

public enum EColumnProperty
{
	Name,
	ReferenceName,
	DataType,
	IsArray,
	ArrayIndex,
	Comment
}

public static class DataColumnExtension
{
	public static object? GetProperty(this DataColumn column, EColumnProperty property)
	{
		return column.ExtendedProperties[property];
	}

	public static void SetProperty(this DataColumn column, EColumnProperty property, object? value)
	{
		column.ExtendedProperties[property] = value;
	}
}