using ClosedXML.Excel;
using System.Data;
using System.Text.RegularExpressions;
using DesignTable.Generators.DataTypes;

namespace DesignTable.Generators;

public class TableExtractor(string tableFilePath)
{
    private readonly List<DataTable> tables = new();

    /// <summary>
    /// 엑셀의 모든 시트에 있는 모든 Table을 추출하여 반환합니다.
    /// </summary>
    public List<DataTable> ExtractTables()
    {
	    if (tableFilePath.Contains("$"))
		    return [];

        using var workbook = new XLWorkbook(tableFilePath);
        foreach (var ws in workbook.Worksheets)
        {
            ExtractExcelTable(ws);
        }

        return tables;
    }

    // 시트당 테이블 하나
    private void ExtractExcelTable(IXLWorksheet sheet)
    {
        if (Regex.IsMatch(sheet.Name, @"^[A-Za-z][A-Za-z0-9]*$") is false)
            throw new InvalidDataException($"Sheet Name must match [a-zA-Z0-9_-], {tableFilePath}:{sheet.Name}");

        var nameRow = sheet.Row(1); // A1 == Row(1)
        string firstFieldName = nameRow.Cell(1).Value.ToString();

        ETableType tableType = DetermineTableType(firstFieldName);
        var dataTable = new DataTable(sheet.Name);
        
        dataTable.ExtendedProperties.Add(ETableProperty.TableType, tableType);
        dataTable.ExtendedProperties.Add(ETableProperty.TablePath, $"{tableFilePath}:${sheet.Name}");

        if (tableType == ETableType.EnumTable)
        {
	        if (!Regex.IsMatch(sheet.Name, @"^E[a-zA-Z]*$"))
		        throw new InvalidDataException("");

			// --- Enum Table 처리 ---
			var secondFieldName = nameRow.Cell(2).Value.ToString();
            if (secondFieldName != "Value")
                throw new InvalidDataException($"Enum Table의 두 번째 컬럼은 반드시 'value'여야 합니다: {sheet.Name}");

            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Value", typeof(int));

            var seenValues = new HashSet<int>();
            var enumDictionary = new Dictionary<string, int>();

            foreach (var dataRow in sheet.RowsUsed().Skip(1)) // 데이터는 두 번째 줄부터
            {
                var name = dataRow.Cell(1).Value.ToString();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!int.TryParse(dataRow.Cell(2).Value.ToString(), out int intValue))
                    throw new InvalidDataException($"Enum value는 int여야 합니다: {sheet.Name}, name={name}");

                if (!seenValues.Add(intValue))
                    throw new InvalidDataException($"중복된 enum value '{intValue}'가 발견되었습니다: {sheet.Name}, name={name}");

                var row = dataTable.NewRow();
                row[0] = name;
                row[1] = intValue;
                dataTable.Rows.Add(row);

                enumDictionary.Add(name, intValue);
            }

            dataTable.ExtendedProperties[ETableProperty.EnumDictionary] = enumDictionary;
        }
        else
        {
            // --- Object Table 처리 ---
            var typeRow = sheet.Row(2);
            var commentRow = sheet.Row(3);
            var dataRows = sheet.Rows().Skip(3); // 4번째 줄부터 데이터

            var columnNameSet = new HashSet<string>();
            int columnCount = 0;
            var columnRegex = new Regex(@"^[A-Za-z][A-Za-z0-9]*$");
            var arrayRegex = new Regex(@"^([A-Za-z][A-Za-z0-9]*)\[(\d+)\]$");

            var idDictionary = new Dictionary<string, DataRow>();

			for (int i = 1; ; i++)
            {
                var cell = nameRow.Cell(i);
                string rawName = cell.Value.ToString();
                if (string.IsNullOrWhiteSpace(rawName))
                    break;

                string originName = rawName;
                string columnName;
                int arrayIndex = 0;
                bool isArray = false;

                if (columnRegex.IsMatch(rawName))
                {
	                originName = rawName;
                    columnName = rawName;
                }
                else if (arrayRegex.IsMatch(rawName))
                {
	                originName = arrayRegex.Match(rawName).Groups[1].Value;
					arrayIndex = int.Parse(arrayRegex.Match(rawName).Groups[2].Value);

                    columnName = $"{originName}_{arrayIndex}";
					isArray = true;
                }
                else
                {
                    throw new InvalidDataException($"컬럼 이름 '{rawName}' 은 유효하지 않습니다. 반드시 영문자로 시작하고 영문자/숫자만 사용하거나 []로 끝나야 합니다. ({sheet.Name})");
                }

                if (!columnNameSet.Add(columnName) && isArray is false)
                    throw new InvalidDataException($"중복된 컬럼 이름 '{columnName}' 이 존재합니다. 배열도 중복으로 간주됩니다. ({sheet.Name})");

                var column = new DataColumn(columnName);
                if (isArray)
                {
	                column.ExtendedProperties.Add(EColumnProperty.IsArray, true);
                    column.ExtendedProperties.Add(EColumnProperty.ArrayIndex, arrayIndex);
				}
                column.ExtendedProperties.Add(EColumnProperty.Name, originName);

				dataTable.Columns.Add(column);
                columnCount++;
            }

            for (int i = 1; i <= columnCount; i++)
            {
                var typeName = typeRow.Cell(i).Value.ToString();
                if (string.IsNullOrWhiteSpace(typeName))
                {
	                throw new InvalidDataException("");
                }

                var column = dataTable.Columns[i - 1];
                var isArray = column.ExtendedProperties[EColumnProperty.IsArray] is true;
                var dataType = DetermineDataType(typeName);

                column.ExtendedProperties[EColumnProperty.ReferenceName] = column.ColumnName;
				column.ColumnName = dataType.GenerateImportOnlyField ? $"{column.ColumnName}_imp" : column.ColumnName;
				column.ExtendedProperties[EColumnProperty.DataType] = dataType;

                var commentText = commentRow.Cell(i).Value.ToString();
                if (!string.IsNullOrWhiteSpace(commentText))
                {
                    column.ExtendedProperties[EColumnProperty.Comment] = commentText;
                }
            }

            var idSet = new HashSet<string>();

            foreach (var row in dataRows)
            {
                if (row.Cell(1).IsEmpty())
                    continue;

                var dataRow = dataTable.NewRow();
                var id = "";

                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
	                var column = dataTable.Columns[i];
	                var dataType = (column.ExtendedProperties[EColumnProperty.DataType] as IDataType)!;
	                var cell = row.Cell(i + 1);

	                object value;
	                if (cell.Value.ToString() is "")
		                value = "";
	                else
	                {
		                var fieldValue = cell.Value.ToString();
		                if (fieldValue.Contains('$') || fieldValue.Contains(','))
			                throw new InvalidDataException();

		                value = dataType.ReadFormattedString
			                ? dataType.Parse(cell.GetFormattedString())
			                : dataType.Parse(cell.Value.ToString()!);
	                }

	                if (i == 0) // 첫 번째 컬럼이 id여야 함
                    {
                        if (!idSet.Add(value.ToString()!))
                        {
                            throw new InvalidDataException($"ObjectTable에 중복된 id '{value}' 가 있습니다. (시트: {sheet.Name})");
                        }

                        id = value.ToString()!;
                        dataRow[i] = id;
                    }
                    else
                    {
	                    dataRow[i] = value;
                    }
                }

                dataTable.Rows.Add(dataRow);
                idDictionary.Add(id, dataRow);
            }

            dataTable.ExtendedProperties[ETableProperty.IdDictionary] = idDictionary;
        }

        tables.Add(dataTable);
    }

    private static ETableType DetermineTableType(string firstFieldName)
    {
        return firstFieldName switch
        {
            "Enum" => ETableType.EnumTable,
            "Id" => ETableType.ObjectTable,
            _ => throw new InvalidOperationException(
                         $"첫 번째 필드명이 'name', 'enum' 또는 'id' 여야 합니다. 실제: '{firstFieldName}'")
        };
    }

    private static IDataType DetermineDataType(string dataType)
    {
        return dataType switch
        {
			"bool" => new BooleanType(),
            "int" => new IntegerType(),
			"float" => new FloatType(),
			"date" => new DateType(),
            "string" => new StringType(),
			"percent" => new PercentType(),
			"range" => new RangeType(),
            _ => new ObjectType() { Name = dataType }
        };
    }
}