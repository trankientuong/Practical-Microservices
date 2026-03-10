using MassTransit;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Domain.Events;

namespace OrderService.Application.Consumers;

/// <summary>
/// Consumes OrderConfirmedEvent from OrchestratorService after successful saga completion
/// </summary>
public class OrchestratorOrderConfirmedConsumer : IConsumer<OrderConfirmedEventFromOrchestrator>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrchestratorOrderConfirmedConsumer> _logger;

    public OrchestratorOrderConfirmedConsumer(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<OrchestratorOrderConfirmedConsumer> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderConfirmedEventFromOrchestrator> context)
    {
        var message = context.Message;
        _logger.LogInformation("✅ [SAGA SUCCESS] Order confirmed from Orchestrator: {OrderId}", message.OrderId);

        try
        {
            var order = await _orderRepository.GetByIdAsync(message.OrderId, context.CancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for confirmation from orchestrator", message.OrderId);
                return;
            }

            order.ConfirmOrder();
            await _orderRepository.UpdateAsync(order, context.CancellationToken);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);

            // Publish OrderConfirmedEvent for NotificationService
            await _publishEndpoint.Publish(new OrderConfirmedEvent(
                order.Id,
                order.UserId,
                order.TotalAmount,
                DateTime.UtcNow),
                context.CancellationToken);

            _logger.LogInformation("Order {OrderId} confirmed and notification event published", message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing orchestrator order confirmation for Order {OrderId}", message.OrderId);
            throw;
        }
    }
}

/// <summary>
/// Consumes OrderCancelledEvent from OrchestratorService after saga failure/compensation
/// </summary>
public class OrchestratorOrderCancelledConsumer : IConsumer<OrderCancelledEventFromOrchestrator>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrchestratorOrderCancelledConsumer> _logger;

    public OrchestratorOrderCancelledConsumer(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<OrchestratorOrderCancelledConsumer> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelledEventFromOrchestrator> context)
    {
        var message = context.Message;
        _logger.LogWarning("❌ [SAGA FAILED] Order cancelled from Orchestrator: {OrderId}, Reason: {Reason}",
            message.OrderId, message.Reason);

        try
        {
            var order = await _orderRepository.GetByIdAsync(message.OrderId, context.CancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for cancellation from orchestrator", message.OrderId);
                return;
            }

            order.Cancel();
            await _orderRepository.UpdateAsync(order, context.CancellationToken);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);

            // Publish OrderCancelledEvent for NotificationService
            await _publishEndpoint.Publish(new OrderCancelledEvent(
                order.Id,
                order.UserId,
                DateTime.UtcNow),
                context.CancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled and notification event published. Reason: {Reason}",
                message.OrderId, message.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing orchestrator order cancellation for Order {OrderId}", message.OrderId);
            throw;
        }
    }
}
