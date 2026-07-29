using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public interface IUMGNavigationLandmarkRegistry
    {
        UMGNavigationLandmarkResult Resolve(UMGNavigationLandmarkQuery query);
        UMGNavigationLandmarkResult FindById(string landmarkId);
        IEnumerable<UMGNavigationLandmark> GetAll();
        IEnumerable<UMGNavigationLandmark> FindByKind(UMGNavigationLandmarkKind kind);
    }
}
