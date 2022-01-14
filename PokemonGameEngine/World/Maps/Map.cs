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
    internal sealed partial class Map
    {
        private const string MAP_PATH = @"Map\";
        private static readonly IdList _ids = new(MAP_PATH + "MapIds.txt");
        private static readonly Dictionary<int, Map> _loadedMaps = new();

        private readonly int _id;
        /// <summary>A reference can be: This map is used in a connection to another map; is the current map; is being warped to</summary>
        private int _numReferences;
#if DEBUG_OVERWORLD
        public string Name => _ids[_id];
#endif

        /// <summary><see cref="_size"/> is loaded before the layout and used to place map connections</summary>
        private readonly Vec2I _size;
        public Vec2I BlockOffsetFromCurrentMap;

        // Layout, connected maps, and events are loaded when the map is visible
        private bool _visibleDataLoaded;
        public Connection[] Connections;
        public MapLayout Layout;
        public MapEvents Events;

        // Details and encounters are only loaded when the map becomes the current map or when being warped to
        private bool _currentDataLoaded;
        public MapDetails Details;
        public EncounterGroups Encounters;

        private Map(int id)
        {
            _loadedMaps.Add(id, this);
            _id = id;

#if DEBUG_OVERWORLD
            Log.WriteLine("Loading map: " + Name);
#endif

            using (EndianBinaryReader r = CreateReader())
            {
                _size.X = r.ReadInt32(4); // Skip 4 byte offset
                _size.Y = r.ReadInt32();
            }
        }

        private static void UpdateAllConnectedMapBlockOffsets()
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Updating map locations...");
            Log.ModifyIndent(+1);
#endif
            var updated = new List<Map>();
            CameraObj.Instance.Map.RecurseBlockOffsets(updated, new Vec2I(0, 0));
#if DEBUG_OVERWORLD
            Log.ModifyIndent(-1);
#endif
        }
        private void RecurseBlockOffsets(List<Map> updated, Vec2I offset)
        {
            // Update this map's result
            updated.Add(this);
            BlockOffsetFromCurrentMap = offset;
#if DEBUG_OVERWORLD
            Log.WriteLine(Name + " is located at: " + offset);
#endif

            // Update connected maps only if they're loaded
            if (Connections is null)
            {
                return;
            }
            for (int i = 0; i < Connections.Length; i++)
            {
                Connection con = Connections[i];
                if (!updated.Contains(con.Map))
                {
                    Vec2I conOffset;
                    switch (con.Dir)
                    {
                        case Connection.Direction.South: conOffset = new Vec2I(con.Offset, _size.Y); break;
                        case Connection.Direction.North: conOffset = new Vec2I(con.Offset, -con.Map._size.Y); break;
                        case Connection.Direction.West: conOffset = new Vec2I(-con.Map._size.X, con.Offset); break;
                        case Connection.Direction.East: conOffset = new Vec2I(_size.X, con.Offset); break;
                        default: throw new InvalidDataException();
                    }
                    con.Map.RecurseBlockOffsets(updated, offset + conOffset);
                }
            }
        }
        public bool IsVisible(Vec2I curMapPos, Vec2I viewSize)
        {
            if (_numReferences == 0)
            {
                return false; // This map was just unloaded
            }
            return GetPositionRect(curMapPos).Intersects(viewSize);
        }
        public Rect GetPositionRect(Vec2I curMapPos)
        {
            return Rect.FromSize((BlockOffsetFromCurrentMap * Overworld.Block_NumPixels) + curMapPos,
                _size * Overworld.Block_NumPixels);
        }

        public void GetPosAndMap(Vec2I pos, out Vec2I newPos, out Map newMap)
        {
            Vec2I posOnCamMap = pos + BlockOffsetFromCurrentMap;
            foreach (Map m in _loadedMaps.Values)
            {
                Vec2I posOnM = posOnCamMap - m.BlockOffsetFromCurrentMap;
                var rect = Rect.FromSize(new Vec2I(0, 0), m._size);
                if (rect.Contains(posOnM))
                {
                    newPos = posOnM;
                    newMap = m;
                    return;
                }
            }
            // No map found at that point, return cam map border block
            newPos = posOnCamMap;
            newMap = CameraObj.Instance.Map;
        }
        public MapLayout.Block GetBlock_CrossMap(Vec2I pos, out Vec2I newPos, out Map newMap)
        {
            GetPosAndMap(pos, out newPos, out newMap);
            return newMap.Layout.GetBlock(newPos);
        }
        public MapLayout.Block GetBlock_InBounds(Vec2I pos)
        {
            return Layout.GetBlock(pos);
        }

        private EndianBinaryReader CreateReader()
        {
            return new EndianBinaryReader(AssetLoader.GetAssetStream(MAP_PATH + _ids[_id] + ".bin"));
        }
        private void LoadData_NowVisible()
        {
            using (EndianBinaryReader r = CreateReader())
            {
                int numConnections = r.ReadByte(12); // Visible data starts at offset 12 (skip 4 byte offset and 8 bytes size)
                if (numConnections == 0)
                {
                    Connections = Array.Empty<Connection>();
                }
                else
                {
                    Connections = new Connection[numConnections];
                    for (int i = 0; i < numConnections; i++)
                    {
                        Connections[i] = new Connection(r);
                    }
                }
                Layout = new MapLayout(r.ReadInt32());
                Events = new MapEvents(r);
            }
        }
        private void LoadData_NowCurrent()
        {
            using (EndianBinaryReader r = CreateReader())
            {
                r.BaseStream.Position = r.ReadUInt32(); // Position for current data is at offset 0 in the file header
                Details = new MapDetails(r);
                Encounters = new EncounterGroups(r);
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
            for (Obj o = Obj.LoadedObjs.First; o is not null; o = o.Next)
            {
                if (o.Map == this && o.Id != Overworld.CameraId && o.Id != CameraObj.Instance.CamAttachedTo.Id)
                {
                    Obj.LoadedObjs.RemoveAndDispose(o);
                }
            }
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
            for (int i = 0; i < Connections.Length; i++)
            {
                Connections[i].Map.DeductReference();
            }
            Connections = null;
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
            Log.ModifyIndent(+1);
#endif
            AddReference();
            if (!_visibleDataLoaded)
            {
                _visibleDataLoaded = true;
                LoadData_NowVisible();
                CreateEventObjs();
            }
            if (!_currentDataLoaded)
            {
                _currentDataLoaded = true;
                LoadData_NowCurrent();
            }
            UpdateAllConnectedMapBlockOffsets();
#if DEBUG_OVERWORLD
            Log.ModifyIndent(-1);
#endif
        }
        public void OnNoLongerCurrentMap()
        {
            DeductReference();
            _currentDataLoaded = false;
            Details = null;
            Encounters = null;
        }

        public void OnWarpingMap()
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Camera & Player are warping to map: " + Name);
#endif
            AddReference();
            if (!_currentDataLoaded)
            {
                _currentDataLoaded = true;
                LoadData_NowCurrent();
            }
        }
        public void OnNoLongerWarpingMap()
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Finished warping to map: " + Name);
#endif
            DeductReference();
        }

        public Obj GetNonCamObj_InBounds(in WorldPos pos, bool checkMovingFrom)
        {
            for (Obj o = Obj.LoadedObjs.First; o is not null; o = o.Next)
            {
                if (o.Map == this && o.Id != Overworld.CameraId
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
            if (!_loadedMaps.TryGetValue(id, out Map m))
            {
                m = new Map(id);
            }
            return m;
        }
        private void AddReference()
        {
            _numReferences++;
#if DEBUG_OVERWORLD
            Log.WriteLine("Adding reference to map: " + Name + " (new count is " + _numReferences + ")");
#endif
        }
        private void DeductReference()
        {
            if (--_numReferences > 0)
            {
#if DEBUG_OVERWORLD
                Log.WriteLine("Removing reference from map: " + Name + " (new count is " + _numReferences + ")");
#endif
                return;
            }

#if DEBUG_OVERWORLD
            Log.WriteLine("Unloading map: " + Name);
#endif
            _loadedMaps.Remove(_id);
        }

#if DEBUG_OVERWORLD
        public override string ToString()
        {
            return Name;
        }
#endif
    }
}
