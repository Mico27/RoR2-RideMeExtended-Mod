using BepInEx.Configuration;
using UnityEngine;

namespace RideMeExtended
{
    public static class RideMeExtendedConfig
    {
        public static bool disableRiderHurtBoxEnabled;
        public static bool jumpRiderOnExitEnabled;
        public static float jumpRiderOnExitVelocity;
        public static Vector3 defaultSeatPositionOffset;
        public static bool canRideOtherTeams;


        public static void ReadConfig()
        {
            canRideOtherTeams = GetSetConfig("Riders", "Can ride the other teams", false, "Allows characters to ride other characters regardless of team").Value;
            disableRiderHurtBoxEnabled = GetSetConfig("Riders", "Disable hurtboxes upon riding", false, "Disable hurtboxes upon riding").Value;
            jumpRiderOnExitEnabled = GetSetConfig("Riders", "Enable jump on seat exit", true, "Enable jump on seat exit").Value;
            jumpRiderOnExitVelocity = GetSetConfig("Riders", "Jump velocity on seat exit", 20f, "Jump velocity on seat exit").Value;
            defaultSeatPositionOffset = GetSetConfig("Rideables", "Default seat position offset", new Vector3(0f, 3f, 0f), "Default seat position offset").Value;
        }

        // this helper automatically makes config entries for disabling survivors
        internal static ConfigEntry<T> GetSetConfig<T>(string section, string key, T defaultValue, string description)
        {
            return RideMeExtended.Instance.Config.Bind<T>(new ConfigDefinition(section, key), defaultValue, new ConfigDescription(description));
        }
    }
}