using Messenger.DTOs;

namespace Messenger.DTOs
{
    // Базовый DTO для чата
    public abstract record ChatBaseDTO(
        Guid Id,
        string ChatName,
        int UsersCount,
        int MaxUsers,
        bool IsPrivate,
        DateTime CreatedAt,
        DateTime? LastActivityAt,
        UserResponseDTO? CreatedBy
    );

    // Для личного чата
    public record ChatResponseDTO(
        Guid Id,
        string ChatName,
        ICollection<UserResponseDTO> Users,
        UserResponseDTO? OtherUser,  // Удобно для личного чата
        DateTime CreatedAt,
        DateTime? LastActivityAt
    ) : ChatBaseDTO(Id, ChatName, Users.Count, 2, true, CreatedAt, LastActivityAt, null);

    public record CreateChatDTO(
    Guid User1Id,
    Guid User2Id,
    string? ChatName
    );

    // Для группы
    public record GroupResponseDTO(
        Guid Id,
        string ChatName,
        ICollection<UserResponseDTO> Users,
        ICollection<UserResponseDTO> Admins,
        int MaxUsers,
        bool IsPrivate,
        DateTime CreatedAt,
        DateTime? LastActivityAt,
        UserResponseDTO? CreatedBy
    ) : ChatBaseDTO(Id, ChatName, Users.Count, MaxUsers, IsPrivate, CreatedAt, LastActivityAt, CreatedBy);

    public record CreateGroupDTO(
        string ChatName,
        int MaxUsers,
        bool IsPrivate,
        Guid CreatedById
    );

    // Для канала
    public record ChannelResponseDTO(
        Guid Id,
        string ChatName,
        ICollection<UserResponseDTO> Users,
        ICollection<UserResponseDTO> Admins,
        int MaxUsers,
        bool IsPrivate,
        DateTime CreatedAt,
        DateTime? LastActivityAt,
        UserResponseDTO? CreatedBy
    ) : ChatBaseDTO(Id, ChatName, Users.Count, MaxUsers, IsPrivate, CreatedAt, LastActivityAt, CreatedBy);

    public record CreateChannelDTO(
        string ChatName,
        int MaxUsers,
        bool IsPrivate,
        Guid CreatedById
    );
}