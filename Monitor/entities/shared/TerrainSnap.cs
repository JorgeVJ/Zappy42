using Godot;

public static class TerrainSnap
{
    public static float GetY(Terrain terrain, int tileX, int tileY, float yOffset = 0f)
    {
        if (terrain == null) return yOffset;
        return terrain.GetTileHeight(tileX, tileY) + yOffset;
    }

    public static Vector3 TileCenter(Terrain terrain, int tileX, int tileY, float yOffset = 0f)
    {
        return new Vector3(
            tileX * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f,
            GetY(terrain, tileX, tileY, yOffset),
            tileY * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f
        );
    }

    // Bilinear interpolation para posiciones mundo arbitrarias (e.g. grass)
    public static float SampleHeight(float[,] heightMap, float worldX, float worldZ,
                                     float tileSize, int mapWidth, int mapHeight)
    {
        float tx = worldX / tileSize;
        float tz = worldZ / tileSize;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(tx), 0, mapWidth);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(tz), 0, mapHeight);
        int x1 = Mathf.Min(x0 + 1, mapWidth);
        int z1 = Mathf.Min(z0 + 1, mapHeight);
        float fx = tx - x0;
        float fz = tz - z0;

        return Mathf.Lerp(
            Mathf.Lerp(heightMap[x0, z0], heightMap[x1, z0], fx),
            Mathf.Lerp(heightMap[x0, z1], heightMap[x1, z1], fx),
            fz);
    }
}
