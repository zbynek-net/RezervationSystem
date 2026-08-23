namespace ReservationSystem.AppMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserContactAndActivation : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "FirstName", c => c.String());
            AddColumn("dbo.AspNetUsers", "LastName", c => c.String());
            // Non-nullable bool needs a default so existing rows backfill; new users default to
            // inactive (item 2) unless the app sets it explicitly.
            AddColumn("dbo.AspNetUsers", "IsActive", c => c.Boolean(nullable: false, defaultValue: false));

            // Keep Admins active (so admins - including you - are not locked out), disable everyone else.
            Sql(@"UPDATE u
                    SET u.IsActive = 1
                    FROM dbo.AspNetUsers u
                    INNER JOIN dbo.AspNetUserRoles ur ON ur.UserId = u.Id
                    INNER JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId
                    WHERE r.Name = 'Admin'");
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "IsActive");
            DropColumn("dbo.AspNetUsers", "LastName");
            DropColumn("dbo.AspNetUsers", "FirstName");
        }
    }
}
