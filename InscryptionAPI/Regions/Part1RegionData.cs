using DiskCardGame;
using InscryptionAPI.Guid;
using System.Reflection;

namespace InscryptionAPI.Regions;

public class Part1RegionData
{
    public string GUID { get; private set; }
    public int Tier { get => tier; private set => tier = value; }
    public RegionData Region { get => region; private set => region = value; }
    public int MinTerrain { get; set; }
    public int MaxTerrain { get; set; }
    public bool AllowTerrainOnEnemySide { get; set; }
    public bool AllowTerrainOnPlayerSide { get; set; }
    public bool RemoveDefaultReachTerrain { get; set; }
    public bool DoNotForceReachTerrain { get; set; }
    public bool AllowLockedTerrainCards { get; set; }
    public bool AllowSacrificableTerrainCards { get; set; }

    private int tier;
    private RegionData region;

    public Part1RegionData(RegionData region, int tier)
    {
        Assembly callingAssembly = Assembly.GetCallingAssembly();
        GUID = TypeManager.GetModIdFromCallstack(callingAssembly);

        AllowSacrificableTerrainCards = false;
        AllowLockedTerrainCards = false;
        MinTerrain = -2;
        MaxTerrain = 3;
        AllowTerrainOnEnemySide = true;
        AllowTerrainOnPlayerSide = true;
        RemoveDefaultReachTerrain = false;
        DoNotForceReachTerrain = false;
        this.region = region;
        this.tier = tier;
    }
}