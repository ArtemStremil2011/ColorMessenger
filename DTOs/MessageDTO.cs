using Messenger.DTOs;

namespace Messenger.DTOs
{
    public record MessageResponseDTO(
        Guid MessageId,
        string MessageText,
        DateTime MessageCreateDate,
        DateTime? MessageLastUpdateDate,
        Guid UserId,
        UserResponseDTO? MessageCreator,
        bool IsDeleted
    );

    public record MessageCreateDTO(
        string MessageText,
        Guid UserId
    );

    public record MessageUpdateDTO(
        Guid MessageId,
        string MessageText
    );
}