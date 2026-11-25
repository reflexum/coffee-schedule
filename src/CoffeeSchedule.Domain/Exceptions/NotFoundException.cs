namespace CoffeeSchedule.Domain.Exceptions;

public sealed class NotFoundException(string message) : DomainException(message);
