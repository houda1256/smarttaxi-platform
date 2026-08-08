using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Identity.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler
    : IQueryHandler<GetUserByIdQuery, Result<GetUserByIdResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository, IRolePermissionRepository rolePermissionRepository)
    {
        _userRepository = userRepository;
        _rolePermissionRepository = rolePermissionRepository;
    }

    public async Task<Result<GetUserByIdResult>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null)
        {
            return Result<GetUserByIdResult>.Failure("Utilisateur introuvable.", ErrorType.NotFound);
        }

        var permissions = await _rolePermissionRepository.GetPermissionsForRolesAsync(user.Roles.ToList(), cancellationToken);

        var result = new GetUserByIdResult(
            user.Id,
            user.Email.Value,
            user.Roles.Select(role => role.ToString()).ToList(),
            permissions.ToList());

        return Result<GetUserByIdResult>.Success(result);
    }
}
