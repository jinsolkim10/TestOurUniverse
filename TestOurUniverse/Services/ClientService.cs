using Microsoft.Extensions.Configuration;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public Task<ServiceResponse> LoginUserAsync(Login model)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse> RegisterUserAsync(UserInfo model)
        {
            throw new NotImplementedException();
        }
    }
}
