using System;
using UnityEngine;
using UnityEngine.Networking;
using RoR2.Networking;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RideMeExtended
{
    public class RideableController : NetworkBehaviour, IInteractable, IDisplayNameProvider
    {

        private void Start()
        {
            if (Run.instance)
            {
                this.TeamIndex = base.GetComponent<TeamComponent>().teamIndex;
                this.CharacterBody = base.GetComponent<CharacterBody>();
                var bodyIndex = BodyCatalog.FindBodyIndex(this.CharacterBody.name);
                var bodyName = BodyCatalog.GetBodyName(bodyIndex);
                if (!string.IsNullOrEmpty(bodyName) &&
                    RideMeExtended.AvailableSeatsDictionary.ContainsKey(bodyName))
                {
                    var seatsGetter = RideMeExtended.AvailableSeatsDictionary[bodyName];
                    if (seatsGetter != null)
                    {
                        var seats = seatsGetter(this.CharacterBody);
                        if (seats != null)
                        {
                            foreach (var seat in seats)
                            {
                                seat.SeatOwner = this;
                                seat.SeatUser = null;
                                this.AvailableSeats.Add(seat);
                            }                            
                        }
                        return;
                    }
                }
                ModelLocator component = base.GetComponent<ModelLocator>();
                if (component && component.modelTransform)
                {
                    ChildLocator component2 = component.modelTransform.GetComponent<ChildLocator>();
                    if (component2)
                    {
                        var chest = component2.FindChild("Chest");
                        if (chest)
                        {
                            this.AvailableSeats.Add(new RideSeat()
                            {
                                SeatUser = null,
                                SeatOwner = this,
                                SeatTransform = chest,
                                PositionOffsetGetter = GetLegacyPositionOffset
                            });
                        }
                        else
                        {
                            this.AvailableSeats.Add(new RideSeat()
                            {
                                SeatUser = null,
                                SeatOwner = this,
                                SeatTransform = base.GetComponent<ModelLocator>().modelTransform,
                                PositionOffsetGetter = GetLegacyPositionOffset
                            });
                        }
                    }
                }
            }
        }

        private static Vector3 GetLegacyPositionOffset(RideSeat seat)
        {
            return RideMeExtendedConfig.defaultSeatPositionOffset.Value;
        }

        public string GetContextString(Interactor activator)
        {
            return Language.GetString(ContextToken);
        }

        public override int GetNetworkChannel()
        {
            return QosChannelIndex.defaultReliable.intVal;
        }

        public Interactability GetInteractability(Interactor activator)
        {
            if (CanRide && activator && activator.gameObject)
            {
                TeamComponent teamComponent = activator.GetComponent<TeamComponent>();
                RiderController riderController = activator.GetComponent<RiderController>();
                RideableController rideableController = activator.GetComponent<RideableController>();
                if (teamComponent &&
                    riderController &&
                    teamComponent.teamIndex == this.TeamIndex &&
                    riderController.gameObject != base.gameObject &&
                    riderController.CurrentSeat == null &&
                    (!rideableController || !rideableController.AvailableSeats.Any(x=>x.SeatUser)) &&
                    this.AvailableSeats.Any(x => !x.SeatUser))
                {
                    return Interactability.Available;
                }
            }
            return Interactability.Disabled;
        }

        [Server]
        public void OnInteractionBegin(Interactor activator)
        {
            RideMeExtended.Instance.Logger.LogMessage("OnInteractionBegin");
            var rider = activator.GetComponent<RiderController>();
            if (CanRide && rider)
            {
                var firstAvailableSeatIndex = GetNextAvailableSeatIndex(rider.CurrentSeat);
                if (firstAvailableSeatIndex != -1)
                {
                    RideMeExtended.Instance.Logger.LogMessage("OnInteractionBegin -> RpcEnterSeat");
                    this.RpcEnterSeat(firstAvailableSeatIndex, activator.gameObject);
                }
            }
        }

        [ClientRpc]
        public void RpcEnterSeat(int seatIndex, GameObject rider)
        {
            RideMeExtended.Instance.Logger.LogMessage("RpcEnterSeat: seatIndex: " + seatIndex + " rider: " + ((rider) ? rider.name : "null"));
            if (rider)
            {
                var riderController = rider.GetComponent<RiderController>();
                if (riderController)
                {
                    var firstAvailableSeat = this.GetSeatAtIndex(seatIndex);
                    if (firstAvailableSeat != null)
                    {
                        var oldSeat = riderController.CurrentSeat;
                        if (riderController.CurrentSeat != null)
                        {
                            riderController.CurrentSeat.SeatUser = null;
                        }
                        firstAvailableSeat.SeatUser = riderController;
                        riderController.CurrentSeat = firstAvailableSeat;
                        CharacterBody characterBody = riderController.CharacterBody;
                        if (characterBody && characterBody.characterMotor)
                        {
                            if (characterBody.characterMotor.Motor)
                            {
                                characterBody.characterMotor.Motor.ForceUnground();
                            }                            
                            characterBody.characterMotor.useGravity = false;
                            characterBody.characterMotor.velocity = Vector3.zero;
                            if (characterBody.characterDirection)
                            {
                                characterBody.characterDirection.enabled = !firstAvailableSeat.AlignToSeatRotation;
                            }
                        }
                        riderController.ToggleRiderCollisions(true);
                        RideMeExtended.OnGlobalSeatChange?.Invoke(riderController, oldSeat, firstAvailableSeat);
                    }
                }
            }
        }

        public int GetNextAvailableSeatIndex(RideSeat currentSeat)
        {
            var seatsCount = this.AvailableSeats.Count;
            if (currentSeat != null && seatsCount > 1)
            {
                var currentIndex = this.AvailableSeats.IndexOf(currentSeat);
                if (currentIndex != -1)
                {
                    var nextIndex = currentIndex + 1;
                    while (currentIndex != nextIndex)
                    {
                        if (nextIndex >= seatsCount)
                        {
                            nextIndex = 0;
                        }
                        if (!this.AvailableSeats[nextIndex].SeatUser)
                        {
                            return nextIndex;
                        }
                        nextIndex++;
                    }
                    return -1;
                }                
            }
            return this.AvailableSeats.FindIndex(x => !x.SeatUser);
        }

        public RideSeat GetSeatAtIndex(int index)
        {
            if (index >= 0 && index < this.AvailableSeats.Count)
            {
                return this.AvailableSeats[index];
            }
            return null;
        }

        public string GetDisplayName()
        {
            return Language.GetString("RIDE_INTERACTION");
        }

        public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
        {
            return false;
        }

        public bool ShouldShowOnScanner()
        {
            return false;
        }

        public void OnEnable()
        {
            InstanceTracker.Add<RideableController>(this);
        }

        public void OnDisable()
        {
            InstanceTracker.Remove<RideableController>(this);            
        }

        public void SetCanRide(bool value)
        {
            if (NetworkServer.active)
            {
                NetworkCanRide = value;
                return;
            }
            this.CmdSetCanRide(value);
        }

        [Command]
        private void CmdSetCanRide(bool value)
        {
            NetworkCanRide = value;
        }

        public bool NetworkCanRide
        {
            get
            {
                return this.CanRide;
            }
            [param: In]
            set
            {

                base.SetSyncVar<bool>(value, ref this.CanRide, 1U);
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool forceAll)
        {
            if (forceAll)
            {
                writer.Write(this.CanRide);
                return true;
            }
            bool flag = false;
            if ((base.syncVarDirtyBits & 1U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(this.CanRide);
            }
            if (!flag)
            {
                writer.WritePackedUInt32(base.syncVarDirtyBits);
            }
            return flag;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                this.CanRide = reader.ReadBoolean();
                return;
            }
            int num = (int)reader.ReadPackedUInt32();
            if ((num & 1) != 0)
            {
                this.CanRide = reader.ReadBoolean();
            }
        }

        internal const string DisplayNameToken = "RIDE_INTERACTION";
        private const string ContextToken = "RIDE_CONTEXT";

        public TeamIndex TeamIndex;
        public CharacterBody CharacterBody;
        public List<RideSeat> AvailableSeats = new List<RideSeat>();

        [SyncVar]
        public bool CanRide = true;

    }
}
