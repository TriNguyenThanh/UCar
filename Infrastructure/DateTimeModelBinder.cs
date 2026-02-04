using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace UCar.Infrastructure;

/// <summary>
/// Custom ModelBinder để parse DateTime từ định dạng dd/MM/yyyy
/// </summary>
public class DateTimeModelBinder : IModelBinder
{
    private static readonly string[] DateFormats = new[]
    {
        "dd/MM/yyyy",
        "dd-MM-yyyy",
        "yyyy-MM-dd",
        "MM/dd/yyyy"
    };

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(value))
            return Task.CompletedTask;

        // Try parsing with multiple date formats
        if (DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, 
            DateTimeStyles.None, out var date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
        }
        else if (DateTime.TryParse(value, out date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(modelName, 
                $"Định dạng ngày không hợp lệ. Vui lòng sử dụng dd/MM/yyyy");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// ModelBinderProvider để đăng ký DateTimeModelBinder cho tất cả DateTime properties
/// </summary>
public class DateTimeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.Metadata.ModelType == typeof(DateTime) || 
            context.Metadata.ModelType == typeof(DateTime?))
        {
            return new DateTimeModelBinder();
        }

        return null;
    }
}
