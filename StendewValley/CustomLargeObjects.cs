using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xTile;
using xTile.Tiles;

namespace StendewValley
{
    public class CustomLargeObject
    {
        // Fields
        public string id;

        private List<CustomObjectTile> elements = new List<CustomObjectTile>();
        private GameLocation location;
        private bool enabled;

        // Constructor
        public CustomLargeObject(string id, bool enabled, GameLocation loc)
        {
            this.id = id;
            this.enabled = enabled;
            this.location = loc;
        }

        // Properties
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (value != enabled)
                {
                    if (value) { AddObject(); }
                    else { RemoveObject(); }
                }
                enabled = value;
            }
        }

        // Methods
        public void SetSprite(int x, int y, string layer, int tileIndex, int tileSheetIndex)
        {
            elements.Add(new CustomObjectTile(x, y, layer, tileIndex, tileSheetIndex));
        }

        public void Spawn()
        {
            if (enabled)
            {
                AddObject();
            }
        }

        // Helpers
        private void RemoveObject()
        {
            foreach (CustomObjectTile tile in elements)
            {
                tile.Remove(location);
            }
        }

        private void AddObject()
        {
            foreach (CustomObjectTile tile in elements)
            {
                tile.Add(location);
            }
        }
        
    }

    internal class CustomObjectTile
    {
        public int x_index;
        public int y_index;
        public string layer;
        public int tileSheetIndex;
        public int tileIndex;

        public CustomObjectTile(int x, int y, string layer, int tileIndex, int tileSheetIndex)
        {
            this.x_index = x;
            this.y_index = y;
            this.layer = layer;
            this.tileIndex = tileIndex;
            this.tileSheetIndex = tileSheetIndex;
        }

        public void Remove(GameLocation loc)
        {
            loc.removeTile(x_index, y_index, layer);
        }

        public void Add(GameLocation loc)
        {
            loc.setMapTileIndex(x_index, y_index, tileIndex, layer, tileSheetIndex);
        }
    }
}
