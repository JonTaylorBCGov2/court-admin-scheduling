﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Hosting;
using CAS.DB.models;

namespace CAS.API.services.ef
{
    /// <summary>
    /// This is a utility service, to load up our Migrations before any code execution. 
    /// </summary>
    public class MigrationAndSeedService
    {
        public IServiceProvider Services { get; }
        private ILogger<MigrationAndSeedService> Logger { get; }

        public MigrationAndSeedService(IServiceProvider services, ILogger<MigrationAndSeedService> logger)
        {
            Services = services;
            Logger = logger;
        }

        public void ExecuteMigrationsAndSeeds()
        {
            try
            {
                Logger.LogInformation("Starting Migrations.");
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CourtAdminDbContext>();
                var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                var migrationsAssembly = db.GetService<IMigrationsAssembly>();
                var historyRepository = db.GetService<IHistoryRepository>();

                var all = migrationsAssembly.Migrations.Keys;
                var applied = historyRepository.Exists() ? historyRepository.GetAppliedMigrations().Select(r => r.MigrationId).ToList() : new List<string>();
                var pending = all.Except(applied).ToList();

                Logger.LogInformation($"Pending {pending.Count} Migrations.");
                Logger.LogDebug($"{string.Join(", ", pending)}");
                db.Database.Migrate();
                Logger.LogInformation("Migration(s) complete.");

                if (applied.Count != 0) return;
                
                ExecuteSeedScripts(db, environment);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Database migration failed on startup.");
            }
        }

        private void ExecuteSeedScripts(CourtAdminDbContext db, IWebHostEnvironment environment)
        {
            var seedPath = environment.IsDevelopment() ? Path.Combine("docker", "seed") : "data";
            var dbSqlPath = environment.IsDevelopment() ? Path.Combine("db", "sql") : Path.Combine("src", "db", "sql");
            var path = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, seedPath);
            var dbPath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, dbSqlPath);
            Logger.LogInformation($"Fresh database detected. Loading SQL from paths: {dbPath} and then {path}");

            var transaction = db.Database.BeginTransaction();
            var lastFile = "";
            try
            {
                var files = GetSqlFilesOrderedByNumber(dbPath).Concat(GetSqlFilesOrderedByNumber(path)).ToList();
                Logger.LogInformation($"Found {files.Count} files.");
                foreach (var file in files)
                {
                    lastFile = file;
                    Logger.LogInformation($"Executing File: {file}");
                    db.Database.ExecuteSqlRaw(File.ReadAllText(file));
                }
                transaction.Commit();
                Logger.LogInformation($"Executing files successful.");
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error while executing {lastFile}. Rolling back all files.");
                transaction.Rollback();
            }
        }

        private IEnumerable<string> GetSqlFilesOrderedByNumber(string path)
        {
            if (Directory.Exists(path))
                return Directory.GetFiles(path, "*.sql").OrderBy(x =>
                    Regex.Match(x, @"\d+").Value);

            Logger.LogWarning($"{path} does not exist.");
            return new List<string>();
        }
    }
}
