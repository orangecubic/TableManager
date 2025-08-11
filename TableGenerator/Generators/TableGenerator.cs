
using System.Data;
using DesignTable.Generators.DataTypes;
using DesignTable.Generators.Templates;

namespace DesignTable.Generators;

public class TableGenerator
{
	public static void GenerateTable(string tableDirectory, int parallelism)
	{
		var excelFiles = Directory.GetFiles(tableDirectory, "*.xlsx");

		var tables = new Dictionary<string, DataTable>();

		foreach (string file in excelFiles)
		{
			var extractedTables = new TableExtractor(file).ExtractTables();

			foreach (var table in extractedTables)
			{
				if (tables.TryAdd(table.TableName, table) is false)
				{
					throw new InvalidDataException("");
				}
			}
		}

		foreach (var dataTable in tables.Values)
		{
			if (dataTable.ExtendedProperties[ETableProperty.TableType] is ETableType.EnumTable)
				continue;

			foreach (var row in dataTable.Rows)
			{
				var dataRow = (row as DataRow)!;

				for (var index = 0; index < dataRow.ItemArray.Length; index++)
				{
					var dataColumn = dataTable.Columns[index];
					var dataType = (dataColumn.ExtendedProperties[EColumnProperty.DataType] as IDataType)!;
					var isArray = dataColumn.ExtendedProperties[EColumnProperty.IsArray] is true;
					var data = dataRow.ItemArray[index];

					if (data?.ToString() is null or "" ||  dataType is not ObjectType type)
						continue;

					var objectDataTable = tables[dataType.Name];
					if (objectDataTable.ExtendedProperties[ETableProperty.TableType] is ETableType.EnumTable)
					{
						type.IsEnum = true;
						dataColumn.ColumnName =
							dataColumn.ExtendedProperties[EColumnProperty.ReferenceName] as string ?? "";

						var enumDictionary =
							(objectDataTable.ExtendedProperties[
								ETableProperty.EnumDictionary] as Dictionary<string, int>)!;

						if (enumDictionary.TryGetValue(data.ToString()!, out _) is false)
						{
							throw new InvalidDataException("Enum Not Found");
						}
					}
					else
					{

						var idDictionary =
							(objectDataTable.ExtendedProperties[ETableProperty.IdDictionary] as Dictionary<string, DataRow>)!;

						if (idDictionary.TryGetValue(data.ToString()!, out _) is false)
						{
							throw new InvalidDataException("Object not found");
						}
					}

				}
			}

			CsvGenerator.GenerateCsv(dataTable, "./csv").Wait();

			CsCodeTemplate.GenerateAsync(tables.Select(t => t.Value).ToList(), "../../../Tables", "TableGenerator.Tables").Wait();
		}
	}
}
