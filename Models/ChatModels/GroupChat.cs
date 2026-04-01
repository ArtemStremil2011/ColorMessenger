using Messenger.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Messenger.Models.ChatModels
{
    public class GroupChat : IChat
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ChatName { get; set; } = string.Empty;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Message> MessagesHistory { get; set; } = new List<Message>();

        [Range(3, 1000)]
        public int MaxUsers { get; set; }

        public bool IsPrivate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? LastActivityAt { get; set; }

        public Guid? CreatedById { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }

        public virtual List<User> Admins { get; set; } = new List<User>();

        public GroupChat()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public void AddUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (Users.Count >= MaxUsers)
                throw new InvalidOperationException($"Достигнут лимит пользователей ({MaxUsers})");

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

            if (addedBy != null && !CanUserManageUsers(addedBy))
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

        public void AddAdmin(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!IsUserInChat(user))
                throw new InvalidOperationException("Пользователь должен быть участником группы");

            if (!IsAdmin(user))
            {
                Admins.Add(user);
                LastActivityAt = DateTime.UtcNow;
            }
        }

        public void RemoveAdmin(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (Admins.Count <= 1 && IsAdmin(user))
                throw new InvalidOperationException("В группе должен быть хотя бы один администратор");

            Admins.Remove(user);
            LastActivityAt = DateTime.UtcNow;
        }
    }
}