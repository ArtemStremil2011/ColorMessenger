namespace Messenger.Models.ChatModels
{
    public interface IChat
    {
        public Guid Id { get; set; }
        public string ChatName { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<Message> MessagesHistory { get; set; }
        public int MaxUsers { get; set; }
        public bool IsPrivate { get; set; }

        // Новые методы
        public void AddUser(User user);
        public void AddUser(User user, User? addedBy = null);
        public void RemoveUser(User user);
        public void RemoveUser(User user, User? removedBy = null);
        public bool IsUserInChat(User user);
        public bool CanUserManageUsers(User user);
    }
}