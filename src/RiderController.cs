using UnityEngine;
using UnityEngine.Networking;
using RoR2.Networking;
using KinematicCharacterController;
using RoR2;

namespace RideMeExtended
{
    public class RiderController : NetworkBehaviour
    {
        private void Start()
        {
            if (Run.instance)
            {
                this.CharacterBody = base.GetComponent<CharacterBody>();
            }
        }

        private void FixedUpdate()
        {
            if (Run.instance && this.CharacterBody && this.CurrentSeat != null)
            {
                var currentSeat = this.CurrentSeat;
                if (currentSeat.SeatTransform && currentSeat.SeatUser == this &&
                    currentSeat.SeatOwner && currentSeat.SeatOwner.CanRide)
                {
                    if (Util.HasEffectiveAuthority(base.gameObject))
                    {
                        if (this.CharacterBody.inputBank.jump.justPressed)
                        {
                            this.CmdExitSeat();
                            return;
                        }
                        else if (this.CharacterBody.inputBank.interact.justPressed)
                        {
                            var nextAvailableSeatIndex = this.CurrentSeat.SeatOwner.GetNextAvailableSeatIndex(this.CurrentSeat);
                            if (nextAvailableSeatIndex != -1)
                            {
                                this.CmdSwapSeat();
                                return;
                            }
                        }
                    }
                    KinematicCharacterMotor motor = this.CharacterBody.characterMotor.Motor;
                    if (motor)
                    {
                        var transform = currentSeat.SeatTransform;
                        var getPositionOffset = currentSeat.PositionOffsetGetter;
                        var newPosition = (getPositionOffset != null) ? transform.position + (transform.rotation * getPositionOffset(this.CharacterBody)) : transform.position;
                        if (currentSeat.AlignToSeatRotation)
                        {
                            motor.SetPositionAndRotation(newPosition, transform.rotation, true);
                        }
                        else
                        {
                            motor.SetPosition(newPosition, true);
                        }
                    }
                }
                else if (Util.HasEffectiveAuthority(base.gameObject))
                {                                     
                    this.CmdExitSeat();
                }
            }
        }
                
        public override int GetNetworkChannel()
        {
            return QosChannelIndex.defaultReliable.intVal;
        }

        [Command]
        public void CmdExitSeat()
        {
            RideMeExtended.Instance.Logger.LogMessage("CmdExitSeat");
            this.RpcExitSeat();
        }

        [ClientRpc]
        public void RpcExitSeat()
        {
            RideMeExtended.Instance.Logger.LogMessage("RpcExitSeat");
            if (this.CurrentSeat != null)
            {
                var oldSeat = this.CurrentSeat;
                this.CurrentSeat.SeatUser = null;
                this.CurrentSeat = null;
                if (this.CharacterBody && this.CharacterBody.characterMotor)
                {
                    var characterMotor = this.CharacterBody.characterMotor;
                    characterMotor.useGravity = true;
                    characterMotor.velocity = Vector3.zero;
                    if (RideMeExtendedConfig.jumpRiderOnExitEnabled.Value)
                    {
                        characterMotor.Motor.ForceUnground();
                        characterMotor.velocity = new Vector3(characterMotor.velocity.x, Mathf.Max(characterMotor.velocity.y, RideMeExtendedConfig.jumpRiderOnExitVelocity.Value), characterMotor.velocity.z);
                    }
                }
                this.ToggleRiderCollisions(false);
                RideMeExtended.OnGlobalSeatChange?.Invoke(this, oldSeat, null);
            }    
        }
        
        [Command]
        public void CmdSwapSeat()
        {
            RideMeExtended.Instance.Logger.LogMessage("CmdSwapSeat");
            if (this.CurrentSeat != null && this.CurrentSeat.SeatOwner != null)
            {
                var nextAvailableSeatIndex = this.CurrentSeat.SeatOwner.GetNextAvailableSeatIndex(this.CurrentSeat);
                if (nextAvailableSeatIndex != -1)
                {
                    this.RpcSwapSeat(nextAvailableSeatIndex);
                }
            }
        }

        [ClientRpc]
        public void RpcSwapSeat(int seatIndex)
        {
            RideMeExtended.Instance.Logger.LogMessage("RpcSwapSeat: seatIndex: " + seatIndex);
            if (this.CurrentSeat != null && this.CurrentSeat.SeatOwner != null)
            {
                var firstAvailableSeat = this.CurrentSeat.SeatOwner.GetSeatAtIndex(seatIndex);
                if (firstAvailableSeat != null && this.CurrentSeat != firstAvailableSeat)
                {
                    var oldSeat = this.CurrentSeat;
                    this.CurrentSeat.SeatUser = null;
                    this.CurrentSeat = firstAvailableSeat;
                    firstAvailableSeat.SeatUser = this;
                    RideMeExtended.OnGlobalSeatChange?.Invoke(this, oldSeat, firstAvailableSeat);
                }
            }
        }

        public void ToggleRiderCollisions(bool disable)
        {
            if (RideMeExtendedConfig.disableRiderHurtBoxEnabled.Value && this.CharacterBody && this.CharacterBody.modelLocator && this.CharacterBody.modelLocator.modelTransform)
            {
                var hurtboxGroup = this.CharacterBody.modelLocator.modelTransform.GetComponent<HurtBoxGroup>();
                if (hurtboxGroup)
                {
                    if (disable)
                    {
                        hurtboxGroup.hurtBoxesDeactivatorCounter++;
                    }
                    else
                    {
                        hurtboxGroup.hurtBoxesDeactivatorCounter--;
                    }
                }
            }
            if (this.CharacterBody && this.CharacterBody.characterMotor && this.CharacterBody.characterMotor.capsuleCollider)
            {
                this.CharacterBody.characterMotor.capsuleCollider.enabled = !disable;
            }            
        }

        public void OnEnable()
        {
            InstanceTracker.Add<RiderController>(this);
        }

        public void OnDisable()
        {
            InstanceTracker.Remove<RiderController>(this);
        }

        public CharacterBody CharacterBody;
        public RideSeat CurrentSeat;
    }

}
