
using CsvHelper;
using CsvHelper.Configuration;
using System.Data;
using System.Globalization;
using System.Text;
using DesignTable.Generators.DataTypes;

namespace DesignTable.Generators;

public static class CsvGenerator
{
	public static async Task GenerateCsv(DataTable dataTable, string directory)
	{
		await using var writer = new StreamWriter($"{directory}{Path.DirectorySeparatorChar}{dataTable.TableName}.csv");
		await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			Delimiter = ",",
			HasHeaderRecord = true
		});

		var arrayGroups = dataTable.Columns
			.Cast<DataColumn>()
			.Where(col => col.ExtendedProperties[EColumnProperty.IsArray] is true)
			.GroupBy(col =>
			{
				var name = col.ExtendedProperties[EColumnProperty.Name]?.ToString();
				return (col.ExtendedProperties[EColumnProperty.DataType] as IDataType)!.GenerateImportOnlyField ? $"{name}_imp" : name;
			})
			.ToDictionary(g => g.Key!, g => g.OrderBy(c => c.ColumnName).ToList());

		var flatColumns = dataTable.Columns
			.Cast<DataColumn>()
			.Where(col => !arrayGroups.SelectMany(g => g.Value).Contains(col))
			.ToList();

		var csvColumns = flatColumns.Select(c => c.ColumnName).ToList();
		csvColumns.AddRange(arrayGroups.Keys); // 그룹 이름이 실제 CSV 컬럼 이름

		// Header
		foreach (var colName in csvColumns)
			csv.WriteField(colName);
		await csv.NextRecordAsync();

		await using var reader = dataTable.CreateDataReader();
		while (reader.Read())
		{
			// 일반 컬럼
			foreach (var col in flatColumns)
			{
				var value = reader[col.ColumnName];
				csv.WriteField(value);
			}

			// 그룹 컬럼
			foreach (var group in arrayGroups)
			{
				var values = group.Value
					.Select(c => reader[c.ColumnName].ToString() ?? string.Empty)
					.Where(s => !string.IsNullOrWhiteSpace(s)); ;
				csv.WriteField(string.Join(';', values));
			}

			await csv.NextRecordAsync();
		}
	}
}
