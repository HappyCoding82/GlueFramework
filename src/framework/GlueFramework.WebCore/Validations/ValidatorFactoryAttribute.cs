using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GlueFramework.WebCore.Validations
{
    public class ValidatorFactoryAttribute : ValidationAttribute 
    {
        private Type _validatorType = null;
        public ValidatorFactoryAttribute(Type validatorType)
        {
            _validatorType = validatorType;
        }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            //ValidatorBase validator = Activator.CreateInstance(_validatorType);
            ValidatorBase validator = (ValidatorBase)validationContext.GetService(_validatorType);
            if (validator == null)
                return ValidationResult.Success;
            else
                return validator.Validate(value, validationContext);
        }
    }

   
}
