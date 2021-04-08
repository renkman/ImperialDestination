﻿using Assets.Contracts.Map;
using Assets.Contracts.Organization;
using Assets.Scripts;
using Assets.Scripts.Map;
using Assets.Scripts.Organization;
using Helpers;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using VoronoiEngine;
using VoronoiEngine.Elements;
using VoronoiEngine.Structures;

namespace Tests
{
    public class ProvinceFactoryTests
    {
        [Test]
        public void TestRandom()
        {
            var collection = new List<int>();
            var index = UnityEngine.Random.Range(0, collection.Count);
            Assert.AreEqual(0, index);
            var result = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => result = collection[index]);
        }

        [UnityTest]
        public IEnumerator CreateProvinces_WithLinePositionsAndHexMap_GeneratesProvinces()
        {
            var map = HexMapBuilder.New.WithWidth(13).WithHeight(9).Build();

            var lines = Enumerable.Range(0, 13).Select(i => new Position(i, 0))
                .Union(Enumerable.Range(0, 13).Select(i => new Position(i, 4)))
                .Union(Enumerable.Range(0, 13).Select(i => new Position(i, 8)))
                .Union(Enumerable.Range(0, 9).Select(i => new Position(0, i)))
                .Union(Enumerable.Range(0, 9).Select(i => new Position(4, i)))
                .Union(Enumerable.Range(0, 9).Select(i => new Position(8, i)))
                .Union(Enumerable.Range(0, 9).Select(i => new Position(12, i)))
                .Distinct().ToList();

            Assert.AreEqual(63, lines.Count);

            var provinceHandle = Addressables.LoadAssetAsync<GameObject>("Province");
            yield return provinceHandle;

            Assert.IsTrue(provinceHandle.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, provinceHandle.Status);
            Assert.IsNotNull(provinceHandle.Result);

            var tileHandle = Addressables.LoadAssetAsync<GameObject>("HexTile");
            yield return tileHandle;

            Assert.IsTrue(tileHandle.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, tileHandle.Status);
            Assert.IsNotNull(tileHandle.Result);

            CreateMap(map, tileHandle.Result);
            yield return null;

            var organizationFactory = new OrganisationFactory();
            var sites = new List<Point> { new Point(2, 2), new Point(6, 2), new Point(10, 2), new Point(2, 6), new Point(6, 6), new Point(10, 6) };

            var provinceFactory = new ProvinceFactory(map, lines, UnityEngine.Object.Instantiate, provinceHandle.Result, organizationFactory);
            var result = provinceFactory.CreateProvinces(sites);

            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.Count);

            var provinceless = map.Where(t => t.Province == null).ToList();

            Debug.Log(provinceless.Count);
            foreach (var tile in provinceless)
                Debug.Log(tile);

            Assert.False(provinceless.Any());

            foreach(var province in result)
            {
                var neighbours = province.GetNeighbours(map);
                Debug.Log($"Neighbours of {province}: {string.Join(", ", neighbours.Select(n => n))}");
            }

            LogMap(map);

            var provinces = result.ToList();
            Assert.AreEqual(new[] { "Region 1", "Region 2" }, provinces[0].GetNeighbours(map).Select(p => p.Name).OrderBy(n => n).ToArray());
            Assert.AreEqual(new[] { "Region 0", "Region 2", "Region 3", "Region 4" }, provinces[1].GetNeighbours(map).Select(p => p.Name).OrderBy(n => n).ToArray());
            Assert.AreEqual(new[] { "Region 0", "Region 1", "Region 4" }, provinces[2].GetNeighbours(map).Select(p => p.Name).OrderBy(n => n).ToArray());
            Assert.AreEqual(new[] { "Region 1", "Region 4", "Region 5" }, provinces[3].GetNeighbours(map).Select(p => p.Name).OrderBy(n => n).ToArray());
            Assert.AreEqual(new[] { "Region 1", "Region 2", "Region 3", "Region 5" }, provinces[4].GetNeighbours(map).Select(p => p.Name).OrderBy(n => n).ToArray());
            Assert.AreEqual(new[] { "Region 3", "Region 4   " }, provinces[5].GetNeighbours(map).Select(p => p.Name).OrderBy(n => n).ToArray());

            // Neighbours of Region 0: Region 1, Region 2
            // Neighbours of Region 1: Region 3, Region 4, Region 2, Region 0
            // Neighbours of Region 2: Region 0, Region 1, Region 4
            // Neighbours of Region 3: Region 5, Region 4, Region 1
            // Neighbours of Region 4: Region 2, Region 0, Region 1, Region 3, Region 5
            // Neighbours of Region 5: Region 4, Region 1, Region 3


            //  0 0 0 0 0 1 1 1 1 3 3 3 3
            //   0 0 0 0 0 1 1 1 1 3 3 3 3
            //  0 0 0 0 0 1 1 1 1 3 3 3 3
            //   0 0 0 0 0 1 1 1 1 3 3 3 3
            //  0 0 0 0 0 1 1 1 1 3 3 3 3
            //   2 2 2 2 2 4 4 4 4 5 5 5 5
            //  2 2 2 2 2 4 4 4 4 5 5 5 5
            //   2 2 2 2 2 4 4 4 4 5 5 5 5
            //  2 2 2 2 2 4 4 4 4 5 5 5 5
        }

        [UnityTest]
        public IEnumerator CreateProvinces_WithComplexLinePositionsAndHexMap_GeneratesProvinces()
        {
            var provinceHandle = Addressables.LoadAssetAsync<GameObject>("Province");
            yield return provinceHandle;

            Assert.IsTrue(provinceHandle.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, provinceHandle.Status);
            Assert.IsNotNull(provinceHandle.Result);

            var tileHandle = Addressables.LoadAssetAsync<GameObject>("HexTile");
            yield return tileHandle;

            Assert.IsTrue(tileHandle.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, tileHandle.Status);
            Assert.IsNotNull(tileHandle.Result);

            //var lines = new List<Position> { new Position(17, 19), new Position(17, 20), new Position(17, 21), new Position(17, 22), new Position(17, 23), new Position(17, 24), new Position(16, 25), new Position(17, 26), new Position(16, 27), new Position(17, 28), new Position(16, 29), new Position(16, 30), new Position(16, 31), new Position(16, 32), new Position(16, 33), new Position(16, 34), new Position(15, 35), new Position(16, 36), new Position(15, 37), new Position(16, 38), new Position(15, 39), new Position(15, 40), new Position(15, 41), new Position(15, 42), new Position(15, 43), new Position(15, 44), new Position(14, 45), new Position(15, 46), new Position(14, 47), new Position(15, 48), new Position(14, 49), new Position(14, 50), new Position(14, 51), new Position(14, 52), new Position(14, 53), new Position(14, 54), new Position(13, 55), new Position(14, 56), new Position(13, 57), new Position(14, 58), new Position(13, 59), new Position(13, 60), new Position(13, 61), new Position(13, 62), new Position(13, 63), new Position(13, 64), new Position(12, 65), new Position(13, 66), new Position(12, 67), new Position(13, 68), new Position(12, 69), new Position(12, 70), new Position(12, 71), new Position(12, 72), new Position(12, 73), new Position(12, 74), new Position(11, 75), new Position(12, 76), new Position(11, 77), new Position(12, 78), new Position(11, 79), new Position(11, 80), new Position(11, 81), new Position(11, 82), new Position(10, 83), new Position(11, 84), new Position(10, 85), new Position(11, 86), new Position(10, 87), new Position(10, 88), new Position(10, 89), new Position(10, 90), new Position(10, 91), new Position(10, 92), new Position(9, 93), new Position(10, 94), new Position(9, 95), new Position(10, 96), new Position(9, 97), new Position(9, 98), new Position(9, 99), new Position(9, 100), new Position(9, 101), new Position(9, 102), new Position(8, 103), new Position(9, 104), new Position(8, 105), new Position(9, 106), new Position(8, 107), new Position(8, 108), new Position(8, 109), new Position(8, 110), new Position(8, 111), new Position(8, 112), new Position(7, 113), new Position(8, 114), new Position(7, 115), new Position(8, 116), new Position(7, 117), new Position(7, 118), new Position(7, 119), new Position(7, 120), new Position(7, 121), new Position(7, 122), new Position(6, 123), new Position(7, 124), new Position(6, 125), new Position(7, 126), new Position(6, 127), new Position(6, 128), new Position(6, 129), new Position(6, 130), new Position(6, 131), new Position(6, 132), new Position(5, 133), new Position(6, 134), new Position(5, 135), new Position(6, 136), new Position(5, 137), new Position(5, 138), new Position(5, 139), new Position(5, 140), new Position(5, 141), new Position(5, 142), new Position(8, 10), new Position(7, 11), new Position(8, 12), new Position(7, 13), new Position(8, 14), new Position(7, 15), new Position(8, 16), new Position(7, 17), new Position(8, 18), new Position(7, 19), new Position(8, 20), new Position(7, 21), new Position(8, 22), new Position(7, 23), new Position(8, 24), new Position(7, 25), new Position(8, 26), new Position(7, 27), new Position(8, 28), new Position(7, 29), new Position(8, 30), new Position(7, 31), new Position(8, 32), new Position(7, 33), new Position(7, 34), new Position(7, 35), new Position(7, 36), new Position(7, 37), new Position(7, 38), new Position(7, 39), new Position(7, 40), new Position(7, 41), new Position(7, 42), new Position(7, 43), new Position(7, 44), new Position(7, 45), new Position(7, 46), new Position(7, 47), new Position(7, 48), new Position(7, 49), new Position(7, 50), new Position(7, 51), new Position(7, 52), new Position(7, 53), new Position(7, 54), new Position(6, 55), new Position(7, 56), new Position(6, 57), new Position(7, 58), new Position(6, 59), new Position(7, 60), new Position(6, 61), new Position(7, 62), new Position(6, 63), new Position(7, 64), new Position(6, 65), new Position(7, 66), new Position(6, 67), new Position(7, 68), new Position(6, 69), new Position(7, 70), new Position(6, 71), new Position(7, 72), new Position(6, 73), new Position(7, 74), new Position(6, 75), new Position(6, 76), new Position(6, 77), new Position(6, 78), new Position(6, 79), new Position(6, 80), new Position(6, 81), new Position(6, 82), new Position(6, 83), new Position(6, 84), new Position(6, 85), new Position(6, 86), new Position(6, 87), new Position(6, 88), new Position(6, 89), new Position(6, 90), new Position(6, 91), new Position(6, 92), new Position(6, 93), new Position(6, 94), new Position(6, 95), new Position(6, 96), new Position(6, 97), new Position(6, 98), new Position(5, 99), new Position(6, 100), new Position(5, 101), new Position(6, 102), new Position(5, 103), new Position(6, 104), new Position(5, 105), new Position(6, 106), new Position(5, 107), new Position(6, 108), new Position(5, 109), new Position(6, 110), new Position(5, 111), new Position(6, 112), new Position(5, 113), new Position(6, 114), new Position(5, 115), new Position(6, 116), new Position(5, 117), new Position(6, 118), new Position(5, 119), new Position(6, 120), new Position(5, 121), new Position(5, 122), new Position(5, 123), new Position(5, 124), new Position(5, 125), new Position(5, 126), new Position(5, 127), new Position(5, 128), new Position(5, 129), new Position(5, 130), new Position(5, 131), new Position(5, 132), new Position(5, 133), new Position(5, 134), new Position(5, 135), new Position(5, 136), new Position(5, 137), new Position(5, 138), new Position(5, 139), new Position(5, 140), new Position(5, 141), new Position(5, 142), new Position(5, 142), new Position(4, 143), new Position(5, 144), new Position(4, 145), new Position(5, 146), new Position(4, 147), new Position(4, 148), new Position(4, 149), new Position(4, 150), new Position(4, 151), new Position(4, 152), new Position(4, 153), new Position(4, 154), new Position(3, 155), new Position(4, 156), new Position(3, 157), new Position(4, 158), new Position(3, 159), new Position(3, 160), new Position(3, 161), new Position(3, 162), new Position(3, 163), new Position(3, 164), new Position(2, 165), new Position(3, 166), new Position(2, 167), new Position(3, 168), new Position(2, 169), new Position(3, 170), new Position(2, 171), new Position(2, 172), new Position(2, 173), new Position(2, 174), new Position(2, 175), new Position(2, 176), new Position(1, 177), new Position(2, 178), new Position(1, 179), new Position(2, 180), new Position(1, 181), new Position(2, 182), new Position(1, 183), new Position(1, 184), new Position(1, 185), new Position(1, 186), new Position(1, 187), new Position(1, 188), new Position(0, 189), new Position(1, 190), new Position(0, 191), new Position(1, 192), new Position(0, 193), new Position(0, 194), new Position(0, 195), new Position(0, 196), new Position(0, 197), new Position(0, 198), new Position(0, 199), new Position(0, 200), new Position(-1, 201), new Position(0, 202), new Position(-1, 203), new Position(0, 204), new Position(-1, 205), new Position(-1, 206), new Position(-1, 207), new Position(-1, 208), new Position(-1, 209), new Position(-1, 210), new Position(-2, 211), new Position(-1, 212), new Position(-2, 213), new Position(-1, 214), new Position(-2, 215), new Position(-1, 216), new Position(-2, 217), new Position(-2, 218), new Position(-2, 219), new Position(-2, 220), new Position(-2, 221), new Position(-2, 222), new Position(-3, 223), new Position(-2, 224), new Position(-3, 225), new Position(-2, 226), new Position(-3, 227), new Position(-2, 228), new Position(-3, 229), new Position(-3, 230), new Position(-3, 231), new Position(-3, 232), new Position(-3, 233), new Position(-3, 234), new Position(-4, 235), new Position(-3, 236), new Position(-4, 237), new Position(-3, 238), new Position(-4, 239), new Position(-4, 240), new Position(-4, 241), new Position(-4, 242), new Position(-4, 243), new Position(-4, 244), new Position(-4, 245), new Position(-4, 246), new Position(-5, 247), new Position(-4, 248), new Position(-5, 249), new Position(-4, 250), new Position(-5, 251), new Position(51, 119), new Position(51, 118), new Position(51, 117), new Position(51, 116), new Position(50, 115), new Position(50, 114), new Position(50, 113), new Position(50, 112), new Position(49, 111), new Position(50, 110), new Position(49, 109), new Position(49, 108), new Position(49, 107), new Position(49, 106), new Position(48, 105), new Position(48, 104), new Position(48, 103), new Position(48, 102), new Position(47, 101), new Position(48, 100), new Position(47, 99), new Position(47, 98), new Position(47, 97), new Position(47, 96), new Position(46, 95), new Position(46, 94), new Position(46, 93), new Position(46, 92), new Position(45, 91), new Position(46, 90), new Position(45, 89), new Position(45, 88), new Position(45, 87), new Position(45, 86), new Position(44, 85), new Position(44, 84), new Position(44, 83), new Position(44, 82), new Position(43, 81), new Position(44, 80), new Position(43, 79), new Position(43, 78), new Position(43, 77), new Position(43, 76), new Position(42, 75), new Position(42, 74), new Position(42, 73), new Position(42, 72), new Position(41, 71), new Position(42, 70), new Position(41, 69), new Position(41, 68), new Position(41, 67), new Position(41, 66), new Position(40, 65), new Position(40, 64), new Position(40, 63), new Position(40, 62), new Position(39, 61), new Position(40, 60), new Position(39, 59), new Position(39, 58), new Position(39, 57), new Position(39, 56), new Position(38, 55), new Position(38, 54), new Position(38, 53), new Position(38, 52), new Position(37, 51), new Position(38, 50), new Position(37, 49), new Position(37, 48), new Position(37, 47), new Position(37, 46), new Position(36, 45), new Position(36, 44), new Position(36, 43), new Position(36, 42), new Position(35, 41), new Position(36, 40), new Position(35, 39), new Position(35, 38), new Position(35, 37), new Position(35, 36), new Position(34, 35), new Position(34, 34), new Position(34, 33), new Position(34, 32), new Position(33, 31), new Position(34, 30), new Position(33, 29), new Position(33, 28), new Position(33, 27), new Position(33, 26), new Position(32, 25), new Position(32, 24), new Position(32, 23), new Position(32, 22), new Position(31, 21), new Position(32, 20), new Position(31, 19), new Position(31, 18), new Position(31, 17), new Position(31, 16), new Position(30, 15), new Position(30, 14), new Position(30, 13), new Position(30, 12), new Position(51, 119), new Position(51, 118), new Position(51, 117), new Position(51, 116), new Position(50, 115), new Position(51, 114), new Position(50, 113), new Position(50, 112), new Position(50, 111), new Position(50, 110), new Position(50, 109), new Position(50, 108), new Position(49, 107), new Position(50, 106), new Position(49, 105), new Position(49, 104), new Position(49, 103), new Position(49, 102), new Position(48, 101), new Position(49, 100), new Position(48, 99), new Position(48, 98), new Position(48, 97), new Position(48, 96), new Position(48, 95), new Position(48, 94), new Position(47, 93), new Position(48, 92), new Position(47, 91), new Position(47, 90), new Position(47, 89), new Position(47, 88), new Position(46, 87), new Position(47, 86), new Position(46, 85), new Position(46, 84), new Position(46, 83), new Position(46, 82), new Position(45, 81), new Position(46, 80), new Position(45, 79), new Position(46, 78), new Position(45, 77), new Position(45, 76), new Position(45, 75), new Position(45, 74), new Position(44, 73), new Position(45, 72), new Position(44, 71), new Position(44, 70), new Position(44, 69), new Position(44, 68), new Position(43, 67), new Position(44, 66), new Position(43, 65), new Position(44, 64), new Position(43, 63), new Position(43, 62), new Position(43, 61), new Position(43, 60), new Position(42, 59), new Position(43, 58), new Position(42, 57), new Position(42, 56), new Position(42, 55), new Position(42, 54), new Position(41, 53), new Position(42, 52), new Position(41, 51), new Position(42, 50), new Position(41, 49), new Position(41, 48), new Position(41, 47), new Position(41, 46), new Position(40, 45), new Position(41, 44), new Position(40, 43), new Position(40, 42), new Position(40, 41), new Position(40, 40), new Position(39, 39), new Position(40, 38), new Position(39, 37), new Position(39, 36), new Position(39, 35), new Position(39, 34), new Position(39, 33), new Position(39, 32), new Position(38, 31), new Position(39, 30), new Position(38, 29), new Position(38, 28), new Position(38, 27), new Position(38, 26), new Position(37, 25), new Position(38, 24), new Position(37, 23), new Position(37, 22), new Position(37, 21), new Position(37, 20), new Position(37, 19), new Position(37, 18), new Position(36, 17), new Position(37, 16), new Position(36, 15), new Position(36, 14), new Position(36, 13), new Position(36, 12), new Position(51, 119), new Position(52, 120), new Position(51, 121), new Position(52, 122), new Position(52, 123), new Position(52, 124), new Position(52, 125), new Position(53, 126), new Position(52, 127), new Position(53, 128), new Position(53, 129), new Position(53, 130), new Position(53, 131), new Position(54, 132), new Position(54, 133), new Position(54, 134), new Position(54, 135), new Position(55, 136), new Position(54, 137), new Position(55, 138), new Position(55, 139), new Position(55, 140), new Position(55, 141), new Position(56, 142), new Position(55, 143), new Position(56, 144), new Position(56, 145), new Position(56, 146), new Position(56, 147), new Position(57, 148), new Position(56, 149), new Position(57, 150), new Position(57, 151), new Position(57, 152), new Position(57, 153), new Position(58, 154), new Position(57, 155), new Position(58, 156), new Position(58, 157), new Position(58, 158), new Position(58, 159), new Position(59, 160), new Position(58, 161), new Position(59, 162), new Position(59, 163), new Position(60, 164), new Position(59, 165), new Position(60, 166), new Position(60, 167), new Position(60, 168), new Position(60, 169), new Position(61, 170), new Position(60, 171), new Position(61, 172), new Position(61, 173), new Position(61, 174), new Position(61, 175), new Position(36, 12), new Position(35, 12), new Position(34, 12), new Position(33, 11), new Position(32, 11), new Position(31, 11), new Position(36, 12), new Position(36, 11), new Position(37, 11), new Position(38, 10), new Position(5, 11), new Position(5, 12), new Position(4, 13), new Position(3, 13), new Position(3, 14), new Position(2, 15), new Position(2, 16), new Position(1, 17), new Position(1, 18), new Position(0, 19), new Position(-1, 19), new Position(-1, 20), new Position(-2, 21), new Position(-2, 22), new Position(-3, 23), new Position(-4, 23), new Position(-4, 24), new Position(-5, 25), new Position(-5, 26), new Position(-6, 27), new Position(-6, 28), new Position(-7, 29), new Position(-8, 29), new Position(-8, 30), new Position(-9, 31), new Position(20, 15), new Position(20, 16), new Position(19, 17), new Position(18, 17), new Position(18, 18), new Position(17, 19), new Position(17, 19), new Position(17, 18), new Position(16, 17), new Position(16, 16), new Position(15, 15), new Position(15, 14), new Position(14, 13), new Position(14, 12), new Position(13, 11), new Position(26, 11), new Position(26, 12), new Position(25, 12), new Position(24, 12), new Position(23, 13), new Position(23, 14), new Position(22, 14), new Position(21, 14), new Position(20, 15), new Position(20, 15), new Position(21, 14), new Position(20, 13), new Position(20, 12), new Position(20, 11), new Position(21, 10), new Position(20, 9), new Position(20, 8), new Position(20, 7), new Position(53, 11), new Position(52, 11), new Position(51, 11), new Position(50, 11), new Position(49, 11), new Position(48, 11), new Position(47, 11), new Position(46, 11), new Position(45, 11), new Position(45, 10), new Position(44, 10), new Position(43, 10), new Position(42, 10), new Position(41, 10), new Position(40, 10), new Position(39, 10), new Position(38, 10), new Position(38, 10), new Position(37, 9), new Position(36, 9), new Position(36, 8), new Position(35, 7), new Position(30, 12), new Position(29, 12), new Position(28, 12), new Position(27, 11), new Position(26, 11), new Position(26, 11), new Position(26, 10), new Position(25, 12) };
            //Position X: 17, Y: 19, Position X: 17, Y: 20, Position X: 17, Y: 21, Position X: 17, Y: 22, Position X: 17, Y: 23, Position X: 17, Y: 24, Position X: 16, Y: 25, Position X: 17, Y: 26, Position X: 16, Y: 27, Position X: 17, Y: 28, Position X: 16, Y: 29, Position X: 16, Y: 30, Position X: 16, Y: 31, Position X: 16, Y: 32, Position X: 16, Y: 33, Position X: 16, Y: 34, Position X: 15, Y: 35, Position X: 16, Y: 36, Position X: 15, Y: 37, Position X: 16, Y: 38, Position X: 15, Y: 39, Position X: 15, Y: 40, Position X: 15, Y: 41, Position X: 15, Y: 42, Position X: 15, Y: 43, Position X: 15, Y: 44, Position X: 14, Y: 45, Position X: 15, Y: 46, Position X: 14, Y: 47, Position X: 15, Y: 48, Position X: 14, Y: 49, Position X: 14, Y: 50, Position X: 14, Y: 51, Position X: 14, Y: 52, Position X: 14, Y: 53, Position X: 14, Y: 54, Position X: 13, Y: 55, Position X: 14, Y: 56, Position X: 13, Y: 57, Position X: 14, Y: 58, Position X: 13, Y: 59, Position X: 13, Y: 60, Position X: 13, Y: 61, Position X: 13, Y: 62, Position X: 13, Y: 63, Position X: 13, Y: 64, Position X: 12, Y: 65, Position X: 13, Y: 66, Position X: 12, Y: 67, Position X: 13, Y: 68, Position X: 12, Y: 69, Position X: 12, Y: 70, Position X: 12, Y: 71, Position X: 12, Y: 72, Position X: 12, Y: 73, Position X: 12, Y: 74, Position X: 11, Y: 75, Position X: 12, Y: 76, Position X: 11, Y: 77, Position X: 12, Y: 78, Position X: 11, Y: 79, Position X: 11, Y: 80, Position X: 11, Y: 81, Position X: 11, Y: 82, Position X: 10, Y: 83, Position X: 11, Y: 84, Position X: 10, Y: 85, Position X: 11, Y: 86, Position X: 10, Y: 87, Position X: 10, Y: 88, Position X: 10, Y: 89, Position X: 10, Y: 90, Position X: 10, Y: 91, Position X: 10, Y: 92, Position X: 9, Y: 93, Position X: 10, Y: 94, Position X: 9, Y: 95, Position X: 10, Y: 96, Position X: 9, Y: 97, Position X: 9, Y: 98, Position X: 9, Y: 99, Position X: 9, Y: 100, Position X: 9, Y: 101, Position X: 9, Y: 102, Position X: 8, Y: 103, Position X: 9, Y: 104, Position X: 8, Y: 105, Position X: 9, Y: 106, Position X: 8, Y: 107, Position X: 8, Y: 108, Position X: 8, Y: 109, Position X: 8, Y: 110, Position X: 8, Y: 111, Position X: 8, Y: 112, Position X: 7, Y: 113, Position X: 8, Y: 114, Position X: 7, Y: 115, Position X: 8, Y: 116, Position X: 7, Y: 117, Position X: 7, Y: 118, Position X: 7, Y: 119, Position X: 7, Y: 120, Position X: 7, Y: 121, Position X: 7, Y: 122, Position X: 6, Y: 123, Position X: 7, Y: 124, Position X: 6, Y: 125, Position X: 7, Y: 126, Position X: 6, Y: 127, Position X: 6, Y: 128, Position X: 6, Y: 129, Position X: 6, Y: 130, Position X: 6, Y: 131, Position X: 6, Y: 132, Position X: 5, Y: 133, Position X: 6, Y: 134, Position X: 5, Y: 135, Position X: 6, Y: 136, Position X: 5, Y: 137, Position X: 5, Y: 138, Position X: 5, Y: 139, Position X: 5, Y: 140, Position X: 5, Y: 141, Position X: 5, Y: 142, Position X: 8, Y: 10, Position X: 7, Y: 11, Position X: 8, Y: 12, Position X: 7, Y: 13, Position X: 8, Y: 14, Position X: 7, Y: 15, Position X: 8, Y: 16, Position X: 7, Y: 17, Position X: 8, Y: 18, Position X: 7, Y: 19, Position X: 8, Y: 20, Position X: 7, Y: 21, Position X: 8, Y: 22, Position X: 7, Y: 23, Position X: 8, Y: 24, Position X: 7, Y: 25, Position X: 8, Y: 26, Position X: 7, Y: 27, Position X: 8, Y: 28, Position X: 7, Y: 29, Position X: 8, Y: 30, Position X: 7, Y: 31, Position X: 8, Y: 32, Position X: 7, Y: 33, Position X: 7, Y: 34, Position X: 7, Y: 35, Position X: 7, Y: 36, Position X: 7, Y: 37, Position X: 7, Y: 38, Position X: 7, Y: 39, Position X: 7, Y: 40, Position X: 7, Y: 41, Position X: 7, Y: 42, Position X: 7, Y: 43, Position X: 7, Y: 44, Position X: 7, Y: 45, Position X: 7, Y: 46, Position X: 7, Y: 47, Position X: 7, Y: 48, Position X: 7, Y: 49, Position X: 7, Y: 50, Position X: 7, Y: 51, Position X: 7, Y: 52, Position X: 7, Y: 53, Position X: 7, Y: 54, Position X: 6, Y: 55, Position X: 7, Y: 56, Position X: 6, Y: 57, Position X: 7, Y: 58, Position X: 6, Y: 59, Position X: 7, Y: 60, Position X: 6, Y: 61, Position X: 7, Y: 62, Position X: 6, Y: 63, Position X: 7, Y: 64, Position X: 6, Y: 65, Position X: 7, Y: 66, Position X: 6, Y: 67, Position X: 7, Y: 68, Position X: 6, Y: 69, Position X: 7, Y: 70, Position X: 6, Y: 71, Position X: 7, Y: 72, Position X: 6, Y: 73, Position X: 7, Y: 74, Position X: 6, Y: 75, Position X: 6, Y: 76, Position X: 6, Y: 77, Position X: 6, Y: 78, Position X: 6, Y: 79, Position X: 6, Y: 80, Position X: 6, Y: 81, Position X: 6, Y: 82, Position X: 6, Y: 83, Position X: 6, Y: 84, Position X: 6, Y: 85, Position X: 6, Y: 86, Position X: 6, Y: 87, Position X: 6, Y: 88, Position X: 6, Y: 89, Position X: 6, Y: 90, Position X: 6, Y: 91, Position X: 6, Y: 92, Position X: 6, Y: 93, Position X: 6, Y: 94, Position X: 6, Y: 95, Position X: 6, Y: 96, Position X: 6, Y: 97, Position X: 6, Y: 98, Position X: 5, Y: 99, Position X: 6, Y: 100, Position X: 5, Y: 101, Position X: 6, Y: 102, Position X: 5, Y: 103, Position X: 6, Y: 104, Position X: 5, Y: 105, Position X: 6, Y: 106, Position X: 5, Y: 107, Position X: 6, Y: 108, Position X: 5, Y: 109, Position X: 6, Y: 110, Position X: 5, Y: 111, Position X: 6, Y: 112, Position X: 5, Y: 113, Position X: 6, Y: 114, Position X: 5, Y: 115, Position X: 6, Y: 116, Position X: 5, Y: 117, Position X: 6, Y: 118, Position X: 5, Y: 119, Position X: 6, Y: 120, Position X: 5, Y: 121, Position X: 5, Y: 122, Position X: 5, Y: 123, Position X: 5, Y: 124, Position X: 5, Y: 125, Position X: 5, Y: 126, Position X: 5, Y: 127, Position X: 5, Y: 128, Position X: 5, Y: 129, Position X: 5, Y: 130, Position X: 5, Y: 131, Position X: 5, Y: 132, Position X: 5, Y: 133, Position X: 5, Y: 134, Position X: 5, Y: 135, Position X: 5, Y: 136, Position X: 5, Y: 137, Position X: 5, Y: 138, Position X: 5, Y: 139, Position X: 5, Y: 140, Position X: 5, Y: 141, Position X: 5, Y: 142, Position X: 5, Y: 142, Position X: 4, Y: 143, Position X: 5, Y: 144, Position X: 4, Y: 145, Position X: 5, Y: 146, Position X: 4, Y: 147, Position X: 4, Y: 148, Position X: 4, Y: 149, Position X: 4, Y: 150, Position X: 4, Y: 151, Position X: 4, Y: 152, Position X: 4, Y: 153, Position X: 4, Y: 154, Position X: 3, Y: 155, Position X: 4, Y: 156, Position X: 3, Y: 157, Position X: 4, Y: 158, Position X: 3, Y: 159, Position X: 3, Y: 160, Position X: 3, Y: 161, Position X: 3, Y: 162, Position X: 3, Y: 163, Position X: 3, Y: 164, Position X: 2, Y: 165, Position X: 3, Y: 166, Position X: 2, Y: 167, Position X: 3, Y: 168, Position X: 2, Y: 169, Position X: 3, Y: 170, Position X: 2, Y: 171, Position X: 2, Y: 172, Position X: 2, Y: 173, Position X: 2, Y: 174, Position X: 2, Y: 175, Position X: 2, Y: 176, Position X: 1, Y: 177, Position X: 2, Y: 178, Position X: 1, Y: 179, Position X: 2, Y: 180, Position X: 1, Y: 181, Position X: 2, Y: 182, Position X: 1, Y: 183, Position X: 1, Y: 184, Position X: 1, Y: 185, Position X: 1, Y: 186, Position X: 1, Y: 187, Position X: 1, Y: 188, Position X: 0, Y: 189, Position X: 1, Y: 190, Position X: 0, Y: 191, Position X: 1, Y: 192, Position X: 0, Y: 193, Position X: 0, Y: 194, Position X: 0, Y: 195, Position X: 0, Y: 196, Position X: 0, Y: 197, Position X: 0, Y: 198, Position X: 0, Y: 199, Position X: 0, Y: 200, Position X: -1, Y: 201, Position X: 0, Y: 202, Position X: -1, Y: 203, Position X: 0, Y: 204, Position X: -1, Y: 205, Position X: -1, Y: 206, Position X: -1, Y: 207, Position X: -1, Y: 208, Position X: -1, Y: 209, Position X: -1, Y: 210, Position X: -2, Y: 211, Position X: -1, Y: 212, Position X: -2, Y: 213, Position X: -1, Y: 214, Position X: -2, Y: 215, Position X: -1, Y: 216, Position X: -2, Y: 217, Position X: -2, Y: 218, Position X: -2, Y: 219, Position X: -2, Y: 220, Position X: -2, Y: 221, Position X: -2, Y: 222, Position X: -3, Y: 223, Position X: -2, Y: 224, Position X: -3, Y: 225, Position X: -2, Y: 226, Position X: -3, Y: 227, Position X: -2, Y: 228, Position X: -3, Y: 229, Position X: -3, Y: 230, Position X: -3, Y: 231, Position X: -3, Y: 232, Position X: -3, Y: 233, Position X: -3, Y: 234, Position X: -4, Y: 235, Position X: -3, Y: 236, Position X: -4, Y: 237, Position X: -3, Y: 238, Position X: -4, Y: 239, Position X: -4, Y: 240, Position X: -4, Y: 241, Position X: -4, Y: 242, Position X: -4, Y: 243, Position X: -4, Y: 244, Position X: -4, Y: 245, Position X: -4, Y: 246, Position X: -5, Y: 247, Position X: -4, Y: 248, Position X: -5, Y: 249, Position X: -4, Y: 250, Position X: -5, Y: 251, Position X: 51, Y: 119, Position X: 51, Y: 118, Position X: 51, Y: 117, Position X: 51, Y: 116, Position X: 50, Y: 115, Position X: 50, Y: 114, Position X: 50, Y: 113, Position X: 50, Y: 112, Position X: 49, Y: 111, Position X: 50, Y: 110, Position X: 49, Y: 109, Position X: 49, Y: 108, Position X: 49, Y: 107, Position X: 49, Y: 106, Position X: 48, Y: 105, Position X: 48, Y: 104, Position X: 48, Y: 103, Position X: 48, Y: 102, Position X: 47, Y: 101, Position X: 48, Y: 100, Position X: 47, Y: 99, Position X: 47, Y: 98, Position X: 47, Y: 97, Position X: 47, Y: 96, Position X: 46, Y: 95, Position X: 46, Y: 94, Position X: 46, Y: 93, Position X: 46, Y: 92, Position X: 45, Y: 91, Position X: 46, Y: 90, Position X: 45, Y: 89, Position X: 45, Y: 88, Position X: 45, Y: 87, Position X: 45, Y: 86, Position X: 44, Y: 85, Position X: 44, Y: 84, Position X: 44, Y: 83, Position X: 44, Y: 82, Position X: 43, Y: 81, Position X: 44, Y: 80, Position X: 43, Y: 79, Position X: 43, Y: 78, Position X: 43, Y: 77, Position X: 43, Y: 76, Position X: 42, Y: 75, Position X: 42, Y: 74, Position X: 42, Y: 73, Position X: 42, Y: 72, Position X: 41, Y: 71, Position X: 42, Y: 70, Position X: 41, Y: 69, Position X: 41, Y: 68, Position X: 41, Y: 67, Position X: 41, Y: 66, Position X: 40, Y: 65, Position X: 40, Y: 64, Position X: 40, Y: 63, Position X: 40, Y: 62, Position X: 39, Y: 61, Position X: 40, Y: 60, Position X: 39, Y: 59, Position X: 39, Y: 58, Position X: 39, Y: 57, Position X: 39, Y: 56, Position X: 38, Y: 55, Position X: 38, Y: 54, Position X: 38, Y: 53, Position X: 38, Y: 52, Position X: 37, Y: 51, Position X: 38, Y: 50, Position X: 37, Y: 49, Position X: 37, Y: 48, Position X: 37, Y: 47, Position X: 37, Y: 46, Position X: 36, Y: 45, Position X: 36, Y: 44, Position X: 36, Y: 43, Position X: 36, Y: 42, Position X: 35, Y: 41, Position X: 36, Y: 40, Position X: 35, Y: 39, Position X: 35, Y: 38, Position X: 35, Y: 37, Position X: 35, Y: 36, Position X: 34, Y: 35, Position X: 34, Y: 34, Position X: 34, Y: 33, Position X: 34, Y: 32, Position X: 33, Y: 31, Position X: 34, Y: 30, Position X: 33, Y: 29, Position X: 33, Y: 28, Position X: 33, Y: 27, Position X: 33, Y: 26, Position X: 32, Y: 25, Position X: 32, Y: 24, Position X: 32, Y: 23, Position X: 32, Y: 22, Position X: 31, Y: 21, Position X: 32, Y: 20, Position X: 31, Y: 19, Position X: 31, Y: 18, Position X: 31, Y: 17, Position X: 31, Y: 16, Position X: 30, Y: 15, Position X: 30, Y: 14, Position X: 30, Y: 13, Position X: 30, Y: 12, Position X: 51, Y: 119, Position X: 51, Y: 118, Position X: 51, Y: 117, Position X: 51, Y: 116, Position X: 50, Y: 115, Position X: 51, Y: 114, Position X: 50, Y: 113, Position X: 50, Y: 112, Position X: 50, Y: 111, Position X: 50, Y: 110, Position X: 50, Y: 109, Position X: 50, Y: 108, Position X: 49, Y: 107, Position X: 50, Y: 106, Position X: 49, Y: 105, Position X: 49, Y: 104, Position X: 49, Y: 103, Position X: 49, Y: 102, Position X: 48, Y: 101, Position X: 49, Y: 100, Position X: 48, Y: 99, Position X: 48, Y: 98, Position X: 48, Y: 97, Position X: 48, Y: 96, Position X: 48, Y: 95, Position X: 48, Y: 94, Position X: 47, Y: 93, Position X: 48, Y: 92, Position X: 47, Y: 91, Position X: 47, Y: 90, Position X: 47, Y: 89, Position X: 47, Y: 88, Position X: 46, Y: 87, Position X: 47, Y: 86, Position X: 46, Y: 85, Position X: 46, Y: 84, Position X: 46, Y: 83, Position X: 46, Y: 82, Position X: 45, Y: 81, Position X: 46, Y: 80, Position X: 45, Y: 79, Position X: 46, Y: 78, Position X: 45, Y: 77, Position X: 45, Y: 76, Position X: 45, Y: 75, Position X: 45, Y: 74, Position X: 44, Y: 73, Position X: 45, Y: 72, Position X: 44, Y: 71, Position X: 44, Y: 70, Position X: 44, Y: 69, Position X: 44, Y: 68, Position X: 43, Y: 67, Position X: 44, Y: 66, Position X: 43, Y: 65, Position X: 44, Y: 64, Position X: 43, Y: 63, Position X: 43, Y: 62, Position X: 43, Y: 61, Position X: 43, Y: 60, Position X: 42, Y: 59, Position X: 43, Y: 58, Position X: 42, Y: 57, Position X: 42, Y: 56, Position X: 42, Y: 55, Position X: 42, Y: 54, Position X: 41, Y: 53, Position X: 42, Y: 52, Position X: 41, Y: 51, Position X: 42, Y: 50, Position X: 41, Y: 49, Position X: 41, Y: 48, Position X: 41, Y: 47, Position X: 41, Y: 46, Position X: 40, Y: 45, Position X: 41, Y: 44, Position X: 40, Y: 43, Position X: 40, Y: 42, Position X: 40, Y: 41, Position X: 40, Y: 40, Position X: 39, Y: 39, Position X: 40, Y: 38, Position X: 39, Y: 37, Position X: 39, Y: 36, Position X: 39, Y: 35, Position X: 39, Y: 34, Position X: 39, Y: 33, Position X: 39, Y: 32, Position X: 38, Y: 31, Position X: 39, Y: 30, Position X: 38, Y: 29, Position X: 38, Y: 28, Position X: 38, Y: 27, Position X: 38, Y: 26, Position X: 37, Y: 25, Position X: 38, Y: 24, Position X: 37, Y: 23, Position X: 37, Y: 22, Position X: 37, Y: 21, Position X: 37, Y: 20, Position X: 37, Y: 19, Position X: 37, Y: 18, Position X: 36, Y: 17, Position X: 37, Y: 16, Position X: 36, Y: 15, Position X: 36, Y: 14, Position X: 36, Y: 13, Position X: 36, Y: 12, Position X: 51, Y: 119, Position X: 52, Y: 120, Position X: 51, Y: 121, Position X: 52, Y: 122, Position X: 52, Y: 123, Position X: 52, Y: 124, Position X: 52, Y: 125, Position X: 53, Y: 126, Position X: 52, Y: 127, Position X: 53, Y: 128, Position X: 53, Y: 129, Position X: 53, Y: 130, Position X: 53, Y: 131, Position X: 54, Y: 132, Position X: 54, Y: 133, Position X: 54, Y: 134, Position X: 54, Y: 135, Position X: 55, Y: 136, Position X: 54, Y: 137, Position X: 55, Y: 138, Position X: 55, Y: 139, Position X: 55, Y: 140, Position X: 55, Y: 141, Position X: 56, Y: 142, Position X: 55, Y: 143, Position X: 56, Y: 144, Position X: 56, Y: 145, Position X: 56, Y: 146, Position X: 56, Y: 147, Position X: 57, Y: 148, Position X: 56, Y: 149, Position X: 57, Y: 150, Position X: 57, Y: 151, Position X: 57, Y: 152, Position X: 57, Y: 153, Position X: 58, Y: 154, Position X: 57, Y: 155, Position X: 58, Y: 156, Position X: 58, Y: 157, Position X: 58, Y: 158, Position X: 58, Y: 159, Position X: 59, Y: 160, Position X: 58, Y: 161, Position X: 59, Y: 162, Position X: 59, Y: 163, Position X: 60, Y: 164, Position X: 59, Y: 165, Position X: 60, Y: 166, Position X: 60, Y: 167, Position X: 60, Y: 168, Position X: 60, Y: 169, Position X: 61, Y: 170, Position X: 60, Y: 171, Position X: 61, Y: 172, Position X: 61, Y: 173, Position X: 61, Y: 174, Position X: 61, Y: 175, Position X: 36, Y: 12, Position X: 35, Y: 12, Position X: 34, Y: 12, Position X: 33, Y: 11, Position X: 32, Y: 11, Position X: 31, Y: 11, Position X: 36, Y: 12, Position X: 36, Y: 11, Position X: 37, Y: 11, Position X: 38, Y: 10, Position X: 5, Y: 11, Position X: 5, Y: 12, Position X: 4, Y: 13, Position X: 3, Y: 13, Position X: 3, Y: 14, Position X: 2, Y: 15, Position X: 2, Y: 16, Position X: 1, Y: 17, Position X: 1, Y: 18, Position X: 0, Y: 19, Position X: -1, Y: 19, Position X: -1, Y: 20, Position X: -2, Y: 21, Position X: -2, Y: 22, Position X: -3, Y: 23, Position X: -4, Y: 23, Position X: -4, Y: 24, Position X: -5, Y: 25, Position X: -5, Y: 26, Position X: -6, Y: 27, Position X: -6, Y: 28, Position X: -7, Y: 29, Position X: -8, Y: 29, Position X: -8, Y: 30, Position X: -9, Y: 31, Position X: 20, Y: 15, Position X: 20, Y: 16, Position X: 19, Y: 17, Position X: 18, Y: 17, Position X: 18, Y: 18, Position X: 17, Y: 19, Position X: 17, Y: 19, Position X: 17, Y: 18, Position X: 16, Y: 17, Position X: 16, Y: 16, Position X: 15, Y: 15, Position X: 15, Y: 14, Position X: 14, Y: 13, Position X: 14, Y: 12, Position X: 13, Y: 11, Position X: 26, Y: 11, Position X: 26, Y: 12, Position X: 25, Y: 12, Position X: 24, Y: 12, Position X: 23, Y: 13, Position X: 23, Y: 14, Position X: 22, Y: 14, Position X: 21, Y: 14, Position X: 20, Y: 15, Position X: 20, Y: 15, Position X: 21, Y: 14, Position X: 20, Y: 13, Position X: 20, Y: 12, Position X: 20, Y: 11, Position X: 21, Y: 10, Position X: 20, Y: 9, Position X: 20, Y: 8, <message truncated>

            var points = new List<Point> { new Point(3.19696375550561, 0.259064570189949), new Point(5.47673264726844, 7.35443416580299), new Point(1.46673120300599, 9.6044423550388), new Point(7.05922293293254, 13.2550710445526), new Point(9.5326367796085, 0.542006226508881), new Point(15.1557762721348, 6.70530752218576), new Point(17.3631668600082, 9.47011462294968), new Point(9.37308112968369, 13.3188468671026), new Point(18.5614522917017, 3.8653869404762), new Point(26.4218715892275, 7.7817545122382), new Point(22.6280591211412, 9.4473085372929), new Point(25.7979922908349, 14.9776541027136), new Point(34.3169988222965, 3.97211253455473), new Point(32.1128553040851, 6.21197153544611), new Point(35.6954905244966, 10.0119497878533), new Point(35.0309263314265, 13.1750887656468), new Point(40.3841408059858, 2.73020903707026), new Point(38.6681405867767, 5.37370114278686), new Point(38.0553190387112, 8.17604658388349), new Point(37.8458784920377, 12.7904092747673) };

            var map = new HexMap(20, 50);

            //new HexGrid(20, 50, tileHandle.Result);           
            var lines = GenerateMap(20, 50, points, map);
            //Debug.Log(string.Join(", ", lines));
            var grid = CreateMap(map, tileHandle.Result);

            Assert.IsNotNull(grid);
            yield return null;

            var organizationFactory = new OrganisationFactory();
            var provinceFactory = new ProvinceFactory(map, lines, UnityEngine.Object.Instantiate, provinceHandle.Result, organizationFactory);
            var result = provinceFactory.CreateProvinces(points);

            Assert.IsNotNull(result);
            Assert.AreEqual(20, result.Count);

            var provinceless = map.Where(t => t.Province == null).ToList();

            Debug.Log(provinceless.Count);
            foreach (var tile in provinceless)
            {
                Debug.Log($"{tile} is on line: {lines.Contains(tile.Position)}");
                foreach(var neighbour in map.GetNeighbours(tile))
                {
                    Debug.Log($"Neighbour: {neighbour} is on line: {lines.Contains(neighbour.Position)}");
                }
            }

            foreach (var province in result)
                Debug.Log($"Province {province.Name} has {province.HexTiles.Count()} tiles");

            Assert.False(provinceless.Any());
            yield return null;
        }

        private HexGrid CreateMap(HexMap map, GameObject hexTile)
        {
            var mapStartPoint = new GameObject();
            mapStartPoint.transform.position = new Vector3(0, 0, 0);
            mapStartPoint.transform.localScale = new Vector3(1, 1, 1);

            var hexGrid = new HexGrid(map.Height, map.Width, hexTile);
            var mapObject = new GameObject("Map");
            for (var y = 0; y < hexGrid.Height; y++)
            {
                for (var x = 0; x < hexGrid.Width; x++)
                {
                    var position = hexGrid.Get(x, y);
                    var tile = CreateTile(hexTile, mapStartPoint, map, TileTerrainType.Plain, position, x, y);
                    tile.transform.SetParent(mapObject.transform);
                }
            }
            return hexGrid;
        }

        private GameObject CreateTile(GameObject tile, GameObject mapStartPoint, HexMap map, TileTerrainType type, Vector3 position, int x, int y)
        {
            var hexTile = TileFactory.Instance.CreateTile(UnityEngine.Object.Instantiate, tile, type, position, mapStartPoint.transform.rotation, x, y, map);
            hexTile.GetComponent<Tile>().Setup();
            return hexTile;
        }

        private List<Position> GenerateMap(int height, int width, List<Point> points, HexMap map)
        {
            var sites = points.Select(p => new Site() { Point = p }).ToList();
            var voronoiFactory = new VoronoiFactory();
            var voronoiMap = voronoiFactory.CreateVoronoiMap(height -1 , width - 1, sites);

            var lines = voronoiMap.Where(g => g is HalfEdge).Cast<HalfEdge>().SelectMany(
                edge =>
                {
                    var start = new Position(edge.Point.XInt, edge.Point.YInt);
                    var end = new Position(edge.EndPoint.XInt, edge.EndPoint.YInt);
                    var line = map.DrawLine(start, end);
                    return line;
                }).ToList();
            return lines;
        }

        private void LogMap(HexMap map)
        {
            for(var y=0; y<map.Height; y++)
            {
                var row = "";
                for (var x = 0; x < map.Width; x++)
                {
                    var tile = map.GetTile(x, y);
                    var province = tile.Province.Name.Last();
                    if (y % 2 != 0 && x == 0)
                        row += " ";
                    row += $"{province} ";
                }
                Debug.Log(row);
            }
        }
    }
}