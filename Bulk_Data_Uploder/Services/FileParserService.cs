using ClosedXML.Excel;
using CsvHelper;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using System.Globalization;
using System.Xml.Linq;

public class FileParserService
{
    public async Task<List<DataRecord>> ParseFileAsync(IFormFile file)
    {
        var records = new List<DataRecord>();

        using (var stream = file.OpenReadStream())
        {
            if (file.ContentType == "text/csv")
            {
                using (var reader = new StreamReader(stream))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    records = csv.GetRecords<DataRecord>().ToList();
                }
            }
            else if (file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheets.First();
                    foreach (var row in worksheet.RowsUsed().Skip(1))
                    {
                        var record = new DataRecord
                        {
                            Id = row.Cell(1).GetValue<int>(),
                            Name = row.Cell(2).GetValue<string>(),
                            Value = row.Cell(3).GetValue<double>()
                        };
                        records.Add(record);
                    }
                }
            }
            else if (file.ContentType == "application/json")
            {
                using (var reader = new StreamReader(stream))
                {
                    var json = await reader.ReadToEndAsync();
                    records = JsonConvert.DeserializeObject<List<DataRecord>>(json);
                }
            }
            else if (file.ContentType == "application/xml")
            {
                using (var reader = new StreamReader(stream))
                {
                    var xml = await reader.ReadToEndAsync();
                    records = XDocument.Parse(xml).Descendants("DataRecord")
                        .Select(x => new DataRecord
                        {
                            Id = int.Parse(x.Element("Id").Value),
                            Name = x.Element("Name").Value,
                            Value = double.Parse(x.Element("Value").Value)
                        }).ToList();
                }
            }
        }

        return records;
    }
}