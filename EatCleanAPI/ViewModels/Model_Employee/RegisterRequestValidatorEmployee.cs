using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EatCleanAPI.ViewModels.Model_Employee
{
    public class RegisterRequestValidatorEmployee: AbstractValidator<RegisterRequestEmployee>
    {
        public RegisterRequestValidatorEmployee()
        {
            

            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required")
                .Matches(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$")
                .WithMessage("Email format not match");

            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password is at least 6 characters")
                .MaximumLength(15).WithMessage("Password can not over 15 characters");

           
        }
    }
}
