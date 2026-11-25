using CoffeeSchedule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeSchedule.Api.Exceptions;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = CreateProblemDetails(httpContext, exception);
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        if (httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                "Request failed with {StatusCode}: {Message}",
                httpContext.Response.StatusCode,
                exception.Message);
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

            return new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Instance = httpContext.Request.Path
            };
        }

        var (statusCode, title, type, detail) = exception switch
        {
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Ресурс не найден",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                exception.Message),
            ConflictException => (
                StatusCodes.Status409Conflict,
                "Конфликт данных",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                exception.Message),
            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                "Доступ запрещён",
                "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                exception.Message),
            DomainException => (
                StatusCodes.Status422UnprocessableEntity,
                "Нарушение бизнес-правила",
                "https://tools.ietf.org/html/rfc4918#section-11.2",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                "Произошла непредвиденная ошибка при обработке запроса.")
        };

        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
    }
}
