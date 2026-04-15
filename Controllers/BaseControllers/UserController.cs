using Messenger.Data;
using Messenger.DTOs;
using Messenger.Models.BaseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Controllers.BaseControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDBContext _context;

        public UserController(AppDBContext context)
        {
            _context = context;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetAllMessageByUser(Guid userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return NotFound($"User with Id {userId} not found");

            var messages = await _context.Messages
                .Include(m => m.MessageCreator)
                .Where(m => m.UserId == userId && !m.IsDeleted)
                .OrderByDescending(m => m.MessageCreateDate)
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

            return Ok(messages);
        } 

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .Select(u => new UserResponseDTO(
                    u.Id,
                    u.Name,
                    u.AvatarPath,
                    u.RegisterDate
                ))
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new UserResponseDTO(
                    u.Id,
                    u.Name,
                    u.AvatarPath,
                    u.RegisterDate
                ))
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound($"User with Id {id} not found");

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserCreateDTO userCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _context.Users
                .AnyAsync(u => u.Name == userCreateDto.Name);

            if (existingUser)
                return BadRequest("Username already exists");

            var user = new User
            {
                Name = userCreateDto.Name,
                Password = userCreateDto.Password,
                AvatarPath = null
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var response = new UserResponseDTO(
                user.Id,
                user.Name,
                user.AvatarPath,
                user.RegisterDate
            );

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateDTO userUpdateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound($"User with Id {id} not found");

            if (!string.IsNullOrEmpty(userUpdateDto.Name))
                user.Name = userUpdateDto.Name;

            if (userUpdateDto.AvatarPath != null)
                user.AvatarPath = userUpdateDto.AvatarPath;

            if (!string.IsNullOrEmpty(userUpdateDto.NewPassword))
                user.Password = userUpdateDto.NewPassword;

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UserExists(id))
                    return NotFound();
                throw;
            }

            var response = new UserResponseDTO(
                user.Id,
                user.Name,
                user.AvatarPath,
                user.RegisterDate
            );

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound($"User with Id {id} not found");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User successfully deleted", id = user.Id });
        }

        private async Task<bool> UserExists(Guid id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }
    }
}