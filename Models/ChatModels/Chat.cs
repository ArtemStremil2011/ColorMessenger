using System;
using System.Collections.Generic;
using System.Linq;

namespace Messenger.Models.ChatModels
{
    public class Chat : IChat
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

        public Chat(string name, ICollection<User> users)
        {
            Id = Guid.NewGuid();
            ChatName = name ?? throw new ArgumentNullException(nameof(name));
            Users = users ?? new List<User>();
            MessagesHistory = new List<Message>();
            MaxUsers = 2;
            IsPrivate = true;
            CreatedAt = DateTime.UtcNow;
            CreatedBy = users?.FirstOrDefault();
        }

        public void AddUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (Users.Count >= MaxUsers)
                throw new InvalidOperationException($"Личный чат не может содержать более {MaxUsers} пользователей");

            if (!IsUserInChat(user))
            {
                Users.Add(user);
                UpdateActivity();
            }
        }

        public void AddUser(User user, User? addedBy)
        {
            // В личном чате добавление работает одинаково, независимо от того, кто добавляет
            AddUser(user);
        }

        public void RemoveUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (Users.Count <= 1 && IsUserInChat(user))
                throw new InvalidOperationException("В чате должен быть хотя бы один пользователь");

            Users.Remove(user);
            UpdateActivity();
        }

        public void RemoveUser(User user, User? removedBy)
        {
            // В личном чате удаление работает одинаково
            RemoveUser(user);
        }

        public bool IsUserInChat(User user)
        {
            if (user == null)
                return false;
            return Users.Any(u => u.Id == user.Id);
        }

        public bool CanUserManageUsers(User user)
        {
            // В личном чате оба пользователя могут управлять
            return IsUserInChat(user);
        }

        private void UpdateActivity()
        {
            LastActivityAt = DateTime.UtcNow;
        }

        public User? GetOtherUser(User currentUser)
        {
            return Users.FirstOrDefault(u => u.Id != currentUser?.Id);
        }
    }
}