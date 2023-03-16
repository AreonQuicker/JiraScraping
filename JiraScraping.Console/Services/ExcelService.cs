using System.Reflection;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace JiraScraping.Console.Services;

public class ExcelService : IDisposable
{
    private readonly ExcelPackage _excel;

    public ExcelService()
    {
        _excel = new ExcelPackage();
    }

    private bool _isDisposed;

    public void AddWorkSheet<T>(string worksheetName, IList<T> data) where T : class, new()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(ExcelService));

        try
        {
            var workSheet = _excel.Workbook.Worksheets.Add(worksheetName);

            MemberInfo[] propertiesToInclude = typeof(T)
                .GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance)
                .ToArray();

            workSheet.Cells["A1"].LoadFromCollection(data, true,
                TableStyles.Light13, BindingFlags.Public,
                propertiesToInclude);

            workSheet.Cells.AutoFitColumns();
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e);
            throw;
        }
    }

    public void Save()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(ExcelService));

        try
        {
            _excel.SaveAs(new FileInfo("Tickets.xlsx"));
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e);
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _excel.Dispose();
        _isDisposed = true;
    }
}