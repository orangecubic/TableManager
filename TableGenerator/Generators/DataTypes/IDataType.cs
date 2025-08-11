
namespace DesignTable.Generators.DataTypes;

public interface IDataType
{
	// 엑셀 파일에서 사용할 Type 이름
	string Name { get; }

	// 생성될 코드에서 Row의 Type으로 외부에 공개될 이름
	string GeneratedType { get; }

	// CSV에서 인식이 안되는 타입일 경우 중간 필드 생성할지
	bool GenerateImportOnlyField { get; }

	// 엑셀 레퍼런스나 데이터 타입 영향 없이 셀에 적힌 값 그대로 읽을 지 여부
	bool ReadFormattedString { get; }

	object Parse(string data);
}
