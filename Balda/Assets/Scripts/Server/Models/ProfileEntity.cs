using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace Assets.Scripts.Server.Models
{
    [Table("profiles")]
    public class ProfileEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }
}