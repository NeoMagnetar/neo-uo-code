using System;
using Server.Mobiles;
using Server;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionStateAccess
    {
        private static readonly System.Collections.Generic.Dictionary<int, AIGMCompanionTravelObjective> TravelObjectives = new System.Collections.Generic.Dictionary<int, AIGMCompanionTravelObjective>();
        private static readonly System.Collections.Generic.Dictionary<int, DateTime> NextTravelPulseUtc = new System.Collections.Generic.Dictionary<int, DateTime>();
        private static readonly System.Collections.Generic.Dictionary<int, DateTime> NextTravelThreatScanUtc = new System.Collections.Generic.Dictionary<int, DateTime>();
        private static readonly System.Collections.Generic.Dictionary<int, bool> TrackingEnabled = new System.Collections.Generic.Dictionary<int, bool>();
        private static readonly System.Collections.Generic.Dictionary<int, AIGMTrackingCyclePhase> TrackingPhase = new System.Collections.Generic.Dictionary<int, AIGMTrackingCyclePhase>();
        private static readonly System.Collections.Generic.Dictionary<int, DateTime> TrackingNextSweepUtc = new System.Collections.Generic.Dictionary<int, DateTime>();
        private static readonly System.Collections.Generic.Dictionary<int, bool> TrackingMonsterLock = new System.Collections.Generic.Dictionary<int, bool>();
        private static readonly System.Collections.Generic.Dictionary<int, DateTime> HoldPositionUntilUtc = new System.Collections.Generic.Dictionary<int, DateTime>();
        private static readonly System.Collections.Generic.Dictionary<int, UMGMovementState> MovementStates = new System.Collections.Generic.Dictionary<int, UMGMovementState>();
        public static bool GetGuardOwnerMode(BaseHire companion)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
                return dakeyras.GuardOwnerMode;

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
                return danyal.GuardOwnerMode;

            AIGMCompanionDardalion dardalion = companion as AIGMCompanionDardalion;
            if (dardalion != null)
                return dardalion.GuardOwnerMode;

            return false;
        }

        public static void SetGuardOwnerMode(BaseHire companion, bool value)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
            {
                dakeyras.GuardOwnerMode = value;
                return;
            }

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
            {
                danyal.GuardOwnerMode = value;
                return;
            }

            AIGMCompanionDardalion dardalion = companion as AIGMCompanionDardalion;
            if (dardalion != null)
                dardalion.GuardOwnerMode = value;
        }

        public static DateTime GetNextSupportActionUtc(BaseHire companion)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
                return dakeyras.NextSupportActionUtc;

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
                return danyal.NextSupportActionUtc;

            AIGMCompanionDardalion dardalion = companion as AIGMCompanionDardalion;
            if (dardalion != null)
                return dardalion.NextSupportActionUtc;

            return DateTime.MinValue;
        }

        public static void SetNextSupportActionUtc(BaseHire companion, DateTime value)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
            {
                dakeyras.NextSupportActionUtc = value;
                return;
            }

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
            {
                danyal.NextSupportActionUtc = value;
                return;
            }

            AIGMCompanionDardalion dardalion = companion as AIGMCompanionDardalion;
            if (dardalion != null)
                dardalion.NextSupportActionUtc = value;
        }

        public static AIGMCompanionTravelObjective GetTravelObjective(BaseHire companion)
        {
            if (companion == null)
                return null;

            AIGMCompanionTravelObjective objective;
            TravelObjectives.TryGetValue(companion.Serial.Value, out objective);
            return objective;
        }

        public static void SetTravelObjective(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            if (companion == null)
                return;

            if (objective == null)
            {
                TravelObjectives.Remove(companion.Serial.Value);
                AIGMExecutionLog.Write("TRAVEL_OBJECTIVE_SET companion={0} action=clear", companion.Serial.Value);
            }
            else
            {
                TravelObjectives[companion.Serial.Value] = objective;
                AIGMExecutionLog.Write("TRAVEL_OBJECTIVE_SET companion={0} action=assign tracked={1} target={2} destination={3}", companion.Serial.Value, objective.HasTrackedPursuit, objective.TrackedTargetSerial, objective.DestinationPoint);
            }
        }

        public static DateTime GetNextTravelPulseUtc(BaseHire companion)
        {
            if (companion == null)
                return DateTime.MinValue;

            DateTime value;
            return NextTravelPulseUtc.TryGetValue(companion.Serial.Value, out value) ? value : DateTime.MinValue;
        }

        public static void SetNextTravelPulseUtc(BaseHire companion, DateTime value)
        {
            if (companion == null)
                return;

            NextTravelPulseUtc[companion.Serial.Value] = value;
        }

        public static DateTime GetNextTravelThreatScanUtc(BaseHire companion)
        {
            if (companion == null)
                return DateTime.MinValue;

            DateTime value;
            return NextTravelThreatScanUtc.TryGetValue(companion.Serial.Value, out value) ? value : DateTime.MinValue;
        }

        public static void SetNextTravelThreatScanUtc(BaseHire companion, DateTime value)
        {
            if (companion == null)
                return;

            NextTravelThreatScanUtc[companion.Serial.Value] = value;
        }

        public static bool GetTrackingEnabled(BaseHire companion)
        {
            if (companion == null)
                return false;

            bool value;
            return TrackingEnabled.TryGetValue(companion.Serial.Value, out value) && value;
        }

        public static void SetTrackingEnabled(BaseHire companion, bool value)
        {
            if (companion == null)
                return;

            TrackingEnabled[companion.Serial.Value] = value;
        }

        public static AIGMTrackingCyclePhase GetTrackingPhase(BaseHire companion)
        {
            if (companion == null)
                return AIGMTrackingCyclePhase.Monsters;

            AIGMTrackingCyclePhase value;
            return TrackingPhase.TryGetValue(companion.Serial.Value, out value) ? value : AIGMTrackingCyclePhase.Monsters;
        }

        public static void SetTrackingPhase(BaseHire companion, AIGMTrackingCyclePhase value)
        {
            if (companion == null)
                return;

            TrackingPhase[companion.Serial.Value] = value;
        }

        public static DateTime GetTrackingNextSweepUtc(BaseHire companion)
        {
            if (companion == null)
                return DateTime.MinValue;

            DateTime value;
            return TrackingNextSweepUtc.TryGetValue(companion.Serial.Value, out value) ? value : DateTime.MinValue;
        }

        public static void SetTrackingNextSweepUtc(BaseHire companion, DateTime value)
        {
            if (companion == null)
                return;

            TrackingNextSweepUtc[companion.Serial.Value] = value;
        }

        public static bool GetTrackingMonsterLock(BaseHire companion)
        {
            if (companion == null)
                return false;

            bool value;
            return TrackingMonsterLock.TryGetValue(companion.Serial.Value, out value) && value;
        }

        public static void SetTrackingMonsterLock(BaseHire companion, bool value)
        {
            if (companion == null)
                return;

            TrackingMonsterLock[companion.Serial.Value] = value;
        }

        public static DateTime GetHoldPositionUntilUtc(BaseHire companion)
        {
            if (companion == null)
                return DateTime.MinValue;

            DateTime value;
            return HoldPositionUntilUtc.TryGetValue(companion.Serial.Value, out value) ? value : DateTime.MinValue;
        }

        public static void SetHoldPositionUntilUtc(BaseHire companion, DateTime value)
        {
            if (companion == null)
                return;

            HoldPositionUntilUtc[companion.Serial.Value] = value;
        }

        public static void LatchHoldPosition(BaseHire companion)
        {
            SetHoldPositionUntilUtc(companion, DateTime.MaxValue);
        }

        public static void ClearHoldPosition(BaseHire companion)
        {
            SetHoldPositionUntilUtc(companion, DateTime.MinValue);
        }

        public static bool IsHoldPositionLatched(BaseHire companion)
        {
            return GetHoldPositionUntilUtc(companion) == DateTime.MaxValue;
        }

        public static UMGMovementState GetMovementState(BaseHire companion)
        {
            if (companion == null)
                return null;

            UMGMovementState state;
            return MovementStates.TryGetValue(companion.Serial.Value, out state) ? state : null;
        }

        public static void SetMovementState(BaseHire companion, UMGMovementState state)
        {
            if (companion == null)
                return;

            if (state == null)
                MovementStates.Remove(companion.Serial.Value);
            else
                MovementStates[companion.Serial.Value] = state;
        }

        public static void ResetAllCompanionIntentState(BaseHire companion)
        {
            if (companion == null)
                return;

            SetTrackingEnabled(companion, false);
            SetTrackingPhase(companion, AIGMTrackingCyclePhase.Monsters);
            SetTrackingNextSweepUtc(companion, DateTime.MinValue);
            SetTrackingMonsterLock(companion, false);

            SetTravelObjective(companion, null);
            SetNextTravelPulseUtc(companion, DateTime.MinValue);
            SetNextTravelThreatScanUtc(companion, DateTime.MinValue);

            ClearHoldPosition(companion);
            SetMovementState(companion, null);
            SetGuardOwnerMode(companion, false);

            companion.Combatant = null;
            companion.ControlTarget = null;
            companion.CantWalk = true;
            companion.Home = companion.Location;
            companion.RangeHome = 0;
            companion.ControlOrder = OrderType.Stay;
        }

        public static AIGMExecutionMode GetExecutionMode(BaseHire companion)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
                return dakeyras.ExecutionMode;

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
                return danyal.ExecutionMode;

            AIGMCompanionDardalion dardalion = companion as AIGMCompanionDardalion;
            if (dardalion != null)
                return dardalion.ExecutionMode;

            return AIGMExecutionMode.SuggestOnly;
        }
    }
}
