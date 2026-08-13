using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IQaSheetParser
{
    IReadOnlyList<QaRow> ParseRows(string fileId, string sheetName, IList<IList<object>> values);
}
