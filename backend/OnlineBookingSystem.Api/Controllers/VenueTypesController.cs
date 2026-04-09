using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineBookingSystem.Shared.Repositories;
using OnlineBookingSystem.Shared.Security;
using OnlineBookingSystem.Shared.ViewModels;

namespace OnlineBookingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenueTypesController : ControllerBase
{
	[HttpGet]
	[AllowAnonymous]
	public async Task<ActionResult> GetActive([FromServices] IBookingSystemRepository repo, CancellationToken ct)
	{
		return Ok(await repo.GetVenueTypesActiveAsync(ct));
	}

	[HttpGet("admin/all")]
	[Authorize(Roles = AppRoles.SuperAdmin)]
	public async Task<ActionResult> GetAllAdmin([FromServices] IBookingSystemRepository repo, CancellationToken ct)
	{
		return Ok(await repo.GetAllVenueTypesAdminAsync(ct));
	}

	[HttpPost("admin")]
	[Authorize(Roles = AppRoles.SuperAdmin)]
	public async Task<ActionResult> UpsertAdmin([FromBody] VenueTypeUpsertVm body, [FromServices] IBookingSystemRepository repo, CancellationToken ct)
	{
		try
		{
			return Ok(new { venueTypeID = await repo.UpsertVenueTypeAsync(body, ct) });
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}
}
