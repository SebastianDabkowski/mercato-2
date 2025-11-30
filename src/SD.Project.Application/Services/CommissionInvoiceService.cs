using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing commission invoices and credit notes.
/// Handles invoice generation, issuance, and PDF export.
/// </summary>
public sealed class CommissionInvoiceService
{
    private readonly ICommissionInvoiceRepository _invoiceRepository;
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly ISettlementRepository _settlementRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly INotificationService _notificationService;
    private readonly IPdfGeneratorService _pdfGeneratorService;

    /// <summary>
    /// Default currency when none is available.
    /// </summary>
    public const string DefaultCurrency = "EUR";

    /// <summary>
    /// Platform issuer details (would typically come from configuration).
    /// </summary>
    private const string IssuerName = "Mercato Platform Ltd.";
    private const string IssuerTaxId = "EU123456789";
    private const string IssuerAddress = "123 Commerce Street";
    private const string IssuerCity = "Warsaw";
    private const string IssuerPostalCode = "00-001";
    private const string IssuerCountry = "Poland";

    public CommissionInvoiceService(
        ICommissionInvoiceRepository invoiceRepository,
        ICreditNoteRepository creditNoteRepository,
        ISettlementRepository settlementRepository,
        IStoreRepository storeRepository,
        INotificationService notificationService,
        IPdfGeneratorService pdfGeneratorService)
    {
        _invoiceRepository = invoiceRepository;
        _creditNoteRepository = creditNoteRepository;
        _settlementRepository = settlementRepository;
        _storeRepository = storeRepository;
        _notificationService = notificationService;
        _pdfGeneratorService = pdfGeneratorService;
    }

    /// <summary>
    /// Generates a commission invoice from a settlement.
    /// </summary>
    public async Task<GenerateInvoiceResultDto> HandleAsync(
        GenerateCommissionInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get the settlement
        var settlement = await _settlementRepository.GetByIdAsync(command.SettlementId, cancellationToken);
        if (settlement is null)
        {
            return GenerateInvoiceResultDto.Failed("Settlement not found.");
        }

        // Check settlement is approved or exported
        if (settlement.Status != SettlementStatus.Approved && settlement.Status != SettlementStatus.Exported)
        {
            return GenerateInvoiceResultDto.Failed($"Cannot generate invoice for settlement in status {settlement.Status}. Settlement must be approved first.");
        }

        // Check if invoice already exists for this settlement
        var existingInvoice = await _invoiceRepository.GetBySettlementIdAsync(command.SettlementId, cancellationToken);
        if (existingInvoice is not null)
        {
            return GenerateInvoiceResultDto.AlreadyExists(existingInvoice.Id, existingInvoice.InvoiceNumber);
        }

        // Get store for seller details
        var store = await _storeRepository.GetByIdAsync(settlement.StoreId, cancellationToken);
        if (store is null)
        {
            return GenerateInvoiceResultDto.Failed("Store not found.");
        }

        // Generate invoice number
        var sequenceNumber = await _invoiceRepository.GetNextSequenceNumberAsync(settlement.Year, cancellationToken);
        var invoiceNumber = $"INV-{settlement.Year}-{sequenceNumber:D5}";

        // Calculate dates
        var issueDate = DateTime.UtcNow;
        var dueDate = issueDate.AddDays(command.PaymentDueDays);

        // Create invoice
        var invoice = new CommissionInvoice(
            settlement.StoreId,
            settlement.SellerId,
            settlement.Id,
            invoiceNumber,
            settlement.Year,
            settlement.Month,
            settlement.Currency,
            command.TaxRate,
            issueDate,
            dueDate,
            store.Name,
            null, // SellerTaxId - would come from store/seller profile
            string.Empty, // SellerAddress
            string.Empty, // SellerCity
            string.Empty, // SellerPostalCode
            string.Empty, // SellerCountry
            IssuerName,
            IssuerTaxId,
            IssuerAddress,
            IssuerCity,
            IssuerPostalCode,
            IssuerCountry);

        // Add commission line
        var commissionDescription = $"Platform commission for {new DateTime(settlement.Year, settlement.Month, 1):MMMM yyyy}";
        invoice.AddLine(
            commissionDescription,
            1,
            settlement.TotalCommission,
            command.TaxRate);

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _invoiceRepository.SaveChangesAsync(cancellationToken);

        return GenerateInvoiceResultDto.Succeeded(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.GrossAmount);
    }

    /// <summary>
    /// Generates invoices for all approved settlements in a period.
    /// </summary>
    public async Task<IReadOnlyList<GenerateInvoiceResultDto>> HandleAsync(
        GenerateAllCommissionInvoicesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var results = new List<GenerateInvoiceResultDto>();

        // Get all approved/exported settlements for the period
        var (settlements, _) = await _settlementRepository.GetFilteredAsync(
            null,
            command.Year,
            command.Month,
            SettlementStatus.Approved,
            0,
            int.MaxValue,
            cancellationToken);

        foreach (var settlement in settlements)
        {
            var result = await HandleAsync(
                new GenerateCommissionInvoiceCommand(
                    settlement.Id,
                    command.TaxRate,
                    command.PaymentDueDays),
                cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Issues a draft invoice.
    /// </summary>
    public async Task<IssueInvoiceResultDto> HandleAsync(
        IssueCommissionInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await _invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return IssueInvoiceResultDto.Failed("Invoice not found.");
        }

        try
        {
            invoice.Issue();
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
            await _invoiceRepository.SaveChangesAsync(cancellationToken);

            // Notify seller
            await _notificationService.SendCommissionInvoiceIssuedAsync(
                invoice.SellerId,
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.GrossAmount,
                invoice.Currency,
                invoice.DueDate,
                cancellationToken);

            return IssueInvoiceResultDto.Succeeded(invoice.Id);
        }
        catch (InvalidOperationException ex)
        {
            return IssueInvoiceResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Marks an invoice as paid.
    /// </summary>
    public async Task<bool> HandleAsync(
        MarkInvoicePaidCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await _invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return false;
        }

        try
        {
            invoice.MarkPaid();
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
            await _invoiceRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Cancels an invoice.
    /// </summary>
    public async Task<bool> HandleAsync(
        CancelInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await _invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return false;
        }

        try
        {
            invoice.Cancel();
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
            await _invoiceRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Updates invoice notes.
    /// </summary>
    public async Task<bool> HandleAsync(
        UpdateInvoiceNotesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await _invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return false;
        }

        try
        {
            invoice.UpdateNotes(command.Notes);
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
            await _invoiceRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a credit note for an invoice.
    /// </summary>
    public async Task<CreateCreditNoteResultDto> HandleAsync(
        CreateCreditNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await _invoiceRepository.GetByIdAsync(command.OriginalInvoiceId, cancellationToken);
        if (invoice is null)
        {
            return CreateCreditNoteResultDto.Failed("Original invoice not found.");
        }

        if (invoice.Status != CommissionInvoiceStatus.Issued && invoice.Status != CommissionInvoiceStatus.Paid)
        {
            return CreateCreditNoteResultDto.Failed($"Cannot create credit note for invoice in status {invoice.Status}.");
        }

        // Get store for seller details
        var store = await _storeRepository.GetByIdAsync(invoice.StoreId, cancellationToken);
        if (store is null)
        {
            return CreateCreditNoteResultDto.Failed("Store not found.");
        }

        // Generate credit note number
        var year = DateTime.UtcNow.Year;
        var sequenceNumber = await _creditNoteRepository.GetNextSequenceNumberAsync(year, cancellationToken);
        var creditNoteNumber = $"CN-{year}-{sequenceNumber:D5}";

        // Create credit note
        var creditNote = new CreditNote(
            invoice.StoreId,
            invoice.SellerId,
            invoice.Id,
            invoice.InvoiceNumber,
            creditNoteNumber,
            command.Type,
            invoice.Currency,
            DateTime.UtcNow,
            command.Reason,
            invoice.SellerName,
            invoice.SellerTaxId,
            invoice.SellerAddress,
            invoice.SellerCity,
            invoice.SellerPostalCode,
            invoice.SellerCountry,
            invoice.IssuerName,
            invoice.IssuerTaxId,
            invoice.IssuerAddress,
            invoice.IssuerCity,
            invoice.IssuerPostalCode,
            invoice.IssuerCountry);

        // Add lines
        if (command.Type == CreditNoteType.Full)
        {
            // Full credit - reverse all invoice lines
            foreach (var line in invoice.Lines)
            {
                creditNote.AddLine(
                    $"Credit: {line.Description}",
                    -line.Quantity,
                    line.UnitPrice,
                    line.TaxRate);
            }
        }
        else if (command.Lines is not null && command.Lines.Count > 0)
        {
            // Partial credit - use provided lines
            foreach (var lineInput in command.Lines)
            {
                creditNote.AddLine(
                    lineInput.Description,
                    lineInput.Quantity,
                    lineInput.UnitPrice,
                    lineInput.TaxRate);
            }
        }
        else
        {
            return CreateCreditNoteResultDto.Failed("Partial credit note requires lines.");
        }

        // Mark original invoice as corrected
        invoice.MarkCorrected(creditNote.Id);

        await _creditNoteRepository.AddAsync(creditNote, cancellationToken);
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _creditNoteRepository.SaveChangesAsync(cancellationToken);

        return CreateCreditNoteResultDto.Succeeded(
            creditNote.Id,
            creditNote.CreditNoteNumber,
            creditNote.GrossAmount);
    }

    /// <summary>
    /// Updates credit note notes.
    /// </summary>
    public async Task<bool> HandleAsync(
        UpdateCreditNoteNotesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creditNote = await _creditNoteRepository.GetByIdAsync(command.CreditNoteId, cancellationToken);
        if (creditNote is null)
        {
            return false;
        }

        creditNote.UpdateNotes(command.Notes);
        await _creditNoteRepository.UpdateAsync(creditNote, cancellationToken);
        await _creditNoteRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Gets an invoice by ID.
    /// </summary>
    public async Task<CommissionInvoiceDto?> HandleAsync(
        GetCommissionInvoiceByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoice = await _invoiceRepository.GetByIdAsync(query.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        return MapToDto(invoice);
    }

    /// <summary>
    /// Gets invoice details with lines.
    /// </summary>
    public async Task<CommissionInvoiceDetailsDto?> HandleAsync(
        GetCommissionInvoiceDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoice = await _invoiceRepository.GetByIdAsync(query.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        // Get related credit notes
        var creditNotes = await _creditNoteRepository.GetByOriginalInvoiceIdAsync(invoice.Id, cancellationToken);
        var store = await _storeRepository.GetByIdAsync(invoice.StoreId, cancellationToken);
        var storeName = store?.Name ?? "Unknown Store";

        return MapToDetailsDto(invoice, creditNotes, storeName);
    }

    /// <summary>
    /// Gets invoices for a store with pagination.
    /// </summary>
    public async Task<PagedResultDto<CommissionInvoiceListItemDto>> HandleAsync(
        GetCommissionInvoicesByStoreIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (invoices, totalCount) = await _invoiceRepository.GetByStoreIdAsync(
            query.StoreId, query.Skip, query.Take, cancellationToken);

        var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        var storeName = store?.Name ?? "Unknown Store";

        var dtos = new List<CommissionInvoiceListItemDto>();
        foreach (var invoice in invoices)
        {
            var creditNotes = await _creditNoteRepository.GetByOriginalInvoiceIdAsync(invoice.Id, cancellationToken);
            dtos.Add(MapToListItemDto(invoice, storeName, creditNotes.Count > 0));
        }

        var pageSize = query.Take > 0 ? query.Take : 20;
        var pageNumber = (query.Skip / pageSize) + 1;

        return PagedResultDto<CommissionInvoiceListItemDto>.Create(dtos, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Gets invoices with filtering and pagination.
    /// </summary>
    public async Task<PagedResultDto<CommissionInvoiceListItemDto>> HandleAsync(
        GetCommissionInvoicesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var skip = (query.PageNumber - 1) * query.PageSize;
        var (invoices, totalCount) = await _invoiceRepository.GetFilteredAsync(
            query.StoreId,
            query.Year,
            query.Month,
            query.Status,
            skip,
            query.PageSize,
            cancellationToken);

        // Get store names
        var storeIds = invoices.Select(i => i.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeNames = stores.ToDictionary(s => s.Id, s => s.Name);

        var dtos = new List<CommissionInvoiceListItemDto>();
        foreach (var invoice in invoices)
        {
            var creditNotes = await _creditNoteRepository.GetByOriginalInvoiceIdAsync(invoice.Id, cancellationToken);
            var storeName = storeNames.GetValueOrDefault(invoice.StoreId, "Unknown Store");
            dtos.Add(MapToListItemDto(invoice, storeName, creditNotes.Count > 0));
        }

        return PagedResultDto<CommissionInvoiceListItemDto>.Create(dtos, query.PageNumber, query.PageSize, totalCount);
    }

    /// <summary>
    /// Gets invoice PDF data.
    /// </summary>
    public async Task<InvoicePdfDataDto?> HandleAsync(
        GetInvoicePdfDataQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoice = await _invoiceRepository.GetByIdAsync(query.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        var lines = invoice.Lines.Select(l => new CommissionInvoiceLineDto(
            l.Id,
            l.Description,
            l.Quantity,
            l.UnitPrice,
            l.TaxRate,
            l.NetAmount,
            l.TaxAmount,
            l.GrossAmount)).ToList();

        return new InvoicePdfDataDto(
            invoice.InvoiceNumber,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            invoice.Currency,
            invoice.NetAmount,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.GrossAmount,
            invoice.SellerName,
            invoice.SellerTaxId,
            invoice.SellerAddress,
            invoice.SellerCity,
            invoice.SellerPostalCode,
            invoice.SellerCountry,
            invoice.IssuerName,
            invoice.IssuerTaxId,
            invoice.IssuerAddress,
            invoice.IssuerCity,
            invoice.IssuerPostalCode,
            invoice.IssuerCountry,
            lines,
            invoice.Notes);
    }

    /// <summary>
    /// Generates and returns invoice PDF.
    /// </summary>
    public async Task<byte[]?> GenerateInvoicePdfAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var pdfData = await HandleAsync(new GetInvoicePdfDataQuery(invoiceId), cancellationToken);
        if (pdfData is null)
        {
            return null;
        }

        return await _pdfGeneratorService.GenerateInvoicePdfAsync(pdfData, cancellationToken);
    }

    /// <summary>
    /// Gets a credit note by ID.
    /// </summary>
    public async Task<CreditNoteDto?> HandleAsync(
        GetCreditNoteByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var creditNote = await _creditNoteRepository.GetByIdAsync(query.CreditNoteId, cancellationToken);
        if (creditNote is null)
        {
            return null;
        }

        return MapToCreditNoteDto(creditNote);
    }

    /// <summary>
    /// Gets credit notes for a store with pagination.
    /// </summary>
    public async Task<PagedResultDto<CreditNoteListItemDto>> HandleAsync(
        GetCreditNotesByStoreIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (creditNotes, totalCount) = await _creditNoteRepository.GetByStoreIdAsync(
            query.StoreId, query.Skip, query.Take, cancellationToken);

        var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        var storeName = store?.Name ?? "Unknown Store";

        var dtos = creditNotes.Select(cn => MapToCreditNoteListItemDto(cn, storeName)).ToList();

        var pageSize = query.Take > 0 ? query.Take : 20;
        var pageNumber = (query.Skip / pageSize) + 1;

        return PagedResultDto<CreditNoteListItemDto>.Create(dtos, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Gets credit notes for an invoice.
    /// </summary>
    public async Task<IReadOnlyList<CreditNoteListItemDto>> HandleAsync(
        GetCreditNotesByInvoiceIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var creditNotes = await _creditNoteRepository.GetByOriginalInvoiceIdAsync(query.InvoiceId, cancellationToken);
        
        if (creditNotes.Count == 0)
        {
            return Array.Empty<CreditNoteListItemDto>();
        }

        var storeId = creditNotes.First().StoreId;
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
        var storeName = store?.Name ?? "Unknown Store";

        return creditNotes.Select(cn => MapToCreditNoteListItemDto(cn, storeName)).ToList();
    }

    /// <summary>
    /// Gets credit note PDF data.
    /// </summary>
    public async Task<CreditNotePdfDataDto?> HandleAsync(
        GetCreditNotePdfDataQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var creditNote = await _creditNoteRepository.GetByIdAsync(query.CreditNoteId, cancellationToken);
        if (creditNote is null)
        {
            return null;
        }

        var lines = creditNote.Lines.Select(l => new CreditNoteLineDto(
            l.Id,
            l.Description,
            l.Quantity,
            l.UnitPrice,
            l.TaxRate,
            l.NetAmount,
            l.TaxAmount,
            l.GrossAmount)).ToList();

        return new CreditNotePdfDataDto(
            creditNote.CreditNoteNumber,
            creditNote.OriginalInvoiceNumber,
            creditNote.IssueDate,
            creditNote.Type.ToString(),
            creditNote.Currency,
            creditNote.NetAmount,
            creditNote.TaxAmount,
            creditNote.GrossAmount,
            creditNote.SellerName,
            creditNote.SellerTaxId,
            creditNote.SellerAddress,
            creditNote.SellerCity,
            creditNote.SellerPostalCode,
            creditNote.SellerCountry,
            creditNote.IssuerName,
            creditNote.IssuerTaxId,
            creditNote.IssuerAddress,
            creditNote.IssuerCity,
            creditNote.IssuerPostalCode,
            creditNote.IssuerCountry,
            lines,
            creditNote.Reason,
            creditNote.Notes);
    }

    /// <summary>
    /// Generates and returns credit note PDF.
    /// </summary>
    public async Task<byte[]?> GenerateCreditNotePdfAsync(
        Guid creditNoteId,
        CancellationToken cancellationToken = default)
    {
        var pdfData = await HandleAsync(new GetCreditNotePdfDataQuery(creditNoteId), cancellationToken);
        if (pdfData is null)
        {
            return null;
        }

        return await _pdfGeneratorService.GenerateCreditNotePdfAsync(pdfData, cancellationToken);
    }

    private static CommissionInvoiceDto MapToDto(CommissionInvoice invoice)
    {
        return new CommissionInvoiceDto(
            invoice.Id,
            invoice.StoreId,
            invoice.SellerId,
            invoice.SettlementId,
            invoice.InvoiceNumber,
            invoice.Year,
            invoice.Month,
            invoice.Status.ToString(),
            invoice.Currency,
            invoice.NetAmount,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.GrossAmount,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            invoice.SellerName,
            invoice.SellerTaxId,
            invoice.SellerAddress,
            invoice.SellerCity,
            invoice.SellerPostalCode,
            invoice.SellerCountry,
            invoice.IssuerName,
            invoice.IssuerTaxId,
            invoice.IssuerAddress,
            invoice.IssuerCity,
            invoice.IssuerPostalCode,
            invoice.IssuerCountry,
            invoice.Notes,
            invoice.CorrectedByNoteId,
            invoice.CreatedAt,
            invoice.IssuedAt,
            invoice.PaidAt,
            invoice.CancelledAt);
    }

    private static CommissionInvoiceDetailsDto MapToDetailsDto(
        CommissionInvoice invoice,
        IReadOnlyList<CreditNote> creditNotes,
        string storeName)
    {
        var lines = invoice.Lines.Select(l => new CommissionInvoiceLineDto(
            l.Id,
            l.Description,
            l.Quantity,
            l.UnitPrice,
            l.TaxRate,
            l.NetAmount,
            l.TaxAmount,
            l.GrossAmount)).ToList();

        var creditNoteDtos = creditNotes.Select(cn => new CreditNoteListItemDto(
            cn.Id,
            cn.StoreId,
            storeName,
            cn.CreditNoteNumber,
            cn.OriginalInvoiceNumber,
            cn.Type.ToString(),
            cn.Currency,
            cn.GrossAmount,
            cn.IssueDate,
            cn.Reason,
            cn.CreatedAt)).ToList();

        return new CommissionInvoiceDetailsDto(
            invoice.Id,
            invoice.StoreId,
            invoice.SellerId,
            invoice.SettlementId,
            invoice.InvoiceNumber,
            invoice.Year,
            invoice.Month,
            invoice.Status.ToString(),
            invoice.Currency,
            invoice.NetAmount,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.GrossAmount,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            invoice.SellerName,
            invoice.SellerTaxId,
            invoice.SellerAddress,
            invoice.SellerCity,
            invoice.SellerPostalCode,
            invoice.SellerCountry,
            invoice.IssuerName,
            invoice.IssuerTaxId,
            invoice.IssuerAddress,
            invoice.IssuerCity,
            invoice.IssuerPostalCode,
            invoice.IssuerCountry,
            lines,
            creditNoteDtos,
            invoice.Notes,
            invoice.CorrectedByNoteId,
            invoice.CreatedAt,
            invoice.IssuedAt,
            invoice.PaidAt,
            invoice.CancelledAt);
    }

    private static CommissionInvoiceListItemDto MapToListItemDto(
        CommissionInvoice invoice,
        string storeName,
        bool hasCreditNote)
    {
        return new CommissionInvoiceListItemDto(
            invoice.Id,
            invoice.StoreId,
            storeName,
            invoice.InvoiceNumber,
            invoice.Year,
            invoice.Month,
            invoice.Status.ToString(),
            invoice.Currency,
            invoice.GrossAmount,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.CreatedAt,
            hasCreditNote);
    }

    private static CreditNoteDto MapToCreditNoteDto(CreditNote creditNote)
    {
        var lines = creditNote.Lines.Select(l => new CreditNoteLineDto(
            l.Id,
            l.Description,
            l.Quantity,
            l.UnitPrice,
            l.TaxRate,
            l.NetAmount,
            l.TaxAmount,
            l.GrossAmount)).ToList();

        return new CreditNoteDto(
            creditNote.Id,
            creditNote.StoreId,
            creditNote.SellerId,
            creditNote.OriginalInvoiceId,
            creditNote.OriginalInvoiceNumber,
            creditNote.CreditNoteNumber,
            creditNote.Type.ToString(),
            creditNote.Currency,
            creditNote.NetAmount,
            creditNote.TaxAmount,
            creditNote.GrossAmount,
            creditNote.IssueDate,
            creditNote.SellerName,
            creditNote.SellerTaxId,
            creditNote.SellerAddress,
            creditNote.SellerCity,
            creditNote.SellerPostalCode,
            creditNote.SellerCountry,
            creditNote.IssuerName,
            creditNote.IssuerTaxId,
            creditNote.IssuerAddress,
            creditNote.IssuerCity,
            creditNote.IssuerPostalCode,
            creditNote.IssuerCountry,
            creditNote.Reason,
            creditNote.Notes,
            lines,
            creditNote.CreatedAt);
    }

    private static CreditNoteListItemDto MapToCreditNoteListItemDto(CreditNote creditNote, string storeName)
    {
        return new CreditNoteListItemDto(
            creditNote.Id,
            creditNote.StoreId,
            storeName,
            creditNote.CreditNoteNumber,
            creditNote.OriginalInvoiceNumber,
            creditNote.Type.ToString(),
            creditNote.Currency,
            creditNote.GrossAmount,
            creditNote.IssueDate,
            creditNote.Reason,
            creditNote.CreatedAt);
    }
}
