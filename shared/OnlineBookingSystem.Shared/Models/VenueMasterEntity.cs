using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBookingSystem.Shared.Models;

[Table("VenueMaster")]
public class VenueMasterEntity
{
	[Key]
	public int VenueID { get; set; }

	public int VenueTypeID { get; set; }

	[MaxLength(100)]
	public string VenueName { get; set; } = "";

	[MaxLength(10)]
	public string VenueCode { get; set; } = "";

	[MaxLength(255)]
	public string Address { get; set; } = "";

	[MaxLength(100)]
	public string City { get; set; } = "";

	[MaxLength(100)]
	public string Division { get; set; } = "Nagpur";

	[MaxLength(500)]
	public string? GoogleMapLink { get; set; }

	/// <summary>Optional physical columns (may exist on DB alongside <see cref="Facilities"/> JSON).</summary>
	public int? Capacity { get; set; }

	[Column("AreaSqMt")]
	public decimal? AreaSqMt { get; set; }

	public int? NoOfRooms { get; set; }

	public int? NoOfKitchen { get; set; }

	public int? NoOfToilet { get; set; }

	public int? NoOfBathroom { get; set; }

	public string? Facilities { get; set; }

	public bool IsActive { get; set; } = true;

	public DateTime CreatedAt { get; set; }
}
