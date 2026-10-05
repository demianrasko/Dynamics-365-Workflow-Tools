namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// A latitude and longitude returned by <see cref="Utility.GeocodeAddress"/>.
    /// </summary>
    public sealed class GeoLocation
    {
        public GeoLocation(decimal latitude, decimal longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public decimal Latitude { get; }

        public decimal Longitude { get; }
    }
}
