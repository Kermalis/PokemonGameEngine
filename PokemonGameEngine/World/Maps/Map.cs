using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World.Data;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;
using System.IO;
#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Debug;
#endif

namespace Kermalis.PokemonGameEngine.World.Maps
{
    internal sealed class Map
    {
        private const string MAP_PATH = @"Map\";
        private static readonly IdList _ids = new(MAP_PATH + "MapIds.txt");
        private static readonly Dictionary<int, WeakReference<Map>> _loadedMaps = new();

        /// <summary>Used to load the reader when needed</summary>
        public readonly string Name;
        /// <summary><see cref="Size"/> is loaded before the layout and used to place map connections</summary>
        public readonly Vec2I Size;
        /// <summary>Connection data is loaded right away since it's always needed</summary>
        public readonly MapConnection[] Connections;

        // Layout, connected maps, and events are loaded when the map is visible
        private bool _visibleDataLoaded;
        public MapLayout Layout;
        public MapEvents Events;

        // Details and encounters are only loaded when the map becomes the current map or when being warped to
        private bool _currentDataLoaded;
        public MapDetails Details;
        public EncounterGroups Encounters;

        public readonly List<Obj> Objs = new();
        /// <summary>This list contains all connected maps when the map is visible</summary>
        private readonly List<Map> _connectedMaps = new();

        public Vec2I BlockOffsetFromCurrentMap;

        private Map(string name)
        {
            Name = name;

            using (EndianBinaryReader r = CreateReader())
            {
                Size.X = r.ReadInt32(8); // Header is 8 bytes long
                Size.Y = r.ReadInt32();
                int numConnections = r.ReadByte();
                Connections = new MapConnection[numConnections];
                for (int i = 0; i < numConnections; i++)
                {
                    Connections[i] = new MapConnection(r);
                }
            }
        }

        public static void UpdateAllConnectedMapBlockOffsets()
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Updating map locations...");
#endif
            var updated = new List<Map>();
            CameraObj.Instance.Map.RecurseBlockOffsets(updated, new Vec2I(0, 0));
        }
        private void RecurseBlockOffsets(List<Map> updated, Vec2I offset)
        {
            // Update this map's result
            updated.Add(this);
            BlockOffsetFromCurrentMap = offset;

            // Update connected maps only if they're loaded
            if (_connectedMaps.Count == 0)
            {
                return;
            }
            for (int i = 0; i < Connections.Length; i++)
            {
                MapConnection con = Connections[i];
                Map conMap = LoadOrGet(con.MapId);
                if (updated.Contains(conMap))
                {
                    continue; // Don't update more than once
                }

                Vec2I conOffset;
                switch (con.Dir)
                {
                    case MapConnection.Direction.South:
                    {
                        conOffset = new Vec2I(con.Offset, Size.Y);
                        break;
                    }
                    case MapConnection.Direction.North:
                    {
                        conOffset = new Vec2I(con.Offset, -conMap.Size.Y);
                        break;
                    }
                    case MapConnection.Direction.West:
                    {
                        conOffset = new Vec2I(-conMap.Size.X, con.Offset);
                        break;
                    }
                    case MapConnection.Direction.East:
                    {
                        conOffset = new Vec2I(Size.X, con.Offset);
                        break;
                    }
                    default: throw new InvalidDataException();
                }

                conMap.RecurseBlockOffsets(updated, offset + conOffset);
            }
        }

        // TODO: #71 - Need to check all loaded maps' positions relative to camera map, and modify xy accordingly
        public void GetXYMap(Vec2I xy, out Vec2I newXY, out Map newMap)
        {
            bool north = xy.Y < 0;
            bool south = xy.Y >= Size.Y;
            bool west = xy.X < 0;
            bool east = xy.X >= Size.X;
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
                                if (xy.X >= c.Offset && xy.X < c.Offset + m.Size.X)
                                {
                                    m.GetXYMap(xy.Plus(-c.Offset, -Size.Y), out newXY, out newMap);
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
                                if (xy.X >= c.Offset && xy.X < c.Offset + m.Size.X)
                                {
                                    m.GetXYMap(xy.Plus(-c.Offset, m.Size.Y), out newXY, out newMap);
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
                                if (xy.Y >= c.Offset && xy.Y < c.Offset + m.Size.Y)
                                {
                                    m.GetXYMap(xy.Plus(m.Size.X, -c.Offset), out newXY, out newMap);
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
                                if (xy.Y >= c.Offset && xy.Y < c.Offset + m.Size.Y)
                                {
                                    m.GetXYMap(xy.Plus(-Size.X, -c.Offset), out newXY, out newMap);
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
        public MapLayout.Block GetBlock_CrossMap(Vec2I xy, out Vec2I newXY, out Map newMap)
        {
            GetXYMap(xy, out newXY, out newMap);
            return newMap.Layout.GetBlock(newXY);
        }
        public MapLayout.Block GetBlock_InBounds(Vec2I xy)
        {
            return Layout.GetBlock(xy);
        }

        private EndianBinaryReader CreateReader()
        {
            return new EndianBinaryReader(AssetLoader.GetAssetStream(MAP_PATH + Name + ".bin"));
        }
        private void LoadData_NowVisible()
        {
            using (EndianBinaryReader r = CreateReader())
            {
                r.BaseStream.Position = r.ReadUInt32(); // Position to visible data is at offset 0 in the file header
                Layout = new MapLayout(r.ReadInt32());
                Events = new MapEvents(r);
            }
        }
        private void LoadData_NowCurrent()
        {
            using (EndianBinaryReader r = CreateReader())
            {
                r.BaseStream.Position = r.ReadUInt32(4); // Position to current data is at offset 4 in the file header
                Details = new MapDetails(r);
                Encounters = new EncounterGroups(r);
            }
        }

        private void LoadConnectedMaps()
        {
            for (int i = 0; i < Connections.Length; i++)
            {
                _connectedMaps.Add(LoadOrGet(Connections[i].MapId));
            }
        }
        private void CreateEventObjs()
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
        private void DeleteEventObjs()
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
            Log.ModifyIndent(+1);
#endif
            if (!_visibleDataLoaded)
            {
                _visibleDataLoaded = true;
                LoadData_NowVisible();
                LoadConnectedMaps();
                CreateEventObjs();
                UpdateAllConnectedMapBlockOffsets();
            }
#if DEBUG_OVERWORLD
            Log.ModifyIndent(-1);
#endif
        }
        public void OnMapNoLongerVisible()
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Map is no longer visible: " + Name);
            Log.ModifyIndent(+1);
#endif
            _visibleDataLoaded = false;
            DeleteEventObjs();
            _connectedMaps.Clear();
            Layout.Delete();
            Events = null;
#if DEBUG_OVERWORLD
            Log.ModifyIndent(-1);
#endif
        }

        public void OnCurrentMap()
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Camera is now on map: " + Name);
#endif
            if (!_visibleDataLoaded)
            {
                _visibleDataLoaded = true;
                LoadData_NowVisible();
                LoadConnectedMaps();
                CreateEventObjs();
            }
            if (!_currentDataLoaded)
            {
                _currentDataLoaded = true;
                LoadData_NowCurrent();
            }
            UpdateAllConnectedMapBlockOffsets();
        }
        public void OnNoLongerCurrentMap()
        {
            _currentDataLoaded = false;
            Details = null;
            Encounters = null;
        }

        public void OnWarpingMap()
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Camera & Player are warping to map: " + Name);
            Log.ModifyIndent(+1);
#endif
            if (!_currentDataLoaded)
            {
                _currentDataLoaded = true;
                LoadData_NowCurrent();
            }
#if DEBUG_OVERWORLD
            Log.ModifyIndent(-1);
#endif
        }

        public Obj GetNonCamObj_InBounds(in WorldPos pos, bool checkMovingFrom)
        {
            foreach (Obj o in Objs)
            {
                if (o.Id != Overworld.CameraId
                    && (pos == o.Pos || (checkMovingFrom && pos == o.MovingFromPos))
                    )
                {
                    return o;
                }
            }
            return null;
        }

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

#if DEBUG_OVERWORLD
        public override string ToString()
        {
            return Name;
        }
#endif
    }
}
