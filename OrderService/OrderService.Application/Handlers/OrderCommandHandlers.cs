using MassTransit;
using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;

namespace OrderService.Application.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(request.UserId, request.ShippingAddress);

        foreach (var item in request.OrderItems)
        {
            order.AddOrderItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish OrderCreatedEvent for NotificationService
        await _publishEndpoint.Publish(new OrderCreatedEvent(
            order.Id,
            order.UserId,
            order.TotalAmount,
            order.OrderDate),
            cancellationToken);

        // Publish OrderPlacedEvent for OrchestratorService to start saga
        await _publishEndpoint.Publish(new OrderPlacedEvent(
            order.Id,
            order.UserId,
            order.TotalAmount,
            order.OrderItems.Select(i => new OrderItemInfo(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice)).ToList(),
            order.OrderDate),
            cancellationToken);

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.UserId,
            order.Status,
            order.TotalAmount,
            order.OrderDate,
            order.ShippedDate,
            order.DeliveredDate,
            order.ShippingAddress,
            order.OrderItems.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice)).ToList());
    }
}

public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public ConfirmOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OrderDto> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order with ID {request.OrderId} not found");

        order.ConfirmOrder();

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event
        await _publishEndpoint.Publish(new OrderConfirmedEvent(
            order.Id,
            order.UserId,
            order.TotalAmount,
            DateTime.UtcNow),
            cancellationToken);

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.UserId,
            order.Status,
            order.TotalAmount,
            order.OrderDate,
            order.ShippedDate,
            order.DeliveredDate,
            order.ShippingAddress,
            order.OrderItems.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice)).ToList());
    }
}

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public ShipOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OrderDto> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order with ID {request.OrderId} not found");

        order.MarkAsShipped();

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event
        await _publishEndpoint.Publish(new OrderShippedEvent(
            order.Id,
            order.UserId,
            order.ShippedDate!.Value),
            cancellationToken);

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.UserId,
            order.Status,
            order.TotalAmount,
            order.OrderDate,
            order.ShippedDate,
            order.DeliveredDate,
            order.ShippingAddress,
            order.OrderItems.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice)).ToList());
    }
}

public class DeliverOrderCommandHandler : IRequestHandler<DeliverOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public DeliverOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OrderDto> Handle(DeliverOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order with ID {request.OrderId} not found");

        order.MarkAsDelivered();

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event
        await _publishEndpoint.Publish(new OrderDeliveredEvent(
            order.Id,
            order.UserId,
            order.DeliveredDate!.Value),
            cancellationToken);

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.UserId,
            order.Status,
            order.TotalAmount,
            order.OrderDate,
            order.ShippedDate,
            order.DeliveredDate,
            order.ShippingAddress,
            order.OrderItems.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice)).ToList());
    }
}

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order with ID {request.OrderId} not found");

        order.Cancel();

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event
        await _publishEndpoint.Publish(new OrderCancelledEvent(
            order.Id,
            order.UserId,
            DateTime.UtcNow),
            cancellationToken);

        return true;
    }
}
