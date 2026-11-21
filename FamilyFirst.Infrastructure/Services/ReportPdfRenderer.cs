using FamilyFirst.Application.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FamilyFirst.Infrastructure.Services;

public static class ReportPdfRenderer
{
    public static byte[] Render(ReportPdfContent content, LicenseType license)
    {
        QuestPDF.Settings.License = license;
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(42);
            page.DefaultTextStyle(style => style.FontSize(11).FontColor("#1A2E4A"));
            page.Header().Column(header =>
            {
                header.Item().Text("FamilyFirst").Bold().FontColor("#8B601C");
                header.Item().PaddingTop(8).Text(content.Title).Bold().FontSize(24);
                header.Item().PaddingTop(4).Text(content.Period);
            });
            page.Content().PaddingTop(20).Column(body =>
            {
                body.Spacing(14);
                foreach (var section in content.Sections)
                {
                    body.Item().Column(block =>
                    {
                        block.Item().EnsureSpace(36).Text(section.Title).Bold().FontSize(14);
                        foreach (var line in section.Lines)
                            block.Item().PaddingTop(4).Text(line);
                    });
                }
            });
            page.Footer().AlignRight().Text(text => text.CurrentPageNumber());
        })).GeneratePdf();
    }
}
