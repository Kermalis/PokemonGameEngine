using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
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
    internal struct AssimpTexture
    {
        public string Path;
        public uint GLTex;
    }
    internal struct AssimpVertex
    {
        public const int OffsetOfPos = 0;
        public const int OffsetOfNormal = 3 * sizeof(float);
        public const int OffsetOfUV = 6 * sizeof(float);
        public const uint SizeOf = (3 + 3 + 2) * sizeof(float);

        public Vector3 Pos;
        public Vector3 Normal;
        public Vector2 UV;
    }
    internal unsafe static class AssimpLoader
    {
        private static readonly Assimp _assimp = Assimp.GetApi();

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
            var loaded = new List<AssimpTexture>();
            ProcessNode(meshes, scene->MRootNode, scene, dir, loaded);
            return meshes;
        }

        private static void ProcessNode(List<Mesh> meshes, Node* node, Scene* scene, string dir, List<AssimpTexture> loaded)
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

        private static Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene, string dir, List<AssimpTexture> loaded)
        {
            var vertices = new AssimpVertex[mesh->MNumVertices];

            for (uint i = 0; i < mesh->MNumVertices; i++)
            {
                AssimpVertex vertex;

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
            var textures = new List<AssimpTexture>();
            if (mesh->MMaterialIndex >= 0)
            {
                Material* material = scene->MMaterials[mesh->MMaterialIndex];

                // Diffuse map
                textures.AddRange(LoadMaterialTextures(material, TextureType.TextureTypeDiffuse, dir, loaded));
                // Not supporting other textures (for now)
            }

            return new Mesh(vertices, indices.ToArray(), textures);
        }

        private static List<AssimpTexture> LoadMaterialTextures(Material* mat, TextureType type, string dir, List<AssimpTexture> loaded)
        {
            GL gl = Game.OpenGL;
            var textures = new List<AssimpTexture>();

            uint count = _assimp.GetMaterialTextureCount(mat, type);
            for (uint i = 0; i < count; i++)
            {
                AssimpString path = default;
                if (_assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null) != Return.ReturnSuccess)
                {
                    throw new Exception("Error loading material textures");
                }

                string sPath = path.AsString;

                for (int j = 0; j < loaded.Count; j++)
                {
                    AssimpTexture l = loaded[j];
                    if (l.Path == sPath)
                    {
                        textures.Add(l);
                        goto dontload; // break out of this loop and continue on the parent loop
                    }
                }
                AssimpTexture texture;
                GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
                texture.GLTex = GLHelper.GenTexture(gl);
                GLHelper.BindTexture(gl, texture.GLTex);
                using (var img = SixLabors.ImageSharp.Image.Load<Rgba32>(Path.Combine(dir, sPath)))
                {
                    fixed (void* imgdata = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
                    {
                        GLTextureUtils.LoadTextureData(gl, imgdata, (uint)img.Width, (uint)img.Height);
                    }
                }
                texture.Path = sPath;
                textures.Add(texture);
                loaded.Add(texture);

            dontload:
                ;
            }

            return textures;
        }

        public static void GameExit()
        {
            _assimp.Dispose();
        }
    }
}
