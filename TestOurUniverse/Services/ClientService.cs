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
        private readonly IConfiguration config;
        public ClientService(ApplicationDbContext applicationDbContext, IConfiguration config)
        {
            this.applicationDbContext = applicationDbContext;
            this.config = config;
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
            else if (model.Email!.ToLower().StartsWith("employee"))
            {
                var chkIfEmployeeExist = await applicationDbContext.UserRoles
                    .Where(_ => _.RoleName!.ToLower().Equals("employee"))
                    .FirstOrDefaultAsync();

                if (chkIfEmployeeExist != null)
                    return new ServiceResponse() { Flag = false, Message = "Sorry Employee already exists, please change the email address" };

                userRole.RoleName = "Employee";
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
                userRole.RoleName = "User";

            userRole.UserId = registeredEntity.Id;
            applicationDbContext.UserRoles.Add(userRole);
            await applicationDbContext.SaveChangesAsync();
            return new ServiceResponse() { Flag = true, Message = "Account Created" };
        }

        // Encrypt user password
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
                //get user role from the roles table
                var getUserRole = await applicationDbContext.UserRoles.Where(_ => _.Id == getUser.Id).FirstOrDefaultAsync();

                //Generate token with the role, and username as email
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

        //Decrypt user database password and encrypt user raw password and compare
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
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, roleName)
            };
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: userClaims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);

        }
    }
}
