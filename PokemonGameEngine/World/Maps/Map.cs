#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Debug;
#endif
using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Maps
{
    internal sealed class Map
    {
        public readonly MapLayout Layout;
        public readonly MapDetails Details;
        public readonly MapConnection[] Connections;
        public readonly EncounterGroups Encounters;
        public readonly MapEvents Events;

        public readonly List<Obj> Objs = new();

        private Map(string name)
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Loading map: " + name);
            Log.ModifyIndent(+1);
#endif
            using (var r = new EndianBinaryReader(AssetLoader.GetAssetStream(MapPath + name + ".bin")))
            {
                Layout = new MapLayout(r.ReadInt32());
                Details = new MapDetails(r);
                int numConnections = r.ReadByte();
                Connections = new MapConnection[numConnections];
                for (int i = 0; i < numConnections; i++)
                {
                    Connections[i] = new MapConnection(r);
                }
                Encounters = new EncounterGroups(r);
                Events = new MapEvents(r);
            }
#if DEBUG_OVERWORLD
            Name = name;
            Log.ModifyIndent(-1);
#endif
        }
        ~Map()
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Unloading map: " + Name);
#endif
            Game.AddTempTask(Layout.Delete);
        }

        // TODO: Loading entire map details and layouts is unnecessary
        public void GetXYMap(int x, int y, out int outX, out int outY, out Map outMap)
        {
            MapLayout ml = Layout;
            bool north = y < 0;
            bool south = y >= ml.BlocksHeight;
            bool west = x < 0;
            bool east = x >= ml.BlocksWidth;
            // If we're out of bounds, try to branch into a connection. If we don't find one, we meet at the bottom
            if (north || south || west || east)
            {
                for (int i = 0; i < Connections.Length; i++)
                {
                    MapConnection c = Connections[i];
                    switch (c.Dir)
                    {
                        case MapConnection.Direction.South:
                        {
                            if (south)
                            {
                                Map m = LoadOrGet(c.MapId);
                                MapLayout l = m.Layout;
                                if (x >= c.Offset && x < c.Offset + l.BlocksWidth)
                                {
                                    m.GetXYMap(x - c.Offset, y - ml.BlocksHeight, out outX, out outY, out outMap);
                                    return;
                                }
                            }
                            break;
                        }
                        case MapConnection.Direction.North:
                        {
                            if (north)
                            {
                                Map m = LoadOrGet(c.MapId);
                                MapLayout l = m.Layout;
                                if (x >= c.Offset && x < c.Offset + l.BlocksWidth)
                                {
                                    m.GetXYMap(x - c.Offset, l.BlocksHeight + y, out outX, out outY, out outMap);
                                    return;
                                }
                            }
                            break;
                        }
                        case MapConnection.Direction.West:
                        {
                            if (west)
                            {
                                Map m = LoadOrGet(c.MapId);
                                MapLayout l = m.Layout;
                                if (y >= c.Offset && y < c.Offset + l.BlocksHeight)
                                {
                                    m.GetXYMap(l.BlocksWidth + x, y - c.Offset, out outX, out outY, out outMap);
                                    return;
                                }
                            }
                            break;
                        }
                        case MapConnection.Direction.East:
                        {
                            if (east)
                            {
                                Map m = LoadOrGet(c.MapId);
                                MapLayout l = m.Layout;
                                if (y >= c.Offset && y < c.Offset + l.BlocksHeight)
                                {
                                    m.GetXYMap(x - ml.BlocksWidth, y - c.Offset, out outX, out outY, out outMap);
                                    return;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            // If we are in bounds, return the current map
            // If we didn't find a connection, we are at the border, which counts as the current map
            outX = x;
            outY = y;
            outMap = this;
        }
        public MapLayout.Block GetBlock_CrossMap(int x, int y, out int outX, out int outY, out Map outMap)
        {
            GetXYMap(x, y, out outX, out outY, out outMap);
            return outMap.GetBlock_InBounds(outX, outY);
        }
        public MapLayout.Block GetBlock_InBounds(int x, int y)
        {
            MapLayout ml = Layout;
            bool north = y < 0;
            bool south = y >= ml.BlocksHeight;
            bool west = x < 0;
            bool east = x >= ml.BlocksWidth;
            // In bounds
            if (!north && !south && !west && !east)
            {
                return ml.Blocks[y][x];
            }
            // Border blocks
            byte bw = ml.BorderWidth;
            byte bh = ml.BorderHeight;
            // No border should render pure black
            if (bw == 0 || bh == 0)
            {
                return null;
            }
            // Has a border
            x %= bw;
            if (west)
            {
                x *= -1;
            }
            y %= bh;
            if (north)
            {
                y *= -1;
            }
            return ml.BorderBlocks[y][x];
        }

        private void LoadObjEvents()
        {
            Flags flags = Engine.Instance.Save.Flags;
            foreach (MapEvents.ObjEvent oe in Events.Objs)
            {
                if (!flags[oe.Flag])
                {
                    _ = new EventObj(oe, this);
                }
            }
        }
        private void UnloadObjEvents()
        {
            foreach (Obj o in Objs)
            {
                if (o.Id != Overworld.CameraId && o != CameraObj.CameraAttachedTo)
                {
                    Obj.LoadedObjs.Remove(o);
                    o.Dispose();
                }
            }
            Objs.Clear();
        }

        public void OnMapNowVisible()
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Map is now visible: " + Name);
#endif
            LoadObjEvents();
        }
        public void OnMapNoLongerVisible()
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Map is no longer visible: " + Name);
#endif
            UnloadObjEvents();
        }

        /// <summary>
        /// <paramref name="exceptThisOne"/> is used so objs aren't checking if they collide with themselves.
        /// <para>The camera is not hardcoded here because we can have some objs disable collisions, plus someone might want to get the camera from this.</para>
        /// <paramref name="checkMovingPrevPos"/> checks if an obj is moving from its <see cref="Obj.PrevPos"/> (that matches the coords we are looking at).
        /// </summary> 
        public List<Obj> GetObjs_InBounds(int x, int y, byte elevation, Obj exceptThisOne, bool checkMovingPrevPos)
        {
            bool Check(WorldPos pos)
            {
                return pos.X == x && pos.Y == y && pos.Elevation == elevation;
            }
            var list = new List<Obj>();
            foreach (Obj o in Objs)
            {
                if (o != exceptThisOne && (Check(o.Pos) || (checkMovingPrevPos && o.IsMoving && Check(o.PrevPos))))
                {
                    list.Add(o);
                }
            }
            return list;
        }

        #region Cache

#if DEBUG_OVERWORLD
        public readonly string Name;
#endif

        private const string MapPath = "Map\\";
        private static readonly IdList _ids = new(MapPath + "MapIds.txt");
        private static readonly Dictionary<int, WeakReference<Map>> _loadedMaps = new();
        public static Map LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            Map m;
            if (!_loadedMaps.TryGetValue(id, out WeakReference<Map> w))
            {
                m = new Map(name);
                _loadedMaps.Add(id, new WeakReference<Map>(m));
            }
            else if (!w.TryGetTarget(out m))
            {
                m = new Map(name);
                w.SetTarget(m);
            }
            return m;
        }

        #endregion
    }
}
