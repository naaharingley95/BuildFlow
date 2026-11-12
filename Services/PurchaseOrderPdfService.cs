using BuildFlow.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BuildFlow.Services
{
    public class PurchaseOrderPdfService
    {
        // Colour palette matching the app's dark theme — PDF uses light background
        private static readonly string AccentBlue  = "#4dabf7";
        private static readonly string DarkHeader  = "#1a1f35";
        private static readonly string LightGray   = "#f4f6fb";
        private static readonly string BorderGray  = "#dee2e6";
        private static readonly string TextMuted   = "#6c757d";

        public byte[] Generate(PurchaseOrder po)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // ── Header ────────────────────────────────────────────────
                    page.Header().Element(ComposeHeader);

                    // ── Content ───────────────────────────────────────────────
                    page.Content().Column(col =>
                    {
                        col.Spacing(14);

                        // PO Title row
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Purchase Order")
                                    .FontSize(20).Bold().FontColor(DarkHeader);
                                c.Item().Text(po.OrderNumber)
                                    .FontSize(14).SemiBold().FontColor(AccentBlue);
                            });
                            row.ConstantItem(130).Column(c =>
                            {
                                c.Item().AlignRight().Text($"Date: {po.OrderDate:dd MMM yyyy}")
                                    .FontColor(TextMuted);
                                c.Item().AlignRight().Text($"Status: {po.Status}")
                                    .SemiBold().FontColor(StatusColor(po.Status));
                            });
                        });

                        col.Item().LineHorizontal(1).LineColor(BorderGray);

                        // Order details grid
                        col.Item().Background(LightGray).Padding(12).Column(c =>
                        {
                            c.Item().Text("Order Details").SemiBold().FontColor(DarkHeader);
                            c.Item().Height(6);
                            c.Item().Row(row =>
                            {
                                DetailCell(row, "Supplier",          po.Supplier?.CompanyName ?? "-");
                                DetailCell(row, "Site",              po.Site?.SiteName ?? "-");
                            });
                            c.Item().Height(4);
                            c.Item().Row(row =>
                            {
                                DetailCell(row, "Order Date",         po.OrderDate.ToString("dd MMM yyyy"));
                                DetailCell(row, "Expected Delivery",  po.ExpectedDeliveryDate.ToString("dd MMM yyyy"));
                            });
                            c.Item().Height(4);
                            c.Item().Row(row =>
                            {
                                DetailCell(row, "Created By", po.CreatedBy?.FullName ?? "-");
                                DetailCell(row, "Created On", po.CreatedAtUtc.ToString("dd MMM yyyy"));
                            });
                            if (!string.IsNullOrWhiteSpace(po.Notes))
                            {
                                c.Item().Height(4);
                                c.Item().Row(row =>
                                    DetailCell(row, "Notes", po.Notes!));
                            }
                        });

                        // Items table
                        col.Item().Column(c =>
                        {
                            c.Item().Text("Order Items").SemiBold().FontColor(DarkHeader).FontSize(11);
                            c.Item().Height(6);
                            c.Item().Table(table =>
                            {
                                // Columns
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);   // Material
                                    cols.RelativeColumn(1.4f);// Ordered Qty
                                    cols.RelativeColumn(1.4f);// Received Qty
                                    cols.RelativeColumn(1.4f);// Outstanding
                                    cols.RelativeColumn(1.6f);// Unit Price
                                    cols.RelativeColumn(1.6f);// Sub Total
                                });

                                // Header row
                                table.Header(header =>
                                {
                                    HeaderCell(header, "Material");
                                    HeaderCell(header, "Ordered Qty");
                                    HeaderCell(header, "Received Qty");
                                    HeaderCell(header, "Outstanding");
                                    HeaderCell(header, "Unit Price (₹)");
                                    HeaderCell(header, "Sub Total (₹)");
                                });

                                // Data rows
                                bool alt = false;
                                foreach (var item in po.Items)
                                {
                                    var bg = alt ? "#ffffff" : LightGray;
                                    alt = !alt;

                                    DataCell(table, item.Material?.Name ?? "-",                      bg, bold: true);
                                    DataCell(table, item.OrderedQuantity.ToString("N2"),              bg);
                                    DataCell(table, item.ReceivedQuantity.ToString("N2"),             bg);
                                    DataCell(table, item.OutstandingQuantity.ToString("N2"),          bg);
                                    DataCell(table, item.UnitPrice.ToString("N2"),                    bg);
                                    DataCell(table, item.SubTotal.ToString("N2"),                     bg, bold: true);
                                }
                            });
                        });

                        // Financial summary
                        col.Item().AlignRight().Width(220).Column(c =>
                        {
                            c.Item().Background(LightGray).Padding(12).Column(fin =>
                            {
                                fin.Item().Text("Financial Summary").SemiBold().FontColor(DarkHeader);
                                fin.Item().Height(6);
                                FinRow(fin, "Subtotal",      $"₹{po.SubTotal:N2}");
                                FinRow(fin, "Tax",           $"₹{po.TaxAmount:N2}");
                                FinRow(fin, "Other Charges", $"₹{po.OtherCharges:N2}");
                                fin.Item().LineHorizontal(1).LineColor(BorderGray);
                                fin.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Total").Bold().FontSize(12);
                                    row.ConstantItem(90).AlignRight()
                                        .Text($"₹{po.TotalAmount:N2}").Bold().FontSize(12).FontColor(AccentBlue);
                                });
                            });
                        });

                        // Notes / cancel reason
                        if (!string.IsNullOrWhiteSpace(po.CancelReason))
                        {
                            col.Item().Background("#fff3cd").Padding(10).Column(c =>
                            {
                                c.Item().Text("Cancellation Reason").SemiBold();
                                c.Item().Text(po.CancelReason!).FontColor(TextMuted);
                            });
                        }
                    });

                    // ── Footer ────────────────────────────────────────────────
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(s => s.FontSize(8).FontColor(TextMuted));
                        text.Span("BuildFlow — Construction Management System   |   Generated on ");
                        text.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm"));
                        text.Span("   |   Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ComposeHeader(IContainer c)
        {
            c.PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("🏗 BuildFlow").Bold().FontSize(16).FontColor(DarkHeader);
                    col.Item().Text("Construction Management System").FontSize(9).FontColor(TextMuted);
                });
                row.ConstantItem(100).AlignRight()
                    .Text("PURCHASE ORDER").SemiBold().FontSize(10).FontColor(AccentBlue);
            });
        }

        private static void DetailCell(RowDescriptor row, string label, string value)
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(label).FontSize(8).FontColor("#6c757d");
                c.Item().Text(value).SemiBold();
            });
        }

        private static void HeaderCell(TableCellDescriptor header, string text)
        {
            header.Cell().Background("#1a1f35").Padding(6)
                  .Text(text).FontColor(Colors.White).SemiBold().FontSize(9);
        }

        private static void DataCell(TableDescriptor table, string text, string bg, bool bold = false)
        {
            var cell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#dee2e6").Padding(5);
            var t    = cell.Text(text).FontSize(9);
            if (bold) t.SemiBold();
        }

        private static void FinRow(ColumnDescriptor col, string label, string value)
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(label).FontColor("#6c757d");
                row.ConstantItem(90).AlignRight().Text(value);
            });
            col.Item().Height(3);
        }

        private static string StatusColor(PurchaseOrderStatus status) => status switch
        {
            PurchaseOrderStatus.Draft             => "#adb5bd",
            PurchaseOrderStatus.Issued            => "#4dabf7",
            PurchaseOrderStatus.PartiallyReceived => "#ffd93d",
            PurchaseOrderStatus.FullyReceived     => "#4ecdc4",
            PurchaseOrderStatus.Cancelled         => "#ff6b6b",
            _                                     => "#6c757d"
        };
    }
}

