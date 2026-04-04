using FinanIA.Application.Auth.Commands;
using FinanIA.Domain.Interfaces;
using FluentAssertions;
using FluentValidation.Results;
using NSubstitute;

namespace FinanIA.Application.Tests.Auth;

public class RegisterUserCommandValidatorTests
{
    private readonly IUserRepository _userRepository;
    private readonly RegisterUserCommandValidator _validator;

    public RegisterUserCommandValidatorTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _validator = new RegisterUserCommandValidator(_userRepository);
    }

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _validator.ValidateAsync(
            new RegisterUserCommand("user@test.com", "password123"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DuplicateEmail_ShouldFailWithEmailError()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _validator.ValidateAsync(
            new RegisterUserCommand("existing@test.com", "password123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task PasswordTooShort_ShouldFailWithPasswordError()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _validator.ValidateAsync(
            new RegisterUserCommand("user@test.com", "abc"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task InvalidEmailFormat_ShouldFailWithEmailError()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _validator.ValidateAsync(
            new RegisterUserCommand("not-an-email", "password123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task EmptyEmail_ShouldFailWithEmailError()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _validator.ValidateAsync(
            new RegisterUserCommand(string.Empty, "password123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
