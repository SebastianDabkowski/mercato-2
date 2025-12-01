using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for product question operations.
/// Handles asking and answering questions about products.
/// </summary>
public sealed class ProductQuestionService
{
    private readonly IProductQuestionRepository _questionRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public ProductQuestionService(
        IProductQuestionRepository questionRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        INotificationService notificationService)
    {
        _questionRepository = questionRepository;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets public (answered and visible) questions for a product.
    /// </summary>
    public async Task<ProductQuestionsListDto> HandleAsync(
        GetPublicProductQuestionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        if (product is null)
        {
            return new ProductQuestionsListDto(query.ProductId, string.Empty, [], 0);
        }

        var questions = await _questionRepository.GetPublicQuestionsForProductAsync(query.ProductId, cancellationToken);

        var questionDtos = questions.Select(q => new ProductQuestionDto(
            q.Id,
            q.ProductId,
            product.Name,
            q.StoreId,
            q.BuyerId,
            q.BuyerDisplayName,
            q.Question,
            q.Answer,
            q.Status,
            q.AskedAt,
            q.AnsweredAt)).ToList();

        return new ProductQuestionsListDto(
            product.Id,
            product.Name,
            questionDtos.AsReadOnly(),
            questionDtos.Count);
    }

    /// <summary>
    /// Gets all questions for a product (seller view).
    /// </summary>
    public async Task<ProductQuestionsListDto?> HandleAsync(
        GetAllProductQuestionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        if (product is null || !product.StoreId.HasValue)
        {
            return null;
        }

        // Verify seller owns the store that owns the product
        var store = await _storeRepository.GetByIdAsync(product.StoreId.Value, cancellationToken);
        if (store is null || store.SellerId != query.SellerId)
        {
            return null;
        }

        var questions = await _questionRepository.GetAllQuestionsForProductAsync(query.ProductId, cancellationToken);

        var questionDtos = questions.Select(q => new ProductQuestionDto(
            q.Id,
            q.ProductId,
            product.Name,
            q.StoreId,
            q.BuyerId,
            q.BuyerDisplayName,
            q.Question,
            q.Answer,
            q.Status,
            q.AskedAt,
            q.AnsweredAt)).ToList();

        return new ProductQuestionsListDto(
            product.Id,
            product.Name,
            questionDtos.AsReadOnly(),
            questionDtos.Count);
    }

    /// <summary>
    /// Gets pending questions for a store.
    /// </summary>
    public async Task<IReadOnlyList<ProductQuestionDto>> HandleAsync(
        GetPendingQuestionsForStoreQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Verify seller owns the store
        var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null || store.SellerId != query.SellerId)
        {
            return [];
        }

        var questions = await _questionRepository.GetPendingQuestionsForStoreAsync(query.StoreId, cancellationToken);

        // Get product names for the questions
        var productIds = questions.Select(q => q.ProductId).Distinct().ToList();
        var productNames = new Dictionary<Guid, string>();
        foreach (var productId in productIds)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is not null)
            {
                productNames[productId] = product.Name;
            }
        }

        return questions.Select(q => new ProductQuestionDto(
            q.Id,
            q.ProductId,
            productNames.GetValueOrDefault(q.ProductId),
            q.StoreId,
            q.BuyerId,
            q.BuyerDisplayName,
            q.Question,
            q.Answer,
            q.Status,
            q.AskedAt,
            q.AnsweredAt)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets all questions for a store (pending and answered).
    /// </summary>
    public async Task<IReadOnlyList<ProductQuestionDto>> HandleAsync(
        GetAllQuestionsForStoreQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Verify seller owns the store
        var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null || store.SellerId != query.SellerId)
        {
            return [];
        }

        var questions = await _questionRepository.GetAllQuestionsForStoreAsync(query.StoreId, cancellationToken);

        // Get product names for the questions
        var productIds = questions.Select(q => q.ProductId).Distinct().ToList();
        var productNames = new Dictionary<Guid, string>();
        foreach (var productId in productIds)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is not null)
            {
                productNames[productId] = product.Name;
            }
        }

        return questions.Select(q => new ProductQuestionDto(
            q.Id,
            q.ProductId,
            productNames.GetValueOrDefault(q.ProductId),
            q.StoreId,
            q.BuyerId,
            q.BuyerDisplayName,
            q.Question,
            q.Answer,
            q.Status,
            q.AskedAt,
            q.AnsweredAt)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets questions asked by a buyer.
    /// </summary>
    public async Task<IReadOnlyList<ProductQuestionDto>> HandleAsync(
        GetBuyerQuestionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var questions = await _questionRepository.GetQuestionsByBuyerAsync(query.BuyerId, cancellationToken);

        // Get product names for the questions
        var productIds = questions.Select(q => q.ProductId).Distinct().ToList();
        var productNames = new Dictionary<Guid, string>();
        foreach (var productId in productIds)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is not null)
            {
                productNames[productId] = product.Name;
            }
        }

        return questions.Select(q => new ProductQuestionDto(
            q.Id,
            q.ProductId,
            productNames.GetValueOrDefault(q.ProductId),
            q.StoreId,
            q.BuyerId,
            q.BuyerDisplayName,
            q.Question,
            q.Answer,
            q.Status,
            q.AskedAt,
            q.AnsweredAt)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the count of unanswered questions for a store.
    /// </summary>
    public async Task<int> HandleAsync(
        GetUnansweredQuestionCountQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _questionRepository.GetUnansweredCountForStoreAsync(query.StoreId, cancellationToken);
    }

    /// <summary>
    /// Gets hidden questions (admin moderation view).
    /// </summary>
    public async Task<IReadOnlyList<ProductQuestionDto>> HandleAsync(
        GetHiddenQuestionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var questions = await _questionRepository.GetHiddenQuestionsAsync(cancellationToken);

        // Get product names for the questions
        var productIds = questions.Select(q => q.ProductId).Distinct().ToList();
        var productNames = new Dictionary<Guid, string>();
        foreach (var productId in productIds)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is not null)
            {
                productNames[productId] = product.Name;
            }
        }

        return questions.Select(q => new ProductQuestionDto(
            q.Id,
            q.ProductId,
            productNames.GetValueOrDefault(q.ProductId),
            q.StoreId,
            q.BuyerId,
            q.BuyerDisplayName,
            q.Question,
            q.Answer,
            q.Status,
            q.AskedAt,
            q.AnsweredAt)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Asks a new question about a product.
    /// </summary>
    public async Task<AskProductQuestionResultDto> HandleAsync(
        AskProductQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate question text
        if (string.IsNullOrWhiteSpace(command.Question))
        {
            return new AskProductQuestionResultDto(false, "Question text is required.");
        }

        if (command.Question.Length > 2000)
        {
            return new AskProductQuestionResultDto(false, "Question cannot exceed 2000 characters.");
        }

        // Get the product
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return new AskProductQuestionResultDto(false, "Product not found.");
        }

        if (!product.StoreId.HasValue)
        {
            return new AskProductQuestionResultDto(false, "Product is not associated with a store.");
        }

        // Only allow questions on active products
        if (!product.IsActive || product.Status != ProductStatus.Active)
        {
            return new AskProductQuestionResultDto(false, "Cannot ask questions about inactive products.");
        }

        // Get buyer display name
        var buyer = await _userRepository.GetByIdAsync(command.BuyerId, cancellationToken);
        if (buyer is null)
        {
            return new AskProductQuestionResultDto(false, "Buyer not found.");
        }

        var buyerDisplayName = buyer.FirstName ?? "Buyer";

        // Create the question
        var question = new ProductQuestion(
            command.ProductId,
            product.StoreId.Value,
            command.BuyerId,
            buyerDisplayName,
            command.Question);

        await _questionRepository.AddAsync(question, cancellationToken);
        await _questionRepository.SaveChangesAsync(cancellationToken);

        // Notify seller
        var store = await _storeRepository.GetByIdAsync(product.StoreId.Value, cancellationToken);
        if (store is not null)
        {
            var seller = await _userRepository.GetByIdAsync(store.SellerId, cancellationToken);
            if (seller?.Email is not null)
            {
                await _notificationService.SendProductQuestionAskedAsync(
                    question.Id,
                    product.Id,
                    product.Name,
                    seller.Email.Value,
                    buyerDisplayName,
                    cancellationToken);
            }
        }

        return new AskProductQuestionResultDto(true, null, question.Id);
    }

    /// <summary>
    /// Answers a product question.
    /// </summary>
    public async Task<AnswerProductQuestionResultDto> HandleAsync(
        AnswerProductQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate answer text
        if (string.IsNullOrWhiteSpace(command.Answer))
        {
            return new AnswerProductQuestionResultDto(false, "Answer text is required.");
        }

        if (command.Answer.Length > 2000)
        {
            return new AnswerProductQuestionResultDto(false, "Answer cannot exceed 2000 characters.");
        }

        // Get the question
        var question = await _questionRepository.GetByIdAsync(command.QuestionId, cancellationToken);
        if (question is null)
        {
            return new AnswerProductQuestionResultDto(false, "Question not found.");
        }

        // Verify seller owns the store
        var store = await _storeRepository.GetByIdAsync(question.StoreId, cancellationToken);
        if (store is null || store.SellerId != command.SellerId)
        {
            return new AnswerProductQuestionResultDto(false, "You are not authorized to answer this question.");
        }

        // Get the product
        var product = await _productRepository.GetByIdAsync(question.ProductId, cancellationToken);
        if (product is null)
        {
            return new AnswerProductQuestionResultDto(false, "Product not found.");
        }

        // Set the answer
        try
        {
            question.SetAnswer(command.Answer);
        }
        catch (InvalidOperationException ex)
        {
            return new AnswerProductQuestionResultDto(false, ex.Message);
        }

        await _questionRepository.UpdateAsync(question, cancellationToken);
        await _questionRepository.SaveChangesAsync(cancellationToken);

        // Notify buyer
        var buyer = await _userRepository.GetByIdAsync(question.BuyerId, cancellationToken);
        if (buyer?.Email is not null)
        {
            await _notificationService.SendProductQuestionAnsweredAsync(
                question.Id,
                product.Id,
                product.Name,
                buyer.Email.Value,
                store.Name,
                cancellationToken);
        }

        return new AnswerProductQuestionResultDto(true, null);
    }

    /// <summary>
    /// Hides a product question (admin moderation).
    /// </summary>
    public async Task<ModerateProductQuestionResultDto> HandleAsync(
        HideProductQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new ModerateProductQuestionResultDto(false, "Reason is required.");
        }

        var question = await _questionRepository.GetByIdAsync(command.QuestionId, cancellationToken);
        if (question is null)
        {
            return new ModerateProductQuestionResultDto(false, "Question not found.");
        }

        try
        {
            question.Hide(command.AdminId, command.Reason);
        }
        catch (ArgumentException ex)
        {
            return new ModerateProductQuestionResultDto(false, ex.Message);
        }

        await _questionRepository.UpdateAsync(question, cancellationToken);
        await _questionRepository.SaveChangesAsync(cancellationToken);

        return new ModerateProductQuestionResultDto(true, null);
    }

    /// <summary>
    /// Unhides a product question (admin moderation).
    /// </summary>
    public async Task<ModerateProductQuestionResultDto> HandleAsync(
        UnhideProductQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var question = await _questionRepository.GetByIdAsync(command.QuestionId, cancellationToken);
        if (question is null)
        {
            return new ModerateProductQuestionResultDto(false, "Question not found.");
        }

        try
        {
            question.Unhide();
        }
        catch (InvalidOperationException ex)
        {
            return new ModerateProductQuestionResultDto(false, ex.Message);
        }

        await _questionRepository.UpdateAsync(question, cancellationToken);
        await _questionRepository.SaveChangesAsync(cancellationToken);

        return new ModerateProductQuestionResultDto(true, null);
    }
}
