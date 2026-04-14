using Core.Persistence.Constants;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Core.Persistence.Extensions;

public static class NpgsqlDbContextOptionsBuilderExtensions
{
    public static NpgsqlDbContextOptionsBuilder WithMigrationHistoryTableInSchema(
        this NpgsqlDbContextOptionsBuilder dbContextOptionsBuilder,
        string schema) =>
            dbContextOptionsBuilder.MigrationsHistoryTable(TableNames.MigrationHistory, schema);
}