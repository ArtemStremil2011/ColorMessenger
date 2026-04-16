using Messenger.Models.ChatModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Messenger.Models.BaseModels
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; 

        [StringLength(20)]
        public string? PhoneNumber { get; set; } 

        [StringLength(500)]
        public string? AvatarPath { get; set; }

        public DateTime RegisterDate { get; set; }

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