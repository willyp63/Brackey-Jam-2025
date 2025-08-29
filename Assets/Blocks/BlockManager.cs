using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class Block
{
    public BlockData blockData;
    public int health;

    public Block(BlockData blockData)
    {
        this.blockData = blockData;
        health = blockData.maxHealth;
    }
}

public class BlockManager : MonoBehaviour
{
    [Header("Player")]
    public Player player;

    [Header("Tilemap Components")]
    public Tilemap tilemap;

    [Header("Damage Tilemap")]
    public Tilemap damageTilemap;

    [Header("Tiles")]
    public BlockData dirtBlock;
    public BlockData iceBlock;
    public BlockData magmaBlock;
    public BlockData stoneBlock;
    public BlockData oreBlock;
    public BlockData largeOreBlock;
    public BlockData barrierBlock;

    [Header("Damage Sprites")]
    public Sprite[] damageSprites;

    public GameObject blockDestroyEffectPrefab;

    [Header("World Settings")]
    public int worldWidth = 40;
    public int worldHeight = 100;

    public float dirtNoiseScale = 0.1f;
    public float dirtThreshold = 0.5f;
    public float dirtThresholdIncreaseAmount = 0.2f;

    public float iceNoiseScale = 0.1f;
    public float iceThreshold = 0.6f;
    public float iceThresholdIncreaseAmount = 0.4f;

    public float magmaNoiseScale = 0.1f;
    public float magmaThreshold = 0.6f;
    public float magmaThresholdDecreaseAmount = 0.4f;

    public float oreNoiseScale = 0.1f;
    public float oreThreshold = 0.7f;
    public float largeOreThreshold = 0.8f;
    public float oreThresholdDecreaseAmount = 0.2f;
    public float largeOreThresholdDecreaseAmount = 0.2f;

    public float caveNoiseScale = 0.05f;
    public float caveThreshold = 0.8f;

    public float damageNoiseScale = 0.05f;
    public float minDamageThreshold = 0.5f;
    public float maxDamageThreshold = 0.6f;
    public float damageThresholdDecreaseAmount = 0.1f;
    public float damageInterval = 5f;
    public float damageIntervalDecreaseAmount = 0.1f;

    [Header("Lava Settings")]
    public Lava lava;
    public float initialLavaSpeed = 0.05f;
    public float lavaSpeedIncreaseAmount = 0.05f;
    public float lavaSpeedIncreaseInterval = 10f;

    public int numTopEmptyLayers = 9;

    [Header("Gold Drop Settings")]
    public GameObject goldNugPrefab;

    [Header("Falling Block Settings")]
    public GameObject fallingBlockPrefab;
    public float fallingBlockInterval = 3f;
    public int blocksToFallPerInterval = 12;

    [Header("Enemy Settings")]
    public GameObject bladeEnemyPrefab;
    public float enemyMinSpawnRadius = 6f;
    public float enemyMaxSpawnRadius = 10f;
    public float enemyDespawnRadius = 12f;
    public int initialEnemies = 3;
    public float enemiesIncreaseInterval = 10f;
    public int enemiesIncreaseAmount = 1;

    private Dictionary<Vector3Int, Block> blockMap = new();

    private Dictionary<BlockData, TileBase> tileMap = new();
    private List<TileBase> damageTiles = new();

    private float lastDamageTickTime = 0f;
    private float currentMinDamageThreshold = 0f;
    private float currentMaxDamageThreshold = 0f;
    private float currentDamageInterval = 0f;
    private float currentLavaSpeed = 0f;
    private int lavaLevel = -1;
    private float lastFallingBlockTime = 0f;
    private float lastLavaSpeedIncreaseTime = 0f;

    // Enemy management
    private List<GameObject> activeEnemies = new List<GameObject>();
    private int targetEnemyCount = 0;
    private float lastEnemyIncreaseTime = 0f;

    void Start()
    {
        currentMinDamageThreshold = minDamageThreshold;
        currentMaxDamageThreshold = maxDamageThreshold;
        currentDamageInterval = damageInterval;

        lavaLevel = -1;
        currentLavaSpeed = initialLavaSpeed;
        lava.SetMoveSpeed(currentLavaSpeed);

        InitializeTileMap();
        GenerateWorld();
    }

    void Update()
    {
        if (Time.time - lastDamageTickTime > currentDamageInterval)
        {
            lastDamageTickTime = Time.time;

            currentMinDamageThreshold -= damageThresholdDecreaseAmount;
            currentMaxDamageThreshold -= damageThresholdDecreaseAmount;
            currentMinDamageThreshold = Mathf.Max(0f, currentMinDamageThreshold);
            currentMaxDamageThreshold = Mathf.Max(
                currentMinDamageThreshold,
                currentMaxDamageThreshold
            );

            currentDamageInterval -= damageIntervalDecreaseAmount;
            currentDamageInterval = Mathf.Max(1f, currentDamageInterval);

            GenerateDamage();
        }

        if (Time.time - lastFallingBlockTime > fallingBlockInterval)
        {
            lastFallingBlockTime = Time.time;
            ConvertBlocksToFallingBlocks();
        }

        if (Time.time - lastLavaSpeedIncreaseTime > lavaSpeedIncreaseInterval)
        {
            lastLavaSpeedIncreaseTime = Time.time;
            currentLavaSpeed += lavaSpeedIncreaseAmount;
            lava.SetMoveSpeed(currentLavaSpeed);
        }
    }

    void InitializeTileMap()
    {
        tileMap.Clear();
        tileMap[dirtBlock] = CreateTile(dirtBlock.sprite);
        tileMap[iceBlock] = CreateTile(iceBlock.sprite);
        tileMap[magmaBlock] = CreateTile(magmaBlock.sprite);
        tileMap[stoneBlock] = CreateTile(stoneBlock.sprite);
        tileMap[oreBlock] = CreateTile(oreBlock.sprite);
        tileMap[largeOreBlock] = CreateTile(largeOreBlock.sprite);
        tileMap[barrierBlock] = CreateTile(barrierBlock.sprite);

        damageTiles.Clear();
        foreach (Sprite sprite in damageSprites)
        {
            damageTiles.Add(CreateTile(sprite));
        }
    }

    TileBase CreateTile(Sprite sprite)
    {
        Tile newTile = ScriptableObject.CreateInstance<Tile>();
        newTile.sprite = sprite;
        return newTile;
    }

    void GenerateWorld()
    {
        float cellSize = tilemap.cellSize.x;
        tilemap.transform.position = new Vector3(
            -worldWidth * cellSize / 2f,
            -worldHeight * cellSize / 2f,
            0
        );
        damageTilemap.transform.position = tilemap.transform.position;

        // Generate world using Perlin noise
        //   - Fill the entire world with stone blocks
        //   - Add dirt, ice, and ore blocks on top of the stone blocks using Perlin noise
        //   - Remove blocks to generate random caves in the world using Perlin noise
        //   - Replace each perimeter block with a barrier block

        // Generate the base world with stone blocks
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                AddTile(new Vector3Int(x, y, 0), stoneBlock);
            }
        }

        // Generate terrain variation using Perlin noise
        GenerateTerrainVariation();

        // Generate caves using Perlin noise
        GenerateCaves();

        // Remove blocks around the player
        // RemoveBlocksAroundPlayer();

        RemoveTopLayers();

        AddSpawnPlatform();

        // Add perimeter barriers
        AddPerimeterBarriers();

        GenerateDamage();
    }

    void GenerateTerrainVariation()
    {
        // Use different noise seeds for different terrain types
        float dirtNoiseOffset = Random.Range(0f, 1000f);
        float iceNoiseOffset = Random.Range(0f, 1000f);
        float oreNoiseOffset = Random.Range(0f, 1000f);
        float magmaNoiseOffset = Random.Range(0f, 1000f);

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);

                // Generate Perlin noise values for different terrain types
                float dirtNoise = Mathf.PerlinNoise(
                    (x + dirtNoiseOffset) * dirtNoiseScale,
                    (y + dirtNoiseOffset) * dirtNoiseScale
                );
                float iceNoise = Mathf.PerlinNoise(
                    (x + iceNoiseOffset) * iceNoiseScale,
                    (y + iceNoiseOffset) * iceNoiseScale
                );
                float magmaNoise = Mathf.PerlinNoise(
                    (x + magmaNoiseOffset) * magmaNoiseScale,
                    (y + magmaNoiseOffset) * magmaNoiseScale
                );
                float oreNoise = Mathf.PerlinNoise(
                    (x + oreNoiseOffset) * oreNoiseScale,
                    (y + oreNoiseOffset) * oreNoiseScale
                );

                // Calculate depth factor for ore generation (more ore at lower y coordinates)
                float depthFactor = 1f - (float)y / worldHeight; // 1.0 at bottom, 0.0 at top

                // Determine block type based on noise values, position, and depth
                BlockData blockType = DetermineBlockType(
                    x,
                    y,
                    dirtNoise,
                    iceNoise,
                    magmaNoise,
                    oreNoise,
                    depthFactor
                );

                // Only replace stone blocks with new terrain
                if (blockType != stoneBlock)
                {
                    AddTile(position, blockType);
                }
            }
        }
    }

    BlockData DetermineBlockType(
        int x,
        int y,
        float dirtNoise,
        float iceNoise,
        float magmaNoise,
        float oreNoise,
        float depthFactor
    )
    {
        // Ore generation (deeper = more common)
        // Apply depth factor to make ore more common at lower y coordinates
        float adjustedOreThreshold = oreThreshold - (depthFactor * oreThresholdDecreaseAmount); // Lower threshold at depth
        float adjustedLargeOreThreshold =
            largeOreThreshold - (depthFactor * largeOreThresholdDecreaseAmount); // Lower threshold at depth
        float adjustedIceThreshold = iceThreshold + (depthFactor * iceThresholdIncreaseAmount);
        float adjustedMagmaThreshold =
            magmaThreshold - (depthFactor * magmaThresholdDecreaseAmount);
        float adjustedDirtThreshold = dirtThreshold + (depthFactor * dirtThresholdIncreaseAmount);

        if (oreNoise > adjustedLargeOreThreshold)
        {
            return largeOreBlock;
        }

        if (oreNoise > adjustedOreThreshold)
        {
            return oreBlock;
        }

        // Ice generation (more common in certain areas)
        if (iceNoise > adjustedIceThreshold)
        {
            return iceBlock;
        }

        // Magma generation (more common in certain areas)
        if (magmaNoise > adjustedMagmaThreshold)
        {
            return magmaBlock;
        }

        // Dirt generation (more common near the "surface" areas)
        if (dirtNoise > adjustedDirtThreshold)
        {
            return dirtBlock;
        }

        // Default to stone
        return stoneBlock;
    }

    void GenerateCaves()
    {
        // Use different noise seeds for cave generation
        float caveNoiseOffset1 = Random.Range(0f, 1000f);
        float caveNoiseOffset2 = Random.Range(0f, 1000f);

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);

                // Generate cave noise using multiple Perlin noise layers
                float caveNoise1 = Mathf.PerlinNoise(
                    (x + caveNoiseOffset1) * caveNoiseScale * 0.5f,
                    (y + caveNoiseOffset1) * caveNoiseScale * 0.5f
                );
                float caveNoise2 = Mathf.PerlinNoise(
                    (x + caveNoiseOffset2) * caveNoiseScale * 0.3f,
                    (y + caveNoiseOffset2) * caveNoiseScale * 0.3f
                );

                // Combine noise values for more natural cave shapes
                float combinedCaveNoise = (caveNoise1 + caveNoise2) * 0.5f;

                if (combinedCaveNoise > caveThreshold)
                {
                    RemoveTile(position);
                }
            }
        }
    }

    void GenerateDamage()
    {
        // Use different noise seeds for cave generation
        float damageNoiseOffset1 = Random.Range(0f, 1000f);
        float damageNoiseOffset2 = Random.Range(0f, 1000f);

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                if (!blockMap.ContainsKey(position))
                {
                    continue;
                }

                // Generate damage noise using multiple Perlin noise layers
                float damageNoise1 = Mathf.PerlinNoise(
                    (x + damageNoiseOffset1) * damageNoiseScale * 0.5f,
                    (y + damageNoiseOffset1) * damageNoiseScale * 0.5f
                );
                float damageNoise2 = Mathf.PerlinNoise(
                    (x + damageNoiseOffset2) * damageNoiseScale * 0.3f,
                    (y + damageNoiseOffset2) * damageNoiseScale * 0.3f
                );

                // Combine noise values for more natural damage shapes
                float combinedDamageNoise = (damageNoise1 + damageNoise2) * 0.5f;

                if (combinedDamageNoise > currentMinDamageThreshold)
                {
                    float damage = Mathf.Clamp(
                        (combinedDamageNoise - currentMinDamageThreshold)
                            / (currentMaxDamageThreshold - currentMinDamageThreshold),
                        0,
                        1
                    );
                    Block block = blockMap[position];
                    DamageBlock(position, (int)(block.blockData.maxHealth * damage));
                }
            }
        }
    }

    void ConvertBlocksToFallingBlocks()
    {
        List<Vector3Int> eligibleBlocks = new List<Vector3Int>();

        // Find all blocks that have empty space below them
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                Vector3Int belowPosition0 = new Vector3Int(x, y - 1, 0);
                Vector3Int belowPosition1 = new Vector3Int(x, y - 2, 0);

                // Check if there's a block at this position and no block below it
                if (
                    blockMap.ContainsKey(position)
                    && !blockMap.ContainsKey(belowPosition0)
                    && !blockMap.ContainsKey(belowPosition1)
                )
                {
                    // Skip barrier blocks and indestructible blocks
                    Block block = blockMap[position];
                    if (!block.blockData.isIndestructible && block.blockData != barrierBlock)
                    {
                        eligibleBlocks.Add(position);
                    }
                }
            }
        }

        // If no eligible blocks, return early
        if (eligibleBlocks.Count == 0)
            return;

        // Select random blocks to convert (up to the specified amount)
        int blocksToConvert = Mathf.Min(blocksToFallPerInterval, eligibleBlocks.Count);
        List<Vector3Int> blocksToConvertList = new List<Vector3Int>();

        for (int i = 0; i < blocksToConvert; i++)
        {
            int randomIndex = Random.Range(0, eligibleBlocks.Count);
            blocksToConvertList.Add(eligibleBlocks[randomIndex]);
            eligibleBlocks.RemoveAt(randomIndex);
        }

        // Convert selected blocks to falling blocks
        foreach (Vector3Int position in blocksToConvertList)
        {
            ConvertBlockToFallingBlock(position);
        }
    }

    void ConvertBlockToFallingBlock(Vector3Int position)
    {
        if (!blockMap.ContainsKey(position))
            return;

        Block block = blockMap[position];

        // Get the world position of the block
        Vector3 worldPosition = tilemap.GetCellCenterWorld(position);

        // Remove the block from the tilemap
        RemoveTile(position);

        // Spawn the falling block
        GameObject fallingBlockObj = Instantiate(
            fallingBlockPrefab,
            worldPosition,
            Quaternion.identity
        );
        FallingBlock fallingBlock = fallingBlockObj.GetComponent<FallingBlock>();

        if (fallingBlock != null)
        {
            fallingBlock.Initialize(block, this);
        }
    }

    void RemoveBlocksAroundPlayer()
    {
        Vector3Int playerPosition = GetNearestBlockPosition(player.transform.position);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int position = playerPosition + new Vector3Int(x, y, 0);
                if (blockMap.ContainsKey(position))
                {
                    RemoveTile(position);
                }
            }
        }
    }

    void AddPerimeterBarriers()
    {
        for (int x = -1; x <= worldWidth; x++)
        {
            // Top barrier
            AddBarrierTile(new Vector3Int(x, worldHeight, 0));
        }

        for (int y = -1; y <= worldHeight; y++)
        {
            // Left barrier
            AddBarrierTile(new Vector3Int(-1, y, 0));
            // Right barrier
            AddBarrierTile(new Vector3Int(worldWidth, y, 0));
        }
    }

    void AddBarrierTile(Vector3Int position)
    {
        // Only add barrier if there isn't already a block there
        if (!blockMap.ContainsKey(position))
        {
            tilemap.SetTile(position, tileMap[barrierBlock]);
            blockMap[position] = new Block(barrierBlock);
        }
    }

    void RemoveTopLayers()
    {
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = worldHeight - 1; y > worldHeight - numTopEmptyLayers; y--)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                RemoveTile(position);
            }
        }
    }

    void AddSpawnPlatform()
    {
        AddTile(
            new Vector3Int(worldWidth / 2 - 2, worldHeight - numTopEmptyLayers, 0),
            barrierBlock
        );
        AddTile(
            new Vector3Int(worldWidth / 2 + 1, worldHeight - numTopEmptyLayers, 0),
            barrierBlock
        );
        AddTile(
            new Vector3Int(worldWidth / 2 - 1, worldHeight - numTopEmptyLayers, 0),
            barrierBlock
        );
        AddTile(new Vector3Int(worldWidth / 2, worldHeight - numTopEmptyLayers, 0), barrierBlock);
    }

    void AddTile(Vector3Int position, BlockData blockData)
    {
        tilemap.SetTile(position, tileMap[blockData]);
        blockMap[position] = new Block(blockData);

        // Clear any existing damage sprite
        damageTilemap.SetTile(position, null);
    }

    void RemoveTile(Vector3Int position)
    {
        tilemap.SetTile(position, null);
        damageTilemap.SetTile(position, null);
        blockMap.Remove(position);
    }

    public float GetFriction(Vector3 worldPosition)
    {
        Vector3Int gridPosition = GetNearestBlockPosition(worldPosition);
        if (!blockMap.ContainsKey(gridPosition))
            return 1f;

        return blockMap[gridPosition].blockData.friction;
    }

    public Vector3Int DamageBlock(Vector3 worldPosition, int damage)
    {
        Vector3Int gridPosition = GetNearestBlockPosition(worldPosition);
        if (!blockMap.ContainsKey(gridPosition))
            return new Vector3Int(-1, -1, -1);

        DamageBlock(gridPosition, damage);

        return gridPosition;
    }

    public void DamageBlock(Vector3Int gridPosition, int damage)
    {
        Block block = blockMap[gridPosition];

        if (block.blockData.isIndestructible)
            return;

        block.health -= damage;

        // Update damage sprite
        UpdateDamageSprite(gridPosition, block);

        if (block.health <= 0)
        {
            DestroyBlock(gridPosition);
        }
    }

    void DestroyBlock(Vector3Int position)
    {
        Block block = blockMap[position];

        if (IsBlockOnScreen(position))
        {
            Vector3 worldPosition = tilemap.GetCellCenterWorld(position);
            SpawnBlockDestroyEffect(worldPosition, block.blockData);
            SpawnLoot(worldPosition, block.blockData);
        }

        RemoveTile(position);
    }

    public void SpawnLoot(Vector3 worldPosition, BlockData blockData)
    {
        if (blockData.minGoldDrop <= 0)
            return;

        int goldDrop = Random.Range(blockData.minGoldDrop, blockData.maxGoldDrop + 1);
        for (int i = 0; i < goldDrop; i++)
        {
            Vector3 goldNugWorldPosition =
                worldPosition
                + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0);

            GameObject goldNug = Instantiate(
                goldNugPrefab,
                goldNugWorldPosition,
                Quaternion.identity
            );
            Rigidbody2D rigidbody = goldNug.GetComponent<Rigidbody2D>();
            rigidbody.AddForce(
                new Vector2(Random.Range(-2, 2), Random.Range(-2, 2)),
                ForceMode2D.Impulse
            );
            Loot loot = goldNug.GetComponent<Loot>();
            loot.Initialize(player);
        }
    }

    public void SpawnBlockDestroyEffect(Vector3 worldPosition, BlockData blockData)
    {
        GameObject blockDestroyEffect = Instantiate(
            blockDestroyEffectPrefab,
            worldPosition,
            Quaternion.identity
        );
        blockDestroyEffect.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        ParticleSystem particleSystem = blockDestroyEffect.GetComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.startColor = blockData.color;
        particleSystem.Play();
        Destroy(blockDestroyEffect, 0.3f);
    }

    void UpdateDamageSprite(Vector3Int position, Block block)
    {
        if (damageSprites == null || damageSprites.Length == 0)
        {
            return; // No damage sprites configured
        }

        // Calculate damage level (0 = no damage, damageSprites.Length = completely damaged)
        float healthPercentage = (float)block.health / block.blockData.maxHealth;
        int damageLevel = Mathf.FloorToInt((1f - healthPercentage) * damageSprites.Length);

        // Clamp damage level to valid range
        damageLevel = Mathf.Clamp(damageLevel, 0, damageSprites.Length - 1);

        // Set the appropriate damage sprite
        if (damageLevel > 0)
        {
            damageTilemap.SetTile(position, damageTiles[damageLevel]);
        }
        else
        {
            // No damage, clear damage sprite
            damageTilemap.SetTile(position, null);
        }
    }

    public Sprite GetDamageSprite(float health, float maxHealth)
    {
        float healthPercentage = health / maxHealth;
        int damageLevel = Mathf.FloorToInt((1f - healthPercentage) * damageSprites.Length);
        return damageSprites[damageLevel];
    }

    public Vector3 GetNearestGridPosition(Vector3 worldPosition)
    {
        return tilemap.GetCellCenterWorld(tilemap.WorldToCell(worldPosition));
    }

    private Vector3Int GetNearestBlockPosition(Vector3 worldPosition)
    {
        // Check the cell that contains the worldPosition
        Vector3Int gridPosition = tilemap.WorldToCell(worldPosition);
        if (blockMap.ContainsKey(gridPosition))
        {
            return gridPosition;
        }

        // Check the 8 surrounding cells and find which one's center is closest to the worldPosition
        Vector3Int closestGridPosition = FindClosestBlockPosition(worldPosition, gridPosition);
        if (closestGridPosition != gridPosition && blockMap.ContainsKey(closestGridPosition))
        {
            return closestGridPosition;
        }

        return new Vector3Int(-1, -1, -1); // No valid block found
    }

    private Vector3Int FindClosestBlockPosition(
        Vector3 worldPosition,
        Vector3Int centerGridPosition
    )
    {
        Vector3Int closestPosition = centerGridPosition;
        float closestDistance = float.MaxValue;

        // Check the 8 surrounding cells plus the center cell
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkPosition = centerGridPosition + new Vector3Int(x, y, 0);

                // Skip if this position doesn't have a block
                if (!blockMap.ContainsKey(checkPosition))
                    continue;

                // Get the world position of the center of this grid cell
                Vector3 cellCenterWorldPosition = tilemap.GetCellCenterWorld(checkPosition);

                // Calculate distance from the hit world position to this cell's center
                float distance = Vector3.Distance(worldPosition, cellCenterWorldPosition);

                // Update closest position if this one is closer
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPosition = checkPosition;
                }
            }
        }

        return closestPosition;
    }

    public Vector3 GetProjectilePerimeterPosition(
        Vector3 blockWorldPosition,
        Vector2 projectileDirection,
        Vector3 projectileSpawnPosition,
        Vector3Int blockGridPosition
    )
    {
        if (!blockMap.ContainsKey(blockGridPosition))
            return blockWorldPosition;

        // Get the block's bounds in world space
        Vector3 cellSize = tilemap.cellSize;
        Vector3 cellCenter = tilemap.GetCellCenterWorld(blockGridPosition);

        // Calculate the half-size of the cell
        float halfCellSizeX = cellSize.x * 0.5f;
        float halfCellSizeY = cellSize.y * 0.5f;

        // Calculate the block's bounds
        float minX = cellCenter.x - halfCellSizeX;
        float maxX = cellCenter.x + halfCellSizeX;
        float minY = cellCenter.y - halfCellSizeY;
        float maxY = cellCenter.y + halfCellSizeY;

        // Calculate intersection with the block's perimeter
        Vector3 intersectionPoint = CalculateLineRectangleIntersection(
            projectileSpawnPosition,
            projectileDirection,
            minX,
            maxX,
            minY,
            maxY
        );

        return intersectionPoint;
    }

    private Vector3 CalculateLineRectangleIntersection(
        Vector3 lineStart,
        Vector2 direction,
        float minX,
        float maxX,
        float minY,
        float maxY
    )
    {
        // Normalize the direction
        Vector2 normalizedDir = direction.normalized;

        // Calculate intersection with each edge of the rectangle
        Vector3[] intersectionPoints = new Vector3[4];
        int validIntersections = 0;

        // Top edge (y = maxY)
        if (Mathf.Abs(normalizedDir.y) > 0.001f) // Avoid division by zero
        {
            float t = (maxY - lineStart.y) / normalizedDir.y;
            if (t > 0)
            {
                float x = lineStart.x + normalizedDir.x * t;
                if (x >= minX && x <= maxX)
                {
                    intersectionPoints[validIntersections] = new Vector3(x, maxY, lineStart.z);
                    validIntersections++;
                }
            }
        }

        // Bottom edge (y = minY)
        if (Mathf.Abs(normalizedDir.y) > 0.001f)
        {
            float t = (minY - lineStart.y) / normalizedDir.y;
            if (t > 0)
            {
                float x = lineStart.x + normalizedDir.x * t;
                if (x >= minX && x <= maxX)
                {
                    intersectionPoints[validIntersections] = new Vector3(x, minY, lineStart.z);
                    validIntersections++;
                }
            }
        }

        // Right edge (x = maxX)
        if (Mathf.Abs(normalizedDir.x) > 0.001f)
        {
            float t = (maxX - lineStart.x) / normalizedDir.x;
            if (t > 0)
            {
                float y = lineStart.y + normalizedDir.y * t;
                if (y >= minY && y <= maxY)
                {
                    intersectionPoints[validIntersections] = new Vector3(maxX, y, lineStart.z);
                    validIntersections++;
                }
            }
        }

        // Left edge (x = minX)
        if (Mathf.Abs(normalizedDir.x) > 0.001f)
        {
            float t = (minX - lineStart.x) / normalizedDir.x;
            if (t > 0)
            {
                float y = lineStart.y + normalizedDir.y * t;
                if (y >= minY && y <= maxY)
                {
                    intersectionPoints[validIntersections] = new Vector3(minX, y, lineStart.z);
                    validIntersections++;
                }
            }
        }

        // Find the closest intersection point to the line start
        Vector3 closestPoint = lineStart;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < validIntersections; i++)
        {
            float distance = Vector3.Distance(lineStart, intersectionPoints[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = intersectionPoints[i];
            }
        }

        return closestPoint;
    }

    private bool IsBlockOnScreen(Vector3Int gridPosition)
    {
        // Convert grid position to world position
        Vector3 worldPosition = tilemap.GetCellCenterWorld(gridPosition);

        return IsBlockOnScreen(worldPosition);
    }

    public bool IsBlockOnScreen(Vector3 worldPosition)
    {
        if (Camera.main == null)
            return false;

        // Get the camera's viewport bounds in world coordinates
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(worldPosition);

        // Check if the block is within the viewport bounds
        // Add a small margin to account for block size
        float margin = 0.1f;
        bool isOnScreen =
            screenPoint.x >= -margin
            && screenPoint.x <= 1 + margin
            && screenPoint.y >= -margin
            && screenPoint.y <= 1 + margin
            && screenPoint.z > 0; // Ensure the block is in front of the camera

        return isOnScreen;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Lava>() != null)
        {
            for (int x = -1; x <= worldWidth; x++)
            {
                Vector3Int position = new Vector3Int(x, lavaLevel, 0);
                if (blockMap.ContainsKey(position))
                {
                    DestroyBlock(position);
                }
            }

            lavaLevel++;
        }
    }
}
