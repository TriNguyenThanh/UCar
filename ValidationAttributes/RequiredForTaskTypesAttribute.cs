using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.ValidationAttributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RequiredForTaskTypesAttribute : ValidationAttribute
{
    private readonly TaskType[] _requiredTaskTypes;

    public RequiredForTaskTypesAttribute(params TaskType[] requiredTaskTypes)
    {
        _requiredTaskTypes = requiredTaskTypes;
        ErrorMessage = "Vui lòng chọn phương tiện cho loại nhiệm vụ này";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Get the TaskType property from the model
        var taskTypeProperty = validationContext.ObjectType.GetProperty("TaskType");
        if (taskTypeProperty == null)
        {
            return ValidationResult.Success;
        }

        var taskTypeValue = taskTypeProperty.GetValue(validationContext.ObjectInstance);
        if (taskTypeValue == null)
        {
            return ValidationResult.Success;
        }

        var taskType = (TaskType)taskTypeValue;

        // Check if this task type requires a vehicle
        if (_requiredTaskTypes.Contains(taskType))
        {
            // Vehicle is required for this task type
            if (value == null)
            {
                return new ValidationResult(ErrorMessage);
            }

            // If value is Guid?, check if it has value
            if (value is Guid guidValue && guidValue == Guid.Empty)
            {
                return new ValidationResult(ErrorMessage);
            }
        }

        return ValidationResult.Success;
    }
}
