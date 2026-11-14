using System;
using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

public enum Biome { Meadow, Forest, Waste, Junk }

/// <summary>
/// Cenário infinito e sempre igual para a mesma semente: ladrilhos por bioma (campo, floresta, terra arrasada,
/// ferro-velho), estradas em grade e objetos espalhados por blocos (árvores, pedras, carcaças...) que bloqueiam
/// a van e os inimigos. Os blocos são criados perto da van e descartados quando ela se afasta.
/// </summary>
public partial class Scenery : Node2D
{
    private const int Tile = 16;
    private const float Cell = 36f;
    private const int CellsPerChunk = 4;
    private const float ChunkSize = Cell * CellsPerChunk;
    private const float LoadRange = 380f;
    private const float UnloadRange = 470f;
    private const float RoadSpacing = 1100f;
    private const float RoadHalf = 24f;
    private const float StartClear = 90f;
    private const float VanHitRadius = 7f;

    private readonly struct Collider
    {
        public readonly Vector2 Center;
        public readonly float Radius;
        public Collider(Vector2 center, float radius) { Center = center; Radius = radius; }
    }

    private sealed class Chunk
    {
        public readonly List<Collider> Colliders = new();
        public readonly List<Sprite2D> Sprites = new();
    }

    private sealed class Kind
    {
        public string Texture;
        public bool BaseAnchor;
        public bool Rotate;
        public (Vector2 offset, float radius)[] Colliders = Array.Empty<(Vector2, float)>();
    }

    private static readonly Dictionary<string, Kind> Kinds = new()
    {
        ["pine"] = new Kind { Texture = "tree_pine", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -2), 4.5f) } },
        ["round"] = new Kind { Texture = "tree_round", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -2), 4.5f) } },
        ["dead"] = new Kind { Texture = "tree_dead", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -2), 3.5f) } },
        ["stump"] = new Kind { Texture = "stump", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -3), 4f) } },
        ["log"] = new Kind { Texture = "log", BaseAnchor = true, Colliders = new[] { (new Vector2(-7, -4), 4f), (new Vector2(7, -4), 4f) } },
        ["rock"] = new Kind { Texture = "rock", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -5), 6f) } },
        ["rock_small"] = new Kind { Texture = "rock_small", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -3), 4f) } },
        ["wreck_a"] = new Kind { Texture = "wreck_a", Rotate = true, Colliders = new[] { (new Vector2(0, -6), 8f), (new Vector2(0, 6), 8f) } },
        ["wreck_b"] = new Kind { Texture = "wreck_b", Rotate = true, Colliders = new[] { (new Vector2(0, -6), 8f), (new Vector2(0, 6), 8f) } },
        ["barrel"] = new Kind { Texture = "barrel", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -4), 4f) } },
        ["tires"] = new Kind { Texture = "tires", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -3), 5f) } },
        ["crate"] = new Kind { Texture = "crate", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -5), 6f) } },
        ["lamp"] = new Kind { Texture = "lamp", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -2), 2f) } },
        ["sign"] = new Kind { Texture = "sign96", BaseAnchor = true, Colliders = new[] { (new Vector2(0, -2), 2.5f) } },
        ["bush"] = new Kind { Texture = "bush", BaseAnchor = true },
    };

    private static readonly (string kind, float weight)[] ForestMix =
        { ("pine", 45), ("round", 25), ("bush", 12), ("stump", 5), ("log", 4), ("rock_small", 5) };
    private static readonly (string kind, float weight)[] MeadowMix =
        { ("round", 30), ("pine", 10), ("bush", 22), ("rock", 8), ("stump", 5), ("log", 4), ("rock_small", 6) };
    private static readonly (string kind, float weight)[] WasteMix =
        { ("dead", 32), ("rock", 24), ("rock_small", 16), ("wreck_a", 5), ("wreck_b", 5), ("log", 5), ("stump", 4) };
    private static readonly (string kind, float weight)[] JunkMix =
        { ("wreck_a", 16), ("wreck_b", 16), ("barrel", 22), ("tires", 14), ("crate", 14), ("rock", 6), ("lamp", 4), ("rock_small", 5) };

    private readonly int _seed;
    private readonly Node2D _world;
    private readonly Dictionary<Vector2I, Chunk> _chunks = new();
    private readonly List<Vector2I> _drop = new();
    private Texture2D _tiles;
    private Vector2 _center;

    public Scenery(Node2D world, int seed)
    {
        _world = world;
        _seed = seed;
        ZIndex = -10;
    }

    public override void _Ready()
    {
        _tiles = Assets.Texture("tiles");
    }

    // ---------------------------------------------------------------- Ruído e biomas

    private static uint Mix(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393) + (uint)(y * 668265263) + (uint)(seed * 1274126177);
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    private static float Hash01(int x, int y, int seed) => (Mix(x, y, seed) & 0xFFFFFF) / 16777216f;

    private float Noise(float x, float y, int salt)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float a = Hash01(x0, y0, _seed + salt), b = Hash01(x0 + 1, y0, _seed + salt);
        float c = Hash01(x0, y0 + 1, _seed + salt), d = Hash01(x0 + 1, y0 + 1, _seed + salt);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    public Biome BiomeAt(float x, float y)
    {
        float n = 0.65f * Noise(x / 520f, y / 520f, 1) + 0.35f * Noise(x / 200f, y / 200f, 2);
        float distance = Mathf.Sqrt(x * x + y * y);
        n = Mathf.Lerp(n, 0.48f, Mathf.Clamp(1f - distance / 320f, 0f, 1f));

        if (n >= 0.58f) return Biome.Forest;
        if (distance > 320f && n < 0.55f && Noise(x / 340f, y / 340f, 3) > 0.7f) return Biome.Junk;
        return n < 0.36f ? Biome.Waste : Biome.Meadow;
    }

    public string AreaName(Vector2 position)
    {
        if (RoadDistance(position.X, position.Y) < RoadHalf) return "ESTRADA";
        return BiomeAt(position.X, position.Y) switch
        {
            Biome.Forest => "FLORESTA",
            Biome.Waste => "TERRA ARRASADA",
            Biome.Junk => "FERRO-VELHO",
            _ => "CAMPO ABERTO",
        };
    }

    // ---------------------------------------------------------------- Estradas

    private float HorizontalRoadY(int index)
    {
        float y = index * RoadSpacing + (Hash01(index, 11, _seed) - 0.5f) * 300f;
        return Mathf.Round(y / Tile) * Tile + Tile / 2f;
    }

    private float VerticalRoadX(int index)
    {
        float x = index * RoadSpacing + (Hash01(index, 29, _seed) - 0.5f) * 300f;
        return Mathf.Round(x / Tile) * Tile + Tile / 2f;
    }

    private float DistanceToHorizontal(float y)
    {
        int j = Mathf.RoundToInt(y / RoadSpacing);
        float best = float.MaxValue;
        for (int k = j - 1; k <= j + 1; k++) best = Mathf.Min(best, Mathf.Abs(y - HorizontalRoadY(k)));
        return best;
    }

    private float DistanceToVertical(float x)
    {
        int j = Mathf.RoundToInt(x / RoadSpacing);
        float best = float.MaxValue;
        for (int k = j - 1; k <= j + 1; k++) best = Mathf.Min(best, Mathf.Abs(x - VerticalRoadX(k)));
        return best;
    }

    private float RoadDistance(float x, float y) => Mathf.Min(DistanceToHorizontal(y), DistanceToVertical(x));

    // ---------------------------------------------------------------- Desenho do chão

    public void Update(Vector2 center)
    {
        _center = center;
        int cx0 = Mathf.FloorToInt((center.X - LoadRange) / ChunkSize), cx1 = Mathf.FloorToInt((center.X + LoadRange) / ChunkSize);
        int cy0 = Mathf.FloorToInt((center.Y - LoadRange) / ChunkSize), cy1 = Mathf.FloorToInt((center.Y + LoadRange) / ChunkSize);
        for (int cy = cy0; cy <= cy1; cy++)
        {
            for (int cx = cx0; cx <= cx1; cx++)
            {
                var key = new Vector2I(cx, cy);
                if (!_chunks.ContainsKey(key)) _chunks[key] = Generate(key);
            }
        }

        _drop.Clear();
        foreach (Vector2I key in _chunks.Keys)
        {
            float mx = (key.X + 0.5f) * ChunkSize, my = (key.Y + 0.5f) * ChunkSize;
            if (Mathf.Abs(mx - center.X) > UnloadRange + ChunkSize / 2f || Mathf.Abs(my - center.Y) > UnloadRange + ChunkSize / 2f)
                _drop.Add(key);
        }
        foreach (Vector2I key in _drop)
        {
            foreach (Sprite2D sprite in _chunks[key].Sprites) sprite.QueueFree();
            _chunks.Remove(key);
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_tiles == null) return;

        int tx0 = Mathf.FloorToInt((_center.X - 176f) / Tile), tx1 = Mathf.FloorToInt((_center.X + 176f) / Tile);
        int ty0 = Mathf.FloorToInt((_center.Y - 106f) / Tile), ty1 = Mathf.FloorToInt((_center.Y + 106f) / Tile);

        for (int ty = ty0; ty <= ty1; ty++)
        {
            for (int tx = tx0; tx <= tx1; tx++)
            {
                float px = tx * Tile + Tile / 2f, py = ty * Tile + Tile / 2f;
                int index = TileIndex(tx, ty, px, py);
                DrawTextureRectRegion(_tiles, new Rect2(tx * Tile, ty * Tile, Tile, Tile),
                    new Rect2(index % 8 * Tile, index / 8 * Tile, Tile, Tile));
            }
        }

        DrawRoadMarks(tx0 * Tile, tx1 * Tile + Tile, ty0 * Tile, ty1 * Tile + Tile);
    }

    private int TileIndex(int tx, int ty, float px, float py)
    {
        uint r = Mix(tx, ty, _seed + 5);
        if (RoadDistance(px, py) <= RoadHalf) return 8 + (int)(r % 3);

        // Borda dos biomas irregular: sorteia um deslocamento antes de decidir.
        float jx = ((r & 0xFF) / 255f - 0.5f) * 28f, jy = (((r >> 8) & 0xFF) / 255f - 0.5f) * 28f;
        switch (BiomeAt(px + jx, py + jy))
        {
            case Biome.Forest: return 4 + (int)((r >> 16) % 2);
            case Biome.Waste: return 6 + (int)((r >> 16) % 2);
            case Biome.Junk: return 11 + (int)((r >> 16) % 2);
            default: return (int)((r >> 16) % 4);
        }
    }

    private void DrawRoadMarks(float x0, float x1, float y0, float y1)
    {
        var dash = new Color(0.82f, 0.74f, 0.32f);
        var edge = new Color(0.55f, 0.56f, 0.6f, 0.8f);

        int j0 = Mathf.RoundToInt((y0 + y1) / 2f / RoadSpacing);
        for (int j = j0 - 1; j <= j0 + 1; j++)
        {
            float cy = HorizontalRoadY(j);
            if (cy < y0 - RoadHalf || cy > y1 + RoadHalf) continue;
            for (float x = Mathf.Floor(x0 / 24f) * 24f; x < x1; x += 24f)
            {
                if (DistanceToVertical(x + 5f) > RoadHalf) DrawRect(new Rect2(x, cy - 1f, 10f, 2f), dash);
            }
            for (float x = x0; x < x1; x += Tile)
            {
                if (DistanceToVertical(x + 8f) <= RoadHalf) continue;
                DrawRect(new Rect2(x, cy - RoadHalf + 1f, Tile, 1f), edge);
                DrawRect(new Rect2(x, cy + RoadHalf - 2f, Tile, 1f), edge);
            }
        }

        int i0 = Mathf.RoundToInt((x0 + x1) / 2f / RoadSpacing);
        for (int i = i0 - 1; i <= i0 + 1; i++)
        {
            float cx = VerticalRoadX(i);
            if (cx < x0 - RoadHalf || cx > x1 + RoadHalf) continue;
            for (float y = Mathf.Floor(y0 / 24f) * 24f; y < y1; y += 24f)
            {
                if (DistanceToHorizontal(y + 5f) > RoadHalf) DrawRect(new Rect2(cx - 1f, y, 2f, 10f), dash);
            }
            for (float y = y0; y < y1; y += Tile)
            {
                if (DistanceToHorizontal(y + 8f) <= RoadHalf) continue;
                DrawRect(new Rect2(cx - RoadHalf + 1f, y, 1f, Tile), edge);
                DrawRect(new Rect2(cx + RoadHalf - 2f, y, 1f, Tile), edge);
            }
        }
    }

    // ---------------------------------------------------------------- Geração dos blocos

    private Chunk Generate(Vector2I key)
    {
        var chunk = new Chunk();
        for (int iy = 0; iy < CellsPerChunk; iy++)
        {
            for (int ix = 0; ix < CellsPerChunk; ix++)
            {
                int gx = key.X * CellsPerChunk + ix, gy = key.Y * CellsPerChunk + iy;
                var rng = new Random(unchecked((int)Mix(gx, gy, _seed + 77)));
                var point = new Vector2(gx * Cell + 5f + (float)rng.NextDouble() * (Cell - 10f), gy * Cell + 5f + (float)rng.NextDouble() * (Cell - 10f));
                PlaceCell(chunk, rng, point);
                PlaceDecor(chunk, rng, gx, gy);
            }
        }
        return chunk;
    }

    private void PlaceCell(Chunk chunk, Random rng, Vector2 point)
    {
        if (point.Length() < StartClear) return;

        float road = RoadDistance(point.X, point.Y);
        double roll = rng.NextDouble();
        if (road < RoadHalf - 5f)
        {
            // Carro abandonado na pista, alinhado com ela.
            if (roll < 0.045)
            {
                bool horizontal = DistanceToHorizontal(point.Y) < DistanceToVertical(point.X);
                float angle = (horizontal ? Mathf.Pi / 2f : 0f) + ((float)rng.NextDouble() - 0.5f) * 0.4f;
                Spawn(chunk, rng.NextDouble() < 0.5 ? "wreck_a" : "wreck_b", point, angle);
            }
            return;
        }
        if (road < RoadHalf + 14f)
        {
            if (roll < 0.035) Spawn(chunk, "lamp", point, 0f);
            else if (roll < 0.045) Spawn(chunk, "sign", point, 0f);
            return;
        }

        Biome biome = BiomeAt(point.X, point.Y);
        (double density, (string kind, float weight)[] mix) = biome switch
        {
            Biome.Forest => (0.66, ForestMix),
            Biome.Waste => (0.15, WasteMix),
            Biome.Junk => (0.58, JunkMix),
            _ => (0.17, MeadowMix),
        };
        if (roll >= density) return;

        float total = 0f;
        foreach ((string _, float weight) in mix) total += weight;
        float pick = (float)rng.NextDouble() * total;
        foreach ((string kind, float weight) in mix)
        {
            pick -= weight;
            if (pick > 0f) continue;
            float angle = 0f;
            if (Kinds[kind].Rotate)
                angle = (rng.NextDouble() < 0.5 ? 0f : Mathf.Pi / 2f) + ((float)rng.NextDouble() - 0.5f) * 0.9f;
            Spawn(chunk, kind, point, angle);
            return;
        }
    }

    private void PlaceDecor(Chunk chunk, Random rng, int gx, int gy)
    {
        for (int n = 0; n < 2; n++)
        {
            var point = new Vector2(gx * Cell + (float)rng.NextDouble() * Cell, gy * Cell + (float)rng.NextDouble() * Cell);
            if (RoadDistance(point.X, point.Y) < RoadHalf + 4f) continue;

            double roll = rng.NextDouble();
            string texture = null;
            switch (BiomeAt(point.X, point.Y))
            {
                case Biome.Meadow: texture = roll < 0.10 ? "flowers" : roll < 0.36 ? "tuft" : null; break;
                case Biome.Forest: texture = roll < 0.16 ? "tuft" : null; break;
                case Biome.Waste: texture = roll < 0.05 ? "bones" : roll < 0.12 ? "tuft" : null; break;
            }
            if (texture == null) continue;

            var sprite = new Sprite2D { Texture = Assets.Texture(texture), Position = point, ZIndex = 0 };
            _world.AddChild(sprite);
            chunk.Sprites.Add(sprite);
        }
    }

    private void Spawn(Chunk chunk, string kindName, Vector2 position, float angle)
    {
        Kind kind = Kinds[kindName];
        Texture2D texture = Assets.Texture(kind.Texture);
        var sprite = new Sprite2D { Texture = texture, Position = position, ZIndex = 1, Rotation = kind.Rotate ? angle : 0f };
        if (kind.BaseAnchor) sprite.Offset = new Vector2(0f, -texture.GetHeight() / 2f);
        _world.AddChild(sprite);
        chunk.Sprites.Add(sprite);

        foreach ((Vector2 offset, float radius) in kind.Colliders)
            chunk.Colliders.Add(new Collider(position + (kind.Rotate ? offset.Rotated(angle) : offset), radius));
    }

    // ---------------------------------------------------------------- Colisão

    private bool TryChunk(int cx, int cy, out Chunk chunk)
    {
        return _chunks.TryGetValue(new Vector2I(cx, cy), out chunk);
    }

    /// <summary>Empurra a van para fora de árvores e pedras. Devolve a velocidade da batida (0 se não bateu).</summary>
    public float CollideVan(Van van)
    {
        float impact = 0f;
        Vector2 position = van.Position;
        float reach = VanHitRadius + 12f;
        int cx0 = Mathf.FloorToInt((position.X - reach) / ChunkSize), cx1 = Mathf.FloorToInt((position.X + reach) / ChunkSize);
        int cy0 = Mathf.FloorToInt((position.Y - reach) / ChunkSize), cy1 = Mathf.FloorToInt((position.Y + reach) / ChunkSize);

        for (int cy = cy0; cy <= cy1; cy++)
        {
            for (int cx = cx0; cx <= cx1; cx++)
            {
                if (!TryChunk(cx, cy, out Chunk chunk)) continue;
                foreach (Collider collider in chunk.Colliders)
                {
                    Vector2 away = position - collider.Center;
                    float distance = away.Length();
                    float limit = collider.Radius + VanHitRadius;
                    if (distance >= limit) continue;

                    Vector2 normal = distance > 0.001f ? away / distance : Vector2.Up;
                    position += normal * (limit - distance);
                    float into = van.Velocity.Dot(normal);
                    if (into < 0f)
                    {
                        impact = Mathf.Max(impact, -into);
                        van.Velocity -= normal * into * 1.3f;
                        van.Velocity *= 0.9f;
                    }
                }
            }
        }

        van.Position = position;
        return impact;
    }

    /// <summary>Tira um inimigo de dentro de obstáculos (eles contornam as árvores deslizando).</summary>
    public Vector2 PushOut(Vector2 position, float radius)
    {
        int cx0 = Mathf.FloorToInt((position.X - radius - 10f) / ChunkSize), cx1 = Mathf.FloorToInt((position.X + radius + 10f) / ChunkSize);
        int cy0 = Mathf.FloorToInt((position.Y - radius - 10f) / ChunkSize), cy1 = Mathf.FloorToInt((position.Y + radius + 10f) / ChunkSize);

        for (int cy = cy0; cy <= cy1; cy++)
        {
            for (int cx = cx0; cx <= cx1; cx++)
            {
                if (!TryChunk(cx, cy, out Chunk chunk)) continue;
                foreach (Collider collider in chunk.Colliders)
                {
                    Vector2 away = position - collider.Center;
                    float distance = away.Length();
                    float limit = collider.Radius + radius * 0.8f;
                    if (distance >= limit) continue;
                    Vector2 normal = distance > 0.001f ? away / distance : Vector2.Up;
                    position += normal * (limit - distance);
                }
            }
        }
        return position;
    }
}
