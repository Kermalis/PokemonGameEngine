using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World.Data;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;
#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Debug;
#endif

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
            Engine.AddTempTask(Layout.Delete);
        }

        // TODO: Loading entire map details and layouts is unnecessary
        public void GetXYMap(Pos2D xy, out Pos2D newXY, out Map newMap)
        {
            MapLayout curL = Layout;
            bool north = xy.Y < 0;
            bool south = xy.Y >= curL.BlocksHeight;
            bool west = xy.X < 0;
            bool east = xy.X >= curL.BlocksWidth;
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
                                if (xy.X >= c.Offset && xy.X < c.Offset + l.BlocksWidth)
                                {
                                    m.GetXYMap(xy.Move(-c.Offset, -curL.BlocksHeight), out newXY, out newMap);
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
                                if (xy.X >= c.Offset && xy.X < c.Offset + l.BlocksWidth)
                                {
                                    m.GetXYMap(xy.Move(-c.Offset, l.BlocksHeight), out newXY, out newMap);
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
                                if (xy.Y >= c.Offset && xy.Y < c.Offset + l.BlocksHeight)
                                {
                                    m.GetXYMap(xy.Move(l.BlocksWidth, -c.Offset), out newXY, out newMap);
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
                                if (xy.Y >= c.Offset && xy.Y < c.Offset + l.BlocksHeight)
                                {
                                    m.GetXYMap(xy.Move(-curL.BlocksWidth, -c.Offset), out newXY, out newMap);
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
            newXY = xy;
            newMap = this;
        }
        public MapLayout.Block GetBlock_CrossMap(Pos2D xy, out Pos2D newXY, out Map newMap)
        {
            GetXYMap(xy, out newXY, out newMap);
            return newMap.GetBlock_InBounds(newXY);
        }
        public MapLayout.Block GetBlock_InBounds(Pos2D xy)
        {
            MapLayout ml = Layout;
            bool north = xy.Y < 0;
            bool south = xy.Y >= ml.BlocksHeight;
            bool west = xy.X < 0;
            bool east = xy.X >= ml.BlocksWidth;
            // In bounds
            if (!north && !south && !west && !east)
            {
                return ml.Blocks[xy.Y][xy.X];
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
            xy.X %= bw;
            if (west)
            {
                xy.X *= -1;
            }
            xy.Y %= bh;
            if (north)
            {
                xy.Y *= -1;
            }
            return ml.BorderBlocks[xy.Y][xy.X];
        }

        private void LoadObjEvents()
        {
            Flags flags = Game.Instance.Save.Flags;
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
                if (o.Id != Overworld.CameraId && o.Id != CameraObj.Instance.CamAttachedTo.Id)
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

        public Obj GetNonCamObj_InBounds(in WorldPos pos, bool checkMovingFrom)
        {
            foreach (Obj o in Objs)
            {
                if (o.Id != Overworld.CameraId
                    && (pos.Equals(o.Pos) || (checkMovingFrom && pos.Equals(o.MovingFromPos)))
                    )
                {
                    return o;
                }
            }
            return null;
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
