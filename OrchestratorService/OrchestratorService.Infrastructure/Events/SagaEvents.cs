namespace OrchestratorService.Infrastructure.Events;

// Events consumed from other services
public record OrderPlacedEvent(Guid OrderId, Guid UserId, decimal TotalAmount, List<OrderItemInfo> Items, DateTime PlacedDate);

public record OrderItemInfo(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record PaymentSucceededEvent(Guid OrderId, Guid PaymentId, decimal Amount, List<OrderItemInfo> Items, DateTime ProcessedDate);

// PaymentService publishes PaymentFailedEventForOrchestrator (to differentiate from NotificationService's PaymentFailedEvent)
public record PaymentFailedEventForOrchestrator(Guid OrderId, string Reason, DateTime FailedDate);

public record StockReservedCompletedEvent(Guid OrderId, Guid ProductId, int ReservedQuantity, DateTime ReservedDate);

public record StockReservationFailedEvent(Guid OrderId, Guid ProductId, string Reason, DateTime FailedDate);

// Events published by orchestrator
public record PaymentRequestedEvent(Guid OrderId, Guid UserId, decimal Amount, List<OrderItemInfo> Items, DateTime RequestedDate);

public record StockReservationRequestedEvent(Guid OrderId, Guid ProductId, int Quantity, DateTime RequestedDate);

public record RefundRequestedEvent(Guid OrderId, Guid PaymentId, decimal Amount, DateTime RequestedDate);

public record OrderConfirmedEvent(Guid OrderId, DateTime ConfirmedDate);

public record OrderCancelledEvent(Guid OrderId, DateTime CancelledDate, string Reason);
