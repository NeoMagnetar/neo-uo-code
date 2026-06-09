using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public sealed class UMGNavigationLandmarkResult
    {
        public bool Succeeded { get; set; }
        public bool WasNoOp { get; set; }
        public bool WasUnknown { get; set; }
        public string Reason { get; set; }
        public string Detail { get; set; }
        public UMGNavigationLandmark Landmark { get; set; }
        public List<UMGNavigationLandmark> Candidates { get; set; }
        public DateTime CompletedUtc { get; set; }

        public UMGNavigationLandmarkResult()
        {
            CompletedUtc = DateTime.UtcNow;
            Candidates = new List<UMGNavigationLandmark>();
        }

        public static UMGNavigationLandmarkResult Found(UMGNavigationLandmark landmark, string reason = null, string detail = null)
        {
            return new UMGNavigationLandmarkResult
            {
                Succeeded = true,
                Landmark = landmark,
                Reason = reason,
                Detail = detail
            };
        }

        public static UMGNavigationLandmarkResult Unknown(string reason = null, string detail = null)
        {
            return new UMGNavigationLandmarkResult
            {
                Succeeded = false,
                WasUnknown = true,
                Reason = reason,
                Detail = detail
            };
        }

        public static UMGNavigationLandmarkResult NoOp(string reason = null, string detail = null)
        {
            return new UMGNavigationLandmarkResult
            {
                Succeeded = false,
                WasNoOp = true,
                Reason = reason,
                Detail = detail
            };
        }

        public static UMGNavigationLandmarkResult Failure(string reason, string detail = null)
        {
            return new UMGNavigationLandmarkResult
            {
                Succeeded = false,
                Reason = reason,
                Detail = detail
            };
        }
    }
}
