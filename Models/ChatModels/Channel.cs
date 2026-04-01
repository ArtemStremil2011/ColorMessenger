using Messenger.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Messenger.Models.ChatModels
{
    public class Channel : IChat
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ChatName { get; set; } = string.Empty;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Message> MessagesHistory { get; set; } = new List<Message>();

        [Range(1, 10000)]
        public int MaxUsers { get; set; }

        public bool IsPrivate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? LastActivityAt { get; set; }

        public Guid? CreatedById { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }

        public virtual List<User> Admins { get; set; } = new List<User>();

        public Channel()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public void AddUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (Users.Count >= MaxUsers)
                throw new InvalidOperationException($"Достигнут лимит подписчиков ({MaxUsers})");

            if (!IsUserInChat(user))
            {
                Users.Add(user);
                LastActivityAt = DateTime.UtcNow;
            }
        }

        public void AddUser(User user, User? addedBy)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (IsPrivate && (addedBy == null || !CanUserManageUsers(addedBy)))
                throw new UnauthorizedAccessException("Только администраторы могут добавлять пользователей");

            AddUser(user);
        }

        public void RemoveUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (IsAdmin(user) && Admins.Count <= 1)
                throw new InvalidOperationException("Нельзя удалить единственного администратора");

            if (Users.Remove(user))
            {
                Admins.Remove(user);
                LastActivityAt = DateTime.UtcNow;
            }
        }

        public void RemoveUser(User user, User? removedBy)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (removedBy != null && !CanUserManageUsers(removedBy))
                throw new UnauthorizedAccessException("Только администраторы могут удалять пользователей");

            RemoveUser(user);
        }

        public bool IsUserInChat(User user)
        {
            return user != null && Users.Any(u => u.Id == user.Id);
        }

        public bool CanUserManageUsers(User user)
        {
            return user != null && IsAdmin(user);
        }

        public bool IsAdmin(User user)
        {
            return user != null && Admins.Any(a => a.Id == user.Id);
        }
    }
}