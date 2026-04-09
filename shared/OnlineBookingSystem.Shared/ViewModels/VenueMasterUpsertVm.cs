namespace OnlineBookingSystem.Shared.ViewModels;

public record VenueMasterUpsertVm(
	int? VenueID,
	int VenueTypeID,
	string VenueName,
	string VenueCode,
	string Address,
	string City,
	string? Division,
	string? GoogleMapLink,
	string? Facilities,
	bool IsActive,
	int? Capacity = null,
	decimal? AreaSqMt = null,
	int? NoOfRooms = null,
	int? NoOfKitchen = null,
	int? NoOfToilet = null,
	int? NoOfBathroom = null);
