using System.Threading.Tasks;
using Microsoft.Identity.Web.Resource;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppOwnsDataWebApi.Models;
using AppOwnsDataWebApi.Services;

namespace AppOwnsDataWebApi.Controllers {

  [ApiController]
  [Route("api/[controller]")]
  [Authorize]
  [RequiredScope("Reports.Embed")]
  [EnableCors("AllowOrigin")]
  public class EmbedController : ControllerBase {

    private PowerBiServiceApi powerBiServiceApi;

    public EmbedController(PowerBiServiceApi powerBiServiceApi) {
      this.powerBiServiceApi = powerBiServiceApi;
    }

    [HttpGet]
    public async Task<IActionResult> Get() {
      var usernameClaim = this.User.FindFirst("preferred_username");
      if (usernameClaim == null) return Unauthorized();
      var result = await this.powerBiServiceApi.GetEmbeddedViewModel(usernameClaim.Value);
      return Ok(result);
    }

  }

}
