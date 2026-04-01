using Messenger.DTOs;

namespace Messenger.DTOs
{
    public record MessageResponseDTO(
        Guid MessageId,
        string MessageText,
        DateTime MessageCreateDate,
        DateTime? MessageLastUpdateDate,
        int UserId,
        UserResponseDTO? MessageCreator,
        bool IsDeleted
    );

    public record MessageCreateDTO(
        string MessageText,
        int UserId
    );

    public record MessageUpdateDTO(
        Guid MessageId,
        string MessageText
    );
}