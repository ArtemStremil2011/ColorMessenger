using System;
using System.ComponentModel.DataAnnotations;

namespace Messenger.Models.BaseModels
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? AvatarPath { get; set; }

        [Required]
        public DateTime RegisterDate { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        public User()
        {
            Id = Guid.NewGuid();
            RegisterDate = DateTime.UtcNow;
        }
    }
}