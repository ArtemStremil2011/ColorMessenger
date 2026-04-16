using Messenger.Data;
using Messenger.DTOs;
using Messenger.Models.BaseModels;
using Messenger.Models.ChatModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Messenger.Controllers.ChatControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly AppDBContext _context;

        public ChatController(AppDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var currentUser = await _context.Users.FindAsync(currentUserId);

            if (currentUser.Role != UserRole.Admin && currentUser.Role != UserRole.SuperAdmin)
                return Forbid("Only administrators can view all chats");

            var chats = await _context.Chats
                .Include(c => c.Users)
                .Include(c => c.CreatedBy)
                .Select(c => new ChatResponseDTO(
                    c.Id,
                    c.ChatName,
                    c.Users.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    null,
                    c.CreatedAt,
                    c.LastActivityAt
                ))
                .ToListAsync();

            return Ok(chats);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var chat = await _context.Chats
                .Include(c => c.Users)
                .Include(c => c.CreatedBy)
                .Where(c => c.Id == id)
                .Select(c => new ChatResponseDTO(
                    c.Id,
                    c.ChatName,
                    c.Users.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    null,
                    c.CreatedAt,
                    c.LastActivityAt
                ))
                .FirstOrDefaultAsync();

            if (chat == null)
                return NotFound($"Chat with Id {id} not found");

            var isUserInChat = await _context.Chats
                .Where(c => c.Id == id)
                .AnyAsync(c => c.Users.Any(u => u.Id == currentUserId));

            if (!isUserInChat)
                return Forbid("You don't have access to this chat");

            return Ok(chat);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserChats(Guid userId)
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var currentUser = await _context.Users.FindAsync(currentUserId);

            if (currentUserId != userId && currentUser.Role != UserRole.Admin && currentUser.Role != UserRole.SuperAdmin)
                return Forbid("You can only view your own chats");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with Id {userId} not found");

            var chats = await _context.Chats
                .Include(c => c.Users)
                .Include(c => c.CreatedBy)
                .Where(c => c.Users.Any(u => u.Id == userId))
                .Select(c => new ChatResponseDTO(
                    c.Id,
                    c.ChatName,
                    c.Users.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    c.Users.Where(u => u.Id != userId).Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).FirstOrDefault(),
                    c.CreatedAt,
                    c.LastActivityAt
                ))
                .ToListAsync();

            return Ok(chats);
        }

        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetChatMessages(Guid chatId, int page = 1, int pageSize = 50)
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var chat = await _context.Chats
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound($"Chat with Id {chatId} not found");

            var isUserInChat = chat.Users.Any(u => u.Id == currentUserId);
            if (!isUserInChat)
                return Forbid("You don't have access to this chat");

            var messages = await _context.Messages
                .Include(m => m.MessageCreator)
                .Where(m => m.ChatId == chatId && !m.IsDeleted)
                .OrderByDescending(m => m.MessageCreateDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MessageResponseDTO(
                    m.MessageId,
                    m.MessageText,
                    m.MessageCreateDate,
                    m.MessageLastUpdateDate,
                    m.UserId,
                    m.ChatId,
                    m.MessageCreator != null ? new UserResponseDTO(
                        m.MessageCreator.Id,
                        m.MessageCreator.Name,
                        m.MessageCreator.AvatarPath,
                        m.MessageCreator.RegisterDate
                    ) : null,
                    m.IsDeleted
                ))
                .ToListAsync();

            return Ok(new
            {
                page,
                pageSize,
                total = await _context.Messages.CountAsync(m => m.ChatId == chatId && !m.IsDeleted),
                messages
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateChatDTO createChatDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (currentUserId != createChatDto.User1Id)
                return Forbid("You cannot create a chat on behalf of another user");

            var user1 = await _context.Users.FindAsync(createChatDto.User1Id);
            if (user1 == null)
                return BadRequest($"User with Id {createChatDto.User1Id} not found");

            var user2 = await _context.Users.FindAsync(createChatDto.User2Id);
            if (user2 == null)
                return BadRequest($"User with Id {createChatDto.User2Id} not found");

            if (user1.Id == user2.Id)
                return BadRequest("Cannot create a chat with yourself");

            var existingChat = await _context.Chats
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Users.Count == 2 &&
                    c.Users.Any(u => u.Id == createChatDto.User1Id) &&
                    c.Users.Any(u => u.Id == createChatDto.User2Id));

            if (existingChat != null)
                return BadRequest("Chat between these users already exists");

            var chat = new Chat
            {
                ChatName = createChatDto.ChatName ?? $"{user1.Name} & {user2.Name}",
                MaxUsers = 2,
                IsPrivate = true,
                CreatedById = createChatDto.User1Id,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };

            chat.Users.Add(user1);
            chat.Users.Add(user2);

            await _context.Chats.AddAsync(chat);
            await _context.SaveChangesAsync();

            await _context.Entry(chat)
                .Collection(c => c.Users)
                .LoadAsync();

            var response = new ChatResponseDTO(
                chat.Id,
                chat.ChatName,
                chat.Users.Select(u => new UserResponseDTO(
                    u.Id,
                    u.Name,
                    u.AvatarPath,
                    u.RegisterDate
                )).ToList(),
                new UserResponseDTO(user2.Id, user2.Name, user2.AvatarPath, user2.RegisterDate),
                chat.CreatedAt,
                chat.LastActivityAt
            );

            return CreatedAtAction(nameof(GetById), new { id = chat.Id }, response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var currentUser = await _context.Users.FindAsync(currentUserId);

            var chat = await _context.Chats
                .Include(c => c.Users)
                .Include(c => c.MessagesHistory)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chat == null)
                return NotFound($"Chat with Id {id} not found");

            var isUserInChat = chat.Users.Any(u => u.Id == currentUserId);

            if (!isUserInChat && currentUser.Role != UserRole.Admin && currentUser.Role != UserRole.SuperAdmin)
                return Forbid("You don't have permission to delete this chat");

            if (chat.MessagesHistory != null && chat.MessagesHistory.Any())
                _context.Messages.RemoveRange(chat.MessagesHistory);

            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Chat successfully deleted", id = chat.Id });
        }

        [HttpPost("{chatId}/send")]
        public async Task<IActionResult> SendMessage(Guid chatId, [FromBody] string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                return BadRequest("Message cannot be empty");

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var chat = await _context.Chats
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound($"Chat with Id {chatId} not found");

            var isUserInChat = chat.Users.Any(u => u.Id == currentUserId);
            if (!isUserInChat)
                return Forbid("You don't have access to this chat");

            var user = await _context.Users.FindAsync(currentUserId);

            var message = new Message
            {
                MessageText = messageText,
                UserId = currentUserId,
                ChatId = chatId,
                MessageCreateDate = DateTime.UtcNow,
                MessageLastUpdateDate = DateTime.UtcNow,
                IsDeleted = false
            };

            chat.LastActivityAt = DateTime.UtcNow;

            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            var response = new MessageResponseDTO(
                message.MessageId,
                message.MessageText,
                message.MessageCreateDate,
                message.MessageLastUpdateDate,
                message.UserId,
                message.ChatId,
                new UserResponseDTO(
                    user.Id,
                    user.Name,
                    user.AvatarPath,
                    user.RegisterDate
                ),
                message.IsDeleted
            );

            return Ok(response);
        }
    }
}