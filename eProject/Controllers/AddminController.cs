using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddminController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;
        
    }
}
