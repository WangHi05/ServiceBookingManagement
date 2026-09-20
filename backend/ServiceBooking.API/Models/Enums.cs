namespace ServiceBooking.API.Models;

public enum UserRole
{
    Customer = 0,
    Admin = 1
}

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}
