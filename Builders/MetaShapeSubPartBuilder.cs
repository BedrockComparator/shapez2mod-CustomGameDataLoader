using System;
using UnityEngine;

namespace CustomResourcesLoader.Builders
{
    /// <summary>
    /// Fluent builder for constructing <see cref="MetaShapeSubPart"/> instances at runtime.
    /// </summary>
    public class MetaShapeSubPartBuilder
    {
        private MetaShapeSubPart _instance;

        private MetaShapeSubPartBuilder() { }

        // ── Entry points ──────────────────────────────────────

        /// <summary>Create a new empty MetaShapeSubPart.</summary>
        public static MetaShapeSubPartBuilder Create()
        {
            var b = new MetaShapeSubPartBuilder();
            b._instance = ScriptableObject.CreateInstance<MetaShapeSubPart>();
            b._instance._AllowColor = true;
            b._instance._AllowChangingColor = true;
            return b;
        }

        /// <summary>Clone from an existing MetaShapeSubPart.</summary>
        public static MetaShapeSubPartBuilder CloneFrom(MetaShapeSubPart original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            var b = new MetaShapeSubPartBuilder();
            b._instance = UnityEngine.Object.Instantiate(original);
            return b;
        }

        // ── Properties ────────────────────────────────────────

        /// <summary>Set the single-character shape code (e.g. 'C', 'R', 'W', 'S', 'P').</summary>
        public MetaShapeSubPartBuilder SetCode(char code)
        {
            _instance._Code = code;
            return this;
        }

        /// <summary>Whether this part can be colored (default: true).</summary>
        public MetaShapeSubPartBuilder AllowColor(bool allow = true)
        {
            _instance._AllowColor = allow;
            return this;
        }

        /// <summary>Whether an existing color on this part can be changed (default: true).</summary>
        public MetaShapeSubPartBuilder AllowChangingColor(bool allow = true)
        {
            _instance._AllowChangingColor = allow;
            return this;
        }

        /// <summary>Whether the shape is destroyed when falling off the map.</summary>
        public MetaShapeSubPartBuilder DestroyOnFallDown(bool destroy = true)
        {
            _instance._DestroyOnFallDown = destroy;
            return this;
        }

        /// <summary>Set the shader material type for rendering this part.</summary>
        public MetaShapeSubPartBuilder SetMaterial(ShapeShaderMaterialType material)
        {
            _instance.Material = material;
            return this;
        }

        /// <summary>Set whether to use an override material instead of the color scheme material.</summary>
        public MetaShapeSubPartBuilder OverrideColorMaterial(bool overrid = true)
        {
            _instance.OverrideMaterial = overrid;
            return this;
        }

        /// <summary>Reference an existing high-detail mesh asset.</summary>
        public MetaShapeSubPartBuilder SetHighDetailMesh(UnityMeshReference mesh)
        {
            _instance.HighDetailMesh = mesh;
            return this;
        }

        /// <summary>Reference an existing LOD mesh asset.</summary>
        public MetaShapeSubPartBuilder SetMesh(LODMeshAsset mesh)
        {
            _instance.Mesh = mesh;
            return this;
        }

        // ── Build ─────────────────────────────────────────────

        public MetaShapeSubPart Build()
        {
            return _instance;
        }

        public MetaShapeSubPart Build(string objectName)
        {
            _instance.name = objectName;
            return _instance;
        }
    }
}
