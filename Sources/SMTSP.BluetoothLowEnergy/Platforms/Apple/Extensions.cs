namespace SMTSP.BluetoothLowEnergy;

public static class Extensions
{
    public static bool HasPlistValue(string key)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return NSBundle.MainBundle.ObjectForInfoDictionary(key) != null;
    }

    public static void EnsureAllowed()
    {
        var hasPeripheralUsage = HasPlistValue("NSBluetoothPeripheralUsageDescription");
        var hasAlwaysUsage = HasPlistValue("NSBluetoothAlwaysUsageDescription");

        if (!hasPeripheralUsage)
        {
            throw new UnauthorizedAccessException("\"NSBluetoothPeripheralUsageDescription\" is not set.");
        }

        if (!hasAlwaysUsage)
        {
            throw new UnauthorizedAccessException("\"NSBluetoothAlwaysUsageDescription\" is not set.");
        }
    }
}
