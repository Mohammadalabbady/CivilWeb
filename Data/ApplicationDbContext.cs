using System;
using System.Collections.Generic;
using CivilWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace CivilWeb.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CivilDatum> CivilData { get; set; }

    public virtual DbSet<MyProdyct> MyProdycts { get; set; }

    public virtual DbSet<Table1> Table1s { get; set; }

    public virtual DbSet<UserPermission> UserPermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CivilDatum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CivilDat__3214EC276CEA1A2C");

            entity.HasIndex(e => e.FirstName, "IX_CivilData_FirstName").HasFilter("([FirstName] IS NOT NULL)");

            entity.HasIndex(e => e.LastName, "IX_CivilData_LastName").HasFilter("([LastName] IS NOT NULL)");

            entity.HasIndex(e => e.SecondName, "IX_CivilData_SecondName").HasFilter("([SecondName] IS NOT NULL)");

            entity.HasIndex(e => e.ThirdName, "IX_CivilData_ThirdName").HasFilter("([ThirdName] IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasMaxLength(10)
                .HasColumnName("ID");
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Gender).HasMaxLength(4);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(10);
            entity.Property(e => e.SecondName).HasMaxLength(50);
            entity.Property(e => e.ThirdName).HasMaxLength(50);
        });

        modelBuilder.Entity<Table1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Table_1");

            entity.Property(e => e.Alive).HasColumnName("alive");
            entity.Property(e => e.FirstName).HasMaxLength(10);
            entity.Property(e => e.FullName).HasMaxLength(35);
            entity.Property(e => e.Gender).HasMaxLength(4);
            entity.Property(e => e.Id)
                .HasMaxLength(10)
                .HasColumnName("ID");
            entity.Property(e => e.LastName).HasMaxLength(11);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(10);
            entity.Property(e => e.SecondName).HasMaxLength(10);
            entity.Property(e => e.ThirdName).HasMaxLength(10);
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Username).IsUnique();

            entity.Property(e => e.Username).HasMaxLength(256).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired().HasDefaultValue("User");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
