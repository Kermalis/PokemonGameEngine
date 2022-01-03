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
        public readonly string Name;
        // These are loaded before the layout and used to place map connections
        public readonly int Width;
        public readonly int Height;
        // Connection data is loaded right away since it's always needed
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
        public readonly List<Map> ConnectedMaps = new();

        public Pos2D BlockOffsetFromCurrentMap;

        private Map(string name)
        {
            Name = name;

            using (EndianBinaryReader r = CreateReader())
            {
                Width = r.ReadInt32(8); // Header is 8 bytes long
                Height = r.ReadInt32();
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
            CameraObj.Instance.Map.RecurseBlockOffsets(updated, new Pos2D(0, 0));
        }
        private void RecurseBlockOffsets(List<Map> updated, Pos2D offset)
        {
            // Update this map's result
            updated.Add(this);
            BlockOffsetFromCurrentMap = offset;

            // Update connected maps only if they're loaded
            if (ConnectedMaps.Count == 0)
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

                Pos2D conOffset;
                switch (con.Dir)
                {
                    case MapConnection.Direction.South:
                    {
                        conOffset = new Pos2D(con.Offset, Height);
                        break;
                    }
                    case MapConnection.Direction.North:
                    {
                        conOffset = new Pos2D(con.Offset, -conMap.Height);
                        break;
                    }
                    case MapConnection.Direction.West:
                    {
                        conOffset = new Pos2D(-conMap.Width, con.Offset);
                        break;
                    }
                    case MapConnection.Direction.East:
                    {
                        conOffset = new Pos2D(Width, con.Offset);
                        break;
                    }
                    default: throw new InvalidDataException();
                }

                conMap.RecurseBlockOffsets(updated, offset + conOffset);
            }
        }

        // TODO: Reported blocks are incorrect for chain map connections (the game will probably never need to grab blocks that way though)
        public void GetXYMap(Pos2D xy, out Pos2D newXY, out Map newMap)
        {
            bool north = xy.Y < 0;
            bool south = xy.Y >= Height;
            bool west = xy.X < 0;
            bool east = xy.X >= Width;
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
                                if (xy.X >= c.Offset && xy.X < c.Offset + m.Width)
                                {
                                    m.GetXYMap(xy.Move(-c.Offset, -Height), out newXY, out newMap);
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
                                if (xy.X >= c.Offset && xy.X < c.Offset + m.Width)
                                {
                                    m.GetXYMap(xy.Move(-c.Offset, m.Height), out newXY, out newMap);
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
                                if (xy.Y >= c.Offset && xy.Y < c.Offset + m.Height)
                                {
                                    m.GetXYMap(xy.Move(m.Width, -c.Offset), out newXY, out newMap);
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
                                if (xy.Y >= c.Offset && xy.Y < c.Offset + m.Height)
                                {
                                    m.GetXYMap(xy.Move(-Width, -c.Offset), out newXY, out newMap);
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
            return newMap.Layout.GetBlock_InBounds(newXY);
        }
        public MapLayout.Block GetBlock_InBounds(Pos2D xy)
        {
            return Layout.GetBlock_InBounds(xy);
        }

        private EndianBinaryReader CreateReader()
        {
            return new EndianBinaryReader(AssetLoader.GetAssetStream(MapPath + Name + ".bin"));
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
                ConnectedMaps.Add(LoadOrGet(Connections[i].MapId));
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
            ConnectedMaps.Clear();
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
                    && (pos.Equals(o.Pos) || (checkMovingFrom && pos.Equals(o.MovingFromPos)))
                    )
                {
                    return o;
                }
            }
            return null;
        }

        #region Cache

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
