using System;
using UnityEngine;

namespace G10.Prototype.Navigation
{
    public enum ShipUpgrade { Speed, DiveSpeed, AscentSpeed, MaximumDepth, Hull, Radar, Photos, Captures, Energy }
    public enum ShipCharge { Radar, Photo, Capture }

    [Serializable]
    public sealed class ShipResourceSettings
    {
        [Min(1)] public float energyCapacity = 100;
        [Min(0.01f)] public float energyPerSecond = 1;
        [Min(1)] public float hullCapacity = 100;
        // Kept for existing scene data; impact damage now always equals actual speed.
        [HideInInspector] public float collisionDamagePerSpeed = 1;
        [Min(0)] public int radarCapacity = 10, photoCapacity = 20, captureCapacity = 5;
    }

    /// <summary>Serializable voyage state shared by zones and journal checkpoints; no Unity object references.</summary>
    [Serializable]
    public sealed class ShipState
    {
        public float speed, diveSpeed, ascentSpeed, maximumDepth;
        public float energyCapacity, energyPerSecond, hullCapacity, collisionDamagePerSpeed;
        public int radarCapacity, photoCapacity, captureCapacity;
        public float energy, hull;
        public int radar, photos, captures;
        public ShipState Copy() => (ShipState)MemberwiseClone();
        public void Refill()
        { energy = energyCapacity; hull = hullCapacity; radar = radarCapacity; photos = photoCapacity; captures = captureCapacity; }
        public bool IsValid => Positive(speed) && Positive(diveSpeed) && Positive(ascentSpeed) && Positive(maximumDepth)
            && Positive(energyCapacity) && Positive(energyPerSecond) && Positive(hullCapacity)
            && float.IsFinite(collisionDamagePerSpeed) && collisionDamagePerSpeed >= 0
            && InRange(energy, energyCapacity) && InRange(hull, hullCapacity)
            && radarCapacity >= 0 && photoCapacity >= 0 && captureCapacity >= 0
            && radar >= 0 && radar <= radarCapacity && photos >= 0 && photos <= photoCapacity && captures >= 0 && captures <= captureCapacity;
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
        private static bool InRange(float value, float max) => float.IsFinite(value) && value >= 0 && value <= max;
    }

    /// <summary>Single owner of ship budgets. Upgrades increase capacity without granting a free refill.</summary>
    public sealed class ShipResources
    {
        private ShipState state;
        public ShipResources(ShipState initial) => Restore(initial);
        public float Energy => state.energy;
        public float EnergyCapacity => state.energyCapacity;
        public float EnergyPerSecond => state.energyPerSecond;
        public float Hull => state.hull;
        public float HullCapacity => state.hullCapacity;
        public float Speed => state.speed;
        public float DiveSpeed => state.diveSpeed;
        public float AscentSpeed => state.ascentSpeed;
        public float MaximumDepth => state.maximumDepth;
        public int Radar => state.radar;
        public int Photos => state.photos;
        public int Captures => state.captures;
        public int RadarCapacity => state.radarCapacity;
        public int PhotoCapacity => state.photoCapacity;
        public int CaptureCapacity => state.captureCapacity;
        public bool CanMove => Energy > 0 && Hull > 0;
        public bool LowResources => Energy <= EnergyCapacity * .2f || Hull <= HullCapacity * .25f || Radar == 0 || Photos == 0 || Captures == 0;
        public ShipState Export() => state.Copy();
        public void Restore(ShipState saved)
        {
            if (saved == null || !saved.IsValid) throw new ArgumentException("Invalid ship state.");
            state = saved.Copy();
            // Preserve old saves without allowing their former multiplier to alter the 1:1 rule.
            state.collisionDamagePerSpeed = 1;
        }
        public void Refill() => state.Refill();
        public float AvailableMovementSeconds(float seconds) => CanMove ? Mathf.Min(Mathf.Max(0, seconds), Energy / EnergyPerSecond) : 0;
        public void ConsumeMovement(float seconds) => state.energy = Mathf.Max(0, state.energy - Mathf.Max(0, seconds) * EnergyPerSecond);
        public void HitTerrain(float impactSpeed)
        {
            if (!float.IsFinite(impactSpeed)) return;
            state.hull = Mathf.Max(0, state.hull - Mathf.Abs(impactSpeed));
        }
        public bool TryUse(ShipCharge charge)
        {
            if (Hull <= 0) return false;
            switch (charge)
            {
                case ShipCharge.Radar: if (state.radar <= 0) return false; state.radar--; break;
                case ShipCharge.Photo: if (state.photos <= 0) return false; state.photos--; break;
                case ShipCharge.Capture: if (state.captures <= 0) return false; state.captures--; break;
                default: return false;
            }
            return true;
        }
        public bool ApplyUpgrade(ShipUpgrade upgrade, float amount)
        {
            if (!float.IsFinite(amount) || amount <= 0) return false;
            var next = state.Copy();
            // Discrete charges must be whole numbers; reject overflow before casting.
            if (upgrade is ShipUpgrade.Radar or ShipUpgrade.Photos or ShipUpgrade.Captures)
                if (amount != Mathf.Floor(amount) || amount >= int.MaxValue) return false;
            switch (upgrade)
            {
                case ShipUpgrade.Speed: next.speed += amount; break;
                case ShipUpgrade.DiveSpeed: next.diveSpeed += amount; break;
                case ShipUpgrade.AscentSpeed: next.ascentSpeed += amount; break;
                case ShipUpgrade.MaximumDepth: next.maximumDepth += amount; break;
                case ShipUpgrade.Hull: next.hullCapacity += amount; break;
                case ShipUpgrade.Energy: next.energyCapacity += amount; break;
                case ShipUpgrade.Radar: next.radarCapacity = (int)Math.Min(int.MaxValue, (long)next.radarCapacity + (int)amount); break;
                case ShipUpgrade.Photos: next.photoCapacity = (int)Math.Min(int.MaxValue, (long)next.photoCapacity + (int)amount); break;
                case ShipUpgrade.Captures: next.captureCapacity = (int)Math.Min(int.MaxValue, (long)next.captureCapacity + (int)amount); break;
                default: return false;
            }
            if (!next.IsValid) return false;
            state = next; return true;
        }
    }
}
