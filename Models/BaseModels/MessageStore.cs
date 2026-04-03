namespace Messenger.Models.BaseModels
{
    public class MessageStore
    {
        public List<Message> Messages;

        public MessageStore()
        {
            Messages = new List<Message>();
        }

        public MessageStore(List<Message> m)
        {
            Messages = m;
        }

        public void AddMessage(Message message)
        {
            Messages.Add(message);
        }

        public void RemoveMessage(Message message)
        {
            Messages.Remove(message);
        }

        public void Clear()
        {
            Messages = new List<Message>();
        }

        public void UpdateMessage(int messageId, Message message)
        {
            Messages[messageId] = message;
        }
    }
}
