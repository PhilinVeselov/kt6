using System;
namespace web_new2.Pages
{
    public class ProjectModel
    {
        public int projectId { get; set; }
        public string projectName { get; set; }
        public string description { get; set; }
        public string status { get; set; }
        public DateTime startDate { get; set; }
        public DateTime? endDate { get; set; }
    }

    public class RoleModel
    {
        public int RoleId { get; set; }
        public string Name { get; set; }
    }

    public class RolesResponseModel
    {
        public string Status { get; set; }
        public List<RoleModel> Roles { get; set; }
    }

    public class ProjectResponseModel
    {
        public string Status { get; set; }
        public ProjectModel Project { get; set; }
    }
    public class ChangeProjectStatusRequest
    {
        public string Status { get; set; }
    }
    public class ProjectUsersResponseModel
    {
        public string Status { get; set; }
        public List<ProjectUserModel> Users { get; set; }
    }
    public class ProjectUserModel
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public int SelectedRole { get; set; } // Добавляем свойство SelectedRole

    }
    public class UpdateUserRoleRequest
    {
        public int RoleId { get; set; }
    }
    public class UserRole
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class AddUserToProjectRequest
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
    }

}

