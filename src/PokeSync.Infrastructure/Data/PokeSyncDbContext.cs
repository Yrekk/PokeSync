using Microsoft.EntityFrameworkCore;
using PokeSync.Domain.Entities;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokeSync.Infrastructure.Data
{
    public class PokeSyncDbContext : DbContext
    {
        public PokeSyncDbContext(DbContextOptions<PokeSyncDbContext> options) : base(options) { }
        public DbSet<ElementType> Types => Set<ElementType>();
        public DbSet<Generation> Generation => Set<Generation>();
        public DbSet<Pokemon> Pokemons => Set<Pokemon>();
        public DbSet<PokemonType> PokemonTypes => Set<PokemonType>();
        public DbSet<PokemonStat> PokemonStats => Set<PokemonStat>();
        public DbSet<PokemonFlavor> PokemonFlavors => Set<PokemonFlavor>();
        public DbSet<IdempotencyKey> idempotencyKeys => Set<IdempotencyKey>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---- ElementType
            modelBuilder.Entity<ElementType>(e =>
            {
                e.ToTable("ElementType");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(50);
                e.HasIndex(x => x.Name).IsUnique(); // Fire, Water, Grass...
            });

            // ---- Generation
            modelBuilder.Entity<Generation>(e =>
            {
                e.ToTable("Generation");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);
                e.HasIndex(x => x.Number).IsUnique(); // Gen 1...N
                e.HasMany(x => x.Pokemons)
                 .WithOne(p => p.Generation!)
                 .HasForeignKey(p => p.GenerationId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ---- Pokemon
            modelBuilder.Entity<Pokemon>(e =>
            {
                e.ToTable("Pokemon");
                e.HasKey(x => x.Id);

                e.Property(x => x.ExternalId)
                    .IsRequired()
                    .HasMaxLength(32);
                e.HasIndex(x => x.ExternalId).IsUnique(); // id PokeAPI

                e.Property(x => x.Number)
                    .IsRequired();
                e.HasIndex(x => x.Number); // recherche/tri

                e.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                e.HasIndex(x => x.Name); // filtrage alpha

                e.Property(x => x.SpriteUrl)
                    .HasMaxLength(512);

                e.Property(x => x.Height)
                    .HasColumnType("decimal(6,2)"); // safe
                e.Property(x => x.Weight)
                    .HasColumnType("decimal(7,2)");
                // Préparer un index combiné pour filtres courants (gen + id)
                e.HasIndex(x => new { x.GenerationId, x.Id });
            });
            // ---- PokemonType (N-N)
            modelBuilder.Entity<PokemonType>(e =>
            {
                e.ToTable("PokemonType");
                e.HasKey(x => new { x.PokemonId, x.TypeId }); // composite PK

                e.HasIndex(x => x.TypeId);

                e.HasOne(x => x.Pokemon)
                    .WithMany(p => p.PokemonTypes)
                    .HasForeignKey(x => x.PokemonId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.ElementType)
                    .WithMany(t => t.PokemonTypes)
                    .HasForeignKey(x => x.TypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---- PokemonStat (1-N)
            modelBuilder.Entity<PokemonStat>(e =>
            {
                e.ToTable("PokemonStat");
                e.HasKey(x => x.Id);

                e.Property(x => x.StatName)
                    .IsRequired()
                    .HasMaxLength(32);

                e.Property(x => x.BaseValue)
                    .IsRequired();

                // Unicité par (Pokemon, StatName) : une ligne par stat
                e.HasIndex(x => new { x.PokemonId, x.StatName }).IsUnique();

                e.HasOne(x => x.Pokemon)
                    .WithMany(p => p.Stats)
                    .HasForeignKey(x => x.PokemonId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---- PokemonFlavor (1-N par langue)
            modelBuilder.Entity<PokemonFlavor>(e =>
            {
                e.ToTable("PokemonFlavor");
                e.HasKey(x => x.Id);

                e.Property(x => x.Language)
                    .IsRequired()
                    .HasMaxLength(8); // ex: "en", "fr", "ja"

                e.Property(x => x.Text)
                    .IsRequired()
                    .HasMaxLength(4000); // large mais maîtrisé

                // Unicité par (Pokemon, Language)
                e.HasIndex(x => new { x.PokemonId, x.Language }).IsUnique();

                e.HasOne(x => x.Pokemon)
                    .WithMany(p => p.Flavors)
                    .HasForeignKey(x => x.PokemonId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---- IdempotencyKey
            modelBuilder.Entity<IdempotencyKey>(e =>
            {
                e.ToTable("IdempotencyKey");
                e.HasKey(x => x.Id);

                e.Property(x => x.ExternalKey)
                    .IsRequired()
                    .HasMaxLength(128);

                e.Property(x => x.CreatedUtc)
                    .IsRequired();

                e.HasIndex(x => x.ExternalKey).IsUnique();   // une clé, un traitement
                e.HasIndex(x => x.CreatedUtc);               // purge/TTL facile
            });
        }

    }
}
