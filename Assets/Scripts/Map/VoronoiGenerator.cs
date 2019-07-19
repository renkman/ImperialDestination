﻿using Assets.Scripts.Map;
using Assets.Scripts.Organization;
using Assets.Scripts.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoronoiEngine;
using VoronoiEngine.Elements;
using VoronoiEngine.Structures;

public class VoronoiGenerator : MonoBehaviour
{
    // Unity fields
    public GameObject HexTile;

    public GameObject MapStartPoint;

    public GameObject Camera;

    public GameObject Province;

    public int Height = 1;

    public int Width = 1;

    public int Regions = 1;

    public int MajorCountries = 7;

    public int MinorCountries = 16;

    public int ProvincesMajorCountries = 8;

    public int ProvincesMinorCountries = 4;

    public List<Color> TerrainColorMapping;
    
    // Private fields
    private GameObject _mapObject;

    private HexGrid _hexGrid;

    private HexMap _map;

    private TileTerrainTypeMap _terrainMap;

    private ICollection<Position> _lines;

    private ICollection<Province> _regions;

    private int _landRegionCount;

    // Start is called before the first frame update
    void Start()
    {
        _mapObject = new GameObject("Map");
        _hexGrid = new HexGrid(Height, Width, HexTile);
        _map = new HexMap(Height, Width);
        _landRegionCount = MajorCountries * ProvincesMajorCountries + MinorCountries * ProvincesMinorCountries;

        var voronoiMap = GenerateMap();
        var points = voronoiMap.Where(g => g is Site).Select(s => s.Point).ToList();
        _regions = DetectRegions(points);
        SetContinents();
        SkinMap();
    }

    private VoronoiMap GenerateMap()
    {
        _terrainMap = new TileTerrainTypeMap(Width, Height);

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                _terrainMap.Add(TileTerrainType.Water, x, y);
            }
        }
        CreateMap();

        var voronoiFactory = new VoronoiFactory();
        var voronoiMap = voronoiFactory.CreateVoronoiMap(Height - 1, Width - 1, Regions);
        _lines = voronoiMap.Where(g => g is HalfEdge).Cast<HalfEdge>().SelectMany(
            edge =>
            {
                var start = new Position(edge.Point.XInt, edge.Point.YInt);
                var end = new Position(edge.EndPoint.XInt, edge.EndPoint.YInt);
                var line = _map.DrawLine(start, end);
                return line;
            }).ToList();
        return voronoiMap;
    }

    private void CreateMap()
    {
        for (var y = 0; y < _hexGrid.Height; y++)
        {
            for (var x = 0; x < _hexGrid.Width; x++)
            {
                var terrain = _terrainMap.Get(x, y);
                var position = _hexGrid.Get(x, y);
                if (!terrain.HasValue)
                    continue;

                var tile = CreateTile(terrain.Value, position, x, y);
                tile.transform.SetParent(_mapObject.transform);
            }
        }
    }

    private GameObject CreateTile(TileTerrainType type, Vector3 position, int x, int y)
    {
        var hexTile = Instantiate(HexTile, position, MapStartPoint.transform.rotation);
        var tile = hexTile.GetComponent<Tile>();
        tile.TileTerrainType = type;
        tile.Position.X = x;
        tile.Position.Y = y;
        _map.AddTile(x, y, tile);
        return hexTile;
    }

    private void SkinMap()
    {
        var tiles = _mapObject.transform.GetComponentsInChildren<Tile>().ToList();
        tiles.ForEach(t =>
        {            
            t.SetColor(TerrainColorMapping[(int)t.TileTerrainType]);
            t.ResetSelectionColor();
        });
    }

    private ICollection<Province> DetectRegions(ICollection<Point> points) =>
        points.OrderBy(p => p.X * Height.CountDigits() * 10 + p.Y).Select((point, index) =>
            {
                var tile = _map.GetTile(point.XInt, point.YInt);
                if (tile == null)
                    Debug.LogError($"Tile is NULL at X: {point.XInt}, Y: {point.YInt} (X: {point.X}, Y: {point.Y})");
                var provinceContainer = Instantiate(Province);
                var province = provinceContainer.GetComponent<Province>();
                province.Name = $"Region {index}";
                FillRegion(province, tile);
                province.DrawBorder(_map);
                return province;
            })
        .GroupBy(p => p.HexTiles.OrderBy(t => t.Position.X * Height.CountDigits() * 10 + t.Position.Y).First().Position)
        .OrderBy(p => p.Key.X * Height.CountDigits() * 10 + p.Key.Y)
        .Select(p => p.Single()).ToList();
    
    private void FillRegion(Province province, Tile start)
    {
        var tileStack = new Stack<Tile>();
        tileStack.Push(start);

        while (tileStack.Count > 0)
        {
            var tile = tileStack.Pop();
            if (province.HexTiles.Contains(tile))
                continue;

            if (tile == null)
            {
                Debug.LogError($"Tile is null, tileStack.Count is {tileStack.Count}, province is {province.Name}");
            }

            province.AddHexTile(tile);

            if (_lines.Contains(tile.Position))
                continue;

            foreach (var neighbour in _map.GetNeighboursWithDirection(tile))
            {
                if(neighbour.Neighbour == null)
                    Debug.LogError($"Neighbour is null, tile is {tile}, tileStack.Count is {tileStack.Count}, province is {province.Name}");

                if (neighbour.Neighbour.Province == null)
                {
                    tileStack.Push(neighbour.Neighbour);
                }
            }
        }
    }

    private void SetContinents()
    {
        //var color = new Color(1, 1, 1, 1);
        //var step = 1f / _regions.Count;
        var majorCountries = MajorCountries;
        var regions = _regions.Where(p => !p.HexTiles.Any(h => h.Position.X == 0 || h.Position.Y == 0 || h.Position.X == Width - 1 || h.Position.Y == Height - 1))
            .ToDictionary(p => p.HexTiles.Select(t => t.Position).First(), p => p);

        var count = 0;
        foreach (var position in regions.Keys)
        {
            if (majorCountries == 0)
                break;

            var region = regions[position];
            //Debug.Log($"Index: {position.X * Height.CountDigits() * 10 + position.Y}: {province.Name}, {position}");
         
            foreach(var tile in region.HexTiles)
            {
                tile.TileTerrainType = TileTerrainType.Plain;
                //tile.SetColor(color);
            }
            //color = new Color(color.r - step, color.g - step, color.b - step, 1);
            if(++count % ProvincesMajorCountries == 0)
                majorCountries--;
        }
    }
}
