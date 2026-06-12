using AutoWashPro.DAL.Entities;
using DAL.Entities;

namespace BLL.Helpers
{
    /// <summary>
    /// Derives estimated wash duration purely from existing DB fields.
    /// No new columns needed — uses VehicleType.BaseWeight and ServicePrice.CapacityWeight.
    /// </summary>
    public static class WashTimeEstimator
    {
        private const int InterVehicleBufferMinutes = 2;

        /// <summary>
        /// Estimates wash time in minutes for one vehicle + its selected services.
        /// </summary>
        public static int EstimateMinutes(int baseWeight, IEnumerable<ServicePrice> servicePrices)
        {
            int baseMinutes = baseWeight switch
            {
                <= 1 => 10,  // microcar
                <= 2 => 14,  // sedan / hatchback
                <= 3 => 18,  // SUV / MPV
                _ => 24   // truck / large vehicle
            };

            // Take MAX service capacity weight as the dominant complexity signal.
            // Mirrors the same take-max logic used in slot availability checks.
            int maxServiceWeight = servicePrices
                .Select(sp => sp.CapacityWeight)
                .DefaultIfEmpty(0)
                .Max();

            int serviceAddon = maxServiceWeight switch
            {
                <= 1 => 0,   // basic wash — no extra time
                <= 2 => 5,   // standard service
                <= 3 => 10,  // full service
                _ => 15   // premium / detail
            };

            return baseMinutes + serviceAddon;
        }

        public static int GetInterVehicleBuffer() => InterVehicleBufferMinutes;
    }
}