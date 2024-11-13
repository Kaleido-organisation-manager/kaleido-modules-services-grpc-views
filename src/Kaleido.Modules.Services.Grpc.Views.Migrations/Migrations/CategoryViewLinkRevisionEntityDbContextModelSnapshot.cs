﻿// <auto-generated />
using System;
using Kaleido.Modules.Services.Grpc.Views.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kaleido.Modules.Services.Grpc.Views.Migrations.Migrations
{
    [DbContext(typeof(CategoryViewLinkRevisionEntityDbContext))]
    partial class CategoryViewLinkRevisionEntityDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Kaleido.Modules.Services.Grpc.Views.Common.Models.CategoryViewLinkRevisionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasColumnType("varchar(8)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("EntityId")
                        .HasColumnType("uuid");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("varchar(36)");

                    b.Property<int>("Revision")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Key");

                    b.ToTable("CategoryViewLinkRevisions", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
