using BepInEx.Configuration;
using UnityEngine;

namespace RideMeExtended
{
    public static class RideMeExtendedConfig
    {
        public static ConfigEntry<bool> disableRiderHurtBoxEnabled;
        public static ConfigEntry<bool> jumpRiderOnExitEnabled;
        public static ConfigEntry<float> jumpRiderOnExitVelocity;
        public static ConfigEntry<Vector3> defaultSeatPositionOffset;
        


        public static void ReadConfig()
        {
            disableRiderHurtBoxEnabled = GetSetConfig("Riders", "Disable hurtboxes upon riding", false, "Disable hurtboxes upon riding");
            jumpRiderOnExitEnabled = GetSetConfig("Riders", "Enable jump on seat exit", true, "Enable jump on seat exit");
            jumpRiderOnExitVelocity = GetSetConfig("Riders", "Jump velocity on seat exit", 20f, "Jump velocity on seat exit");
            defaultSeatPositionOffset = GetSetConfig("Rideables", "Default seat position offset", new Vector3(0f, 3f, 0f), "Default seat position offset");
        }

        // this helper automatically makes config entries for disabling survivors
        internal static ConfigEntry<T> GetSetConfig<T>(string section, string key, T defaultValue, string description)
        {
            return RideMeExtended.Instance.Config.Bind<T>(new ConfigDefinition(section, key), defaultValue, new ConfigDescription(description));
        }
    }
}