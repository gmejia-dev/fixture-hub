using FixtureHub.Application.Abstractions.Idempotency;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Application.Teams.CreateTeam;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.IntegrationTests.Idempotency;

[Collection(nameof(SqlServerCollection))]
public sealed class IdempotencyTests(SqlServerFixture database)
{
    [Fact]
    public async Task CreateTeam_RepeatedWithTheSameKey_ReturnsTheSameIdAndSavesOneTeam()
    {
        var key = Guid.NewGuid().ToString();
        var command = new CreateTeamCommand(TestData.UniqueName("Los Halcones"), "El Salvador");

        var first = await database.SendAsync<CreateTeamCommand, Guid>(command, key);
        var repeated = await database.SendAsync<CreateTeamCommand, Guid>(command, key);

        Assert.Equal(first.Value, repeated.Value);
        Assert.Equal(1, await CountTeamsAsync(command.Name));
        Assert.Equal(1, await database.QueryAsync(context => context.IdempotencyRecords.CountAsync(record => record.Key == key)));
    }

    [Fact]
    public async Task CreateTeam_SameKeyWithADifferentBody_ReturnsKeyReused()
    {
        var key = Guid.NewGuid().ToString();
        await database.SendAsync<CreateTeamCommand, Guid>(
            new CreateTeamCommand(TestData.UniqueName("Los Halcones"), "El Salvador"), key);

        var reused = await database.SendAsync<CreateTeamCommand, Guid>(
            new CreateTeamCommand(TestData.UniqueName("Los Pumas"), "Guatemala"), key);

        Assert.Equal("Idempotency.KeyReused", reused.Error.Code);
    }

    [Fact]
    public async Task CreateTeam_SameKeyInParallel_SavesOneTeamAndBothGetTheSameId()
    {
        var key = Guid.NewGuid().ToString();
        var command = new CreateTeamCommand(TestData.UniqueName("Los Halcones"), "El Salvador");

        var results = await Task.WhenAll(
            database.SendAsync<CreateTeamCommand, Guid>(command, key),
            database.SendAsync<CreateTeamCommand, Guid>(command, key));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(results[0].Value, results[1].Value);
        Assert.Equal(1, await CountTeamsAsync(command.Name));
    }

    [Fact]
    public async Task CreateTeam_KeyLockedByAnotherTransaction_WaitsUntilItIsReleased()
    {
        var key = Guid.NewGuid().ToString();
        await using var holder = database.CreateScope();
        var holderUnitOfWork = holder.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await holderUnitOfWork.BeginTransactionAsync(CancellationToken.None);
        Assert.True(await holder.ServiceProvider.GetRequiredService<IIdempotencyStore>().TryLockAsync(key, CancellationToken.None));

        var waiting = database.SendAsync<CreateTeamCommand, Guid>(
            new CreateTeamCommand(TestData.UniqueName("Los Halcones"), "El Salvador"), key);
        var finishedWhileLocked = await Task.WhenAny(waiting, Task.Delay(TimeSpan.FromSeconds(1))) == waiting;
        await holderUnitOfWork.RollbackAsync(CancellationToken.None);

        Assert.False(finishedWhileLocked);
        Assert.True((await waiting).IsSuccess);
    }

    [Fact]
    public async Task CreateTeam_WithAnExpiredKey_RunsTheCommandAndReplacesTheRecord()
    {
        var key = Guid.NewGuid().ToString();
        await AddExpiredRecordAsync(key);
        var command = new CreateTeamCommand(TestData.UniqueName("Los Halcones"), "El Salvador");

        var result = await database.SendAsync<CreateTeamCommand, Guid>(command, key);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await CountTeamsAsync(command.Name));
        var record = await database.QueryAsync(context => context.IdempotencyRecords.SingleAsync(record => record.Key == key));
        Assert.False(record.IsExpired(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task ExpiredKey_DeletedByAnotherConnectionBeforeItIsReplaced_IsStillReplaced()
    {
        var key = Guid.NewGuid().ToString();
        await AddExpiredRecordAsync(key);
        await using var scope = database.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
        await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        Assert.True(await store.TryLockAsync(key, CancellationToken.None));
        Assert.NotNull(await store.FindAsync(key, CancellationToken.None));

        await database.QueryAsync(context => context.IdempotencyRecords.Where(record => record.Key == key).ExecuteDeleteAsync());
        await store.RemoveAsync(key, CancellationToken.None);
        var now = DateTimeOffset.UtcNow;
        store.Add(new IdempotencyRecord(key, new string('1', IdempotencyRecord.FingerprintLength), null, now, now.AddHours(24)));
        var commit = await unitOfWork.CommitAsync(CancellationToken.None);

        Assert.True(commit.IsSuccess);
        var record = await database.QueryAsync(context => context.IdempotencyRecords.SingleAsync(record => record.Key == key));
        Assert.False(record.IsExpired(DateTimeOffset.UtcNow));
    }

    private Task<int> AddExpiredRecordAsync(string key)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(-1);

        return database.QueryAsync(context =>
        {
            context.IdempotencyRecords.Add(new IdempotencyRecord(
                key, new string('0', IdempotencyRecord.FingerprintLength), null, expiresAt.AddHours(-24), expiresAt));
            return context.SaveChangesAsync();
        });
    }

    private Task<int> CountTeamsAsync(string name) =>
        database.QueryAsync(context => context.Teams.CountAsync(team => team.Name == name));
}
