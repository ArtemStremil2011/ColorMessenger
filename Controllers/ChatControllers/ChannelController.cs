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
    public class ChannelController : ControllerBase
    {
        private readonly AppDBContext _context;

        public ChannelController(AppDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var channels = await _context.Set<Channel>()
                .Include(c => c.Users)
                .Include(c => c.Admins)
                .Include(c => c.CreatedBy)
                .Select(c => new ChannelResponseDTO(
                    c.Id,
                    c.ChatName,
                    c.Users.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    c.Admins.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    c.MaxUsers,
                    c.IsPrivate,
                    c.CreatedAt,
                    c.LastActivityAt,
                    c.CreatedBy != null ? new UserResponseDTO(
                        c.CreatedBy.Id,
                        c.CreatedBy.Name,
                        c.CreatedBy.AvatarPath,
                        c.CreatedBy.RegisterDate
                    ) : null
                ))
                .ToListAsync();

            return Ok(channels);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var channel = await _context.Set<Channel>()
                .Include(c => c.Users)
                .Include(c => c.Admins)
                .Include(c => c.CreatedBy)
                .Where(c => c.Id == id)
                .Select(c => new ChannelResponseDTO(
                    c.Id,
                    c.ChatName,
                    c.Users.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    c.Admins.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    c.MaxUsers,
                    c.IsPrivate,
                    c.CreatedAt,
                    c.LastActivityAt,
                    c.CreatedBy != null ? new UserResponseDTO(
                        c.CreatedBy.Id,
                        c.CreatedBy.Name,
                        c.CreatedBy.AvatarPath,
                        c.CreatedBy.RegisterDate
                    ) : null
                ))
                .FirstOrDefaultAsync();

            if (channel == null)
                return NotFound($"Channel with Id {id} not found");

            return Ok(channel);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserChannels(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with Id {userId} not found");

            var channels = await _context.Set<Channel>()
                .Include(c => c.Users)
                .Include(c => c.Admins)
                .Include(c => c.CreatedBy)
                .Where(c => c.Users.Any(u => u.Id == userId))
                .Select(c => new ChannelResponseDTO(
                    c.Id,
                    c.ChatName,
                    c.Users.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    c.Admins.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    c.MaxUsers,
                    c.IsPrivate,
                    c.CreatedAt,
                    c.LastActivityAt,
                    c.CreatedBy != null ? new UserResponseDTO(
                        c.CreatedBy.Id,
                        c.CreatedBy.Name,
                        c.CreatedBy.AvatarPath,
                        c.CreatedBy.RegisterDate
                    ) : null
                ))
                .ToListAsync();

            return Ok(channels);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateChannelDTO createChannelDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var creator = await _context.Users.FindAsync(createChannelDto.CreatedById);
            if (creator == null)
                return BadRequest($"User with Id {createChannelDto.CreatedById} not found");

            var channel = new Channel
            {
                ChatName = createChannelDto.ChatName,
                MaxUsers = createChannelDto.MaxUsers,
                IsPrivate = createChannelDto.IsPrivate,
                CreatedById = createChannelDto.CreatedById,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };

            channel.Users.Add(creator);
            channel.Admins.Add(creator);

            await _context.Set<Channel>().AddAsync(channel);
            await _context.SaveChangesAsync();

            await _context.Entry(channel)
                .Collection(c => c.Users)
                .LoadAsync();
            await _context.Entry(channel)
                .Collection(c => c.Admins)
                .LoadAsync();

            var response = new ChannelResponseDTO(
                channel.Id,
                channel.ChatName,
                channel.Users.Select(u => new UserResponseDTO(
                    u.Id,
                    u.Name,
                    u.AvatarPath,
                    u.RegisterDate
                )).ToList(),
                channel.Admins.Select(u => new UserResponseDTO(
                    u.Id,
                    u.Name,
                    u.AvatarPath,
                    u.RegisterDate
                )).ToList(),
                channel.MaxUsers,
                channel.IsPrivate,
                channel.CreatedAt,
                channel.LastActivityAt,
                new UserResponseDTO(creator.Id, creator.Name, creator.AvatarPath, creator.RegisterDate)
            );

            return CreatedAtAction(nameof(GetById), new { id = channel.Id }, response);
        }

        [HttpPost("{channelId}/subscribe/{userId}")]
        public async Task<IActionResult> Subscribe(Guid channelId, Guid userId, [FromBody] Guid? addedById)
        {
            var channel = await _context.Set<Channel>()
                .Include(c => c.Users)
                .Include(c => c.Admins)
                .FirstOrDefaultAsync(c => c.Id == channelId);

            if (channel == null)
                return NotFound($"Channel with Id {channelId} not found");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with Id {userId} not found");

            User? addedBy = null;
            if (addedById.HasValue && channel.IsPrivate)
            {
                addedBy = await _context.Users.FindAsync(addedById.Value);
                if (addedBy == null)
                    return BadRequest($"User with Id {addedById.Value} not found");
            }

            try
            {
                channel.AddUser(user, addedBy);
                await _context.SaveChangesAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(new { message = "User subscribed to channel successfully" });
        }

        [HttpPost("{channelId}/unsubscribe/{userId}")]
        public async Task<IActionResult> Unsubscribe(Guid channelId, Guid userId, [FromBody] Guid? removedById)
        {
            var channel = await _context.Set<Channel>()
                .Include(c => c.Users)
                .Include(c => c.Admins)
                .FirstOrDefaultAsync(c => c.Id == channelId);

            if (channel == null)
                return NotFound($"Channel with Id {channelId} not found");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with Id {userId} not found");

            User? removedBy = null;
            if (removedById.HasValue)
            {
                removedBy = await _context.Users.FindAsync(removedById.Value);
                if (removedBy == null)
                    return BadRequest($"User with Id {removedById.Value} not found");
            }

            try
            {
                channel.RemoveUser(user, removedBy);
                await _context.SaveChangesAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(new { message = "User unsubscribed from channel successfully" });
        }

        [HttpPost("{channelId}/admins/{userId}")]
        public async Task<IActionResult> AddAdmin(Guid channelId, Guid userId)
        {
            var channel = await _context.Set<Channel>()
                .Include(c => c.Users)
                .Include(c => c.Admins)
                .FirstOrDefaultAsync(c => c.Id == channelId);

            if (channel == null)
                return NotFound($"Channel with Id {channelId} not found");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with Id {userId} not found");

            if (!channel.IsUserInChat(user))
                return BadRequest("User must be subscribed to the channel");

            if (!channel.IsAdmin(user))
            {
                channel.Admins.Add(user);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "User added as admin successfully" });
        }

        [HttpDelete("{channelId}/admins/{userId}")]
        public async Task<IActionResult> RemoveAdmin(Guid channelId, Guid userId)
        {
            var channel = await _context.Set<Channel>()
                .Include(c => c.Users)
                .Include(c => c.Admins)
                .FirstOrDefaultAsync(c => c.Id == channelId);

            if (channel == null)
                return NotFound($"Channel with Id {channelId} not found");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with Id {userId} not found");

            try
            {
                if (channel.Admins.Count <= 1 && channel.IsAdmin(user))
                    return BadRequest("Cannot remove the only admin");

                channel.Admins.Remove(user);
                await _context.SaveChangesAsync();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(new { message = "Admin removed successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var channel = await _context.Set<Channel>()
                .Include(c => c.MessagesHistory)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (channel == null)
                return NotFound($"Channel with Id {id} not found");

            if (channel.MessagesHistory != null && channel.MessagesHistory.Any())
                _context.Messages.RemoveRange(channel.MessagesHistory);

            _context.Set<Channel>().Remove(channel);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Channel successfully deleted", id = channel.Id });
        }
    }
}