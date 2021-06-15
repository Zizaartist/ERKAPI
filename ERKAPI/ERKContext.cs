using System;
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
        public virtual DbSet<PostMedia> PostMedia { get; set; }
        public virtual DbSet<Report> Reports { get; set; }
        public virtual DbSet<Question> Questions { get; set; }
        public virtual DbSet<Subscription> Subscriptions { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<City> Cities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

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

                entity.Property(e => e.Likes).IsRequired();

                entity.Property(e => e.Dislikes).IsRequired();

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Posts)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Post_User");

                entity.HasOne(d => d.Repost)
                    .WithMany(p => p.Reposts)
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
                    .WithOne(p => p.PostData)
                    .HasForeignKey<PostData>(d => d.PostId)
                    .HasConstraintName("FK_PostData_Post");
            });

            modelBuilder.Entity<PostMedia>(entity =>
            {
                entity.ToTable("PostMedia");

                entity.Property(e => e.Path)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.PostData)
                    .WithMany(p => p.PostMedia)
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
                    .OnDelete(DeleteBehavior.Cascade)
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

                entity.HasOne(e => e.Country)
                    .WithMany(e => e.Users)
                    .HasForeignKey(e => e.CountryId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.City)
                    .WithMany(e => e.Users)
                    .HasForeignKey(e => e.CityId)
                    .OnDelete(DeleteBehavior.NoAction);

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
                    e =>
                    {
                        e.HasKey(x => new { x.SubscriberId, x.SubscribedToId });
                        e.ToTable("Subscription");
                    }
                );

                entity.HasMany(e => e.BlacklistedPosts)
                    .WithMany(e => e.UsersWhoBlacklisted)
                    .UsingEntity<BlacklistedPost>(
                    e => e
                        .HasOne(x => x.Post)
                        .WithMany(x => x.BlacklistedPostEntities)
                        .HasForeignKey(x => x.PostId),
                    e => e
                        .HasOne(x => x.User)
                        .WithMany(x => x.BlacklistedPostEntities)
                        .HasForeignKey(x => x.UserId),
                    e =>
                    {
                        e.HasKey(x => new { x.UserId, x.PostId });
                        e.ToTable("BlacklistedPost");
                    }
                );
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("Question");

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.Questions)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Country>(entity => 
            {
                entity.ToTable("Country");

                entity.HasKey(e => e.CountryId);
            });

            modelBuilder.Entity<City>(entity =>
            {
                entity.ToTable("City");

                entity.HasKey(e => e.CityId);

                entity.HasOne(e => e.Country)
                    .WithMany(e => e.Cities)
                    .HasForeignKey(e => e.CountryId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
