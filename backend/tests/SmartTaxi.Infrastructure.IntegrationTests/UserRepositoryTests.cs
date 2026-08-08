using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Infrastructure.Identity.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class UserRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public UserRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_PersistsAllRoles()
    {
        var email = Email.Create($"{Guid.NewGuid()}@example.com");
        var user = User.Create(email, HashedPassword.Create("hashed"), UserRole.Customer);
        user.AssignRole(UserRole.Driver);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new UserRepository(writeContext);
            await repository.AddAsync(user, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var readRepository = new UserRepository(readContext);
        var reloaded = await readRepository.GetByIdAsync(user.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.True(reloaded!.HasRole(UserRole.Customer));
        Assert.True(reloaded.HasRole(UserRole.Driver));
        Assert.Equal(2, reloaded.Roles.Count);
    }

    [Fact]
    public async Task UpdateAsync_AfterRemovingRole_PersistsRemoval()
    {
        var email = Email.Create($"{Guid.NewGuid()}@example.com");
        var user = User.Create(email, HashedPassword.Create("hashed"), UserRole.Customer);
        user.AssignRole(UserRole.Driver);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new UserRepository(writeContext);
            await repository.AddAsync(user, CancellationToken.None);
        }

        await using (var updateContext = _fixture.CreateContext())
        {
            var repository = new UserRepository(updateContext);
            var tracked = await repository.GetByIdAsync(user.Id, CancellationToken.None);
            tracked!.RemoveRole(UserRole.Driver);
            await repository.UpdateAsync(tracked, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var readRepository = new UserRepository(readContext);
        var reloaded = await readRepository.GetByIdAsync(user.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.False(reloaded!.HasRole(UserRole.Driver));
        Assert.Single(reloaded.Roles);
    }
}
