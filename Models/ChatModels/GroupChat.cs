using System;
using System.Collections.Generic;
using System.Linq;

namespace Messenger.Models.ChatModels
{
    public class GroupChat : IChat
    {
        public Guid Id { get; set; }
        public string ChatName { get; set; } = string.Empty;
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Message> MessagesHistory { get; set; } = new List<Message>();
        public int MaxUsers { get; set; }
        public bool IsPrivate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public User? CreatedBy { get; set; }
        public List<User> Admins { get; set; } = new List<User>();

        public GroupChat(string name, ICollection<User> users, bool isPrivate, int maxUsers)
        {
            Id = Guid.NewGuid();
            ChatName = name ?? throw new ArgumentNullException(nameof(name));
            Users = users ?? new List<User>();
            MessagesHistory = new List<Message>();
            MaxUsers = maxUsers;
            IsPrivate = isPrivate;
            CreatedAt = DateTime.UtcNow;
            CreatedBy = users?.FirstOrDefault();

            Admins = new List<User>();
            var firstUser = users?.FirstOrDefault();
            if (firstUser != null)
            {
                Admins.Add(firstUser);
            }
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
                UpdateActivity();
            }
        }

        public void AddUser(User user, User? addedBy)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (addedBy != null && !CanUserManageUsers(addedBy))
                throw new UnauthorizedAccessException("Только администраторы могут добавлять пользователей");

            if (Users.Count >= MaxUsers)
                throw new InvalidOperationException($"Достигнут лимит пользователей ({MaxUsers})");

            if (!IsUserInChat(user))
            {
                Users.Add(user);
                UpdateActivity();
            }
        }

        public void RemoveUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (IsAdmin(user) && Admins.Count <= 1)
                throw new InvalidOperationException("Нельзя удалить единственного администратора группы");

            Users.Remove(user);
            Admins.Remove(user);
            UpdateActivity();
        }

        public void RemoveUser(User user, User? removedBy)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (removedBy != null && !CanUserManageUsers(removedBy))
                throw new UnauthorizedAccessException("Только администраторы могут удалять пользователей");

            if (IsAdmin(user) && Admins.Count <= 1)
                throw new InvalidOperationException("Нельзя удалить единственного администратора группы");

            Users.Remove(user);
            Admins.Remove(user);
            UpdateActivity();
        }

        public bool IsUserInChat(User user)
        {
            if (user == null)
                return false;
            return Users.Any(u => u.Id == user.Id);
        }

        public bool CanUserManageUsers(User user)
        {
            if (user == null)
                return false;
            return IsAdmin(user);
        }

        public bool IsAdmin(User user)
        {
            if (user == null)
                return false;
            return Admins.Any(a => a.Id == user.Id);
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
            }
        }

        public void RemoveAdmin(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (Admins.Count <= 1 && IsAdmin(user))
                throw new InvalidOperationException("В группе должен быть хотя бы один администратор");

            Admins.Remove(user);
        }

        private void UpdateActivity()
        {
            LastActivityAt = DateTime.UtcNow;
        }
    }
}