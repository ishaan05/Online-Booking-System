using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineBookingSystem.Shared.Helpers;
using OnlineBookingSystem.Shared.Repositories;
using OnlineBookingSystem.Shared.Security;
using OnlineBookingSystem.Shared.ViewModels;

namespace OnlineBookingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController : ControllerBase
{
	[HttpGet]
	[AllowAnonymous]
	public async Task<ActionResult> GetActive([FromServices] IBookingSystemRepository repo, CancellationToken ct)
	{
		return Ok(await repo.GetActiveVenuesPublicAsync(ct));
	}

	[HttpGet("{id:int}")]
	[AllowAnonymous]
	public async Task<ActionResult> GetById(int id, [FromServices] IBookingSystemRepository repo, CancellationToken ct)
	{
		VenueDetailVm v = await repo.GetVenueDetailAsync(id, ct);
		return (v == (VenueDetailVm)null) ? ((ActionResult)NotFound()) : ((ActionResult)Ok(v));
	}

	public sealed class VenueActiveBody
	{
		public bool IsActive { get; set; }
	}

	[HttpPatch("admin/{id:int}/active")]
	[Authorize(Roles = AppRoles.SuperAdmin)]
	public async Task<ActionResult> SetVenueActive(int id, [FromBody] VenueActiveBody? body, [FromServices] IBookingSystemRepository repo, CancellationToken ct)
	{
		if (body == null)
		{
			return BadRequest(new { message = "Body is required." });
		}
		int? officeUserId = User.GetOfficeUserId();
		if (officeUserId == null)
		{
			return Unauthorized();
		}
		OfficePortalAccessVm? access = await repo.GetOfficePortalAccessAsync(officeUserId.Value, ct);
		if (access == null)
		{
			return Unauthorized();
		}
		try
		{
			await repo.SetVenueActiveAsync(access, id, body.IsActive, ct);
		}
		catch (UnauthorizedAccessException)
		{
			return Forbid();
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		return NoContent();
	}

	[HttpGet("admin/all")]
	[Authorize(Roles = AppRoles.OfficeStaff)]
	public async Task<ActionResult> GetAllAdmin([FromServices] IBookingSystemRepository repo, CancellationToken ct)
	{
		int? officeUserId = User.GetOfficeUserId();
		if (officeUserId == null)
		{
			return Unauthorized();
		}
		OfficePortalAccessVm? access = await repo.GetOfficePortalAccessAsync(officeUserId.Value, ct);
		if (access == null)
		{
			return Unauthorized();
		}
		return Ok(await repo.GetAllVenuesAdminAsync(access, ct));
	}

	[HttpPost]
	[Authorize(Roles = AppRoles.SuperAdmin)]
	public async Task<ActionResult> Upsert([FromBody] VenueMasterUpsertVm body, [FromServices] IBookingSystemRepository repo, CancellationToken ct)
	{
		try
		{
			return Ok(new
			{
				VenueID = await repo.UpsertVenueAsync(body, ct)
			});
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpDelete("{id:int}")]
	[Authorize(Roles = AppRoles.SuperAdmin)]
	public async Task<ActionResult> Delete(int id, [FromServices] IBookingSystemRepository repo, CancellationToken ct)
	{
		await repo.DeleteVenueAsync(id, ct);
		return NoContent();
	}

	[HttpGet("rate-charts")]
	[Authorize(Roles = AppRoles.OfficeStaff)]
	public async Task<ActionResult> RateCharts([FromServices] IBookingSystemRepository repo, CancellationToken ct)
	{
		int? officeUserId = User.GetOfficeUserId();
		if (officeUserId == null)
		{
			return Unauthorized();
		}
		OfficePortalAccessVm? access = await repo.GetOfficePortalAccessAsync(officeUserId.Value, ct);
		if (access == null)
		{
			return Unauthorized();
		}
		return Ok(await repo.GetRateChartsAsync(access, ct));
	}
}
