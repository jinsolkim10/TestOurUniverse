using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestOurUniverse.Data;

namespace TestOurUniverse.Services
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IConfiguration configuration;

        public ClientService(ApplicationDbContext applicationDbContext, IConfiguration configuration)
        {
            this.applicationDbContext = applicationDbContext;
            this.configuration = configuration;
        }

        public async Task<ServiceResponse> RegisterUserAsync(UserInfo model)
        {
            var userRole = new UserRole();
            //admin이 이미 존재하는지 체크
            if (model.Email!.ToLower().StartsWith("admin"))
            {
                var chkIfAdminExist = await applicationDbContext.UserRoles.Where(_ => _.RoleName!.ToLower().Equals("admin")).FirstOrDefaultAsync();
                if (chkIfAdminExist != null) return new ServiceResponse() { Flag = false, Message = "Sorry Admin already exist, please change the email address" };

                userRole.RoleName = "Admin";
            }
            else if (model.Email!.ToLower().StartsWith("creator"))
            {
                var chkIfEmployeeExist = await applicationDbContext.UserRoles
                    .Where(_ => _.RoleName!.ToLower().Equals("creator"))
                    .FirstOrDefaultAsync();

                if (chkIfEmployeeExist != null)
                    return new ServiceResponse() { Flag = false, Message = "Sorry Creator already exists, please change the email address" };

                userRole.RoleName = "Creator";
            }

            var checkIfUserAlreadyCreated = await applicationDbContext.UserInfos.Where(_ => _.Email!.ToLower().Equals(model.Email!.ToLower())).FirstOrDefaultAsync();
            if (checkIfUserAlreadyCreated != null) return new ServiceResponse() { Flag = false, Message = $"Email: {model.Email} already registered" };


            var hashedPassword = HashPassword(model.Password!);
            var registeredEntity = applicationDbContext.UserInfos.Add(new UserInfo()
            {
                Name = model.Name!,
                Email = model.Email,
                Password = hashedPassword,
                Phone = model.Phone.ToString()!,
            }).Entity;
            await applicationDbContext.SaveChangesAsync();


            if (string.IsNullOrEmpty(userRole.RoleName))
                userRole.RoleName = "Customer";

            userRole.UserId = registeredEntity.Id;
            applicationDbContext.UserRoles.Add(userRole);
            await applicationDbContext.SaveChangesAsync();
            return new ServiceResponse() { Flag = true, Message = "Account Created" };
        }

        //패스워드 암호화
        private static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var randomGenerator = RandomNumberGenerator.Create())
            {
                randomGenerator.GetBytes(salt);
            }
            var rfcPassword = new Rfc2898DeriveBytes(password, salt, 1000, HashAlgorithmName.SHA1);
            byte[] rfcPasswordHash = rfcPassword.GetBytes(20);

            byte[] passwordHash = new byte[36];
            Array.Copy(salt, 0, passwordHash, 0, 16);
            Array.Copy(rfcPasswordHash, 0, passwordHash, 16, 20);
            return Convert.ToBase64String(passwordHash);
        }


        public async Task<ServiceResponse> LoginUserAsync(Login model)
        {
            var getUser = await applicationDbContext.UserInfos.Where(_ => _.Email!.Equals(model.Email)).FirstOrDefaultAsync();
            if (getUser == null) return new ServiceResponse() { Flag = false, Message = "User not found" };

            var checkIfPasswordMatch = VerifyUserPassword(model.Password!, getUser.Password!);
            if (checkIfPasswordMatch)
            {
                //롤 테이블에서 유저 정보 구하기
                var getUserRole = await applicationDbContext.UserRoles.Where(_ => _.Id == getUser.Id).FirstOrDefaultAsync();

                //역할과 이메일 주소를 사용자 이름으로 사용하여 토큰을 생성
                var token = GenerateToken(getUser.Name, model.Email!, getUserRole!.RoleName!);

                var checkIfTokenExist = await applicationDbContext.TokenInfos.Where(_ => _.UserId == getUser.Id).FirstOrDefaultAsync();
                if (checkIfTokenExist == null)
                {
                    applicationDbContext.TokenInfos.Add(new TokenInfo() { Token = token, UserId = getUser.Id });
                    await applicationDbContext.SaveChangesAsync();
                    return new ServiceResponse() { Flag = true, Token = token };
                }
                checkIfTokenExist.Token = token;
                await applicationDbContext.SaveChangesAsync();
                return new ServiceResponse() { Flag = true, Token = token };
            }
            return new ServiceResponse() { Flag = false, Message = "Invalid email or password" };
        }

        //사용자 데이터베이스 암호를 해독하고 사용자 원시 암호를 암호화한 후 비교
        private static bool VerifyUserPassword(string rawPassword, string databasePassword)
        {
            byte[] dbPasswordHash = Convert.FromBase64String(databasePassword);
            byte[] salt = new byte[16];
            Array.Copy(dbPasswordHash, 0, salt, 0, 16);
            var rfcPassword = new Rfc2898DeriveBytes(rawPassword, salt, 1000, HashAlgorithmName.SHA1);
            byte[] rfcPasswordHash = rfcPassword.GetBytes(20);
            for (int i = 0; i < rfcPasswordHash.Length; i++)
            {
                if (dbPasswordHash[i + 16] != rfcPasswordHash[i])
                    return false;
            }
            return true;
        }

        private string GenerateToken(string name, string email, string roleName)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, roleName)
            };
            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: userClaims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);

        }
    }
}
