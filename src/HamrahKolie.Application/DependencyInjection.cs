using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace HamrahKolie.Application;

/// <summary>ثبت سرویس‌های لایه Application در DI.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // ثبت خودکار همه Validatorهای FluentValidation در این اسمبلی.
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
