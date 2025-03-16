using Microsoft.EntityFrameworkCore;
using Wpm.Clinic.DataAccess;

namespace Wpm.Clinic.DataAccess
{
    public class ClinicDBContext(DbContextOptions<ClinicDBContext> options) : DbContext(options)
    {
        public DbSet<Consulation> Consulations { get; set; }
    }

    public record Consulation(Guid Id, int PatientId, string PatientName, int PatientAge, DateTime StartTime);
}


public static class ClinicDbContextExtensions
{
    public static void EnsureDbIsCreated(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetService<ClinicDBContext>();
        context!.Database.EnsureCreated();
    }
}