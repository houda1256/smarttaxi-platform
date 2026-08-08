using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Referrals.Abstractions;

namespace SmartTaxi.Application.Identity.Referrals.Queries.GetMyReferralCode;

/// <summary>Lazily generates and persists a referral code on first request — not at registration time.</summary>
public sealed class GetMyReferralCodeQueryHandler : IQueryHandler<GetMyReferralCodeQuery, Result<string>>
{
    private const string NotFoundError = "Utilisateur introuvable.";

    private readonly IUserRepository _userRepository;
    private readonly IReferralCodeGenerator _codeGenerator;

    public GetMyReferralCodeQueryHandler(IUserRepository userRepository, IReferralCodeGenerator codeGenerator)
    {
        _userRepository = userRepository;
        _codeGenerator = codeGenerator;
    }

    public async Task<Result<string>> Handle(GetMyReferralCodeQuery query, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null)
        {
            return Result<string>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var hadCode = user.ReferralCode is not null;
        user.EnsureReferralCode(_codeGenerator.Generate());

        if (!hadCode)
        {
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        return Result<string>.Success(user.ReferralCode!);
    }
}
