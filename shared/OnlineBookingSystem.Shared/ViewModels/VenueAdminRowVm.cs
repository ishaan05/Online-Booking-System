using System;

namespace OnlineBookingSystem.Shared.ViewModels;

public record VenueAdminRowVm(
	int VenueID,
	int VenueTypeID,
	string VenueName,
	string VenueCode,
	string Address,
	string City,
	string Division,
	string? GoogleMapLink,
	string? Facilities,
	bool IsActive,
	DateTime CreatedAt,
	int? Capacity,
	decimal? AreaSqMt,
	int? NoOfRooms,
	int? NoOfKitchen,
	int? NoOfToilet,
	int? NoOfBathroom);
