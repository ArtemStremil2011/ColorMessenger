using Messenger.DTOs;
using System.ComponentModel.DataAnnotations;

namespace Messenger.DTOs
{
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

    public record ChatResponseDTO(
        Guid Id,
        string ChatName,
        ICollection<UserResponseDTO> Users,  
        UserResponseDTO? OtherUser,  
        DateTime CreatedAt,
        DateTime? LastActivityAt
    ) : ChatBaseDTO(Id, ChatName, Users.Count, 2, true, CreatedAt, LastActivityAt, null);

    public record CreateChatDTO(
        [Required] Guid User1Id,
        [Required] Guid User2Id,
        string? ChatName
    );

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
        [Required]
        [StringLength(100)]
        string ChatName,

        [Range(3, 1000)]
        int MaxUsers,

        bool IsPrivate,

        [Required] Guid CreatedById
    );

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
        [Required]
        [StringLength(100)]
        string ChatName,

        [Range(1, 10000)]
        int MaxUsers,

        bool IsPrivate,

        [Required] Guid CreatedById
    );
}