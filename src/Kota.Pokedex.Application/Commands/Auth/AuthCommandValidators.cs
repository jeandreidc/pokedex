using FluentValidation;

namespace Kota.Pokedex.Application.Commands.Auth;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand> {
    public RegisterUserCommandValidator() {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand> {
    public LoginUserCommandValidator() {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
