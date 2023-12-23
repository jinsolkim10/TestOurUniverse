using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOurUniverse.Services
{
    public interface IClientService
    {
        Task<ServiceResponse> RegisterUserAsync(UserInfo model);
        Task<ServiceResponse> LoginUserAsync(Login model);
    }
}
