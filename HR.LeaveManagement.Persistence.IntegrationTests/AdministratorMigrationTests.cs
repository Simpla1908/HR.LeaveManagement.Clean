using HR.LeaveManagement.Identity.DbContext;
using HR.LeaveManagement.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HR.LeaveManagement.Persistence.IntegrationTests;

public class AdministratorMigrationTests
{
    private const string Email = "simplicelandu1908@gmail.com";

    [Fact]
    public Task Migration_creates_an_administrator_with_a_valid_Identity_password()
        => WithDatabase(async db =>
        {
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var user = await db.Users.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal("Simplice", user.FirstName);
            Assert.Equal("Landu", user.LastName);
            Assert.Equal("Simpla", user.UserName);
            Assert.Equal("SIMPLA", user.NormalizedUserName);
            Assert.Equal(Email, user.Email);
            Assert.Equal(Email.ToUpperInvariant(), user.NormalizedEmail);
            Assert.True(user.LockoutEnabled);
            Assert.False(string.IsNullOrEmpty(user.SecurityStamp));
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<ApplicationUser>().VerifyHashedPassword(user, user.PasswordHash!, "Simpl@1908"));
            await AssertAdministrator(db, user.Id);
        });

    [Fact]
    public Task Migration_promotes_an_existing_account_without_replacing_its_credentials()
        => WithDatabase(async db =>
        {
            var user = Account("Simpla", Email);
            user.FirstName = "Existing";
            user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, "Existing!Pass123");
            db.Users.Add(user);
            var role = new IdentityRole("Administrator") { NormalizedName = "ADMINISTRATOR" };
            db.Roles.Add(role);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            var originalHash = user.PasswordHash;
            var originalStamp = user.SecurityStamp;

            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
            db.ChangeTracker.Clear();

            var existing = await db.Users.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(user.Id, existing.Id);
            Assert.Equal("Existing", existing.FirstName);
            Assert.Equal(originalHash, existing.PasswordHash);
            Assert.Equal(originalStamp, existing.SecurityStamp);
            Assert.Equal(role.Id, (await db.Roles.SingleAsync(TestContext.Current.CancellationToken)).Id);
            await AssertAdministrator(db, existing.Id);
        });

    [Theory]
    [InlineData("SomeoneElse", Email)]
    [InlineData("Simpla", "someone-else@example.test")]
    public Task Migration_rejects_conflicting_accounts(string userName, string email)
        => WithDatabase(async db =>
        {
            var user = Account(userName, email);
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var error = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.MigrateAsync(TestContext.Current.CancellationToken));

            Assert.Equal(51000, error.Number);
            Assert.Equal(user.Id, (await db.Users.SingleAsync(TestContext.Current.CancellationToken)).Id);
            Assert.Empty(await db.UserRoles.ToListAsync(TestContext.Current.CancellationToken));
            Assert.DoesNotContain("20261114120000_SeedSimpliceAdministrator",
                await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
        });

    private static ApplicationUser Account(string userName, string email) => new()
    {
        UserName = userName, NormalizedUserName = userName.ToUpperInvariant(),
        Email = email, NormalizedEmail = email.ToUpperInvariant(),
        FirstName = "Test", LastName = "Existing"
    };

    private static async Task AssertAdministrator(HrLeaveManagementIdentityDbContext db, string userId)
    {
        var role = await db.Roles.SingleAsync(x => x.NormalizedName == "ADMINISTRATOR", TestContext.Current.CancellationToken);
        var membership = await db.UserRoles.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(role.Id, membership.RoleId);
    }

    private static async Task WithDatabase(Func<HrLeaveManagementIdentityDbContext, Task> test)
    {
        var databaseName = "HRLeaveAdminMigrationTests_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("HR_TEST_CONNECTION")
            ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true")
        { InitialCatalog = databaseName };
        await using var db = new HrLeaveManagementIdentityDbContext(
            new DbContextOptionsBuilder<HrLeaveManagementIdentityDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261107211145_InitialIdentity", TestContext.Current.CancellationToken);
            await test(db);
        }
        finally
        {
            // Seule la base temporaire propre à ce test peut être supprimée.
            Assert.Equal(databaseName, db.Database.GetDbConnection().Database);
            Assert.StartsWith("HRLeaveAdminMigrationTests_", databaseName);
            await db.Database.EnsureDeletedAsync();
        }
    }
}
