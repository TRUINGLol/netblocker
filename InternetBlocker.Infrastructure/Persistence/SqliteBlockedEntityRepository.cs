using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using InternetBlocker.Core.Interfaces;
using InternetBlocker.Core.Models;
using Microsoft.Data.Sqlite;

namespace InternetBlocker.Infrastructure.Persistence;

public class SqliteBlockedEntityRepository : IBlockedEntityRepository
{
    private readonly string _connectionString;

    public SqliteBlockedEntityRepository(string dbPath)
    {
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS BlockedEntities (
                Id TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                Name TEXT NOT NULL,
                Type INTEGER NOT NULL,
                IsEnabled INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL
            )");
    }

    public async Task<IEnumerable<BlockedEntity>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var dto = await connection.QueryAsync<BlockedEntityDto>("SELECT * FROM BlockedEntities");
        return dto.Select(d => new BlockedEntity(
            d.Id, d.Value, d.Name, (BlockedEntityType)d.Type, d.IsEnabled == 1, DateTime.Parse(d.CreatedAt)));
    }

    public async Task AddAsync(BlockedEntity entity)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO BlockedEntities (Id, Value, Name, Type, IsEnabled, CreatedAt)
            VALUES (@Id, @Value, @Name, @Type, @IsEnabled, @CreatedAt)",
            new { 
                entity.Id, 
                entity.Value, 
                entity.Name, 
                Type = (int)entity.Type, 
                IsEnabled = entity.IsEnabled ? 1 : 0, 
                CreatedAt = (entity.CreatedAt ?? DateTime.Now).ToString("O") 
            });
    }

    public async Task DeleteAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM BlockedEntities WHERE Id = @id", new { id });
    }

    public async Task UpdateAsync(BlockedEntity entity)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE BlockedEntities 
            SET Value = @Value, Name = @Name, IsEnabled = @IsEnabled 
            WHERE Id = @Id",
            new { 
                entity.Id, 
                entity.Value, 
                entity.Name, 
                IsEnabled = entity.IsEnabled ? 1 : 0 
            });
    }

    private class BlockedEntityDto
    {
        public string Id { get; set; } = "";
        public string Value { get; set; } = "";
        public string Name { get; set; } = "";
        public int Type { get; set; }
        public int IsEnabled { get; set; }
        public string CreatedAt { get; set; } = "";
    }
}
