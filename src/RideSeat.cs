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
        public Func<CharacterBody, Vector3> PositionOffsetGetter;
        public bool AlignToSeatRotation;
    }
}
