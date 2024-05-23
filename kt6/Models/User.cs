using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagement.API.Models
{

    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Status { get; set; } // Enum ('Админ', 'Пользователь')
    }
    public class ErrorResponse
    {
        [Key]
        public int Id { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
    public class AddUserToProjectRequest
    {
        [Required(ErrorMessage = "Идентификатор проекта обязателен")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Email пользователя обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Идентификатор роли обязателен")]
        public int RoleId { get; set; }
    }
    public class UpdateUserRoleRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required(ErrorMessage = "Необходимо указать идентификатор новой роли")]
        public int RoleId { get; set; }
    }

    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RoleId { get; set; }
        public string Name { get; set; }
    }

    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
    public class ChangeProjectStatusRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Status { get; set; }
    }
    public class ProjectComment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CommentId { get; set; }
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }

        // Навигационные свойства
        [ForeignKey("ProjectId")]
        public Project Project { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }

    public class ProjectSkill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectSkillId { get; set; }
        public int ProjectId { get; set; }
        public int SkillId { get; set; }

        // Навигационные свойства
        [ForeignKey("ProjectId")]
        public Project Project { get; set; }

        [ForeignKey("SkillId")]
        public Skill Skill { get; set; }
    }

    public class ProjectRoleUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectRoleId { get; set; }
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }

        // Навигационные свойства
        [ForeignKey("ProjectId")]
        public Project Project { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("RoleId")]
        public Role Role { get; set; }
    }

    public class Skill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SkillId { get; set; }
        public string Name { get; set; }

      
    }

    public class UserLoginModel
    {
        [Required(ErrorMessage = "Необходим Email")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Необходим Пароль")]
        public string Password { get; set; }
    }

}
