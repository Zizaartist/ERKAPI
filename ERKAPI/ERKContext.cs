﻿using System;
using ERKAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace ERKAPI
{
    public partial class ERKContext : DbContext
    {
        public ERKContext()
        {
        }

        public ERKContext(DbContextOptions<ERKContext> options)
            : base(options)
        {
        }

        public virtual DbSet<BlacklistedPost> BlacklistedPosts { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<Diaspora> Diasporas { get; set; }
        public virtual DbSet<DiasporaRequest> DiasporaRequests { get; set; }
        public virtual DbSet<Opinion> Opinions { get; set; }
        public virtual DbSet<Post> Posts { get; set; }
        public virtual DbSet<PostData> PostData { get; set; }
        public virtual DbSet<PostImage> PostImages { get; set; }
        public virtual DbSet<Report> Reports { get; set; }
        public virtual DbSet<Subscription> Subscriptions { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<BlacklistedPost>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.PostId });

                entity.ToTable("BlacklistedPost");

                entity.HasOne(d => d.Post)
                    .WithMany(p => p.BlacklistedPosts)
                    .HasForeignKey(d => d.PostId)
                    .HasConstraintName("FK_BlacklistedPost_Post");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.BlacklistedPosts)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_BlacklistedPost_User");
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.ToTable("Comment");

                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.Property(e => e.Text)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Comment_User");

                entity.HasOne(d => d.Post)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.PostId)
                    .HasConstraintName("FK_Comment_Post");
            });

            modelBuilder.Entity<Diaspora>(entity =>
            {
                entity.ToTable("Diaspora");

                entity.Property(e => e.Info)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(250);
            });

            modelBuilder.Entity<DiasporaRequest>(entity =>
            {
                entity.ToTable("DiasporaRequest");

                entity.Property(e => e.Info)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.HasOne(d => d.Requester)
                    .WithMany(p => p.DiasporaRequests)
                    .HasForeignKey(d => d.RequesterId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_DiasporaRequest_User");
            });

            modelBuilder.Entity<Opinion>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.PostId });

                entity.ToTable("Opinion");

                entity.HasOne(d => d.Post)
                    .WithMany(p => p.Opinions)
                    .HasForeignKey(d => d.PostId)
                    .HasConstraintName("FK_Opinion_Post");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Opinions)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Opinion_User");
            });

            modelBuilder.Entity<Post>(entity =>
            {
                entity.ToTable("Post");

                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Posts)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Post_User");

                entity.HasOne(d => d.Repost)
                    .WithMany(p => p.InverseRepost)
                    .HasForeignKey(d => d.RepostId)
                    .HasConstraintName("FK_Post_Post");
            });

            modelBuilder.Entity<PostData>(entity =>
            {
                entity.HasKey(e => e.PostDataId);

                entity.HasIndex(e => e.PostId, "IX_PostData")
                    .IsUnique();

                entity.Property(e => e.Text)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.HasOne(d => d.Post)
                    .WithOne(p => p.PostDatum)
                    .HasForeignKey<PostData>(d => d.PostId)
                    .HasConstraintName("FK_PostData_Post");
            });

            modelBuilder.Entity<PostImage>(entity =>
            {
                entity.ToTable("PostImage");

                entity.Property(e => e.Image)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.HasOne(d => d.PostData)
                    .WithMany(p => p.PostImages)
                    .HasForeignKey(d => d.PostDataId)
                    .HasConstraintName("FK_PostImage_PostData");
            });

            modelBuilder.Entity<Report>(entity =>
            {
                entity.ToTable("Report");

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Reports)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Report_User");

                entity.HasOne(d => d.Post)
                    .WithMany(p => p.Reports)
                    .HasForeignKey(d => d.PostId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Report_Post");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.Property(e => e.Avatar).HasMaxLength(15);

                entity.Property(e => e.DateOfBirth).HasColumnType("date");

                entity.Property(e => e.Email).HasMaxLength(250);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.Phone)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.SubscriberCount).IsRequired();

                entity.Property(e => e.SubscriptionCount).IsRequired();

                entity.HasMany(e => e.Subscribers)
                    .WithMany(e => e.Subscriptions)
                    .UsingEntity<Subscription>(
                    e => e
                        .HasOne(x => x.Subscriber)
                        .WithMany(x => x.SubscribersEntities)
                        .HasForeignKey(x => x.SubscriberId),
                    e => e
                        .HasOne(x => x.SubscribedTo)
                        .WithMany(x => x.SubscriptionsEntities)
                        .HasForeignKey(x => x.SubscribedToId),
                    e => {
                        e.HasKey(x => new { x.SubscriberId, x.SubscribedToId });
                        e.ToTable("Subscription");
                    }
                );
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}