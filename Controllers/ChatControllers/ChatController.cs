using Messenger.Data;
using Messenger.DTOs;
using Messenger.Models.BaseModels;
using Messenger.Models.ChatModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            return Ok(chat);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserChats(Guid userId)
        {
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateChatDTO createChatDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user1 = await _context.Users.FindAsync(createChatDto.User1Id);
            if (user1 == null)
                return BadRequest($"User with Id {createChatDto.User1Id} not found");

            var user2 = await _context.Users.FindAsync(createChatDto.User2Id);
            if (user2 == null)
                return BadRequest($"User with Id {createChatDto.User2Id} not found");

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
            var chat = await _context.Chats
                .Include(c => c.MessagesHistory)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chat == null)
                return NotFound($"Chat with Id {id} not found");

            if (chat.MessagesHistory != null && chat.MessagesHistory.Any())
                _context.Messages.RemoveRange(chat.MessagesHistory);

            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Chat successfully deleted", id = chat.Id });
        }
    }
}