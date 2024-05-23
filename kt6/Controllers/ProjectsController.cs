using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StudentManagement.API.Data;
using StudentManagement.API.Models;
using StudentManagement.API.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Security.Claims;
using System.Reflection.Metadata;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.IO;
using NSec.Cryptography;


namespace StudentManagement.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }
        //регистрация
        [HttpPost("register")]
        public async Task<IActionResult> Register(User model)
        {
            // Валидация данных
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверка на существование пользователя с такой же почтой
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                return Conflict(new { message = "Пользователь с такой почтой уже существует" });
            }

            // Хеширование пароля
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Установка значения "Пользователь" по умолчанию для статуса, если значение не предоставлено
            if (string.IsNullOrEmpty(model.Status))
            {
                model.Status = "Пользователь";
            }

            // Создание нового пользователя
            var newUser = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = hashedPassword,
                Status = model.Status // Использование значения из запроса или значения по умолчанию
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Регистрация успешна" });
        }




        //авторизация
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginModel model)
        {
            // Валидация данных
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Найти пользователя по email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            // Проверка пароля
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            // Определение роли пользователя
            string role = user.Status == "Админ" ? "Админ" : "Пользователь";

            // Генерация JWT токена с учетом роли
            var token = _jwtService.GenerateJwtToken(user.UserId, role);

            // Возвращаем токен клиенту
            return Ok(new { access_token = token });
        }

        //просомтр пользователей
        [HttpGet("users")]
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var isAdmin = HttpContext.User.IsInRole("Админ");

                // Проверяем, является ли пользователь администратором
                if (!isAdmin)
                {
                    // Если пользователь не администратор, возвращаем ошибку доступа
                    return StatusCode(403, new { status = "error", message = "Доступ запрещен" });
                }

                // Если пользователь администратор, получаем всех пользователей
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.UserId,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.Status
                    })
                    .ToListAsync();

                return Ok(new { status = "success", users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        //удаление пользователя
        [HttpDelete("users/{userId}")]
[Authorize(Roles = "Админ")] // Пользователь должен быть аутентифицирован и иметь роль "Админ"
public async Task<IActionResult> DeleteUser(int userId)
{
    try
    {
        // Поиск пользователя по идентификатору
        var user = await _context.Users.FindAsync(userId);

        // Проверка, найден ли пользователь
        if (user == null)
        {
            return NotFound(new { status = "error", message = "Пользователь не найден" });
        }

        // Удаление пользователя из контекста данных
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new { status = "success", message = "Пользователь успешно удален" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { status = "error", message = ex.Message });
    }
}

        //просомр профиля
        [HttpGet("my_profile")]
        [Authorize]
        public async Task<IActionResult> MyProfile()
        {
            try
            {
                // Получение идентификатора пользователя из токена
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null)
                {
                    return BadRequest(new { status = "error", message = "Идентификатор пользователя не найден в токене" });
                }

                var currentUserId = int.Parse(currentUserIdClaim.Value);

                // Получение данных о пользователе из базы данных
                var user = await _context.Users
                    .Where(u => u.UserId == currentUserId)
                    .Select(u => new
                    {
                        u.UserId,
                        u.Status,
                        u.FirstName,
                        u.LastName,
                        u.Email
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { status = "error", message = "Пользователь не найден" });
                }

                return Ok(new { status = "success", user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        [HttpGet("project/{projectId}")]
        [Authorize]
        public async Task<IActionResult> GetProject(int projectId)
        {
            try
            {
                // Проверка, что пользователь - админ (юзер) или администратор проекта (роль)
                var isAdmin = HttpContext.User.IsInRole("Админ");
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (!isAdmin)
                {
                    if (currentUserIdClaim == null)
                    {
                        return BadRequest(new { status = "error", message = "Идентификатор пользователя не найден в токене" });
                    }

                    var currentUserId = int.Parse(currentUserIdClaim.Value);

                    var projectRoleUser = await _context.ProjectRoleUsers
                        .FirstOrDefaultAsync(pru => pru.ProjectId == projectId && pru.UserId == currentUserId && pru.Role.Name == "Администратор");

                    if (projectRoleUser == null)
                    {
                        return StatusCode(403, new { status = "error", message = "Доступ запрещен" });
                    }
                }

                // Загрузка данных проекта из базы данных или другого источника
                var project = await _context.Projects.FindAsync(projectId);

                if (project == null)
                {
                    return NotFound(new { status = "error", message = "Проект не найден" });
                }

                // Возврат данных проекта
                return Ok(new { status = "success", project });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        //редактирование проекта
        [HttpPut("edit_project/{project_id}")]
        [Authorize]
        public async Task<IActionResult> EditProject(int project_id, Project project)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null)
                {
                    return BadRequest(new { status = "error", message = "Идентификатор пользователя не найден в токене" });
                }

                var currentUserId = int.Parse(currentUserIdClaim.Value);

                // Проверка, что пользователь - админ (юзер) или администратор проекта (роль)
                var isAdmin = HttpContext.User.IsInRole("Админ");
                if (!isAdmin)
                {
                    var projectRoleUser = await _context.ProjectRoleUsers
                        .FirstOrDefaultAsync(pru => pru.ProjectId == project_id && pru.UserId == currentUserId && pru.Role.Name == "Администратор");
                    if (projectRoleUser == null)
                    {
                        return StatusCode(403, new { status = "error", message = "Доступ запрещен" });
                    }
                }

                // Обновление данных проекта
                var existingProject = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == project_id);
                if (existingProject == null)
                {
                    return NotFound(new { status = "error", message = "Проект не найден" });
                }

                existingProject.ProjectName = project.ProjectName;
                existingProject.Description = project.Description;
                existingProject.Status = project.Status;

                _context.Projects.Update(existingProject);
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Проект обновлен" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        //редактирование профиля
        // Изменение типа возвращаемого значения на IActionResult
        [HttpPut("edit_profile")]
        [Authorize]
        public async Task<IActionResult> EditProfile(User user)
        {
            try
            {
                // Получение идентификатора пользователя из токена
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null)
                {
                    return BadRequest(new { status = "error", message = "Идентификатор пользователя не найден в токене" });
                }

                var currentUserId = int.Parse(currentUserIdClaim.Value);

                // Проверка наличия пользователя
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentUserId);
                if (existingUser == null)
                {
                    return NotFound(new { status = "error", message = "Пользователь не найден" });
                }

                // Обновление данных профиля
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Email = user.Email;
                existingUser.Password = user.Password; // Обновление пароля может потребовать дополнительной обработки

                // Сохранение изменений
                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                // Возвращение обновленных данных профиля
                return Ok(existingUser); // Или создайте новый объект UserProfileModel на основе данных пользователя

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }




        //просомтр проекта
        [HttpGet("projects")]
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> ViewProjects()
        {
            try
            {
                var currentUserStatus = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                List<Project> projects;
                if (currentUserStatus == "Админ")
                {
                    // Если текущий пользователь - администратор, получаем все проекты
                    projects = await _context.Projects.ToListAsync();
                }
                else
                {
                    // Если текущий пользователь - не администратор, получаем только проекты, в которых он участвует
                    projects = await _context.Projects
                        .Where(p => _context.ProjectRoleUsers.Any(pru => pru.UserId == currentUserId && pru.ProjectId == p.ProjectId))
                        .ToListAsync();
                }

                return Ok(new { status = "success", projects });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        //создание роли
        [HttpPost("create_role")]
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> CreateRole(Role model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Name))
                {
                    return BadRequest(new { status = "error", message = "Необходимо указать название роли" });
                }

                // Создание роли
                var newRole = new Role
                {
                    Name = model.Name
                };

                _context.Roles.Add(newRole);
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Роль создана", role_id = newRole.RoleId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = "Ошибка базы данных: " + ex.Message });
            }
        }
        //просмотр ролей
        [HttpGet("roles")]
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                // Получение текущего пользователя из токена
                var currentUserStatus = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                // Проверка на администратора


                // Запрос на получение всех ролей
                var roles = await _context.Roles
                    .Select(r => new { r.RoleId, r.Name })
                    .ToListAsync();

                return Ok(new { status = "success", roles });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        //удаление ролей
        [HttpDelete("roles/{roleId}")]
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            try
            {
                // Получение текущего пользователя из токена
                var currentUserStatus = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                // Проверка на администратора
                if (currentUserStatus != "Админ")
                {
                    return StatusCode(403, new { status = "error", message = "Недостаточно прав доступа" });
                }

                // Проверка на существование роли
                var existingRole = await _context.Roles.FindAsync(roleId);
                if (existingRole == null)
                {
                    return NotFound(new { status = "error", message = "Роль не найдена" });
                }

                // Удаление роли
                _context.Roles.Remove(existingRole);
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Роль удалена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        //смена статуса проекта
        [HttpPut("change_project_status/{projectId}")]
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> ChangeProjectStatus(int projectId, ChangeProjectStatusRequest request)
        {
            try
            {
                // Получение текущего пользователя из токена
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var currentUserStatus = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                // Получение текущего статуса проекта
                var existingProject = await _context.Projects.FindAsync(projectId);
                if (existingProject == null)
                {
                    return NotFound(new { status = "error", message = "Проект не найден" });
                }
                var currentStatus = existingProject.Status;

                // Проверка на допустимые переходы статуса
                var validStatusTransitions = new Dictionary<string, List<string>>
                {
                    {"На рассмотрении", new List<string>{"Одобрен"}},
                    {"Одобрен", new List<string>{"В процессе выполнения"}},
                    {"В процессе выполнения", new List<string>{"Отправлен на ревью"}},
                    {"Отправлен на ревью", new List<string>{"Ревью пройдено"}},
                    {"Ревью пройдено", new List<string>{"Выполнен"}}
                };
                if (!validStatusTransitions.ContainsKey(currentStatus) || !validStatusTransitions[currentStatus].Contains(request.Status))
                {
                    return BadRequest(new { status = "error", message = "Недопустимый переход статуса" });
                }

                // Ограничение на смену статуса "Одобрен" и "Ревью пройдено" только для Админов (юзеров)
                if ((currentStatus == "На рассмотрении" || currentStatus == "Отправлен на ревью") &&
                    (request.Status == "Одобрен" || request.Status == "Ревью пройдено") &&
                    currentUserStatus != "Админ")
                {
                    return StatusCode(403, new { status = "error", message = "Недостаточно прав для изменения статуса" });
                }

                // Обновление статуса проекта
                existingProject.Status = request.Status;
                _context.Projects.Update(existingProject);
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Статус проекта обновлен" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        //удаление пользователя из проекта
        [HttpDelete("remove_user_from_project/{projectId}/{userId}")]
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> RemoveUserFromProject(int projectId, int userId)
        {
            try
            {
                // Проверяем, существует ли связь пользователя с проектом
                var existingUserRole = await _context.ProjectRoleUsers.FirstOrDefaultAsync(pru => pru.ProjectId == projectId && pru.UserId == userId);
                if (existingUserRole == null)
                {
                    return NotFound(new { status = "error", message = "Связь пользователя с проектом не найдена" });
                }

                // Удаляем связь пользователя с проектом
                _context.ProjectRoleUsers.Remove(existingUserRole);
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Пользователь успешно удален из проекта" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        //редактирвоание ролли польщзователя в проекте
        [HttpPut("update_user_role/{projectId}/{userId}")] // Обновление роли пользователя в проекте
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> UpdateUserRole(int projectId, int userId, UpdateUserRoleRequest request)
        {
            try
            {
                // Проверяем, существует ли пользователь с указанным ID
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { status = "error", message = "Пользователь не найден" });
                }

                // Проверяем, существует ли связь пользователя с проектом
                var existingUserRole = await _context.ProjectRoleUsers.FirstOrDefaultAsync(pru => pru.UserId == userId && pru.ProjectId == projectId);
                if (existingUserRole == null)
                {
                    return NotFound(new { status = "error", message = "Связь пользователя с проектом не найдена" });
                }

                // Проверяем, существует ли роль с указанным ID
                var role = await _context.Roles.FindAsync(request.RoleId);
                if (role == null)
                {
                    return NotFound(new { status = "error", message = "Роль не найдена" });
                }

                // Обновляем роль пользователя в проекте
                existingUserRole.RoleId = request.RoleId;
                _context.ProjectRoleUsers.Update(existingUserRole);
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Роль пользователя успешно обновлена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        //редактирование роли
        [HttpPut("roles/{roleId}")]
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> UpdateRole(int roleId, Role model)
        {
            try
            {
                // Получение текущего пользователя из токена
                var currentUserStatus = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                // Проверка на администратора
                if (currentUserStatus != "Админ")
                {
                    return StatusCode(403, new { status = "error", message = "Недостаточно прав доступа" });
                }

                // Проверка на существование роли
                var existingRole = await _context.Roles.FindAsync(roleId);
                if (existingRole == null)
                {
                    return NotFound(new { status = "error", message = "Роль не найдена" });
                }

                // Обновление роли
                existingRole.Name = model.Name;
                _context.Roles.Update(existingRole);
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Роль обновлена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        //просомтр пользователей в проекте
        [HttpGet("project_users/{project_id}")]
        [Authorize]
        public async Task<IActionResult> GetProjectUsers(int project_id)
        {
            try
            {
                // Получение текущего пользователя из токена
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null)
                {
                    return BadRequest(new { status = "error", message = "Идентификатор пользователя не найден в токене" });
                }

                var currentUserId = int.Parse(currentUserIdClaim.Value);

                // Проверка роли текущего пользователя в проекте
                var isAdmin = HttpContext.User.IsInRole("Админ");
                if (!isAdmin)
                {
                    var isProjectAdmin = await _context.ProjectRoleUsers
                        .AnyAsync(pru => pru.ProjectId == project_id && pru.UserId == currentUserId && pru.Role.Name == "Администратор");
                    if (!isProjectAdmin)
                    {
                        return StatusCode(403, new { status = "error", message = "Доступ запрещен" });
                    }
                }

                // Получение пользователей проекта с их ролями
                var projectUsers = await _context.ProjectRoleUsers
                    .Where(pru => pru.ProjectId == project_id)
                    .Include(pru => pru.User)
                    .Include(pru => pru.Role)
                    .Select(pru => new
                    {
                        pru.UserId,
                        pru.User.FirstName,
                        pru.User.LastName,
                        Role = pru.Role.Name
                    })
                    .ToListAsync();

                return Ok(new { status = "success", users = projectUsers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        //добавление пользователя в проект
        [HttpPost("add_user_to_project")]
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> AddUserToProject(AddUserToProjectRequest request)
        {
            try
            {
                // Проверяем, существует ли пользователь с указанным Email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    return NotFound(new { status = "error", message = "Пользователь не найден" });
                }

                // Проверяем, существует ли уже связь пользователя с проектом
                var existingUserRole = await _context.ProjectRoleUsers
                    .FirstOrDefaultAsync(pru => pru.ProjectId == request.ProjectId && pru.UserId == user.UserId);
                if (existingUserRole != null)
                {
                    return Conflict(new { status = "error", message = "Пользователь уже участвует в проекте" });
                }

                // Добавляем пользователя в проект
                var newUserRole = new ProjectRoleUser
                {
                    ProjectId = request.ProjectId,
                    UserId = user.UserId,
                    RoleId = request.RoleId
                };
                _context.ProjectRoleUsers.Add(newUserRole);
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Пользователь успешно добавлен в проект" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        //удаление проекта
        [HttpDelete("delete_project/{project_id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProject(int project_id)
        {
            try
            {
                // Найти проект для удаления
                var project = await _context.Projects.FindAsync(project_id);
                if (project == null)
                {
                    return NotFound(new { status = "error", message = "Проект не найден" });
                }

                // Удалить комментарии, связанные с проектом
                var projectComments = _context.ProjectComments.Where(pc => pc.ProjectId == project_id);
                _context.ProjectComments.RemoveRange(projectComments);

                // Удалить записи ролей пользователей, связанные с проектом
                var projectRoleUsers = _context.ProjectRoleUsers.Where(pru => pru.ProjectId == project_id);
                _context.ProjectRoleUsers.RemoveRange(projectRoleUsers);

                // Удалить проект
                _context.Projects.Remove(project);

                // Сохранить изменения
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Проект удален" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        //скачивание портфолио
        [HttpGet("download_portfolio/{userId}")]
        [Authorize] // Пользователь должен быть аутентифицирован
        public async Task<IActionResult> DownloadPortfolio(int userId)
        {
            try
            {
                // Получаем текущего пользователя
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null)
                {
                    return BadRequest(new { status = "error", message = "Идентификатор пользователя не найден в токене" });
                }

                var currentUserId = int.Parse(currentUserIdClaim.Value);

                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentUserId);

                // Проверяем, имеет ли текущий пользователь права администратора
                if (currentUser.Status == "Админ" || currentUser.UserId == userId)
                {
                    // Проверяем, существует ли пользователь с указанным ID
                    var user = await _context.Users.FindAsync(userId);
                    if (user == null)
                    {
                        return NotFound(new { status = "error", message = "Пользователь не найден" });
                    }

                    // Создаем PDF документ
                    PdfDocument pdf = new PdfDocument();
                    pdf.Info.Title = "Портфолио";
                    PdfPage page = pdf.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    // Устанавливаем путь к шрифту Montserrat
                    string fontPath = "/Users/phulaveselov/Desktop/учеба во вгуэсе/4семестр/ado/kt6/kt6/kt6/fonts/Montserrat-Italic-VariableFont_wght.ttf";
                    XFont font = new XFont(fontPath, 12); // Указываем путь к файлу шрифта

                    // Начинаем добавлять информацию о пользователе
                    int yPos = 40;
                    gfx.DrawString("Информация о пользователе:", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                    yPos += 20;
                    gfx.DrawString($"Имя: {user.FirstName}", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                    yPos += 20;
                    gfx.DrawString($"Фамилия: {user.LastName}", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                    yPos += 20;
                    gfx.DrawString($"Email: {user.Email}", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                    yPos += 30;

                    // Получаем список проектов пользователя
                    var userProjects = await _context.ProjectRoleUsers
                        .Include(pru => pru.Project)
                        .Include(pru => pru.Role)
                        .Where(pru => pru.UserId == userId)
                        .Select(pru => new
                        {
                            ProjectName = pru.Project.ProjectName,
                            Description = pru.Project.Description,
                            Status = pru.Project.Status,
                            StartDate = pru.Project.StartDate,
                            EndDate = pru.Project.EndDate,
                            Role = pru.Role.Name // Добавляем информацию о роли пользователя в проекте
                        })
                        .ToListAsync();

                    // Добавляем информацию о проектах пользователя
                    gfx.DrawString("Информация о проектах:", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                    yPos += 20;
                    foreach (var project in userProjects)
                    {
                        gfx.DrawString($"Название проекта: {project.ProjectName}", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                        yPos += 20;
                        gfx.DrawString($"Описание проекта: {project.Description}", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                        yPos += 20;
                        gfx.DrawString($"Статус проекта: {project.Status}", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                        yPos += 20;
                        gfx.DrawString($"Дата начала: {project.StartDate}", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                        yPos += 20;
                        gfx.DrawString($"Дата окончания: {project.EndDate}", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                        yPos += 20;
                        gfx.DrawString($"Роль в проекте: {project.Role}", font, XBrushes.Black, new XRect(40, yPos, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                        yPos += 30;
                    }

                    // Сохраняем PDF в поток памяти
                    MemoryStream stream = new MemoryStream();
                    pdf.Save(stream, false);
                    stream.Seek(0, SeekOrigin.Begin);

                    // Отправляем файл пользователю для скачивания
                    return File(stream, "application/pdf", "portfolio.pdf");
                }
                else
                {
                    return Unauthorized(new { status = "error", message = "У вас нет прав доступа для скачивания этого портфолио" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
        //создание проекта
        [HttpPost("create_project")]
        [Authorize]
        public async Task<IActionResult> CreateProject(Project project)
        {
            try
            {
                if (string.IsNullOrEmpty(project.ProjectName) || string.IsNullOrEmpty(project.Description))
                {
                    return BadRequest(new { status = "error", message = "Необходимы все поля: название и описание" });
                }
                // Создание проекта
                var newProject = new Project
                {
                    ProjectName = project.ProjectName,
                    Description = project.Description,
                    Status = "На рассмотрении",
                    StartDate = DateTime.UtcNow // Устанавливаем текущую дату и время как дату создания проекта
                };
                // Добавление проекта в контекст базы данных
                _context.Projects.Add(newProject);
                await _context.SaveChangesAsync();
                // Назначение создателя проекта администратором
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Администратор"); // Предполагается, что есть роль "Администратор"
                var newProjectRoleUser = new ProjectRoleUser
                {
                    ProjectId = newProject.ProjectId,
                    UserId = currentUserId,
                    RoleId = adminRole?.RoleId ?? 0 // Предполагается, что ID роли администратора - 1
                };
                _context.ProjectRoleUsers.Add(newProjectRoleUser);
                await _context.SaveChangesAsync();

                return Ok(new { status = "success", message = "Проект создан", project_id = newProject.ProjectId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = "Ошибка базы данных: " + ex.Message });
            }
        }
    }
}
