using Microsoft.EntityFrameworkCore;
using Messenger.Models.BaseModels;
using Messenger.Models.ChatModels;

namespace Messenger.Data
{
    public class AppDBContext : DbContext
    {
        // DbSet для всех сущностей
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<GroupChat> GroupChats { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Настройка подключения к SQLite
            optionsBuilder.UseSqlite("Data Source=messenger.db");

            // Опционально: включить логирование SQL запросов для отладки
            // optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ========== Настройка User ==========
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Password).IsRequired().HasMaxLength(100);
                entity.Property(u => u.AvatarPath).HasMaxLength(500);

                // Индексы для быстрого поиска
                entity.HasIndex(u => u.Name).IsUnique();
            });

            // ========== Настройка Message ==========
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.MessageId);
                entity.Property(m => m.MessageText).IsRequired().HasMaxLength(5000);
                entity.Property(m => m.MessageCreateDate).IsRequired();
                entity.Property(m => m.IsDeleted).HasDefaultValue(false);

                // Связь Message -> User (один ко многим)
                entity.HasOne(m => m.MessageCreator)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Индекс для сортировки по дате
                entity.HasIndex(m => m.MessageCreateDate);
            });

            // ========== Настройка Channel ==========
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.ChatName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.MaxUsers).HasDefaultValue(100);
                entity.Property(c => c.IsPrivate).HasDefaultValue(false);
                entity.Property(c => c.CreatedAt).IsRequired();

                // Связь Channel -> CreatedBy (многие к одному)
                entity.HasOne(c => c.CreatedBy)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                // Связь многие-ко-многим Channel <-> Users
                entity.HasMany(c => c.Users)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("ChannelUsers"));

                // Связь многие-ко-многим Channel <-> Admins
                entity.HasMany(c => c.Admins)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("ChannelAdmins"));

                // Связь один-ко-многим Channel -> Messages
                entity.HasMany(c => c.MessagesHistory)
                    .WithOne()
                    .HasForeignKey("ChannelId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== Настройка Chat (личный чат) ==========
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.ChatName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.MaxUsers).HasDefaultValue(2);
                entity.Property(c => c.IsPrivate).HasDefaultValue(true);
                entity.Property(c => c.CreatedAt).IsRequired();

                // Связь Chat -> CreatedBy
                entity.HasOne(c => c.CreatedBy)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                // Связь многие-ко-многим Chat <-> Users
                entity.HasMany(c => c.Users)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("ChatUsers"));

                // Связь один-ко-многим Chat -> Messages
                entity.HasMany(c => c.MessagesHistory)
                    .WithOne()
                    .HasForeignKey("ChatId")
                    .OnDelete(DeleteBehavior.Cascade);

                // Уникальное ограничение: в личном чате не может быть больше 2 пользователей
                entity.ToTable(t => t.HasCheckConstraint("CK_Chat_MaxUsers", "[MaxUsers] = 2"));
            });

            // ========== Настройка GroupChat ==========
            modelBuilder.Entity<GroupChat>(entity =>
            {
                entity.HasKey(g => g.Id);
                entity.Property(g => g.ChatName).IsRequired().HasMaxLength(100);
                entity.Property(g => g.MaxUsers).HasDefaultValue(100);
                entity.Property(g => g.IsPrivate).HasDefaultValue(false);
                entity.Property(g => g.CreatedAt).IsRequired();

                // Связь GroupChat -> CreatedBy
                entity.HasOne(g => g.CreatedBy)
                    .WithMany()
                    .HasForeignKey(g => g.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                // Связь многие-ко-многим GroupChat <-> Users
                entity.HasMany(g => g.Users)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("GroupChatUsers"));

                // Связь многие-ко-многим GroupChat <-> Admins
                entity.HasMany(g => g.Admins)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("GroupChatAdmins"));

                // Связь один-ко-многим GroupChat -> Messages
                entity.HasMany(g => g.MessagesHistory)
                    .WithOne()
                    .HasForeignKey("GroupChatId")
                    .OnDelete(DeleteBehavior.Cascade);

                // Ограничение на минимальное количество пользователей
                entity.ToTable(t => t.HasCheckConstraint("CK_GroupChat_MaxUsers", "[MaxUsers] >= 3"));
            });

            // ========== Глобальные настройки ==========

            // Отключаем каскадное удаление для всех внешних ключей по умолчанию
            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                if (foreignKey.DeleteBehavior == DeleteBehavior.Cascade)
                    foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // Конфигурация для SQLite: поддержка GUID
            modelBuilder.Entity<User>().Property(u => u.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Channel>().Property(c => c.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Chat>().Property(c => c.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<GroupChat>().Property(g => g.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Message>().Property(m => m.MessageId).HasDefaultValueSql("NEWID()");

            base.OnModelCreating(modelBuilder);
        }
    }
}   