﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TB.DanceDance.Data.PostgreSQL;

#nullable disable

namespace TB.DanceDance.Data.PostgreSQL.Migrations
{
    [DbContext(typeof(DanceDbContext))]
    [Migration("20230528200209_initial")]
    partial class initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.Event", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.EventAssigmentRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("EventId")
                        .HasColumnType("uuid");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("EventId");

                    b.ToTable("EventAssigmentRequests");
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.Group", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Groups");
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.GroupAssigmentRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("GroupId")
                        .HasColumnType("uuid");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("GroupId");

                    b.ToTable("GroupAssigmentRequests");
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.SharedWith", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid?>("EventId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("GroupId")
                        .HasColumnType("uuid");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("VideoId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("EventId");

                    b.HasIndex("GroupId");

                    b.HasIndex("VideoId");

                    b.ToTable("SharedWith");
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.Video", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("BlobId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<TimeOnly>("Duration")
                        .HasColumnType("time without time zone");

                    b.Property<Guid>("MetadataAsJson")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("RecordedDateTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset>("SharedDateTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("UploadedBy")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Videos");
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.EventAssigmentRequest", b =>
                {
                    b.HasOne("TB.DanceDance.Data.PostgreSQL.Models.Event", null)
                        .WithMany()
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.GroupAssigmentRequest", b =>
                {
                    b.HasOne("TB.DanceDance.Data.PostgreSQL.Models.Group", null)
                        .WithMany()
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.SharedWith", b =>
                {
                    b.HasOne("TB.DanceDance.Data.PostgreSQL.Models.Event", "Event")
                        .WithMany("HasSharedVideos")
                        .HasForeignKey("EventId");

                    b.HasOne("TB.DanceDance.Data.PostgreSQL.Models.Group", "Group")
                        .WithMany("HasSharedVideos")
                        .HasForeignKey("GroupId");

                    b.HasOne("TB.DanceDance.Data.PostgreSQL.Models.Video", "Video")
                        .WithMany("SharedWith")
                        .HasForeignKey("VideoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");

                    b.Navigation("Group");

                    b.Navigation("Video");
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.Event", b =>
                {
                    b.Navigation("HasSharedVideos");
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.Group", b =>
                {
                    b.Navigation("HasSharedVideos");
                });

            modelBuilder.Entity("TB.DanceDance.Data.PostgreSQL.Models.Video", b =>
                {
                    b.Navigation("SharedWith");
                });
#pragma warning restore 612, 618
        }
    }
}
