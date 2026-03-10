using MassTransit;
using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces;
using ProductService.Domain.Events;

namespace ProductService.Application.Consumers;

/// <summary>
/// Consumes StockReservationRequestedEvent from OrchestratorService and reserves stock
/// </summary>
public class StockReservationRequestedConsumer : IConsumer<StockReservationRequestedEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<StockReservationRequestedConsumer> _logger;

    public StockReservationRequestedConsumer(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<StockReservationRequestedConsumer> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservationRequestedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("📦 Stock reservation requested for Order {OrderId}, Product {ProductId}, Quantity: {Quantity}",
            message.OrderId, message.ProductId, message.Quantity);

        try
        {
            var product = await _productRepository.GetByIdAsync(message.ProductId, context.CancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found for stock reservation", message.ProductId);
                
                await _publishEndpoint.Publish(new StockReservationFailedEvent(
                    message.OrderId,
                    message.ProductId,
                    "Product not found",
                    DateTime.UtcNow),
                    context.CancellationToken);
                
                return;
            }

            if (!product.IsActive)
            {
                _logger.LogWarning("Product {ProductId} is not active", message.ProductId);
                
                await _publishEndpoint.Publish(new StockReservationFailedEvent(
                    message.OrderId,
                    message.ProductId,
                    "Product is not active",
                    DateTime.UtcNow),
                    context.CancellationToken);
                
                return;
            }

            if (product.StockQuantity < message.Quantity)
            {
                _logger.LogWarning("Insufficient stock for Product {ProductId}. Available: {Available}, Requested: {Requested}",
                    message.ProductId, product.StockQuantity, message.Quantity);
                
                await _publishEndpoint.Publish(new StockReservationFailedEvent(
                    message.OrderId,
                    message.ProductId,
                    $"Insufficient stock. Available: {product.StockQuantity}, Requested: {message.Quantity}",
                    DateTime.UtcNow),
                    context.CancellationToken);
                
                return;
            }

            // Reserve stock by reducing quantity
            product.ReduceStock(message.Quantity);
            await _productRepository.UpdateAsync(product, context.CancellationToken);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);

            // Publish success event to Orchestrator
            await _publishEndpoint.Publish(new StockReservedCompletedEvent(
                message.OrderId,
                message.ProductId,
                message.Quantity,
                DateTime.UtcNow),
                context.CancellationToken);

            _logger.LogInformation("✅ Stock reserved successfully for Order {OrderId}, Product {ProductId}, Quantity: {Quantity}",
                message.OrderId, message.ProductId, message.Quantity);

            // Also publish StockUpdatedEvent for notification purposes
            await _publishEndpoint.Publish(new StockUpdatedEvent(
                product.Id,
                product.StockQuantity,
                DateTime.UtcNow),
                context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for Order {OrderId}, Product {ProductId}",
                message.OrderId, message.ProductId);

            await _publishEndpoint.Publish(new StockReservationFailedEvent(
                message.OrderId,
                message.ProductId,
                $"Error reserving stock: {ex.Message}",
                DateTime.UtcNow),
                context.CancellationToken);

            throw;
        }
    }
}
