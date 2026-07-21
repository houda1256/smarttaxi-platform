using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Identity.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, Result<RegisterUserResult>>
{
    private const int MinPasswordLength = 8;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<RegisterUserResult>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch (ArgumentException ex)
        {
            return Result<RegisterUserResult>.Failure(ex.Message, ErrorType.Validation);
        }

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            return Result<RegisterUserResult>.Failure("Un compte existe déjà avec cet email.", ErrorType.Conflict);
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < MinPasswordLength)
        {
            return Result<RegisterUserResult>.Failure(
                $"Le mot de passe doit contenir au moins {MinPasswordLength} caractères.", ErrorType.Validation);
        }

        var hashedPassword = HashedPassword.Create(_passwordHasher.Hash(command.Password));
        var user = User.Create(email, hashedPassword, UserRole.Customer);

        await _userRepository.AddAsync(user, cancellationToken);

        return Result<RegisterUserResult>.Success(new RegisterUserResult(user.Id, user.Email.Value));
    }
}
