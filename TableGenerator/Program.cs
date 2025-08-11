
using DesignTable;
using DesignTable.Generators;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
.SetBasePath(Directory.GetCurrentDirectory()) // 현재 디렉토리 설정
.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // appsettings.json 추가
.Build();

var config = new Configuration();

configuration.Bind(config);

//var excelFiles = Directory.GetFiles(config.TablePath, "*.xlsx");

DesignTable.Generators.TableGenerator.GenerateTable("./excel", 10);

Console.WriteLine(Random.Shared.Next(1,1));

var tables = await TableGenerator.Tables.DesignTable.LoadFromDirectoryAsync("./csv");

Console.WriteLine(tables);