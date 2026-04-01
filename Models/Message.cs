namespace Messenger.Models
{
    public class Message
    {
        public Guid MassageId;
        public string MassageText;
        public DateTime MesssageCreateDate;
        public DateTime MesssageLastUpdateDate;
        public int UserID { get; set; }
        public User MessageCreator { get; set; }

        public bool ISDelete;
    }
}
