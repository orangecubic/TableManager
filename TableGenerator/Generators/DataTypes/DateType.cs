using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignTable.Generators.DataTypes;

public class DateType : IDataType
{
	public string Name => "date";
	public string GeneratedType => "DateTime";
	public bool GenerateImportOnlyField => false;

	public bool ReadFormattedString => false;

	public object Parse(string data)
	{
		var date = DateTime.Parse(data);
		return date.ToString(CultureInfo.InvariantCulture);
	}

}
