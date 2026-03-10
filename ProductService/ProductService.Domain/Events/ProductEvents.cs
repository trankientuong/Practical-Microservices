namespace ProductService.Domain.Events;

public record ProductCreatedEvent(Guid ProductId, string Name, decimal Price, DateTime CreatedAt);

public record ProductUpdatedEvent(Guid ProductId, string Name, decimal Price, DateTime UpdatedAt);

public record StockUpdatedEvent(Guid ProductId, int NewStockQuantity, DateTime UpdatedAt);

public record ProductDeactivatedEvent(Guid ProductId, DateTime DeactivatedAt);

// External events consumed from OrchestratorService
public record StockReservationRequestedEvent(Guid OrderId, Guid ProductId, int Quantity, DateTime RequestedDate);

// Events published to OrchestratorService
public record StockReservedCompletedEvent(Guid OrderId, Guid ProductId, int ReservedQuantity, DateTime ReservedDate);

public record StockReservationFailedEvent(Guid OrderId, Guid ProductId, string Reason, DateTime FailedDate);
