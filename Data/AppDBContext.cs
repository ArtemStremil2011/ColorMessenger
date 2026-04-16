using Microsoft.EntityFrameworkCore;
using Messenger.Models.BaseModels;
using Messenger.Models.ChatModels;

namespace Messenger.Data
{
    public class AppDBContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<GroupChat> GroupChats { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=messenger.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(50);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PhoneNumber).HasMaxLength(20);
                entity.Property(u => u.AvatarPath).HasMaxLength(500);
                entity.HasIndex(u => u.Name).IsUnique();
                if (entity.Metadata.GetProperties().Any(p => p.Name == "PhoneNumber"))
                    entity.HasIndex(u => u.PhoneNumber).IsUnique();
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.MessageId);
                entity.Property(m => m.MessageText).IsRequired().HasMaxLength(5000);
                entity.Property(m => m.MessageCreateDate).IsRequired();
                entity.Property(m => m.IsDeleted).HasDefaultValue(false);
                entity.HasOne(m => m.MessageCreator)
                    .WithMany(u => u.Messages)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(m => m.MessageCreateDate);
            });

            modelBuilder.Entity<Channel>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.ChatName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.MaxUsers).HasDefaultValue(100);
                entity.Property(c => c.IsPrivate).HasDefaultValue(false);
                entity.Property(c => c.CreatedAt).IsRequired();
                entity.HasOne(c => c.CreatedBy)
                    .WithMany(u => u.Channels)
                    .HasForeignKey(c => c.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(c => c.Users)
                    .WithMany(u => u.Channels)
                    .UsingEntity(j => j.ToTable("ChannelUsers"));

                entity.HasMany(c => c.Admins)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("ChannelAdmins"));

                entity.HasMany(c => c.MessagesHistory)
                    .WithOne()
                    .HasForeignKey("ChatId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.ChatName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.MaxUsers).HasDefaultValue(2);
                entity.Property(c => c.IsPrivate).HasDefaultValue(true);
                entity.Property(c => c.CreatedAt).IsRequired();

                entity.HasOne(c => c.CreatedBy)
                    .WithMany(u => u.Chats)
                    .HasForeignKey(c => c.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(c => c.Users)
                    .WithMany(u => u.Chats)
                    .UsingEntity(j => j.ToTable("ChatUsers"));

                entity.HasMany(c => c.MessagesHistory)
                    .WithOne(m => m.Chat)
                    .HasForeignKey(m => m.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(t => t.HasCheckConstraint("CK_Chat_MaxUsers", "[MaxUsers] = 2"));
            });

            modelBuilder.Entity<GroupChat>(entity =>
            {
                entity.HasKey(g => g.Id);
                entity.Property(g => g.ChatName).IsRequired().HasMaxLength(100);
                entity.Property(g => g.MaxUsers).HasDefaultValue(100);
                entity.Property(g => g.IsPrivate).HasDefaultValue(false);
                entity.Property(g => g.CreatedAt).IsRequired();

                entity.HasOne(g => g.CreatedBy)
                    .WithMany(u => u.GroupChats)
                    .HasForeignKey(g => g.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(g => g.Users)
                    .WithMany(u => u.GroupChats)
                    .UsingEntity(j => j.ToTable("GroupChatUsers"));

                entity.HasMany(g => g.Admins)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("GroupChatAdmins"));

                entity.HasMany(g => g.MessagesHistory)
                    .WithOne()
                    .HasForeignKey("ChatId")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(t => t.HasCheckConstraint("CK_GroupChat_MaxUsers", "[MaxUsers] >= 3"));
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}