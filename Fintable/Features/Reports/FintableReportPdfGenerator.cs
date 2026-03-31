using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Fintable.Features.Reports;

public static class FintableReportPdfGenerator
{
    private static readonly string[] MonthNames =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

    private static readonly Color IncomeColor = Colors.Blue.Darken2;
    private static readonly Color ExpenseColor = Colors.Red.Darken2;
    private static readonly Color CreditCardColor = Colors.Orange.Darken2;
    private static readonly Color UnknownColor = Colors.Grey.Darken2;

    private static readonly Color HeaderBackground = Colors.Grey.Lighten3;
    private static readonly Color PaidBackground = Colors.Green.Lighten4;

    public static byte[] Generate(FintableReportDto report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item()
                        .PaddingBottom(8)
                        .Text($"Fintable Report — {report.Year}")
                        .Bold().FontSize(16).FontColor(Colors.Grey.Darken3);
                });

                page.Content().Element(c => ComposeTable(c, report));

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeTable(IContainer container, FintableReportDto report)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.ConstantColumn(50);

                for (var i = 0; i < 12; i++)
                    columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("Category");
                header.Cell().Element(HeaderStyle).Text("Kind");

                for (var m = 0; m < 12; m++)
                    header.Cell().Element(HeaderStyle).AlignRight().Text(MonthNames[m]);
            });

            foreach (var row in report.Rows)
            {
                var fontColor = GetKindColor(row.Kind);

                table.Cell()
                    .Element(CellStyle)
                    .DefaultTextStyle(x => x.FontColor(fontColor).SemiBold())
                    .Text(row.Category);

                table.Cell()
                    .Element(CellStyle)
                    .DefaultTextStyle(x => x.FontColor(fontColor))
                    .Text(row.Kind);

                foreach (var cell in row.Months)
                {
                    table.Cell()
                        .Element(c => ValueCellStyle(c, cell.Paid == true))
                        .AlignRight()
                        .DefaultTextStyle(x => x.FontColor(fontColor))
                        .Text(FormatValue(cell));
                }
            }
        });
    }

    private static string FormatValue(FintableReportCellDto cell)
    {
        if (cell.Paid is null)
            return "";

        return cell.Value.ToString("N2");
    }

    private static Color GetKindColor(string kind) => kind switch
    {
        "Income" => IncomeColor,
        "Expense" => ExpenseColor,
        "CreditCard" => CreditCardColor,
        _ => UnknownColor,
    };

    private static IContainer HeaderStyle(IContainer container) =>
        container
            .Background(HeaderBackground)
            .Border(0.5f)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(4)
            .DefaultTextStyle(x => x.Bold().FontSize(7));

    private static IContainer CellStyle(IContainer container) =>
        container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingHorizontal(4)
            .PaddingVertical(3);

    private static IContainer ValueCellStyle(IContainer container, bool paid)
    {
        if (paid)
            container = container.Background(PaidBackground);

        return container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingHorizontal(4)
            .PaddingVertical(3);
    }
}
