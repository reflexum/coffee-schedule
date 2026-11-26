namespace CoffeeSchedule.Domain.Exceptions;

public sealed class UnauthorizedException(string message) : DomainException(message);
