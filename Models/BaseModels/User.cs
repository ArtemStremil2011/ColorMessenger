using Messenger.Models.ChatModels;
using System.ComponentModel.DataAnnotations;

namespace Messenger.Models.BaseModels
{
    // Enum - это перечисление возможных ролей
    // User = 0 - обычный пользователь (значение по умолчанию)
    // Admin = 1 - администратор (может управлять другими пользователями)
    // SuperAdmin = 2 - главный администратор (неограниченные права)
    public enum UserRole
    {
        User = 0,        // Обычный пользователь
        Admin = 1,       // Администратор
        SuperAdmin = 2   // Супер администратор
    }

    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(500)]
        public string? AvatarPath { get; set; }

        public DateTime RegisterDate { get; set; }

        public bool IsPhoneNumberConfirmed { get; set; } = false;
        public string? PhoneVerificationCode { get; set; }
        public DateTime? VerificationCodeExpiry { get; set; }

        // НОВОЕ ПОЛЕ: Роль пользователя
        // По умолчанию все новые пользователи получают роль User
        // Значение хранится в БД как число (0, 1, 2)
        public UserRole Role { get; set; } = UserRole.User;

        // Навигационные свойства (связи с другими таблицами)
        public virtual ICollection<Message>? Messages { get; set; }
        public virtual ICollection<Channel>? Channels { get; set; }
        public virtual ICollection<Chat>? Chats { get; set; }
        public virtual ICollection<GroupChat>? GroupChats { get; set; }

        public User()
        {
            Id = Guid.NewGuid();
            RegisterDate = DateTime.UtcNow;
        }
    }
}