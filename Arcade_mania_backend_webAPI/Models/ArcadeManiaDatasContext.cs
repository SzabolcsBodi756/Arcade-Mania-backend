using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Arcade_mania_backend_webAPI.Models;

public partial class ArcadeManiaDatasContext : DbContext
{
    public ArcadeManiaDatasContext()
    {
    }

    public ArcadeManiaDatasContext(DbContextOptions<ArcadeManiaDatasContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Game> Games { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<UserHighScore> UserHighScores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("games");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.UserName, "uq_users_user_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(60)
                .IsFixedLength()
                .HasColumnName("password_hash");

            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .HasColumnName("user_name");

            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role")
                .HasDefaultValue("User");
        });

        modelBuilder.Entity<UserHighScore>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.GameId }).HasName("PRIMARY");

            entity.ToTable("user_high_scores");

            entity.HasIndex(e => e.GameId, "fk_user_high_scores_game");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.GameId).HasColumnName("game_id");
            entity.Property(e => e.HighScore)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("high_score");

            entity.HasOne(d => d.Game).WithMany(p => p.UserHighScores)
                .HasForeignKey(d => d.GameId)
                .HasConstraintName("fk_user_high_scores_game");

            entity.HasOne(d => d.User).WithMany(p => p.UserHighScores)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_high_scores_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
