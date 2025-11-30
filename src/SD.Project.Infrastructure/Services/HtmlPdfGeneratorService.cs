using System.Text;
using Microsoft.Extensions.Logging;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Simple HTML-based PDF generator service.
/// Uses text-based rendering that can be printed as PDF by browsers.
/// In production, this could be replaced with a proper PDF library.
/// </summary>
public sealed class HtmlPdfGeneratorService : IPdfGeneratorService
{
    private readonly ILogger<HtmlPdfGeneratorService> _logger;

    public HtmlPdfGeneratorService(ILogger<HtmlPdfGeneratorService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<byte[]> GenerateInvoicePdfAsync(InvoicePdfDataDto invoiceData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating PDF for invoice {InvoiceNumber}", invoiceData.InvoiceNumber);

        var html = GenerateInvoiceHtml(invoiceData);
        var bytes = Encoding.UTF8.GetBytes(html);

        return Task.FromResult(bytes);
    }

    /// <inheritdoc />
    public Task<byte[]> GenerateCreditNotePdfAsync(CreditNotePdfDataDto creditNoteData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating PDF for credit note {CreditNoteNumber}", creditNoteData.CreditNoteNumber);

        var html = GenerateCreditNoteHtml(creditNoteData);
        var bytes = Encoding.UTF8.GetBytes(html);

        return Task.FromResult(bytes);
    }

    private static string GenerateInvoiceHtml(InvoicePdfDataDto data)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>Invoice {data.InvoiceNumber}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCommonStyles());
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine("<div class=\"header\">");
        sb.AppendLine("<h1>COMMISSION INVOICE</h1>");
        sb.AppendLine($"<p class=\"invoice-number\">{data.InvoiceNumber}</p>");
        sb.AppendLine("</div>");

        // Parties
        sb.AppendLine("<div class=\"parties\">");
        
        // Issuer (From)
        sb.AppendLine("<div class=\"party\">");
        sb.AppendLine("<h3>From:</h3>");
        sb.AppendLine($"<p><strong>{data.IssuerName}</strong></p>");
        if (!string.IsNullOrEmpty(data.IssuerTaxId))
        {
            sb.AppendLine($"<p>Tax ID: {data.IssuerTaxId}</p>");
        }
        sb.AppendLine($"<p>{data.IssuerAddress}</p>");
        sb.AppendLine($"<p>{data.IssuerPostalCode} {data.IssuerCity}</p>");
        sb.AppendLine($"<p>{data.IssuerCountry}</p>");
        sb.AppendLine("</div>");

        // Seller (To)
        sb.AppendLine("<div class=\"party\">");
        sb.AppendLine("<h3>To:</h3>");
        sb.AppendLine($"<p><strong>{data.SellerName}</strong></p>");
        if (!string.IsNullOrEmpty(data.SellerTaxId))
        {
            sb.AppendLine($"<p>Tax ID: {data.SellerTaxId}</p>");
        }
        sb.AppendLine($"<p>{data.SellerAddress}</p>");
        sb.AppendLine($"<p>{data.SellerPostalCode} {data.SellerCity}</p>");
        sb.AppendLine($"<p>{data.SellerCountry}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        // Dates
        sb.AppendLine("<div class=\"dates\">");
        sb.AppendLine($"<p><strong>Issue Date:</strong> {data.IssueDate:dd MMM yyyy}</p>");
        sb.AppendLine($"<p><strong>Due Date:</strong> {data.DueDate:dd MMM yyyy}</p>");
        sb.AppendLine($"<p><strong>Billing Period:</strong> {data.PeriodStart:dd MMM yyyy} - {data.PeriodEnd:dd MMM yyyy}</p>");
        sb.AppendLine("</div>");

        // Items table
        sb.AppendLine("<table class=\"items\">");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Description</th>");
        sb.AppendLine("<th class=\"right\">Qty</th>");
        sb.AppendLine("<th class=\"right\">Unit Price</th>");
        sb.AppendLine("<th class=\"right\">Tax Rate</th>");
        sb.AppendLine("<th class=\"right\">Net</th>");
        sb.AppendLine("<th class=\"right\">Tax</th>");
        sb.AppendLine("<th class=\"right\">Gross</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</thead>");
        sb.AppendLine("<tbody>");

        foreach (var line in data.Lines)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{line.Description}</td>");
            sb.AppendLine($"<td class=\"right\">{line.Quantity:N2}</td>");
            sb.AppendLine($"<td class=\"right\">{line.UnitPrice:N2} {data.Currency}</td>");
            sb.AppendLine($"<td class=\"right\">{line.TaxRate:N0}%</td>");
            sb.AppendLine($"<td class=\"right\">{line.NetAmount:N2} {data.Currency}</td>");
            sb.AppendLine($"<td class=\"right\">{line.TaxAmount:N2} {data.Currency}</td>");
            sb.AppendLine($"<td class=\"right\">{line.GrossAmount:N2} {data.Currency}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        // Totals
        sb.AppendLine("<div class=\"totals\">");
        sb.AppendLine($"<p><strong>Net Total:</strong> {data.NetAmount:N2} {data.Currency}</p>");
        sb.AppendLine($"<p><strong>Tax ({data.TaxRate:N0}%):</strong> {data.TaxAmount:N2} {data.Currency}</p>");
        sb.AppendLine($"<p class=\"grand-total\"><strong>TOTAL DUE:</strong> {data.GrossAmount:N2} {data.Currency}</p>");
        sb.AppendLine("</div>");

        // Notes
        if (!string.IsNullOrEmpty(data.Notes))
        {
            sb.AppendLine("<div class=\"notes\">");
            sb.AppendLine("<h3>Notes:</h3>");
            sb.AppendLine($"<p>{data.Notes}</p>");
            sb.AppendLine("</div>");
        }

        // Footer
        sb.AppendLine("<div class=\"footer\">");
        sb.AppendLine($"<p>This invoice was generated on {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string GenerateCreditNoteHtml(CreditNotePdfDataDto data)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>Credit Note {data.CreditNoteNumber}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCommonStyles());
        sb.AppendLine(".header h1 { color: #dc3545; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine("<div class=\"header\">");
        sb.AppendLine("<h1>CREDIT NOTE</h1>");
        sb.AppendLine($"<p class=\"invoice-number\">{data.CreditNoteNumber}</p>");
        sb.AppendLine($"<p>Correcting Invoice: {data.OriginalInvoiceNumber}</p>");
        sb.AppendLine($"<p>Type: {data.Type}</p>");
        sb.AppendLine("</div>");

        // Parties
        sb.AppendLine("<div class=\"parties\">");
        
        // Issuer (From)
        sb.AppendLine("<div class=\"party\">");
        sb.AppendLine("<h3>From:</h3>");
        sb.AppendLine($"<p><strong>{data.IssuerName}</strong></p>");
        if (!string.IsNullOrEmpty(data.IssuerTaxId))
        {
            sb.AppendLine($"<p>Tax ID: {data.IssuerTaxId}</p>");
        }
        sb.AppendLine($"<p>{data.IssuerAddress}</p>");
        sb.AppendLine($"<p>{data.IssuerPostalCode} {data.IssuerCity}</p>");
        sb.AppendLine($"<p>{data.IssuerCountry}</p>");
        sb.AppendLine("</div>");

        // Seller (To)
        sb.AppendLine("<div class=\"party\">");
        sb.AppendLine("<h3>To:</h3>");
        sb.AppendLine($"<p><strong>{data.SellerName}</strong></p>");
        if (!string.IsNullOrEmpty(data.SellerTaxId))
        {
            sb.AppendLine($"<p>Tax ID: {data.SellerTaxId}</p>");
        }
        sb.AppendLine($"<p>{data.SellerAddress}</p>");
        sb.AppendLine($"<p>{data.SellerPostalCode} {data.SellerCity}</p>");
        sb.AppendLine($"<p>{data.SellerCountry}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        // Dates
        sb.AppendLine("<div class=\"dates\">");
        sb.AppendLine($"<p><strong>Issue Date:</strong> {data.IssueDate:dd MMM yyyy}</p>");
        sb.AppendLine("</div>");

        // Reason
        sb.AppendLine("<div class=\"reason\">");
        sb.AppendLine("<h3>Reason for Credit:</h3>");
        sb.AppendLine($"<p>{data.Reason}</p>");
        sb.AppendLine("</div>");

        // Items table
        sb.AppendLine("<table class=\"items\">");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Description</th>");
        sb.AppendLine("<th class=\"right\">Qty</th>");
        sb.AppendLine("<th class=\"right\">Unit Price</th>");
        sb.AppendLine("<th class=\"right\">Tax Rate</th>");
        sb.AppendLine("<th class=\"right\">Net</th>");
        sb.AppendLine("<th class=\"right\">Tax</th>");
        sb.AppendLine("<th class=\"right\">Gross</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</thead>");
        sb.AppendLine("<tbody>");

        foreach (var line in data.Lines)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{line.Description}</td>");
            sb.AppendLine($"<td class=\"right\">{line.Quantity:N2}</td>");
            sb.AppendLine($"<td class=\"right\">{line.UnitPrice:N2} {data.Currency}</td>");
            sb.AppendLine($"<td class=\"right\">{line.TaxRate:N0}%</td>");
            sb.AppendLine($"<td class=\"right\">{line.NetAmount:N2} {data.Currency}</td>");
            sb.AppendLine($"<td class=\"right\">{line.TaxAmount:N2} {data.Currency}</td>");
            sb.AppendLine($"<td class=\"right\">{line.GrossAmount:N2} {data.Currency}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        // Totals
        sb.AppendLine("<div class=\"totals\">");
        sb.AppendLine($"<p><strong>Net Credit:</strong> {data.NetAmount:N2} {data.Currency}</p>");
        sb.AppendLine($"<p><strong>Tax Credit:</strong> {data.TaxAmount:N2} {data.Currency}</p>");
        sb.AppendLine($"<p class=\"grand-total\"><strong>TOTAL CREDIT:</strong> {data.GrossAmount:N2} {data.Currency}</p>");
        sb.AppendLine("</div>");

        // Notes
        if (!string.IsNullOrEmpty(data.Notes))
        {
            sb.AppendLine("<div class=\"notes\">");
            sb.AppendLine("<h3>Notes:</h3>");
            sb.AppendLine($"<p>{data.Notes}</p>");
            sb.AppendLine("</div>");
        }

        // Footer
        sb.AppendLine("<div class=\"footer\">");
        sb.AppendLine($"<p>This credit note was generated on {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string GetCommonStyles()
    {
        return @"
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body { font-family: Arial, sans-serif; font-size: 12px; line-height: 1.5; padding: 20px; }
            .header { text-align: center; margin-bottom: 30px; padding-bottom: 20px; border-bottom: 2px solid #333; }
            .header h1 { font-size: 24px; margin-bottom: 10px; color: #0066cc; }
            .invoice-number { font-size: 18px; font-weight: bold; }
            .parties { display: flex; justify-content: space-between; margin-bottom: 30px; }
            .party { width: 45%; }
            .party h3 { margin-bottom: 10px; font-size: 14px; color: #666; }
            .dates { margin-bottom: 30px; padding: 15px; background: #f5f5f5; border-radius: 5px; }
            .dates p { margin-bottom: 5px; }
            .reason { margin-bottom: 30px; padding: 15px; background: #fff3cd; border-radius: 5px; border: 1px solid #ffc107; }
            .reason h3 { margin-bottom: 10px; color: #856404; }
            .items { width: 100%; border-collapse: collapse; margin-bottom: 30px; }
            .items th, .items td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }
            .items th { background: #f5f5f5; font-weight: bold; }
            .items .right { text-align: right; }
            .totals { text-align: right; padding: 20px; background: #f5f5f5; border-radius: 5px; margin-bottom: 30px; }
            .totals p { margin-bottom: 5px; }
            .grand-total { font-size: 16px; margin-top: 10px; padding-top: 10px; border-top: 2px solid #333; }
            .notes { margin-bottom: 30px; padding: 15px; background: #e7f3ff; border-radius: 5px; }
            .notes h3 { margin-bottom: 10px; }
            .footer { text-align: center; color: #666; font-size: 10px; padding-top: 20px; border-top: 1px solid #ddd; }
            @media print { body { print-color-adjust: exact; -webkit-print-color-adjust: exact; } }
        ";
    }
}
