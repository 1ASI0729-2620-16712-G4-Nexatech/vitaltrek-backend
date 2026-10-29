using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Provides extension methods for <see cref="ModelBuilder"/> to configure Entity Framework database naming conventions.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Maximum identifier length accepted by MySQL for tables, keys, foreign keys and indexes.
    /// </summary>
    private const int MaxIdentifierLength = 64;

    /// <summary>
    ///     Applies a snake_case naming convention with pluralized table names to all entities, properties, keys,
    ///     foreign keys, and indexes in the Entity Framework model.
    /// </summary>
    /// <param name="builder">The <see cref="ModelBuilder"/> instance being configured.</param>
    public static void UseSnakeCaseNamingConvention(this ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName)) entity.SetTableName(tableName.ToPlural().ToSnakeCase());

            foreach (var property in entity.GetProperties())
                property.SetColumnName(property.GetColumnName().ToSnakeCase());

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (!string.IsNullOrEmpty(keyName)) key.SetName(keyName.ToSnakeCase().ToDatabaseIdentifier());
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var foreignKeyName = foreignKey.GetConstraintName();
                if (!string.IsNullOrEmpty(foreignKeyName)) foreignKey.SetConstraintName(foreignKeyName.ToSnakeCase().ToDatabaseIdentifier());
            }

            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (!string.IsNullOrEmpty(indexName)) index.SetDatabaseName(indexName.ToSnakeCase().ToDatabaseIdentifier());
            }
        }
    }

    /// <summary>
    ///     Shortens an identifier that exceeds the MySQL limit, keeping a readable prefix and a deterministic hash
    ///     suffix so the generated name stays unique and stable between migrations.
    /// </summary>
    /// <param name="identifier">The snake_case identifier.</param>
    /// <returns>The identifier, shortened when necessary.</returns>
    private static string ToDatabaseIdentifier(this string identifier)
    {
        var name = identifier.TrimEnd('~', '_');
        if (name.Length <= MaxIdentifierLength) return name;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..8].ToLowerInvariant();
        return $"{name[..(MaxIdentifierLength - hash.Length - 1)].TrimEnd('_')}_{hash}";
    }
}
