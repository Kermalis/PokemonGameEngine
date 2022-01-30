using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders.Battle;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal unsafe static class AssimpLoader
    {
        private static readonly Assimp _assimp;

        static AssimpLoader()
        {
            _assimp = Assimp.GetApi();
        }

        public static List<Mesh> ImportModel(string asset)
        {
            asset = AssetLoader.GetPath(asset);
            Scene* scene = _assimp.ImportFile(asset, (uint)(PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs));

            // Check for errors
            if (scene is null || scene->MFlags == (uint)SceneFlags.Incomplete || scene->MRootNode is null)
            {
                throw new InvalidDataException(_assimp.GetErrorStringS());
            }

            string dir = Path.GetDirectoryName(asset);

            var meshes = new List<Mesh>();
            var loaded = new Dictionary<string, uint>();
            ProcessNode(meshes, scene->MRootNode, scene, dir, loaded);
            _assimp.FreeScene(scene);
            return meshes;
        }

        private static void ProcessNode(List<Mesh> meshes, Node* node, Scene* scene, string dir, Dictionary<string, uint> loaded)
        {
            for (uint i = 0; i < node->MNumMeshes; i++)
            {
                Silk.NET.Assimp.Mesh* mesh = scene->MMeshes[node->MMeshes[i]];
                meshes.Add(ProcessMesh(mesh, scene, dir, loaded));
            }

            for (uint i = 0; i < node->MNumChildren; i++)
            {
                ProcessNode(meshes, node->MChildren[i], scene, dir, loaded);
            }
        }

        private static Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene, string dir, Dictionary<string, uint> loaded)
        {
            var vertices = new VBOData_BattleModel[mesh->MNumVertices];

            for (uint i = 0; i < mesh->MNumVertices; i++)
            {
                VBOData_BattleModel vertex;

                // Positions
                vertex.Pos = mesh->MVertices[i];

                // Normals (we import with normals added so we always have them)
                vertex.Normal = mesh->MNormals[i];

                // UVs
                if (mesh->MTextureCoords[0] is not null)
                {
                    Vector3 v = mesh->MTextureCoords[0][i];
                    vertex.UV = new Vector2(v.X, v.Y);
                }
                else
                {
                    vertex.UV = default; // 0,0
                }

                vertices[i] = vertex;
            }

            // Get vertex indices from mesh faces (triangles)
            var indices = new List<uint>();
            for (uint i = 0; i < mesh->MNumFaces; i++)
            {
                Face f = mesh->MFaces[i];
                for (uint j = 0; j < f.MNumIndices; j++)
                {
                    indices.Add(f.MIndices[j]);
                }
            }

            // Materials
            var textures = new List<uint>();
            if (mesh->MMaterialIndex >= 0)
            {
                Material* material = scene->MMaterials[mesh->MMaterialIndex];

                // Diffuse map
                textures.AddRange(LoadMaterialTextures(material, TextureType.TextureTypeDiffuse, dir, loaded));
                // Not supporting other textures (for now)
            }

            return new Mesh(vertices, indices, textures);
        }

        private static List<uint> LoadMaterialTextures(Material* mat, TextureType type, string dir, Dictionary<string, uint> loaded)
        {
            var textures = new List<uint>();

            uint count = _assimp.GetMaterialTextureCount(mat, type);
            for (uint i = 0; i < count; i++)
            {
                AssimpString aPath = default;
                if (_assimp.GetMaterialTexture(mat, type, i, &aPath, null, null, null, null, null, null) != Return.ReturnSuccess)
                {
                    throw new Exception("Error loading material textures");
                }
                string path = aPath.AsString;
                if (!loaded.TryGetValue(path, out uint texture))
                {
                    GL gl = Display.OpenGL;
                    texture = gl.GenTexture();
                    gl.BindTexture(TextureTarget.Texture2D, texture);
                    using (var img = SixLabors.ImageSharp.Image.Load<Rgba32>(Path.Combine(dir, path)))
                    {
                        fixed (void* imgdata = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
                        {
                            GLTextureUtils.LoadTextureData(gl, imgdata, new Vec2I(img.Width, img.Height));
                        }
                    }
                    loaded.Add(path, texture);
                }
                textures.Add(texture);
            }

            return textures;
        }

        public static void Quit()
        {
            _assimp.Dispose();
        }
    }
}
