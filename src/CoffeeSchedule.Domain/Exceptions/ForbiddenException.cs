namespace CoffeeSchedule.Domain.Exceptions;

public sealed class ForbiddenException(string message) : DomainException(message);
