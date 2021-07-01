using BepInEx;
using BepInEx.Logging;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace RideMeExtended
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    [R2APISubmoduleDependency(nameof(LanguageAPI))]
    public sealed class RideMeExtended : BaseUnityPlugin
    {
        public const string
            MODNAME = "RideMeExtended",
            MODAUTHOR = "Mico27",
            MODUID = "com." + MODAUTHOR + "." + MODNAME,
            MODVERSION = "1.0.0";
        // a prefix for name tokens to prevent conflicts
        public const string developerPrefix = MODAUTHOR;
        public void Awake()
        {
            Instance = this;
            try
            {
                RideMeExtendedConfig.ReadConfig();
                LanguageAPI.Add("RIDE_INTERACTION", "Ride me");
                LanguageAPI.Add("RIDE_CONTEXT", "Ride me");
                On.RoR2.BodyCatalog.Init += BodyCatalog_Init;
                On.RoR2.GlobalEventManager.OnInteractionBegin += GlobalEventManager_OnInteractionBegin;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message + " - " + e.StackTrace);
            }
        }

        private void BodyCatalog_Init(On.RoR2.BodyCatalog.orig_Init orig)
        {
            orig.Invoke();
            foreach (GameObject gameObject in Reflection.GetFieldValue<GameObject[]>(typeof(BodyCatalog), "bodyPrefabs"))
            {
                if (!RiderBodyBlacklist.Contains(gameObject.name))
                {
                    if (!gameObject.GetComponent<RiderController>())
                    {
                        gameObject.AddComponent<RiderController>();
                    }
                }
                if (!RideableBodyBlacklist.Contains(gameObject.name))
                {
                    if (!gameObject.GetComponent<RideableController>())
                    {
                        gameObject.AddComponent<RideableController>();
                    }
                    Highlight highlight = gameObject.GetComponent<Highlight>();
                    if (!highlight)
                    {
                        highlight = gameObject.AddComponent<Highlight>();
                    }
                    highlight.highlightColor = Highlight.HighlightColor.interactive;
                    highlight.isOn = false;
                    highlight.strength = 1f;
                    var modelLocator = gameObject.GetComponent<ModelLocator>();
                    if (modelLocator && modelLocator.modelTransform && modelLocator.modelTransform.gameObject)
                    {
                        var characterModel = modelLocator.modelTransform.gameObject.GetComponent<CharacterModel>();
                        if (characterModel)
                        {
                            highlight.targetRenderer = characterModel.mainSkinnedMeshRenderer;
                        }
                    }
                    if (!highlight.targetRenderer)
                    {
                        highlight.targetRenderer = gameObject.GetComponentInChildren<Renderer>();
                    }                    
                    GenericDisplayNameProvider genericDisplayNameProvider = gameObject.GetComponent<GenericDisplayNameProvider>();
                    if (!genericDisplayNameProvider)
                    {
                        genericDisplayNameProvider = gameObject.AddComponent<GenericDisplayNameProvider>();
                    }
                    genericDisplayNameProvider.displayToken = "RIDE_INTERACTION";
                    EntityLocator entityLocator = gameObject.GetComponent<EntityLocator>();
                    if (!entityLocator)
                    {
                        entityLocator = gameObject.AddComponent<EntityLocator>();
                    }
                    entityLocator.entity = gameObject;
                }
            }
        }

        private void GlobalEventManager_OnInteractionBegin(On.RoR2.GlobalEventManager.orig_OnInteractionBegin orig, GlobalEventManager self, Interactor interactor, IInteractable interactable, GameObject interactableObject)
        {
            if (!interactableObject.GetComponent<RideableController>())
            {
                orig.Invoke(self, interactor, interactable, interactableObject);
            }
        }

        public static void RegisterRideSeats(string bodyName, Func<CharacterBody, List<RideSeat>> rideSeatsGetter)
        {
            AvailableSeatsDictionary[bodyName] = rideSeatsGetter;
        }

        public static RideMeExtended Instance;
        public static SeatChangeCallback OnGlobalSeatChange;
        public static HashSet<string> RiderBodyBlacklist = new HashSet<string>();
        public static HashSet<string> RideableBodyBlacklist = new HashSet<string>();

        internal static Dictionary<string, Func<CharacterBody, List<RideSeat>>> AvailableSeatsDictionary = new Dictionary<string, Func<CharacterBody, List<RideSeat>>>();

        public delegate void SeatChangeCallback(RiderController rider, RideSeat oldSeat, RideSeat newSeat);

        public new ManualLogSource Logger
        {
            get
            {
                return base.Logger;
            }
        }
    }
}
