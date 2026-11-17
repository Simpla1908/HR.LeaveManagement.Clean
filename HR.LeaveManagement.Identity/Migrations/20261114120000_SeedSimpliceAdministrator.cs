using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR.LeaveManagement.Identity.Migrations;

public partial class SeedSimpliceAdministrator : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Hash Identity V3 : PBKDF2-HMAC-SHA512, 100 000 itérations et sel aléatoire.
        // Un compte existant conserve son mot de passe et ses données.
        migrationBuilder.Sql("""
            DECLARE @Email nvarchar(256) = N'SIMPLICELANDU1908@GMAIL.COM';
            DECLARE @UserName nvarchar(256) = N'SIMPLA';
            DECLARE @UserId nvarchar(450);
            DECLARE @RoleId nvarchar(450);

            IF (SELECT COUNT(*) FROM [AspNetUsers] WHERE [NormalizedEmail] = @Email) > 1
                THROW 51000, 'Plusieurs comptes utilisent cet email. Corrigez les doublons avant la migration.', 1;

            IF EXISTS (
                SELECT 1 FROM [AspNetUsers]
                WHERE ([NormalizedEmail] = @Email AND ISNULL([NormalizedUserName], N'') <> @UserName)
                   OR ([NormalizedUserName] = @UserName AND ISNULL([NormalizedEmail], N'') <> @Email)
            )
                THROW 51000, 'Cet email ou ce nom utilisateur appartient a un autre compte. Verifiez les comptes avant la migration.', 1;

            SELECT @UserId = [Id] FROM [AspNetUsers] WHERE [NormalizedEmail] = @Email;
            SELECT @RoleId = [Id] FROM [AspNetRoles] WHERE [NormalizedName] = N'ADMINISTRATOR';

            IF @RoleId IS NULL
            BEGIN
                SET @RoleId = N'0e0cd931-2c91-4cc2-b3b4-599c3bb96af7';
                INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
                VALUES (@RoleId, N'Administrator', N'ADMINISTRATOR', N'9fbcb259-073e-4adf-b5c2-a1a53a3ec5f2');
            END;

            IF @UserId IS NULL
            BEGIN
                SET @UserId = N'deeed8b0-995a-4c64-a408-b91d79370af5';
                INSERT INTO [AspNetUsers] (
                    [Id], [FirstName], [LastName], [UserName], [NormalizedUserName],
                    [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash],
                    [SecurityStamp], [ConcurrencyStamp], [PhoneNumberConfirmed],
                    [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount]
                )
                VALUES (
                    @UserId, N'Simplice', N'Landu', N'Simpla', @UserName,
                    N'simplicelandu1908@gmail.com', @Email, 0,
                    N'AQAAAAIAAYagAAAAEBj9jo87n7StqGy51TbLVj+Ubc8bC6dlG2OEYI2irlvoP3f5p6fOdVnt/sYgAZ+WZw==',
                    N'146d22ce-68f6-46ef-904f-87c4d96a0121',
                    N'7188dd61-1f2f-49d5-9d01-74ce3bf6a27b', 0, 0, 1, 0
                );
            END;

            IF NOT EXISTS (
                SELECT 1 FROM [AspNetUserRoles] WHERE [UserId] = @UserId AND [RoleId] = @RoleId
            )
                INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (@UserId, @RoleId);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Impossible de distinguer un rôle préexistant d'un rôle ajouté à un ancien compte.
        // Ne pas supprimer de compte ni retirer des droits lors d'un retour arrière implicite.
        throw new NotSupportedException(
            "Cette migration de donnees ne peut pas etre annulee automatiquement. " +
            "Gerez explicitement le compte et ses roles avant un retour a InitialIdentity.");
    }
}
