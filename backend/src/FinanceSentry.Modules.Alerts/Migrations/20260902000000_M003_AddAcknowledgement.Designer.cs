// Hand-written designer partial: the migration was authored in a sandbox without the
// EF tooling, so `dotnet ef migrations add` never generated this file. Without the two
// attributes below EF Core does not discover the migration at all — Database.Migrate()
// applied nothing on deploy while the model snapshot already carried the new columns,
// and every alerts query failed with 42703 "column a.AcknowledgedAt does not exist".
using FinanceSentry.Modules.Alerts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Alerts.Migrations
{
    [DbContext(typeof(AlertsDbContext))]
    [Migration("20260902000000_M003_AddAcknowledgement")]
    partial class M003_AddAcknowledgement
    {
    }
}
