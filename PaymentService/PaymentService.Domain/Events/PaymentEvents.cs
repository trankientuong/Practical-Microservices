namespace PaymentService.Domain.Events;

// Payment events that will be published to RabbitMQ
public record PaymentCreatedEvent(Guid PaymentId, Guid OrderId, decimal Amount, DateTime CreatedDate);

public record PaymentProcessingEvent(Guid PaymentId, Guid OrderId, DateTime ProcessingDate);

public record PaymentCompletedEvent(Guid PaymentId, Guid OrderId, decimal Amount, DateTime CompletedDate);

public record PaymentFailedEvent(Guid PaymentId, Guid OrderId, string Reason, DateTime FailedDate);

public record PaymentRefundedEvent(Guid PaymentId, Guid OrderId, decimal Amount, DateTime RefundedDate);

// External events that PaymentService consumes (from OrchestratorService)
public record PaymentRequestedEvent(Guid OrderId, Guid UserId, decimal Amount, List<OrderItemInfo> Items, DateTime RequestedDate);

public record OrderItemInfo(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record RefundRequestedEvent(Guid OrderId, Guid PaymentId, decimal Amount, DateTime RequestedDate);

// Events published to OrchestratorService
public record PaymentSucceededEvent(Guid OrderId, Guid PaymentId, decimal Amount, List<OrderItemInfo> Items, DateTime ProcessedDate);

public record PaymentFailedEventForOrchestrator(Guid OrderId, string Reason, DateTime FailedDate);

// External events that PaymentService consumes (LEGACY - for backward compatibility with old flow)
public record OrderConfirmedEvent(Guid OrderId, Guid UserId, decimal TotalAmount, DateTime ConfirmedDate);

public record OrderCancelledEvent(Guid OrderId, Guid UserId, DateTime CancelledDate);
