using FinanIA.Domain.Interfaces;
using FluentValidation;

namespace FinanIA.Application.Auth.Commands;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("Formato de e-mail inválido.")
            .MustAsync(async (email, ct) => !await userRepository.ExistsByEmailAsync(email, ct))
            .WithMessage("E-mail já está em uso.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(6).WithMessage("A senha deve ter no mínimo 6 caracteres.");
    }
}
