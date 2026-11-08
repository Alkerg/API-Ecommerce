using APIEcommerce.Data;
using APIEcommerce.Models;
using APIEcommerce.Models.Dtos;
using APIEcommerce.Repository.IRepository;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace APIEcommerce.Repository
{
    public class UserRepository: IUserRepository
    {
        public readonly ApplicationDBContext _db;
        private string? secretKey;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;

        public UserRepository(ApplicationDBContext db, IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IMapper mapper)
        {
            _db = db;
            secretKey = configuration.GetValue<string>("ApiSettings:SecretKey");
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
        }

        public ApplicationUser? GetUser(string id)
        {
            return _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
        }

        public ICollection<ApplicationUser> GetUsers()
        {
            return _db.ApplicationUsers.OrderBy(u => u.UserName).ToList();
        }

        public bool IsUniqueUser(string username)
        {
            return !_db.Users.Any(u => u.Username.ToLower().Trim() == username.ToLower().Trim());
        }

        public async Task<UserLoginResponseDto> Login(UserLoginDto userLoginDto)
        {
            if (string.IsNullOrEmpty(userLoginDto.Username))
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "El campo username es requerido"
                };
            }

            var user = await _db.ApplicationUsers.FirstOrDefaultAsync<ApplicationUser>(u => u.UserName != null && u.UserName.ToLower().Trim() == userLoginDto.Username.ToLower().Trim());

            if(user == null)
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "Usuario no encontrado"
                };
            }

            if (userLoginDto.Password == null)
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "Password requerido"
                };
            }

            bool isValid = await _userManager.CheckPasswordAsync(user, userLoginDto.Password);

            if(!isValid)
            {
                return new UserLoginResponseDto()
                {
                    Token = "",
                    User = null,
                    Message = "Credenciales incorrectas"
                };
            }

            // JWT
            var handlerToken = new JwtSecurityTokenHandler();
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("SecretKey no esta configurada");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var key = Encoding.UTF8.GetBytes(secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]{
                    new Claim("id", user.Id.ToString()),
                    new Claim("username", user.UserName ?? string.Empty),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault() ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handlerToken.CreateToken(tokenDescriptor);

            return new UserLoginResponseDto()
            {
                Token = handlerToken.WriteToken(token),
                User = _mapper.Map<UserDataDto>(user),
                Message = "Usuario logueado correctamente"
            };


        }

        public async Task<UserDataDto> Register(CreateUserDto createUserDto)
        {
            /*var encryptedPassword = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);
            var user = new User()
            {
                Username = createUserDto.Username ?? "No username",
                Name = createUserDto.Name,
                Role = createUserDto.Role,
                Password = encryptedPassword
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            
            return user;*/

            if (string.IsNullOrEmpty(createUserDto.Username))
            {
                throw new ArgumentNullException("El username es requerido");
            }



            if (createUserDto.Password == null)
            {
                throw new ArgumentNullException("El password es requerido");
            }

            var user = new ApplicationUser()
            {
                UserName = createUserDto.Username,
                Email = createUserDto.Username,
                NormalizedEmail = createUserDto.Username.ToUpper(),
                Name = createUserDto.Name
            };

            var result = await _userManager.CreateAsync(user, createUserDto.Password);

            if (result.Succeeded)
            {
                var userRole = createUserDto.Role ?? "User";
                var roleExists = await _roleManager.RoleExistsAsync(userRole);
                if (!roleExists)
                {
                    var identityRole = new IdentityRole(userRole);
                    await _roleManager.CreateAsync(identityRole);
                }

                await _userManager.AddToRoleAsync(user, userRole);
                var createdUser = _db.ApplicationUsers.FirstOrDefault(u => u.UserName == createUserDto.Username);

                return _mapper.Map<UserDataDto>(createdUser);
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ApplicationException($"No se pudo realizar el registro: {errors}");

        }
    }
}
