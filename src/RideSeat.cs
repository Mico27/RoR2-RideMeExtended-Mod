using RoR2;
using System;
using UnityEngine;

namespace RideMeExtended
{
    public class RideSeat
    {
        public string Name;
        public Transform SeatTransform;
        public RideableController SeatOwner;
        public RiderController SeatUser;
        public Func<RideSeat, Vector3> PositionOffsetGetter;
        public Func<RideSeat, Quaternion> RotationOffsetGetter;
        public bool AlignToSeatRotation;
    }
}
