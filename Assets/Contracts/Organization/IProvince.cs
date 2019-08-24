﻿using Assets.Contracts.Map;
using System.Collections.Generic;

namespace Assets.Contracts.Organization
{
    public interface IProvince
    {
        string Name { get; set; }

        ICountry Owner { get; set; }

        TileBase Capital { get; }

        bool IsCapital { get; set; }

        IList<IProvince> GetNeighbours(IHexMap map);

        IEnumerable<TileBase> HexTiles { get; }
    }
}