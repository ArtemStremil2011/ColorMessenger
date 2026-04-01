namespace Messenger.DTOs
{
    // Для чтения данных
    public record UserResponseDTO(
        Guid Id,
        string Name,
        string? AvatarPath,
        DateTime RegisterDate
    );

    // Для создания/обновления
    public record UserCreateDTO(
        string Name,
        string Password
    );

    public record UserUpdateDTO(
        string? Name,
        string? AvatarPath,
        string? CurrentPassword,
        string? NewPassword
    );

    // Для логина
    public record UserLoginDTO(
        string Name,
        string Password
    );
}