using Messenger.Data;
using Messenger.DTOs;
using Messenger.Models.BaseModels;
using Messenger.Models.ChatModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Controllers.ChatControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupChatController : ControllerBase
    {
        private readonly AppDBContext _context;

        public GroupChatController(AppDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var groups = await _context.Set<GroupChat>()
                .Include(g => g.Users)
                .Include(g => g.Admins)
                .Include(g => g.CreatedBy)
                .Select(g => new GroupResponseDTO(
                    g.Id,
                    g.ChatName,
                    g.Users.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    g.Admins.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    g.MaxUsers,
                    g.IsPrivate,
                    g.CreatedAt,
                    g.LastActivityAt,
                    g.CreatedBy != null ? new UserResponseDTO(
                        g.CreatedBy.Id,
                        g.CreatedBy.Name,
                        g.CreatedBy.AvatarPath,
                        g.CreatedBy.RegisterDate
                    ) : null
                ))
                .ToListAsync();

            return Ok(groups);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var group = await _context.Set<GroupChat>()
                .Include(g => g.Users)
                .Include(g => g.Admins)
                .Include(g => g.CreatedBy)
                .Where(g => g.Id == id)
                .Select(g => new GroupResponseDTO(
                    g.Id,
                    g.ChatName,
                    g.Users.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    g.Admins.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    g.MaxUsers,
                    g.IsPrivate,
                    g.CreatedAt,
                    g.LastActivityAt,
                    g.CreatedBy != null ? new UserResponseDTO(
                        g.CreatedBy.Id,
                        g.CreatedBy.Name,
                        g.CreatedBy.AvatarPath,
                        g.CreatedBy.RegisterDate
                    ) : null
                ))
                .FirstOrDefaultAsync();

            if (group == null)
                return NotFound($"Group with Id {id} not found");

            return Ok(group);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserGroups(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with Id {userId} not found");

            var groups = await _context.Set<GroupChat>()
                .Include(g => g.Users)
                .Include(g => g.Admins)
                .Include(g => g.CreatedBy)
                .Where(g => g.Users.Any(u => u.Id == userId))
                .Select(g => new GroupResponseDTO(
                    g.Id,
                    g.ChatName,
                    g.Users.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    g.Admins.Select(u => new UserResponseDTO(
                        u.Id,
                        u.Name,
                        u.AvatarPath,
                        u.RegisterDate
                    )).ToList(),
                    g.MaxUsers,
                    g.IsPrivate,
                    g.CreatedAt,
                    g.LastActivityAt,
                    g.CreatedBy != null ? new UserResponseDTO(
                        g.CreatedBy.Id,
                        g.CreatedBy.Name,
                        g.CreatedBy.AvatarPath,
                        g.CreatedBy.RegisterDate
                    ) : null
                ))
                .ToListAsync();

            return Ok(groups);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGroupDTO createGroupDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var creator = await _context.Users.FindAsync(createGroupDto.CreatedById);
            if (creator == null)
                return BadRequest($"User with Id {createGroupDto.CreatedById} not found");

            var group = new GroupChat
            {
                ChatName = createGroupDto.ChatName,
                MaxUsers = createGroupDto.MaxUsers,
                IsPrivate = createGroupDto.IsPrivate,
                CreatedById = createGroupDto.CreatedById,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };

            group.Users.Add(creator);
            group.Admins.Add(creator);

            await _context.Set<GroupChat>().AddAsync(group);
            await _context.SaveChangesAsync();

            await _context.Entry(group)
                .Collection(g => g.Users)
                .LoadAsync();
            await _context.Entry(group)
                .Collection(g => g.Admins)
                .LoadAsync();

            var response = new GroupResponseDTO(
                group.Id,
                group.ChatName,
                group.Users.Select(u => new UserResponseDTO(
                    u.Id,
                    u.Name,
                    u.AvatarPath,
                    u.RegisterDate
                )).ToList(),
                group.Admins.Select(u => new UserResponseDTO(
                    u.Id,
                    u.Name,
                    u.AvatarPath,
                    u.RegisterDate
                )).ToList(),
                group.MaxUsers,
                group.IsPrivate,
                group.CreatedAt,
                group.LastActivityAt,
                new UserResponseDTO(creator.Id, creator.Name, creator.AvatarPath, creator.RegisterDate)
            );

            return CreatedAtAction(nameof(GetById), new { id = group.Id }, response);
        }

        [HttpPost("{groupId}/users/{userId}")]
        public async Task<IActionResult> AddUser(Guid groupId, Guid userId, [FromBody] Guid? addedById)
        {
            var group = await _context.Set<GroupChat>()
                .Include(g => g.Users)
                .Include(g => g.Admins)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound($"Group with Id {groupId} not found");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with Id {userId} not found");

            User? addedBy = null;
            if (addedById.HasValue)
            {
                addedBy = await _context.Users.FindAsync(addedById.Value);
                if (addedBy == null)
                    return BadRequest($"User with Id {addedById.Value} not found");
            }

            try
            {
                group.AddUser(user, addedBy);
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

            return Ok(new { message = "User added to group successfully" });
        }

        [HttpDelete("{groupId}/users/{userId}")]
        public async Task<IActionResult> RemoveUser(Guid groupId, Guid userId, [FromBody] Guid? removedById)
        {
            var group = await _context.Set<GroupChat>()
                .Include(g => g.Users)
                .Include(g => g.Admins)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound($"Group with Id {groupId} not found");

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
                group.RemoveUser(user, removedBy);
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

            return Ok(new { message = "User removed from group successfully" });
        }

        [HttpPost("{groupId}/admins/{userId}")]
        public async Task<IActionResult> AddAdmin(Guid groupId, Guid userId)
        {
            var group = await _context.Set<GroupChat>()
                .Include(g => g.Users)
                .Include(g => g.Admins)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound($"Group with Id {groupId} not found");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with Id {userId} not found");

            try
            {
                group.AddAdmin(user);
                await _context.SaveChangesAsync();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(new { message = "User added as admin successfully" });
        }

        [HttpDelete("{groupId}/admins/{userId}")]
        public async Task<IActionResult> RemoveAdmin(Guid groupId, Guid userId)
        {
            var group = await _context.Set<GroupChat>()
                .Include(g => g.Users)
                .Include(g => g.Admins)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound($"Group with Id {groupId} not found");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"User with Id {userId} not found");

            try
            {
                group.RemoveAdmin(user);
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
            var group = await _context.Set<GroupChat>()
                .Include(g => g.MessagesHistory)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return NotFound($"Group with Id {id} not found");

            if (group.MessagesHistory != null && group.MessagesHistory.Any())
                _context.Messages.RemoveRange(group.MessagesHistory);

            _context.Set<GroupChat>().Remove(group);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Group successfully deleted", id = group.Id });
        }
    }
}