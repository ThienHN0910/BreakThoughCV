using System.IO;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace BreakThroughCV.API.Services;

public class PdfTextService
{
    public async Task<string> ExtractTextAsync(Stream pdfStream)
    {
        // PdfPig works with synchronous APIs; read stream into memory first.
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms);
        ms.Position = 0;

        var sb = new StringBuilder();
        using (var document = PdfDocument.Open(ms))
        {
            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }
        }

        return sb.ToString();
    }
}
