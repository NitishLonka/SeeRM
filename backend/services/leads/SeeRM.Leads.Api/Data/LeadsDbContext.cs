using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SeeRM.Leads.Domain;

namespace SeeRM.Leads.Data;

/// <summary>
/// Maps to the existing schema created by sqlscripts/leads/001_create_tables.sql (database-first;
/// no EF migrations). Column names are snake_case versions of the property names.
/// </summary>
public class LeadsDbContext(DbContextOptions<LeadsDbContext> options) : DbContext(options)
{
    public DbSet<Lead> Leads => Set<Lead>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Lead>(e =>
        {
            e.ToTable("leads");
            e.Property(x => x.FirstName).HasMaxLength(80);
            e.Property(x => x.LastName).HasMaxLength(80);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Phone).HasMaxLength(15);
            e.Property(x => x.CompanyName).HasMaxLength(150);
            e.Property(x => x.JobTitle).HasMaxLength(120);
            e.Property(x => x.Source).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.EstimatedValue).HasPrecision(12, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);

            // created_at has a DB default; updated_at is maintained by a DB trigger.
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
        });

        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var prop in entity.GetProperties())
                prop.SetColumnName(ToSnakeCase(prop.Name));
    }

    private static string ToSnakeCase(string name)
    {
        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
